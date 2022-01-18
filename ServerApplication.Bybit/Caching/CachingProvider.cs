using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketStream;
using DataLayer;
using DataLayer.Models.Stream;
using ExchangeModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace ServerApplication.Bybit.Caching
{
    public static class CachingProvider
    {
        private const string orderBookFormat = "orderBook.{0}.{1}"; //  ex.symbol
        private const string lastCandleFormat = "lastCandle.{0}.{1}.{2}"; //  ex.symbol.timeFrame
        private const string footprintFormat = "footprint.{0}.{1}.{2}"; //  ex.symbol.timeframe

        private const string _bybitFuturesFrFormat = "fr." + ApplicationValues.BybitFuturesName + ".isStreaming";
        private const string _bybitFuturesLiqFormat = "liq." + ApplicationValues.BybitFuturesName + ".isStreaming";

        private const string trade_SymbolIsStreaming = "trade.{0}.{1}.isStreaming"; //  ex.symbol
        private const string kline_SymbolIsStreaming = "kline.{0}.{1}.isStreaming"; //  ex.symbol
        private const string depth_SymbolIsStreaming = "depth.{0}.{1}.isStreaming"; //  ex.symbol

        #region bybit futures is streaming state
        
        public static bool SetBybitFuturesIsFrStreaming(this IMemoryCache cache, bool val) =>
            cache.Set(_bybitFuturesFrFormat, val);
        public static bool TryGetBybitFuturesIsFrStreaming(this IMemoryCache cache, out bool val) =>
            cache.TryGetValue(_bybitFuturesFrFormat, out val);
        
        public static bool SetBybitFuturesIsLiqStreaming(this IMemoryCache cache, bool val) =>
            cache.Set(_bybitFuturesLiqFormat, val);
        public static bool TryGetBybitFuturesIsLiqStreaming(this IMemoryCache cache, out bool val) =>
            cache.TryGetValue(_bybitFuturesLiqFormat, out val);
        
        #endregion

        #region trade is streaming state

        private static string GetTradeSymbolIsStreamingCacheKey(string exchange, string symbol)
        {
            string key = string.Format(trade_SymbolIsStreaming, exchange, symbol);
            return key;
        }
        public static bool TryGetTradeSymbolIsStreaming(this IMemoryCache cache, string exchange, string symbol)
        {
            string key = GetTradeSymbolIsStreamingCacheKey(exchange, symbol);

            cache.TryGetValue(key, out bool state);
            return state;
        }
        public static void SetTradeSymbolIsStreaming(this IMemoryCache cache, string exchange, string symbol, bool state)
        {
            string key = GetTradeSymbolIsStreamingCacheKey(exchange, symbol);
            cache.Set(key, state);
        }

        public static void SetTradeSymbolIsStreaming(this IMemoryCache cache, string exchange, string[] symbols, bool state)
        {
            foreach (var symbol in symbols)
            {
                cache.SetTradeSymbolIsStreaming(exchange, symbol, state);
            }
        }

        #endregion

        #region kline is streaming state

        private static string GetKlineSymbolIsStreamingCacheKey(string exchange, string symbol)
        {
            string key = string.Format(kline_SymbolIsStreaming, exchange, symbol);
            return key;
        }
        public static bool TryGetKlineSymbolIsStreaming(this IMemoryCache cache, string exchange, string symbol)
        {
            string key = GetKlineSymbolIsStreamingCacheKey(exchange, symbol);

            cache.TryGetValue(key, out bool state);
            return state;
        }
        public static void SetKlineSymbolIsStreaming(this IMemoryCache cache, string exchange, string symbol, bool state)
        {
            string key = GetKlineSymbolIsStreamingCacheKey(exchange, symbol);
            cache.Set(key, state);
        }

        public static void SetKlineSymbolIsStreaming(this IMemoryCache cache, string exchange, string[] symbols, bool state)
        {
            foreach (var symbol in symbols)
            {
                cache.SetKlineSymbolIsStreaming(exchange, symbol, state);
            }
        }

        #endregion

        #region depth is streaming state

        private static string GetDepthSymbolIsStreamingCacheKey(string exchange, string symbol)
        {
            string key = string.Format(depth_SymbolIsStreaming, exchange, symbol);
            return key;
        }
        public static bool TryGetDepthSymbolIsStreaming(this IMemoryCache cache, string exchange, string symbol)
        {
            string key = GetDepthSymbolIsStreamingCacheKey(exchange, symbol);

            cache.TryGetValue(key, out bool state);
            return state;
        }
        public static void SetDepthSymbolIsStreaming(this IMemoryCache cache, string exchange, string symbol, bool state)
        {
            string key = GetDepthSymbolIsStreamingCacheKey(exchange, symbol);
            cache.Set(key, state);
        }

        public static void SetDepthSymbolIsStreaming(this IMemoryCache cache, string exchange, string[] symbols, bool state)
        {
            foreach (var symbol in symbols)
            {
                cache.SetDepthSymbolIsStreaming(exchange, symbol, state);
            }
        }

        #endregion

        #region Footprint
        public static string GetFootprintsCacheKey(string exchange, string symbol, string timeframe)
        {
            return string.Format(footprintFormat, exchange, symbol, timeframe);
        }

        public static void SetFootPrints(this IMemoryCache cache, string exchange, string symbol, string timeframe, FootPrints footPrints)
        {
            cache.Set(GetFootprintsCacheKey(exchange, symbol, timeframe), footPrints);
        }
        public static FootPrints TryGetFootPrints(this IMemoryCache cache, string exchange, string symbol, string timeframe)
        {
            cache.TryGetValue(GetFootprintsCacheKey(exchange, symbol, timeframe), out FootPrints footPrints);
            return footPrints;
        }
        #endregion

        #region Open Candle

        private static string GetOpenCandleCacheKey(string exchange, string symbol, string timeFrame)
        {
            return string.Format(lastCandleFormat, exchange, symbol, timeFrame);
        }
        public static DataLayer.Candle TryGetOpenCandle(this IMemoryCache cache, string exchange, string symbol, string timeFrame)
        {
            cache.TryGetValue(GetOpenCandleCacheKey(exchange, symbol, timeFrame), out Candle candle);
            return candle;
        }
        public static void SetOpenCandle(this IMemoryCache cache, string exchange, string symbol, string timeFrame, DataLayer.Candle openCandle)
        {
            cache.Set(GetOpenCandleCacheKey(exchange, symbol, timeFrame), openCandle);
        }

        #endregion

        #region Order Book

        private static string GetOrderBookCacheKey(string exchange, string symbol)
        {
            return string.Format(orderBookFormat, exchange, symbol);
        }
        public static StreamingOrderBook TryGetOrderBook(this IMemoryCache cache, string exchange, string symbol)
        {
            cache.TryGetValue(GetOrderBookCacheKey(exchange, symbol), out StreamingOrderBook orderBook);
            return orderBook;
        }
        public static void SetOrderBook(this IMemoryCache cache, string exchange, string symbol, StreamingOrderBook orderBook)
        {
            cache.Set(GetOrderBookCacheKey(exchange, symbol), orderBook);
        }

        #endregion
    }
}
