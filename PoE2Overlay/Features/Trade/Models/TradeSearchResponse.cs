using System.Collections.Generic;
using Newtonsoft.Json;

namespace PoE2Overlay.Features.Trade.Models
{
    public class TradeSearchResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("result")]
        public List<string> Result { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }

    public class TradeFetchResponse
    {
        [JsonProperty("result")]
        public List<FetchedItem> Result { get; set; }
    }

    public class FetchedItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("listing")]
        public ItemListing Listing { get; set; }

        [JsonProperty("item")]
        public ItemDetail Item { get; set; }
    }

    public class ItemListing
    {
        [JsonProperty("indexed")]
        public string Indexed { get; set; }

        [JsonProperty("account")]
        public AccountInfo Account { get; set; }

        [JsonProperty("price")]
        public PriceInfo Price { get; set; }
    }

    public class AccountInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("lastCharacterName")]
        public string LastCharacterName { get; set; }
    }

    public class PriceInfo
    {
        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }

    public class ItemDetail
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("typeLine")]
        public string TypeLine { get; set; }

        [JsonProperty("ilvl")]
        public int ItemLevel { get; set; }

        [JsonProperty("explicitMods")]
        public List<string> ExplicitMods { get; set; }

        [JsonProperty("implicitMods")]
        public List<string> ImplicitMods { get; set; }
    }

    public class TradeResult
    {
        public int TotalCount { get; set; }
        public List<FetchedItem> Items { get; set; }
        public string QueryId { get; set; }
        public string Error { get; set; }
    }
}
