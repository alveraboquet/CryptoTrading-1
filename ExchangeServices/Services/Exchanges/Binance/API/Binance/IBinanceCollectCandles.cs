using DataLayer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Enums;
namespace ExchangeServices
{
    public interface IBinanceCollectCandles
    {
        /// <exception cref="BinanceTooManyRequestException"/>
        Task<IEnumerable<DataLayer.ResCandle>> CollectCandleIfDoesNotExist(string symbol, KlineInterval interval, long start, long end, long minOpenTime, PairInfo pair);
    }
}
