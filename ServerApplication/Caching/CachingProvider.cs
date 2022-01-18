﻿using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketStream;
using DataLayer;
using DataLayer.Models.Stream;
using ExchangeModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace ServerApplication.Caching
{
    public static class CachingProvider
    {
        private const string orderBookFormat = "orderBook.{0}.{1}"; //  ex.symbol
        private const string lastCandleFormat = "lastCandle.{0}.{1}.{2}"; //  ex.symbol.timeFrame
        private const string footprintFormat = "footprint.{0}.{1}.{2}"; //  ex.symbol.timeframe

        private const string binanceFuturesUsdLiqFrFormat = "liqFr." + ApplicationValues.BinanceUsdName + ".isStreaming";

        private const string tradeKline_SymbolIsStreaming = "trade_kline.{0}.{1}.isStreaming"; //  ex.symbol
        private const string depth_SymbolIsStreaming = "depth.{0}.{1}.isStreaming"; //  ex.symbol

        #region binancefutures Is Streamings
        public static bool SetBinanceFuturesUsdIsLiqFrStreaming(this IMemoryCache cache, bool val) =>
            cache.Set(binanceFuturesUsdLiqFrFormat, val);
        public static bool TryGetBinanceFuturesUsdIsLiqFrStreaming(this IMemoryCache cache, out bool val) =>
            cache.TryGetValue(binanceFuturesUsdLiqFrFormat, out val);
        #endregion

        #region trade_kline is streaming state

        private static string GetTradeKlineSymbolIsStreamingCacheKey(string exchange, string symbol)
        {
            string key = string.Format(tradeKline_SymbolIsStreaming, exchange, symbol);
            return key;
        }
        public static bool TryGetTradeKlineSymbolIsStreaming(this IMemoryCache cache, string exchange, string symbol)
        {
            string key = GetTradeKlineSymbolIsStreamingCacheKey(exchange, symbol);

            cache.TryGetValue(key, out bool state);
            return state;
        }
        public static void SetTradeKlineSymbolIsStreaming(this IMemoryCache cache, string exchange, string symbol, bool state)
        {
            string key = GetTradeKlineSymbolIsStreamingCacheKey(exchange, symbol);
            cache.Set(key, state);
        }

        public static void SetTradeKlineSymbolIsStreaming(this IMemoryCache cache, string exchange, string[] symbols, bool state)
        {
            foreach (var symbol in symbols)
            {
                cache.SetTradeKlineSymbolIsStreaming(exchange, symbol, state);
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
