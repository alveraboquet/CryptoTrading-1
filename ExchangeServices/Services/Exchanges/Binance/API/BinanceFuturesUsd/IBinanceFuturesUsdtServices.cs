using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Futures.MarketData;
using Binance.Net.Objects.Spot.MarketData;
using CryptoExchange.Net.ExchangeInterfaces;
using CryptoExchange.Net.Objects;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeServices
{
    public interface IBinanceFuturesUsdtServices
    {
        /// <exception cref="BinanceTooManyRequestException"/>
        Task<IEnumerable<BinanceFuturesUsdtSymbol>> GetSymbolsAsync();

        /// <exception cref="BinanceTooManyRequestException"/>
        Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, KlineInterval timeFrame, DateTime? startTime, DateTime? endTime, int? limit = 1000);
        /// <exception cref="BinanceTooManyRequestException"/>
        Task<IEnumerable<ResCandle>> GetOpenInterestCandles(string symbol, PeriodInterval timeFrame, DateTime? startTime, DateTime? endTime, int? limit = 500);

        /// <exception cref="BinanceTooManyRequestException"/>
        Task<DateTime> GetEndTimeMax(string symbol, KlineInterval timeFrame);
        /// <exception cref="BinanceTooManyRequestException"/>
        Task<DateTime> GetOpenInterestEndTimeMax(string symbol, PeriodInterval timeFrame);


        /// <exception cref="BinanceTooManyRequestException"/>
        Task<DateTime> GetStartTimeMax(string symbol, KlineInterval timeFrame);
    }
}
