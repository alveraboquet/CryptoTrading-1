using DataLayer.Models;
using System.Linq;
using Binance.Net.Enums;
using Bitfinex.Net.Objects;
using DataLayer;
using ExchangeModels.Enums;
using FtxApi.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Binance.Net.Objects.Futures.MarketData;

namespace Utilities
{
    public static class Converter
    {
        /// <summary>
        /// to prevent European float number formating
        /// </summary>
        public static string G29(this decimal num)
        {
            string val = num.ToString("0.##############################");
            return val.Replace(',', '.');
        }

        public static string GetFundingRateSymbolName(this BinanceFuturesUsdtSymbol symbol) => $"FR.{symbol.Name}";
        public static void GetLiquidationSymbolNames(this BinanceFuturesUsdtSymbol symbol, out string name, out string sellName, out string buyName)
        {
            name = $"LIQ.{symbol.Name}";
            sellName = $"LIQSELL.{symbol.Name}";
            buyName = $"LIQBUY.{symbol.Name}";
        }
        public static string GetOpenIntersetSymbolName(this BinanceFuturesUsdtSymbol symbol) => $"OI.{symbol.Name}";

        public static DateTime UnixTimeStampToDateTime(this long ticks, bool isMilliSecond = true)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            if (isMilliSecond)
                dtDateTime = dtDateTime.AddMilliseconds(ticks);
            else
                dtDateTime = dtDateTime.AddSeconds(ticks);
            return dtDateTime;
        }

        public static IEnumerable<ResHeatmap> Convert8KToHeatmap(this IEnumerable<ResHeatmap> values, Mode mode)
        {
            var heatmaps = values.ToList();
            for (int i = 0; i < heatmaps.Count; i++)
            {
                heatmaps[i] = heatmaps[i].Convert8KToHeatmap(mode);
            }
            return heatmaps;
        }
        public static CustomPairType GetCustomePairType(this string symbol)
        {
            if (symbol.StartsWith("FR."))
                return CustomPairType.Fr;

            else if (symbol.StartsWith("LIQ."))
                return CustomPairType.Liq;

            else if (symbol.StartsWith("LIQBUY."))
                return CustomPairType.LiqBuy;

            else if (symbol.StartsWith("LIQSELL."))
                return CustomPairType.LiqSell;

            else if (symbol.StartsWith("OI."))
                return CustomPairType.OI;

            else
                return CustomPairType.None;
        }
        public static ResHeatmap Convert8KToHeatmap(this ResHeatmap value, Mode mode)
        {
            var heatmap = new Heatmap(mode, value.OpenPrice);

            var count = heatmap.Mode.GetBlocksCounts();

            for (int i = 0; i < heatmap.Blocks.Count; i++)
            {
                heatmap.Blocks[i] = value.Blocks.GetRange(i * count, count).Sum();
            }

            return new ResHeatmap()
            {
                Blocks = heatmap.Blocks,
                OpenTime = value.OpenTime,
                OpenPrice = heatmap.OpenPrice
            };
        }

        public static int GetBlocksCounts(this Mode mode)
        {
            return mode switch
            {
                Mode.FULLHD => 4,
                Mode.FOURK => 2,
                Mode.EightK => 1,
                _ or Mode.HD => 8,
            };
        }


        public static long GetMiliseconds(this KlineInterval interval)
        {
            long res = (long)interval.ToTimeSpan().TotalMilliseconds;
            return res;
        }

        public static string ToStringFormat(this Mode mode)
        {
            return mode switch
            {
                Mode.FULLHD => "FULLHD",
                Mode.FOURK => "4K",
                Mode.EightK => "8K",
                _ or Mode.HD => "HD",
            };
        }
        public static TimeSpan ToTimeSpan(this string timeFrameInStringFormat)
        {
            return timeFrameInStringFormat switch
            {
                "1m" => TimeSpan.FromMinutes(1),
                "5m" => TimeSpan.FromMinutes(5),
                "15m" => TimeSpan.FromMinutes(15),
                "30m" => TimeSpan.FromMinutes(30),
                "1H" => TimeSpan.FromHours(1),
                "2H" => TimeSpan.FromHours(2),
                "4H" => TimeSpan.FromHours(4),
                "6H" => TimeSpan.FromHours(6),
                "12H" => TimeSpan.FromHours(12),
                "1D" => TimeSpan.FromDays(1),
                "3D" => TimeSpan.FromDays(3),
                _ => throw new Exception($"Wrong TimeFrame '{timeFrameInStringFormat}'. AvailableTimeFrame:1m, 5m, 15m, 30m, 1H, 2H, 4H, 6H, 12H, 1D, 3D."),
            };
        }

        public static string ToStringFormat(this KlineInterval timeFrame)
        {
            return timeFrame switch
            {
                KlineInterval.OneMinute => "1m",
                KlineInterval.ThreeMinutes => "3m",
                KlineInterval.FiveMinutes => "5m",
                KlineInterval.FifteenMinutes => "15m",
                KlineInterval.ThirtyMinutes => "30m",
                KlineInterval.OneHour => "1H",
                KlineInterval.TwoHour => "2H",
                KlineInterval.FourHour => "4H",
                KlineInterval.SixHour => "6H",
                KlineInterval.EightHour => "8H",
                KlineInterval.TwelveHour => "12H",
                KlineInterval.OneDay => "1D",
                KlineInterval.ThreeDay => "3D",
                KlineInterval.OneWeek => "1W",
                KlineInterval.OneMonth => "1M",
                _ => null,
            };
        }

        public static string ToStringFormat(this BinanceAvailableTimeFrame timeFrame)
        {
            return timeFrame switch
            {
                BinanceAvailableTimeFrame.OneMinute => "1m",
                BinanceAvailableTimeFrame.FiveMinutes => "5m",
                BinanceAvailableTimeFrame.FifteenMinutes => "15m",
                BinanceAvailableTimeFrame.ThirtyMinutes => "30m",
                BinanceAvailableTimeFrame.OneHour => "1H",
                BinanceAvailableTimeFrame.TwoHour => "2H",
                BinanceAvailableTimeFrame.FourHour => "4H",
                BinanceAvailableTimeFrame.SixHour => "6H",
                BinanceAvailableTimeFrame.TwelveHour => "12H",
                BinanceAvailableTimeFrame.OneDay => "1D",
                BinanceAvailableTimeFrame.ThreeDay => "3D",
                _ => null,
            };
        }

        public static BinanceAvailableTimeFrame ToBinanceAvailableTimeFrame(this string timeFrameInStringFormat)
        {
            return timeFrameInStringFormat switch
            {
                "1m" => BinanceAvailableTimeFrame.OneMinute,
                "5m" => BinanceAvailableTimeFrame.FiveMinutes,
                "15m" => BinanceAvailableTimeFrame.FifteenMinutes,
                "30m" => BinanceAvailableTimeFrame.ThirtyMinutes,
                "1H" => BinanceAvailableTimeFrame.OneHour,
                "2H" => BinanceAvailableTimeFrame.TwoHour,
                "4H" => BinanceAvailableTimeFrame.FourHour,
                "6H" => BinanceAvailableTimeFrame.SixHour,
                "12H" => BinanceAvailableTimeFrame.TwelveHour,
                "1D" => BinanceAvailableTimeFrame.OneDay,
                "3D" => BinanceAvailableTimeFrame.ThreeDay,
                _ => throw new Exception($"No avaiable time frame {timeFrameInStringFormat}."),
            };
        }

        public static TimeSpan ToTimeSpan(this KlineInterval timeFrame)
        {
            return timeFrame switch
            {
                KlineInterval.OneMinute => TimeSpan.FromMinutes(1),
                KlineInterval.ThreeMinutes => TimeSpan.FromMinutes(3),
                KlineInterval.FiveMinutes => TimeSpan.FromMinutes(5),
                KlineInterval.FifteenMinutes => TimeSpan.FromMinutes(15),
                KlineInterval.ThirtyMinutes => TimeSpan.FromMinutes(30),
                KlineInterval.OneHour => TimeSpan.FromHours(1),
                KlineInterval.TwoHour => TimeSpan.FromHours(2),
                KlineInterval.FourHour => TimeSpan.FromHours(4),
                KlineInterval.SixHour => TimeSpan.FromHours(6),
                KlineInterval.EightHour => TimeSpan.FromHours(8),
                KlineInterval.TwelveHour => TimeSpan.FromHours(12),
                KlineInterval.OneDay => TimeSpan.FromDays(1),
                KlineInterval.ThreeDay => TimeSpan.FromDays(3),
                KlineInterval.OneWeek => TimeSpan.FromDays(7),
                KlineInterval.OneMonth => TimeSpan.FromDays(30),
                _ => throw new Exception($"Wrong KlineInterval '{timeFrame}'."),
            };
        }

        public static KlineInterval ToBinanceTimeFrame(this string timeFrameInStringFormat)
        {
            return timeFrameInStringFormat switch
            {
                "1m" => KlineInterval.OneMinute,
                "3m" => KlineInterval.ThreeMinutes,
                "5m" => KlineInterval.FiveMinutes,
                "15m" => KlineInterval.FifteenMinutes,
                "30m" => KlineInterval.ThirtyMinutes,
                "1H" => KlineInterval.OneHour,
                "2H" => KlineInterval.TwoHour,
                "4H" => KlineInterval.FourHour,
                "6H" => KlineInterval.SixHour,
                "8H" => KlineInterval.EightHour,
                "12H" => KlineInterval.TwelveHour,
                "1D" => KlineInterval.OneDay,
                "3D" => KlineInterval.ThreeDay,
                "1W" => KlineInterval.OneWeek,
                "1M" => KlineInterval.OneMonth,
                _ => throw new Exception($"Wrong time frame {timeFrameInStringFormat}."),
            };
        }

        public static PeriodInterval ToBinancePeriodInterval(this string timeFrameInStringFormat)
        {
            return timeFrameInStringFormat switch
            {
                "5m" => PeriodInterval.FiveMinutes,
                "15m" => PeriodInterval.FifteenMinutes,
                "30m" => PeriodInterval.ThirtyMinutes,
                "1H" => PeriodInterval.OneHour,
                "2H" => PeriodInterval.TwoHour,
                "4H" => PeriodInterval.FourHour,
                "6H" => PeriodInterval.SixHour,
                "12H" => PeriodInterval.TwelveHour,
                "1D" => PeriodInterval.OneDay,
                _ => throw new Exception($"Wrong time frame {timeFrameInStringFormat}."),
            };
        }

        public static string ToStringFormat(this PeriodInterval interval)
        {
            return interval switch
            {
                PeriodInterval.FiveMinutes => "5m",
                PeriodInterval.FifteenMinutes => "15m",
                PeriodInterval.ThirtyMinutes => "30m",
                PeriodInterval.OneHour => "1H",
                PeriodInterval.TwoHour=> "2H",
                PeriodInterval.FourHour => "4H",
                PeriodInterval.SixHour => "6H",
                PeriodInterval.TwelveHour => "12H",
                PeriodInterval.OneDay => "1D",
                _ => throw new Exception($"Wrong time frame {interval}."),
            };
        }

        public static KlineInterval ToBinanceTimeFrame(this BinanceAvailableTimeFrame timeFrame)
        {
            return timeFrame switch
            {
                BinanceAvailableTimeFrame.FiveMinutes => KlineInterval.FiveMinutes,
                BinanceAvailableTimeFrame.FifteenMinutes => KlineInterval.FifteenMinutes,
                BinanceAvailableTimeFrame.ThirtyMinutes => KlineInterval.ThirtyMinutes,
                BinanceAvailableTimeFrame.OneHour => KlineInterval.OneHour,
                BinanceAvailableTimeFrame.TwoHour => KlineInterval.TwoHour,
                BinanceAvailableTimeFrame.FourHour => KlineInterval.FourHour,
                BinanceAvailableTimeFrame.SixHour => KlineInterval.SixHour,
                BinanceAvailableTimeFrame.TwelveHour => KlineInterval.TwelveHour,
                BinanceAvailableTimeFrame.OneDay => KlineInterval.OneDay,
                BinanceAvailableTimeFrame.ThreeDay => KlineInterval.ThreeDay,
                _ or BinanceAvailableTimeFrame.OneMinute => KlineInterval.OneMinute,
            };
        }

        public static long ToUnixTimestamp(this DateTime time)
        {
            return ((DateTimeOffset)time).ToUnixTimeMilliseconds();
        }

        public static string ToStringFormat(this FtxResolution timeFrame)
        {
            return timeFrame switch
            {
                FtxResolution.FifteenSeconds => "15s",
                FtxResolution.OneMinute => "1m",
                FtxResolution.FiveMinutes => "5m",
                FtxResolution.FifteenMinutes => "15m",
                FtxResolution.OneHour => "1H",
                FtxResolution.FourHour => "4H",
                FtxResolution.OneDay => "1D",
                _ => null,
            };
        }

        public static int GetSeconds(this CoinbaseTimeFrame timeFrame)
        {
            return timeFrame switch
            {
                CoinbaseTimeFrame.OneMinute => 60,
                CoinbaseTimeFrame.FiveMinutes => 300,
                CoinbaseTimeFrame.FifteenMinutes => 900,
                CoinbaseTimeFrame.OneHour => 3600,
                CoinbaseTimeFrame.SixHours => 21600,
                CoinbaseTimeFrame.OneDay => 86400,
                _ => 0,
            };
        }

        public static string ToStringFormat(this CoinbaseTimeFrame timeFrame)
        {
            return timeFrame switch
            {
                CoinbaseTimeFrame.OneMinute => "1m",
                CoinbaseTimeFrame.FiveMinutes => "5m",
                CoinbaseTimeFrame.FifteenMinutes => "15m",
                CoinbaseTimeFrame.OneHour => "1H",
                CoinbaseTimeFrame.SixHours => "6H",
                CoinbaseTimeFrame.OneDay => "1D",
                _ => null,
            };
        }

        public static string ToStringFormat(this TimeFrame timeFrame)
        {
            return timeFrame switch
            {
                TimeFrame.OneMinute => "1m",
                TimeFrame.FiveMinute => "5m",
                TimeFrame.FifteenMinute => "15m",
                TimeFrame.ThirtyMinute => "30m",
                TimeFrame.OneHour => "1H",
                TimeFrame.ThreeHour => "3H",
                TimeFrame.SixHour => "6H",
                TimeFrame.TwelveHour => "12H",
                TimeFrame.OneDay => "1D",
                TimeFrame.SevenDay => "1W",
                TimeFrame.FourteenDay => "2W",
                TimeFrame.OneMonth => "1M",
                _ => null,
            };
        }
    }
}
