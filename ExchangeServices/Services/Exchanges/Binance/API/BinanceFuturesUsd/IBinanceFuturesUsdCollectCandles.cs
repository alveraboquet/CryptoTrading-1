using Binance.Net.Enums;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeServices
{
    public interface IBinanceFuturesUsdCollectCandles
    {
        /// <exception cref="BinanceTooManyRequestException"/>
        Task<IEnumerable<DataLayer.ResCandle>> CollectCandleIfDoesNotExist(string symbol, KlineInterval interval, long start, long end, long minOpenTime, PairInfo pair);
        Task<IEnumerable<DataLayer.ResCandle>> CollectOpenInterestCandlesIfDoesNotExist(string symbol, PeriodInterval interval, long start, long end, PairInfo pair, long minOpenTime, long maxOpenTime, Action<IEnumerable<ResCandle>> addRange);
    }
}
