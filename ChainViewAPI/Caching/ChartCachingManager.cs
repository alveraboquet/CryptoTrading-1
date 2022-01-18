using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using ChainViewAPI.Models;

namespace ChainViewAPI
{
    public static class ChartCachingManager
    {
        private static ConcurrentDictionary<string, CandleSortedSet> _sortedCandles;
        private static ConcurrentDictionary<string, FootprintSortedSet> _sortedFootprint;
        private static ConcurrentDictionary<string, HeatmapSortedSet> _sortedHeatmap;
        static ChartCachingManager()
        {
            _sortedHeatmap = new ConcurrentDictionary<string, HeatmapSortedSet>();
            _sortedFootprint = new ConcurrentDictionary<string, FootprintSortedSet>();
            _sortedCandles = new ConcurrentDictionary<string, CandleSortedSet>();
        }

        private static string CreateKey(string exchange, string symbol, string timeframe) 
            => $"{exchange}.{symbol}.{timeframe}";

        /// <summary>
        /// returns a CandleSortedSet if exist otherwise creates a new one and returns it.
        /// </summary>
        public static CandleSortedSet GetSortedCandles(string exchange, string symbol, string timeframe)
        {
            string key = CreateKey(exchange, symbol, timeframe);
            if (_sortedCandles.TryGetValue(key, out var value))
                return value;
            else
            {
                var val = new CandleSortedSet(exchange, symbol, timeframe);
                _sortedCandles[key] = val;
                return val;
            }
        }

        /// <summary>
        /// returns a FootprintSortedSet if exist otherwise creates a new one and returns it.
        /// </summary>
        public static FootprintSortedSet GetSortedFootprints(string exchange, string symbol, string timeframe)
        {
            string key = CreateKey(exchange, symbol, timeframe);
            if (_sortedFootprint.TryGetValue(key, out var value))
                return value;
            else
            {
                var val = new FootprintSortedSet(exchange, symbol, timeframe);
                _sortedFootprint[key] = val;
                return val;
            }
        }

        /// <summary>
        /// returns a FootprintSortedSet if exist otherwise creates a new one and returns it.
        /// </summary>
        public static HeatmapSortedSet GetSortedHeatmap(string exchange, string symbol, string timeframe)
        {
            string key = CreateKey(exchange, symbol, timeframe);
            if (_sortedHeatmap.TryGetValue(key, out var value))
                return value;
            else
            {
                var val = new HeatmapSortedSet(exchange, symbol, timeframe);
                _sortedHeatmap[key] = val;
                return val;
            }
        }
    }
}
