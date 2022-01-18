using DataLayer;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChainViewAPI
{
    public static class CachingProvider
    {
        private const string PairinfoListResponseKey = "pair.info.list.response";
        private const string PairinfoListKey = "pair.info.list";
        private const string PairinfoSearchKey = "pair.info.search.{0}.{1}";//exchange, text
        private static TimeSpan PairinfoSearchExpiration = TimeSpan.FromMinutes(5);

        public static string TryGetPairInfoListResponse(this IMemoryCache cache)
        {
            cache.TryGetValue(PairinfoListResponseKey, out string state);
            return state;
        }
        public static void SetPairInfoListResponse(this IMemoryCache cache, string pairs)
        {
            cache.Set(PairinfoListResponseKey, pairs);
        }

        public static List<PairInfo> TryGetPairInfoList(this IMemoryCache cache)
        {
            cache.TryGetValue(PairinfoListKey, out List<PairInfo> pairs);
            return pairs;
        }
        public static bool TryGetPairInfo(this IMemoryCache cache, string exchange, string symbolName, out PairInfo pair)
        {
            pair = null;

            var pairs = cache.TryGetPairInfoList();
            if (pairs == null) return false;

            pair = pairs.FirstOrDefault(p => p.Exchange.Equals(exchange) &&
                                    p.Symbol.Equals(symbolName));

            return pair != default;
        }

        public static void SetPairInfoList(this IMemoryCache cache, List<PairInfo> pairs)
        {
            cache.Set(PairinfoListKey, pairs);
        }

        #region SymbolSearch
        public static bool TryGetSymbolSearch(this IMemoryCache cache, string ex, string text, out string val)
        {
            return cache.TryGetValue(
                        GetSymbolSearchCacheKey(ex, text)
                        , out val);
        }
        public static void SetSymbolSearch(this IMemoryCache cache, string ex, string text, List<PairInfo> pairs)
        {
            cache.Set(
                GetSymbolSearchCacheKey(ex, text),
                pairs,
                PairinfoSearchExpiration);
        }
        private static string GetSymbolSearchCacheKey(string ex, string text)
        {
            return string.Format(PairinfoSearchKey, ex, text);
        }

        #endregion
    }
}
