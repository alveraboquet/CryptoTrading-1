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
    public interface IBinanceFuturesCoinServices
    {
        Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, KlineInterval timeFrame, DateTime? startTime, DateTime? endTime, int? limit = 1000);

        Task<IEnumerable<BinanceRecentTrade>> GetMarketBookAsync(string symbol, int? limit = 1000);
        Task<WebCallResult<IBinanceKline>> GetOpenCandelAsync(string symbol, KlineInterval timeFrame);
        Task<DateTime> GetEndTimeMax(string symbol, KlineInterval timeFrame);
        Task<IEnumerable<BinanceFuturesCoinSymbol>> GetSymbolsAsync();
        Task<DateTime> GetStartTimeMax(string symbol, KlineInterval timeFrame);
        Task<BinanceOrderBook> GetOrderBookAsync(string symbol, int? limit = 1000);
    }
}
