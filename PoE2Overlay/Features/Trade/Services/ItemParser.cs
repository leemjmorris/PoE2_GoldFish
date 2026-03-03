using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using PoE2Overlay.Features.Trade.Models;

namespace PoE2Overlay.Features.Trade.Services
{
    public static class ItemParser
    {
        private const string Separator = "--------";

        public static ParsedItem Parse(string clipboardText)
        {
            if (string.IsNullOrWhiteSpace(clipboardText))
                return null;

            if (!clipboardText.Contains(Separator))
                return null;

            var item = new ParsedItem { RawText = clipboardText };
            var sections = SplitSections(clipboardText);

            if (sections.Count < 2)
                return null;

            ParseHeader(sections[0], item);

            if (item.Rarity == ItemRarity.Unknown)
                return null;

            // 모드 파싱용 - 아이템 레벨 이후의 섹션들이 모드 영역
            bool passedItemLevel = false;
            bool firstModSection = true;

            for (int i = 1; i < sections.Count; i++)
            {
                var section = sections[i];
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToArray();

                if (lines.Length == 0) continue;

                if (TryParseRequirements(lines, item)) continue;
                if (TryParseItemLevel(lines, item))
                {
                    passedItemLevel = true;
                    continue;
                }

                // 플레이버 텍스트 스킵 (보통 마지막 섹션, 이탤릭 설명)
                if (IsFlavorText(lines, item)) continue;

                // 아이템 레벨 이후의 섹션들을 모드로 파싱
                if (passedItemLevel)
                {
                    ParseMods(lines, item, firstModSection);
                    firstModSection = false;
                }
            }

            item.IsValid = true;
            return item;
        }

        private static List<string> SplitSections(string text)
        {
            return text.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        private static void ParseHeader(string headerSection, ParsedItem item)
        {
            var lines = headerSection.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string pendingName = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("Item Class:"))
                {
                    item.ItemClass = trimmed.Substring("Item Class:".Length).Trim();
                }
                else if (trimmed.StartsWith("Rarity:"))
                {
                    var rarityStr = trimmed.Substring("Rarity:".Length).Trim();
                    item.Rarity = rarityStr switch
                    {
                        "Normal" => ItemRarity.Normal,
                        "Magic" => ItemRarity.Magic,
                        "Rare" => ItemRarity.Rare,
                        "Unique" => ItemRarity.Unique,
                        "Currency" => ItemRarity.Currency,
                        "Gem" => ItemRarity.Gem,
                        "Divination Card" => ItemRarity.DivinationCard,
                        _ => ItemRarity.Unknown
                    };
                }
                else
                {
                    // Unique/Rare: 첫줄=Name, 둘째줄=BaseType
                    // Normal/Magic: BaseType만
                    if (pendingName == null &&
                        (item.Rarity == ItemRarity.Unique || item.Rarity == ItemRarity.Rare))
                    {
                        pendingName = trimmed;
                    }
                    else
                    {
                        item.BaseType = trimmed;
                    }
                }
            }

            // Unique/Rare에서 Name과 BaseType 구분
            if (pendingName != null)
            {
                if (item.BaseType != null)
                {
                    item.Name = pendingName;
                }
                else
                {
                    item.BaseType = pendingName;
                }
            }
        }

        private static bool TryParseRequirements(string[] lines, ParsedItem item)
        {
            var reqLine = lines.FirstOrDefault(l =>
                l.StartsWith("Requires:") || l.StartsWith("Requirements:"));
            if (reqLine == null) return false;

            // 단일 줄 형식: "Requires: Level 84, 147 Int"
            var content = reqLine.Contains(':') ? reqLine.Substring(reqLine.IndexOf(':') + 1) : "";

            // 여러 줄 형식일 수도 있음
            var allText = string.Join(" ", lines);

            var levelMatch = Regex.Match(allText, @"Level\s+(\d+)");
            if (levelMatch.Success)
                item.RequiredLevel = int.Parse(levelMatch.Groups[1].Value);

            var strMatch = Regex.Match(allText, @"(\d+)\s+Str");
            if (strMatch.Success)
                item.RequiredStr = int.Parse(strMatch.Groups[1].Value);

            var dexMatch = Regex.Match(allText, @"(\d+)\s+Dex");
            if (dexMatch.Success)
                item.RequiredDex = int.Parse(dexMatch.Groups[1].Value);

            var intMatch = Regex.Match(allText, @"(\d+)\s+Int");
            if (intMatch.Success)
                item.RequiredInt = int.Parse(intMatch.Groups[1].Value);

            return true;
        }

        private static bool TryParseItemLevel(string[] lines, ParsedItem item)
        {
            var ilvlLine = lines.FirstOrDefault(l => l.StartsWith("Item Level:"));
            if (ilvlLine == null) return false;

            var match = Regex.Match(ilvlLine, @"Item Level:\s*(\d+)");
            if (match.Success)
                item.ItemLevel = int.Parse(match.Groups[1].Value);

            return true;
        }

        private static bool IsFlavorText(string[] lines, ParsedItem item)
        {
            // 유니크 아이템의 플레이버 텍스트: 보통 숫자가 없고 여러 줄
            if (item.Rarity != ItemRarity.Unique) return false;

            // 모드처럼 보이지 않는 텍스트 (숫자% 패턴이 없는 여러 줄)
            bool hasModPattern = lines.Any(l =>
                Regex.IsMatch(l, @"\d+%?\s+(increased|reduced|more|less|to|added)") ||
                l.Contains("(implicit)") ||
                l.Contains("(crafted)"));

            return !hasModPattern && lines.Length >= 2;
        }

        private static void ParseMods(string[] lines, ParsedItem item, bool isFirstModSection)
        {
            foreach (var line in lines)
            {
                var trimmed = line;
                if (string.IsNullOrEmpty(trimmed)) continue;

                // { Prefix Modifier "..." } 또는 { Suffix Modifier "..." } 태그 스킵
                if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                    continue;

                // "Grants Skill:" 라인 스킵
                if (trimmed.StartsWith("Grants Skill:")) continue;

                // "Unidentified" 스킵
                if (trimmed == "Unidentified") continue;

                var mod = new ItemMod { RawText = trimmed };

                // 숫자값 추출
                var valueMatch = Regex.Match(trimmed, @"(\d+(?:\.\d+)?)");
                if (valueMatch.Success)
                    mod.Value = double.Parse(valueMatch.Groups[1].Value, CultureInfo.InvariantCulture);

                if (trimmed.Contains("(implicit)"))
                {
                    mod.Type = ModType.Implicit;
                    mod.RawText = trimmed.Replace("(implicit)", "").Trim();
                    item.ImplicitMods.Add(mod);
                }
                else if (trimmed.Contains("(crafted)"))
                {
                    mod.Type = ModType.Crafted;
                    mod.RawText = trimmed.Replace("(crafted)", "").Trim();
                    item.ExplicitMods.Add(mod);
                }
                else if (trimmed.Contains("(enchant)"))
                {
                    mod.Type = ModType.Enchant;
                    mod.RawText = trimmed.Replace("(enchant)", "").Trim();
                    item.ImplicitMods.Add(mod);
                }
                else if (trimmed.Contains("(fractured)"))
                {
                    mod.Type = ModType.Fractured;
                    mod.RawText = trimmed.Replace("(fractured)", "").Trim();
                    item.ExplicitMods.Add(mod);
                }
                else
                {
                    // 첫 번째 모드 섹션은 implicit일 수 있음
                    if (isFirstModSection && item.ImplicitMods.Count == 0
                        && item.ExplicitMods.Count == 0)
                    {
                        mod.Type = ModType.Implicit;
                        item.ImplicitMods.Add(mod);
                    }
                    else
                    {
                        mod.Type = ModType.Explicit;
                        item.ExplicitMods.Add(mod);
                    }
                }
            }
        }

        /// <summary>
        /// 숫자 값의 ±tolerance% 범위 계산
        /// </summary>
        public static (double min, double max) CalculateRange(double value, double tolerancePercent = 10)
        {
            double delta = Math.Abs(value) * (tolerancePercent / 100.0);
            return (Math.Floor(value - delta), Math.Ceiling(value + delta));
        }
    }
}
