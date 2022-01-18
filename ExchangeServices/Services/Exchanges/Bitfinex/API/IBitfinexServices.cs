using Bitfinex.Net.Objects;
using CryptoExchange.Net.Objects;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeServices
{
    public interface IBitfinexServices
    {
        Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, TimeFrame timeFrame, DateTime startTime, DateTime endTime, int? limit = null, Sorting sorting = Sorting.OldFirst);

        Task<IEnumerable<BitfinexTradeSimple>> GetMarketBookAsync(string symbol, DateTime startTime, DateTime endTime, Sorting sorting = Sorting.OldFirst, int? limit = null);

        Task<IEnumerable<string>> GetSymbolsAsync();

        Task<IEnumerable<BitfinexOrderBookEntry>> GetOrderBookAsync(string symbol, Precision precision, int? limit = null);
         Task<WebCallResult<BitfinexKline>> GetLastKline(TimeFrame timeFrame, string symbol, string fundingPeriod = null, CancellationToken ct = default);
        Task<DateTime> GetStartTimeMax(string symbol, TimeFrame timeFrame);
    }
}
