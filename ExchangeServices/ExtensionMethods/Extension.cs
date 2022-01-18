using System.Linq;
using System.Threading.Tasks;
using Utilities;

namespace ExchangeServices.ExtensionMethods
{
    public static class ExtensionMet
    {
        #region Binance Start

        /// <summary>
        /// checks StartTimeMax of this timeframeOption, if it was null does an Api call to binance and gets the value.
        /// </summary>
        /// <returns>true if StartTimeMax wasn't initialized, false otherwise.</returns>
        /// <exception cref="BinanceTooManyRequestException"/>
        public static Task<(bool hasChanged, long startTimeMax)> TryGetBinanceStartTimeMax(this DataLayer.PairInfo pair,
            string timeframe, IBinanceServices api)
        {
            var timeframeO = pair.TimeFrameOptions.FirstOrDefault(p => p.TimeFrame == timeframe);
            return timeframeO.TryGetBinanceStartTimeMax(pair.Symbol, api);
        }

        /// <summary>
        /// checks StartTimeMax of this timeframeOption, if it was null does an Api call to binance and gets the value.
        /// </summary>
        /// <returns>true if StartTimeMax wasn't initialized, false otherwise.</returns>
        /// <exception cref="BinanceTooManyRequestException"/>
        public static async Task<(bool hasChanged, long startTimeMax)> TryGetBinanceStartTimeMax(this DataLayer.Models.TimeFrameOption timeframeO,
            string symbol, IBinanceServices api)
        {
            long startTimeMax;
            if (timeframeO.StartTimeMax == null)
            {
                try
                {
                    timeframeO.StartTimeMax = (await api.GetStartTimeMax(symbol, timeframeO.TimeFrame.ToBinanceTimeFrame())).ToUnixTimestamp();
                    startTimeMax = timeframeO.StartTimeMax ?? 0;
                    return (true, startTimeMax);
                }
                catch (BinanceTooManyRequestException)
                { throw; }
            }
            else
            {
                startTimeMax = timeframeO.StartTimeMax ?? 0;
                return (false, startTimeMax);
            }
        }

        #endregion

        #region BinanceFutures Usd Start

        /// <summary>
        /// checks StartTimeMax of this timeframeOption, if it was null does an Api call to binance and gets the value.
        /// </summary>
        /// <returns>true if StartTimeMax wasn't initialized, false otherwise.</returns>
        /// <exception cref="BinanceTooManyRequestException"/>
        public static Task<(bool hasChanged, long startTimeMax)> TryGetBinanceFuturesUsdStartTimeMax(this DataLayer.PairInfo pair,
            string timeframe, IBinanceFuturesUsdtServices api)
        {
            var timeframeO = pair.TimeFrameOptions.FirstOrDefault(p => p.TimeFrame == timeframe);
            return timeframeO.TryGetBinanceFuturesUsdStartTimeMax(pair.Symbol, api);
        }

        /// <summary>
        /// checks StartTimeMax of this timeframeOption, if it was null does an Api call to binance and gets the value.
        /// </summary>
        /// <returns>true if StartTimeMax wasn't initialized, false otherwise.</returns>
        /// <exception cref="BinanceTooManyRequestException"/>
        public static async Task<(bool hasChanged, long startTimeMax)> TryGetBinanceFuturesUsdStartTimeMax(this DataLayer.Models.TimeFrameOption timeframeO,
            string symbol, IBinanceFuturesUsdtServices api)
        {
            long startTimeMax;
            if (timeframeO.StartTimeMax == null)
            {
                try
                {
                    timeframeO.StartTimeMax = (await api.GetStartTimeMax(symbol, timeframeO.TimeFrame.ToBinanceTimeFrame())).ToUnixTimestamp();
                    startTimeMax = timeframeO.StartTimeMax ?? 0;
                    return (true, startTimeMax);
                }
                catch (BinanceTooManyRequestException)
                { throw; }
            }
            else
            {
                startTimeMax = timeframeO.StartTimeMax ?? 0;
                return (false, startTimeMax);
            }
        }

        #endregion

        #region Binance End

        /// <summary>
        /// checks EndTimeMax of this timeframeOption, if it was null does an Api call to binance and gets the value.
        /// </summary>
        /// <returns>true if EndTimeMax wasn't initialized, false otherwise.</returns>
        /// <exception cref="BinanceTooManyRequestException"/>
        public static async Task<(bool hasChanged, long? endTimeMax)> TryGetBinanceEndTimeMax(this DataLayer.PairInfo pair, string timeframe, IBinanceServices api)
        {
            long? endTimeMax;
            if (pair.IsListed)
            {
                endTimeMax = null;
                return (false, endTimeMax);
            }

            var timeframeO = pair.TimeFrameOptions.FirstOrDefault(p => p.TimeFrame == timeframe);
            if (timeframeO.EndTimeMax == null)
            {
                try
                {
                    endTimeMax = timeframeO.EndTimeMax = (await api.GetEndTimeMax(pair.Symbol, timeframe.ToBinanceTimeFrame())).ToUnixTimestamp();
                    return (true, endTimeMax);
                }
                catch (BinanceTooManyRequestException)
                { throw; }
            }
            else
            {
                endTimeMax = timeframeO.EndTimeMax;
                return (false, endTimeMax);
            }
        }

        #endregion

        #region BinanceFutures Usd End

        /// <summary>
        /// checks EndTimeMax of this timeframeOption, if it was null does an Api call to binance and gets the value.
        /// </summary>
        /// <returns>true if EndTimeMax wasn't initialized, false otherwise.</returns>
        /// <exception cref="BinanceTooManyRequestException"/>
        public static async Task<(bool hasChanged, long? endTimeMax)> TryGetBinanceFuturesEndTimeMax(this DataLayer.PairInfo pair, string timeframe, IBinanceFuturesUsdtServices api)
        {
            long? endTimeMax;
            if (pair.IsListed)
            {
                endTimeMax = null;
                return (false, endTimeMax);
            }

            var timeframeO = pair.TimeFrameOptions.FirstOrDefault(p => p.TimeFrame == timeframe);
            if (timeframeO.EndTimeMax == null)
            {
                try
                {
                    endTimeMax = timeframeO.EndTimeMax = (await api.GetEndTimeMax(pair.Symbol, timeframe.ToBinanceTimeFrame())).ToUnixTimestamp();
                    return (true, endTimeMax);
                }
                catch (BinanceTooManyRequestException)
                { throw; }
            }
            else
            {
                endTimeMax = timeframeO.EndTimeMax;
                return (false, endTimeMax);
            }
        }
        
        #endregion
    }
}