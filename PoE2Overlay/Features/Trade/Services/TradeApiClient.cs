using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoE2Overlay.Features.Trade.Models;

namespace PoE2Overlay.Features.Trade.Services
{
    public class TradeApiClient : IDisposable
    {
        private static readonly HttpClient _client;
        private const string BaseUrl = "https://www.pathofexile.com/api/trade2";

        private DateTime _nextAllowedRequest = DateTime.MinValue;

        static TradeApiClient()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent",
                "PoE2Overlay/1.0 (contact: poe2overlay@github.com)");
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<TradeResult> SearchAndFetchAsync(
            TradeSearchRequest request, string league)
        {
            await WaitForRateLimit();

            // Step 1: POST search
            var searchUrl = $"{BaseUrl}/search/poe2/{league}";
            var json = JsonConvert.SerializeObject(request,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage searchResponse;
            try
            {
                searchResponse = await SendWithRetryAsync(
                    () => _client.PostAsync(searchUrl, new StringContent(json, Encoding.UTF8, "application/json")));
            }
            catch (HttpRequestException ex)
            {
                return new TradeResult { Error = $"Network error: {ex.Message}" };
            }

            ParseRateLimitHeaders(searchResponse);

            if (searchResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = searchResponse.Headers.RetryAfter;
                if (retryAfter?.Delta != null)
                    _nextAllowedRequest = DateTime.UtcNow + retryAfter.Delta.Value;
                else
                    _nextAllowedRequest = DateTime.UtcNow.AddSeconds(60);

                return new TradeResult { Error = "Rate limited. Please wait and try again." };
            }

            if (!searchResponse.IsSuccessStatusCode)
            {
                return new TradeResult
                {
                    Error = $"Search failed: {searchResponse.StatusCode}"
                };
            }

            var searchResult = JsonConvert.DeserializeObject<TradeSearchResponse>(
                await searchResponse.Content.ReadAsStringAsync());

            if (searchResult?.Result == null || searchResult.Result.Count == 0)
                return new TradeResult { TotalCount = 0, Items = new List<FetchedItem>() };

            // Step 2: GET fetch (최대 10개)
            await WaitForRateLimit();

            var itemIds = string.Join(",", searchResult.Result.Take(10));
            var fetchUrl = $"{BaseUrl}/fetch/{itemIds}?query={searchResult.Id}";

            HttpResponseMessage fetchResponse;
            try
            {
                fetchResponse = await SendWithRetryAsync(() => _client.GetAsync(fetchUrl));
            }
            catch (HttpRequestException ex)
            {
                return new TradeResult { Error = $"Fetch error: {ex.Message}" };
            }

            ParseRateLimitHeaders(fetchResponse);

            if (fetchResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return new TradeResult { Error = "Rate limited during fetch. Please wait." };
            }

            if (!fetchResponse.IsSuccessStatusCode)
            {
                return new TradeResult
                {
                    Error = $"Fetch failed: {fetchResponse.StatusCode}"
                };
            }

            var fetchResult = JsonConvert.DeserializeObject<TradeFetchResponse>(
                await fetchResponse.Content.ReadAsStringAsync());

            return new TradeResult
            {
                TotalCount = searchResult.Total,
                Items = fetchResult?.Result ?? new List<FetchedItem>(),
                QueryId = searchResult.Id
            };
        }

        private static readonly int[] RetryableStatusCodes = { 500, 502, 503, 504 };

        private static async Task<HttpResponseMessage> SendWithRetryAsync(
            Func<Task<HttpResponseMessage>> requestFunc, int maxRetries = 2)
        {
            int delayMs = 1000;
            for (int attempt = 0; ; attempt++)
            {
                var response = await requestFunc();
                if (response.IsSuccessStatusCode || attempt >= maxRetries)
                    return response;

                var status = (int)response.StatusCode;
                if (!RetryableStatusCodes.Contains(status))
                    return response;

                await Task.Delay(delayMs);
                delayMs = Math.Min(delayMs * 2, 8000);
            }
        }

        private void ParseRateLimitHeaders(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("X-Rate-Limit-Ip-State", out var stateValues))
            {
                var state = stateValues.FirstOrDefault();
                if (state != null)
                {
                    var parts = state.Split(':');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int current))
                    {
                        if (current > 8)
                            _nextAllowedRequest = DateTime.UtcNow.AddSeconds(2);
                    }
                }
            }
        }

        private async Task WaitForRateLimit()
        {
            var now = DateTime.UtcNow;
            if (_nextAllowedRequest > now)
            {
                var delay = _nextAllowedRequest - now;
                await Task.Delay(delay);
            }
            _nextAllowedRequest = DateTime.UtcNow.AddMilliseconds(500);
        }

        public void Dispose()
        {
            // HttpClient는 static이므로 Dispose하지 않음
        }
    }
}
