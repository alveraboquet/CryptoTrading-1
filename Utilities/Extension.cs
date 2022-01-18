using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Binance.Net.Objects.Futures.MarketData;
using Binance.Net.Objects.Spot.MarketData;
using DataLayer;

namespace Utilities
{
    public static class Extension
    {
        public static bool IsLiqFrOiSymbol(this string symbol)
        {
            return symbol.StartsWith("FR.") ||
                          symbol.StartsWith("OI.") ||
                          symbol.StartsWith("LIQSELL.") ||
                          symbol.StartsWith("LIQBUY.") ||
                          symbol.StartsWith("LIQ.");
        }
        public static bool IsLiqFrOiSymbol(this PairInfo pair) =>
            pair.Symbol.IsLiqFrOiSymbol();

        public static bool IsOiPair(this PairInfo pair) => pair.Symbol.StartsWith("OI.");
        public static bool IsFrPair(this PairInfo pair) => pair.Symbol.StartsWith("FR.");
        public static bool IsLiqPair(this PairInfo pair) => pair.Symbol.StartsWith("LIQ.");
        public static bool IsLiqSellPair(this PairInfo pair) => pair.Symbol.StartsWith("LIQSELL.");
        public static bool IsLiqBuyPair(this PairInfo pair) => pair.Symbol.StartsWith("LIQBUY.");

        /// <summary>
        /// Corrects symbol name for custom pairs
        /// </summary>
        public static string GetSymbol(this PairInfo pair)
        {
            if (!pair.Symbol.Contains('.'))
                return pair.Symbol;
            else
            {
                return pair.Symbol.Split('.')[1];
            }
        }

        public static List<DataLayer.PairInfo> RemoveCostumePairs(this IEnumerable<DataLayer.PairInfo> pairInfos)
        {
            return (from p in pairInfos
                    where !p.Symbol.StartsWith("FR.") &&
                          !p.Symbol.StartsWith("OI.") &&
                          !p.Symbol.StartsWith("LIQSELL.") &&
                          !p.Symbol.StartsWith("LIQBUY.") &&
                          !p.Symbol.StartsWith("LIQ.")
                    select p).ToList();
        }

        public static int ToInt(this bool b)
        {
            if (b)
                return 1;
            else
                return 0;
        }

        public static int GetExchangeId(string exchange)
        {
            return (exchange.ToLower()) switch
            {
                ApplicationValues.BinanceUsdName => 2,
                ApplicationValues.BinanceCoinName => 3,

                ApplicationValues.BitfinexName => 4,
                ApplicationValues.BitmexName => 5,
                ApplicationValues.BitstampName => 6,
                ApplicationValues.FTXName => 7,
                ApplicationValues.CoinbaseName => 3,
                
                ApplicationValues.BybitName => 8,
                ApplicationValues.BybitFuturesName => 9,

                _ or ApplicationValues.BinanceName => 1,
            };
        }
        public static int GetAllfundsChanId(string exchange)
        {
            return int.Parse($"{GetExchangeId(exchange)}00001");
        }
        public static int GetChanId(string exchange, string pair, string channel, string timeFrame = null)
        {
            int GetChannelId()
            {
                return (channel.ToLower()) switch
                {
                    "orderbook" => 2,
                    "trade" => 3,
                    _ or "candle" => 1,
                };
            }

            int GetTimeFrameId()
            {
                return timeFrame switch
                {
                    "5m" => 51,
                    "15m" => 151,
                    "30m" => 301,
                    "1H" => 12,
                    "2H" => 22,
                    "4H" => 42,
                    "6H" => 62,
                    "12H" => 122,
                    "1D" => 13,
                    "3D" => 33,
                    _ or "1m" => 11,
                };
            }

            uint GetPairId()
            {
                byte[] encoded = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(pair));
                var value = BitConverter.ToUInt32(encoded, 0) % 10000;

                return value;
            }
            
            string chanId = $"{GetExchangeId(exchange)}{GetPairId()}{GetChannelId()}";
            if (timeFrame != null) chanId += GetTimeFrameId();

            return int.Parse(chanId);
        }

        public static long GetCloseTime(this Candle candle) =>
            candle.OpenTime + (long)candle.TimeFrame.ToTimeSpan().TotalMilliseconds;

        public static PairInfo GetPairInfoBySymbol(this IEnumerable<PairInfo> pairInfos, string symbol) =>
                pairInfos.FirstOrDefault(p => p.Symbol == symbol);

        public static decimal GetPriceRange(this Mode mode, decimal lastCandleClosePrice)
        {
            return mode switch
            {
                Mode.FULLHD => 0.005M * lastCandleClosePrice,
                Mode.FOURK => 0.0025M * lastCandleClosePrice,
                Mode.EightK => 0.00125M * lastCandleClosePrice,
                _ or Mode.HD => 0.01M * lastCandleClosePrice,
            };
        }

        /// <summary>
        /// Gets the element by 'key' if exist otherwise add a new one to the dictionary and return that
        /// </summary>
        public static List<Guid> GetOrMakeNew(this ConcurrentDictionary<string, List<Guid>> dictionary, string key)
        {
            List<Guid> value;

            try
            {
                 value = dictionary[key];
            }
            catch (KeyNotFoundException)
            { value = null; }


            if (value != null)
            {
                return value;
            }
            else
            {
                var val = new List<Guid>();
                lock (dictionary)
                    dictionary[key] =  val;
                return val;
            }
        }

        public static bool IsListed(this BinanceFuturesCoinSymbol symbol) =>
            symbol.Status == Binance.Net.Enums.SymbolStatus.Trading;

        public static bool IsListed(this BinanceFuturesUsdtSymbol symbol) =>
            symbol.Status == Binance.Net.Enums.SymbolStatus.Trading;

        public static bool IsListed(this BinanceSymbol symbol) =>
            symbol.Status == Binance.Net.Enums.SymbolStatus.Trading;

        public static int GetQuoteAssetPrecision(this BinanceFuturesUsdtSymbol symbol)
        {
            /// minPrice for example: 0.0004000 we want 4 for QuoteAssetPrecision
            /// example: 10.000 or 1.0 we want 0 for QuoteAssetPrecision
            var arr = symbol.PriceFilter.TickSize.G29().Split('.');
            var precision = (arr.Length > 1) ? arr[1].Length : 0;
            return precision;
        }

        public static int GetQuoteAssetPrecision(this BinanceSymbol symbol)
        {
            /// minPrice for example: 0.0004000 we want 4 for QuoteAssetPrecision
            /// example: 10.000 or 1.0 we want 0 for QuoteAssetPrecision
            var arr = symbol.PriceFilter.MinPrice.G29().Split('.');
            var precision = (arr.Length > 1) ? arr[1].Length : 0;
            return precision;
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
                (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
