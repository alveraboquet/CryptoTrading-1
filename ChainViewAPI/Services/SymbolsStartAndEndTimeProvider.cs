using ExchangeServices;
using ExchangeServices.ExtensionMethods;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities;

namespace ChainViewAPI.Services
{
    public class SymbolsStartAndEndTimeProvider
    {
        private readonly IBinanceFuturesUsdtServices _binanceFuturesApi;
        private readonly IBinanceServices _binanceApi;
        private readonly IMemoryCache _cache;
        public SymbolsStartAndEndTimeProvider(IBinanceFuturesUsdtServices binanceFuturesApi, IBinanceServices binanceApi,
            IMemoryCache cache)
        {
            this._cache = cache;
            this._binanceApi = binanceApi;
            this._binanceFuturesApi = binanceFuturesApi;
        }

        /// <summary>
        /// checks StartTimeMax of this timeframeOption, if it was null does an Api call to binance and gets the value.
        /// </summary>
        /// <returns>true if StartTimeMax wasn't initialized, false otherwise.</returns>
        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<(bool hasChanged, long startTimeMax)> GetStartTimeMax(DataLayer.PairInfo pair, string timeframe)
        {
            switch (pair.Exchange)
            {
                case ApplicationValues.BinanceName:
                    return await pair.TryGetBinanceStartTimeMax(timeframe, this._binanceApi);

                case ApplicationValues.BinanceUsdName:
                    if (pair.IsOiPair())
                        return (false, DateTime.UtcNow.AddDays(-29).ToUnixTimestamp());

                    else if (pair.IsLiqFrOiSymbol())
                    {
                        _cache.TryGetPairInfo(pair.Exchange, pair.GetSymbol(), out var newPair);
                        var res = await newPair.TryGetBinanceFuturesUsdStartTimeMax(timeframe, _binanceFuturesApi);
                        return (res.startTimeMax != pair.GetTimeFrameOrDefault(timeframe).StartTimeMax, res.startTimeMax);
                    }

                    else
                        return await pair.TryGetBinanceFuturesUsdStartTimeMax(timeframe, _binanceFuturesApi);

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// checks EndTimeMax of this timeframeOption, if it was null does an Api call to binance and gets the value.
        /// </summary>
        /// <returns>true if EndTimeMax wasn't initialized, false otherwise.</returns>
        /// <exception cref="BinanceTooManyRequestException"/>
        public async  Task<(bool hasChanged, long? endTimeMax)> TryGetEndTimeMax(DataLayer.PairInfo pair, string timeframe)
        {
            switch (pair.Exchange)
            {
                case ApplicationValues.BinanceName:
                    return await pair.TryGetBinanceEndTimeMax(timeframe, _binanceApi);
                    
                case ApplicationValues.BinanceUsdName:
                    if (pair.IsOiPair())
                        return (false, DateTime.UtcNow.ToUnixTimestamp());

                    else if (pair.IsLiqFrOiSymbol())
                    {
                        _cache.TryGetPairInfo(pair.Exchange, pair.GetSymbol(), out var newPair);
                        var (hasChanged, endTimeMax) = await newPair.TryGetBinanceFuturesEndTimeMax(timeframe, _binanceFuturesApi);
                        return (endTimeMax != pair.GetTimeFrameOrDefault(timeframe).EndTimeMax, endTimeMax);
                    }

                    else
                        return await pair.TryGetBinanceFuturesEndTimeMax(timeframe, _binanceFuturesApi);

                default:
                    throw new NotImplementedException();
            };
        }
    }
}