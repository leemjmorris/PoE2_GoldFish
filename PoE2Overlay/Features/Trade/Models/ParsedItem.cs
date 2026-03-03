using System.Collections.Generic;

namespace PoE2Overlay.Features.Trade.Models
{
    public enum ItemRarity
    {
        Normal,
        Magic,
        Rare,
        Unique,
        Currency,
        Gem,
        DivinationCard,
        Unknown
    }

    public enum ModType
    {
        Implicit,
        Explicit,
        Crafted,
        Enchant,
        Fractured
    }

    public class ItemMod
    {
        public string RawText { get; set; }
        public string StatId { get; set; }
        public ModType Type { get; set; }
        public double? Value { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class ParsedItem
    {
        public string ItemClass { get; set; }
        public ItemRarity Rarity { get; set; }
        public string Name { get; set; }
        public string BaseType { get; set; }
        public int? ItemLevel { get; set; }
        public int? RequiredLevel { get; set; }
        public int? RequiredStr { get; set; }
        public int? RequiredDex { get; set; }
        public int? RequiredInt { get; set; }
        public List<ItemMod> ImplicitMods { get; set; } = new();
        public List<ItemMod> ExplicitMods { get; set; } = new();
        public string RawText { get; set; }
        public bool IsValid { get; set; }
    }
}
