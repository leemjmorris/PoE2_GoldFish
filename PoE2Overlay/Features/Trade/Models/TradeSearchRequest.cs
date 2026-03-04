using System.Collections.Generic;
using Newtonsoft.Json;

namespace PoE2Overlay.Features.Trade.Models
{
    public class TradeSearchRequest
    {
        [JsonProperty("query")]
        public TradeQuery Query { get; set; } = new();

        [JsonProperty("sort")]
        public Dictionary<string, string> Sort { get; set; } = new() { { "price", "asc" } };
    }

    public class TradeQuery
    {
        [JsonProperty("status")]
        public StatusFilter Status { get; set; } = new() { Option = "online" };

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("stats")]
        public List<StatFilterGroup> Stats { get; set; } = new();

        [JsonProperty("filters")]
        public QueryFilters Filters { get; set; }
    }

    public class StatusFilter
    {
        [JsonProperty("option")]
        public string Option { get; set; } = "online";
    }

    public class StatFilterGroup
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "and";

        [JsonProperty("filters")]
        public List<StatFilter> Filters { get; set; } = new();

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }
    }

    public class StatFilter
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("value")]
        public StatFilterValue Value { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }
    }

    public class StatFilterValue
    {
        [JsonProperty("min")]
        public double? Min { get; set; }

        [JsonProperty("max")]
        public double? Max { get; set; }
    }

    public class QueryFilters
    {
        [JsonProperty("misc_filters")]
        public MiscFilters MiscFilters { get; set; }

        [JsonProperty("weapon_filters")]
        public WeaponFilters WeaponFilters { get; set; }

        [JsonProperty("armour_filters")]
        public ArmourFilters ArmourFilters { get; set; }
    }

    public class MiscFilters
    {
        [JsonProperty("filters")]
        public MiscFilterValues Filters { get; set; } = new();
    }

    public class MiscFilterValues
    {
        [JsonProperty("ilvl")]
        public RangeFilter ItemLevel { get; set; }

        [JsonProperty("quality")]
        public RangeFilter Quality { get; set; }

        [JsonProperty("corrupted")]
        public BoolFilter Corrupted { get; set; }

        [JsonProperty("mirrored")]
        public BoolFilter Mirrored { get; set; }
    }

    public class WeaponFilters
    {
        [JsonProperty("filters")]
        public WeaponFilterValues Filters { get; set; } = new();
    }

    public class WeaponFilterValues
    {
        [JsonProperty("pdps")]
        public RangeFilter PhysicalDps { get; set; }

        [JsonProperty("edps")]
        public RangeFilter ElementalDps { get; set; }

        [JsonProperty("dps")]
        public RangeFilter TotalDps { get; set; }

        [JsonProperty("aps")]
        public RangeFilter AttacksPerSecond { get; set; }

        [JsonProperty("crit")]
        public RangeFilter CriticalHitChance { get; set; }
    }

    public class ArmourFilters
    {
        [JsonProperty("filters")]
        public ArmourFilterValues Filters { get; set; } = new();
    }

    public class ArmourFilterValues
    {
        [JsonProperty("ar")]
        public RangeFilter Armour { get; set; }

        [JsonProperty("ev")]
        public RangeFilter EvasionRating { get; set; }

        [JsonProperty("es")]
        public RangeFilter EnergyShield { get; set; }

        [JsonProperty("spirit")]
        public RangeFilter Spirit { get; set; }
    }

    public class RangeFilter
    {
        [JsonProperty("min")]
        public double? Min { get; set; }

        [JsonProperty("max")]
        public double? Max { get; set; }
    }

    public class BoolFilter
    {
        [JsonProperty("option")]
        public string Option { get; set; }
    }
}
