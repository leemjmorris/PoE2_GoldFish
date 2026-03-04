using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
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
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private bool _isLoaded;
        private bool _loadFailed;

        public bool IsLoaded => _isLoaded;

        static StatIdResolver()
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent", "PoE2Overlay/1.0 (contact: poe2overlay@github.com)");
        }

        public async Task LoadStatsAsync()
        {
            if (_isLoaded) return;

            await _loadLock.WaitAsync();
            try
            {
                if (_isLoaded) return;

                _loadFailed = false;
                var json = await _client.GetStringAsync(
                    "https://www.pathofexile.com/api/trade2/data/stats");
                var data = JObject.Parse(json);
                var result = data["result"] as JArray;

                if (result == null)
                {
                    _loadFailed = true;
                    return;
                }

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
            catch (Exception ex)
            {
                _loadFailed = true;
                Debug.WriteLine($"[StatIdResolver] Failed to load stats: {ex.Message}");
            }
            finally
            {
                _loadLock.Release();
            }
        }

        public async Task EnsureLoadedAsync()
        {
            if (_isLoaded) return;
            await LoadStatsAsync();
        }

        public string Resolve(string modText, ModType type)
        {
            var normalized = NormalizeModText(modText);
            var map = type switch
            {
                ModType.Implicit => _implicitMap,
                _ => _explicitMap
            };

            // 1. 정확한 매칭
            if (map.TryGetValue(normalized, out var id)) return id;

            // 2. 퍼지 매칭 fallback (Jaccard 유사도, 임계값 0.6)
            return FuzzyResolve(normalized, map);
        }

        private static string FuzzyResolve(string normalized, Dictionary<string, string> map)
        {
            var queryTokens = new HashSet<string>(
                normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (queryTokens.Count == 0) return null;

            string bestId = null;
            double bestScore = 0.6; // 최소 임계값

            foreach (var (key, id) in map)
            {
                var keyTokens = new HashSet<string>(
                    key.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                double intersection = queryTokens.Intersect(keyTokens).Count();
                double union = queryTokens.Union(keyTokens).Count();
                double score = union > 0 ? intersection / union : 0;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = id;
                }
            }

            return bestId;
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
