using Binance.Net.Enums;
using Binance.Net.Interfaces;
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
    public interface IBinanceServices
    {
        /// <exception cref="BinanceTooManyRequestException"/>
        Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, KlineInterval timeFrame, DateTime? startTime, DateTime? endTime, int? limit = 1000);
        /// <exception cref="BinanceTooManyRequestException"/>
        Task<IEnumerable<BinanceSymbol>> GetSymbolsAsync();
        /// <exception cref="BinanceTooManyRequestException"/>
        Task<DateTime> GetStartTimeMax(string symbol, KlineInterval timeFrame);
        /// <exception cref="BinanceTooManyRequestException"/>
        Task<DateTime> GetEndTimeMax(string symbol, KlineInterval timeFrame);
    }
}
