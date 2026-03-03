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
    }

    public class RangeFilter
    {
        [JsonProperty("min")]
        public int? Min { get; set; }

        [JsonProperty("max")]
        public int? Max { get; set; }
    }
}
