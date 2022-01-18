using Binance.Net.Enums;
using DataLayer.Models;
using DataLayer;
using FtxApi;
using FtxApi.Enums;
using FtxApi.Models.Markets;
using FtxApi.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace ExchangeServices
{
    public class FTXServices : IFTXServices
    {
        private FtxRestApi _client;
        public FTXServices(FtxRestApi client)
        {
            _client = client;
        }

        public async Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, FtxResolution timeFrame, int limit, DateTime startTime, DateTime endTime)
        {
            var result = await _client.GetHistoricalPricesAsync(symbol, timeFrame, limit, startTime, endTime);
            
            if (!result.Success) return null;

            var candles = result.Result.Select(c => new Candle(
                openTime: c.StartTime.ToUnixTimestamp(),
                open: c.Open,
                high: c.High,
                low: c.Low,
                close: c.Close,
                volume: c.Volume,
                timeframe: timeFrame.ToStringFormat(),
                exchange: ApplicationValues.FTXName,
                symbol: symbol));

            return candles;
        }

        public async Task<IEnumerable<Candle>> GetLastCandelsAsync(string symbol, FtxResolution timeFrame, int limit)
        {
            var result = await _client.GetHistoricalPricesAsync(symbol, timeFrame, limit, DateTime.Now, DateTime.Now);

            if (!result.Success) return null;

            var candles = result.Result.Select(c => new Candle(
                openTime: c.StartTime.ToUnixTimestamp(),
                open: c.Open,
                high: c.High,
                low: c.Low,
                close: c.Close,
                volume: c.Volume,
                timeframe: timeFrame.ToStringFormat(),
                exchange: ApplicationValues.FTXName,
                symbol: symbol));

            return candles;
        }

        /// <summary>
        /// Gets current trades in market book
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="limit">limt for trades count max 5000</param>
        /// <param name="start">from date</param>
        /// <param name="end">to date</param>
        /// <returns>list of trades</returns>
        public async Task<List<FtxApi.Models.Trade>> GetMarketBookAsync(string symbol, int limit, DateTime start, DateTime end)
        {
            var trades = (await _client.GetMarketTradesAsync(symbol, limit, start, end));
            if (!trades.Success) return null;
            return trades.Result;
        }

        public async Task<Orderbook> GetOrderBookAsync(string symbol)
        {
            var orderBook = await _client.GetMarketOrderBookAsync(symbol);
            if (!orderBook.Success) return null;

            return orderBook.Result;
        }

        public async Task<IEnumerable<string>> GetSymbolsAsync()
        {
            var symbols = await _client.GetMarketsAsync();
            if (!symbols.Success) return null;

            return symbols.Result.Select(s => s.Name);
        }
    }
}
