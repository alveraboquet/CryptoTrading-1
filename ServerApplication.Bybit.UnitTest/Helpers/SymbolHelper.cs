using System.Collections.Generic;
using DataLayer;
using DataLayer.Models;

namespace ServerApplication.Bybit.UnitTest.Helpers
{
    public class SymbolHelper
    {
        public static PairInfo GetPair(string symbol, string exchange)
        {
            List<TimeFrameOption> timeFrameOptions = new List<TimeFrameOption>()
            {
                new TimeFrameOption()
                {
                    TimeFrame = "1m",
                    StartTimeMax = null,
                    EndTimeMax = null,
                },
                new TimeFrameOption()
                {
                    TimeFrame = "5m",
                    StartTimeMax = null,
                    EndTimeMax = null,
                },
                new TimeFrameOption()
                {
                    TimeFrame = "15m",
                    StartTimeMax = null,
                    EndTimeMax = null,
                },
                new TimeFrameOption()
                {
                    TimeFrame = "30m",
                    StartTimeMax = null,
                    EndTimeMax = null,
                },
                new TimeFrameOption()
                {
                    TimeFrame = "1H",
                    StartTimeMax = null,
                    EndTimeMax = null,
                },
                new TimeFrameOption()
                {
                    TimeFrame = "2H",
                    StartTimeMax = null,
                    EndTimeMax = null,
                },
                new TimeFrameOption()
                {
                    TimeFrame = "4H",
                    StartTimeMax = null,
                    EndTimeMax = null,
                },
                new TimeFrameOption()
                {
                    TimeFrame = "6H",
                    StartTimeMax = null,
                    EndTimeMax = null,
                },
                new TimeFrameOption()
                {
                    TimeFrame = "1D",
                    StartTimeMax = null,
                    EndTimeMax = null,
                }
            };

            return new PairInfo()
            {
                Exchange = exchange,
                IsAvailableFootprint = true,
                IsLinechart = false,
                IsAvailableHeatmap = true,
                IsAvailableVolume = true,
                Symbol = symbol,
                QuoteAssetPrecision = 2,
                TimezoneDailyCloseFormat = "UTC 00:00",
                TimeFrameOptions = timeFrameOptions,
                IsListed = true
            };
        }
    }
}