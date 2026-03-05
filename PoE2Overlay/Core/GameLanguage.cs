using System.Collections.Generic;

namespace PoE2Overlay.Core
{
    /// <summary>
    /// PoE2 클립보드 텍스트에서 사용되는 모든 언어별 키워드를 정의합니다.
    /// </summary>
    public interface IGameLanguage
    {
        string LanguageCode { get; }

        // Header keywords
        string ItemClass { get; }
        string Rarity { get; }

        // Rarity values
        string RarityNormal { get; }
        string RarityMagic { get; }
        string RarityRare { get; }
        string RarityUnique { get; }
        string RarityCurrency { get; }
        string RarityGem { get; }
        string RarityDivinationCard { get; }
        string RarityFlask { get; }
        string RarityRune { get; }
        string RarityWaystone { get; }
        string RarityTablet { get; }

        // Sections
        string ItemLevel { get; }
        string Requires { get; }
        string Requirements { get; }

        // Stats
        string Level { get; }
        string Str { get; }
        string Dex { get; }
        string Int { get; }

        // Mod tags
        string Implicit { get; }
        string Crafted { get; }
        string Enchant { get; }
        string Fractured { get; }

        // Skip patterns
        string GrantsSkill { get; }
        string Unidentified { get; }

    }

    public class GameLanguageEn : IGameLanguage
    {
        public string LanguageCode => "en";

        public string ItemClass => "Item Class:";
        public string Rarity => "Rarity:";

        public string RarityNormal => "Normal";
        public string RarityMagic => "Magic";
        public string RarityRare => "Rare";
        public string RarityUnique => "Unique";
        public string RarityCurrency => "Currency";
        public string RarityGem => "Gem";
        public string RarityDivinationCard => "Divination Card";
        public string RarityFlask => "Flask";
        public string RarityRune => "Rune";
        public string RarityWaystone => "Waystone";
        public string RarityTablet => "Tablet";

        public string ItemLevel => "Item Level:";
        public string Requires => "Requires:";
        public string Requirements => "Requirements:";

        public string Level => "Level";
        public string Str => "Str";
        public string Dex => "Dex";
        public string Int => "Int";

        public string Implicit => "(implicit)";
        public string Crafted => "(crafted)";
        public string Enchant => "(enchant)";
        public string Fractured => "(fractured)";

        public string GrantsSkill => "Grants Skill:";
        public string Unidentified => "Unidentified";

    }

    public class GameLanguageKo : IGameLanguage
    {
        public string LanguageCode => "ko";

        public string ItemClass => "아이템 종류:";
        public string Rarity => "희귀도:";

        public string RarityNormal => "일반";
        public string RarityMagic => "마법";
        public string RarityRare => "희귀";
        public string RarityUnique => "고유";
        public string RarityCurrency => "화폐";
        public string RarityGem => "젬";
        public string RarityDivinationCard => "점술 카드";
        public string RarityFlask => "플라스크";
        public string RarityRune => "룬";
        public string RarityWaystone => "웨이스톤";
        public string RarityTablet => "태블릿";

        public string ItemLevel => "아이템 레벨:";
        public string Requires => "요구사항:";
        public string Requirements => "요구사항:";

        public string Level => "레벨";
        public string Str => "힘";
        public string Dex => "민첩";
        public string Int => "지능";

        public string Implicit => "(고정)";
        public string Crafted => "(제작)";
        public string Enchant => "(인챈트)";
        public string Fractured => "(분열)";

        public string GrantsSkill => "스킬 부여:";
        public string Unidentified => "미감정";

    }

    public static class GameLanguageProvider
    {
        private static readonly Dictionary<string, IGameLanguage> Languages = new()
        {
            { "en", new GameLanguageEn() },
            { "ko", new GameLanguageKo() }
        };

        public static IGameLanguage Get(string code)
        {
            return Languages.TryGetValue(code, out var lang) ? lang : Languages["en"];
        }

        public static IGameLanguage Current => Get(AppSettings.Instance.GameLanguage);
    }
}
