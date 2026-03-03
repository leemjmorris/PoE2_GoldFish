using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PoE2Overlay.Features.Trade.Models;

namespace PoE2Overlay.Features.Trade.Services
{
    public class StatIdResolver
    {
        private static readonly HttpClient _client = new();
        private readonly Dictionary<string, string> _explicitMap = new();
        private readonly Dictionary<string, string> _implicitMap = new();
        private bool _isLoaded;

        static StatIdResolver()
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent", "PoE2Overlay/1.0 (contact: poe2overlay@github.com)");
        }

        public async Task LoadStatsAsync()
        {
            if (_isLoaded) return;

            try
            {
                var json = await _client.GetStringAsync(
                    "https://www.pathofexile.com/api/trade2/data/stats");
                var data = JObject.Parse(json);
                var result = data["result"] as JArray;

                if (result == null) return;

                foreach (var category in result)
                {
                    var entries = category["entries"] as JArray;
                    if (entries == null) continue;

                    foreach (var entry in entries)
                    {
                        var id = entry["id"]?.ToString();
                        var text = entry["text"]?.ToString();
                        if (id == null || text == null) continue;

                        var normalized = NormalizeModText(text);

                        if (id.StartsWith("explicit."))
                            _explicitMap.TryAdd(normalized, id);
                        else if (id.StartsWith("implicit."))
                            _implicitMap.TryAdd(normalized, id);
                    }
                }

                _isLoaded = true;
            }
            catch { }
        }

        public string Resolve(string modText, ModType type)
        {
            var normalized = NormalizeModText(modText);
            var map = type switch
            {
                ModType.Implicit => _implicitMap,
                _ => _explicitMap
            };

            return map.TryGetValue(normalized, out var id) ? id : null;
        }

        private static string NormalizeModText(string text)
        {
            // 숫자를 "#"으로 교체: "67% increased..." -> "#% increased..."
            var result = Regex.Replace(text.Trim(), @"\d+(\.\d+)?", "#");
            // 괄호 안의 범위 표기 제거: "(60-80)" -> ""
            result = Regex.Replace(result, @"\(#-#\)", "#");
            return result;
        }
    }
}
