
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketStream;
using DataLayer;
using DataLayer.Models.Stream;
using ExchangeModels.BinanceFutures;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Redis
{
    public interface ICacheService
    {
        Task<bool> SetAllFundingRateAsync(string exchange, byte[] fundingRateJson);
        Task<List<FundingRateUpdate>> GetAllFundingRateAsync(string exchange);
        bool GetServerApplicationStoped(out long? stopTime);
        Task<bool> SetServerApplicationStoped(bool status, long stopTime);
        bool SetFootPrints(string exchange, string symbol, string timeframe, FootPrints footPrints);
        Task<bool> SetFootPrintsAsync(string exchange, string symbol, string timeframe, FootPrints footPrints);

        Task<FootPrints> TryGetFootPrints(string exchange, string symbol, string timeframe);

        Task<bool> SetOpenCandleAsync(Redis.OpenCandle openCandle);
        bool SetOpenCandle(Redis.OpenCandle openCandle);

        Task<bool> SetOpenCandleAsync(DataLayer.Candle candle);
        bool SetOpenCandle(DataLayer.Candle candle);

        Task<Redis.OpenCandle> TryGetOpenCandle(string exchange, string symbol, string timeFrame);

        Task<StreamingOrderBook> TryGetOrderBook(string exchange, string symbol);

        Task<bool> SetOrderBookAsync(string exchange, string symbol, StreamingOrderBook orderBook);
    }
}
