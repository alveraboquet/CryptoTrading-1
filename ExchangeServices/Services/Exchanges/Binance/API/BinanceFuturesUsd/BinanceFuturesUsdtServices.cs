using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot.MarketData;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using CryptoExchange.Net.ExchangeInterfaces;
using Utilities;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Objects;
using Binance.Net.Objects.Futures.MarketData;
using System.Threading;

namespace ExchangeServices
{
    public class BinanceFuturesUsdtServices : IBinanceFuturesUsdtServices
    {
        private BinanceClient _client;
        private bool locked;
        private Timer timer;
        public BinanceFuturesUsdtServices(BinanceClient binanceClient)
        {
            locked = false;
            var now = DateTime.Now;
            int startIn = 61000 - ((now.Second * 1000) + now.Millisecond);

            timer = new Timer((o) => locked = false,
                null, startIn, 60000);
            _client = binanceClient;
        }

        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<IEnumerable<BinanceFuturesUsdtSymbol>> GetSymbolsAsync()
        {
            var result = await _client.FuturesUsdt.System.GetExchangeInfoAsync();
            try
            {
                IsLocked(result.ResponseStatusCode);
            }
            catch (BinanceTooManyRequestException)
            { throw; }
            if (!result.Success) return null;
            return result.Data.Symbols;
        }

        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, KlineInterval timeFrame,
            DateTime? startTime, DateTime? endTime, int? limit = 1000)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            var result = await _client.FuturesUsdt.Market.GetKlinesAsync(symbol, timeFrame, startTime, endTime, limit);
            try
            {
                IsLocked(result.ResponseStatusCode);
            }
            catch (BinanceTooManyRequestException)
            { throw; }

            if (!result.Success)
                return null;

            var candles = result.Data.Select(c => new Candle(
                openTime: c.OpenTime.ToUnixTimestamp(),
                open: c.Open,
                high: c.High,
                low: c.Low,
                close: c.Close,
                volume: c.BaseVolume,
                timeframe: timeFrame.ToStringFormat(),
                exchange: ApplicationValues.BinanceUsdName,
                symbol: symbol));

            return candles;
        }
        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<IEnumerable<ResCandle>> GetOpenInterestCandles(string symbol, PeriodInterval timeFrame,
            DateTime? startTime, DateTime? endTime, int? limit = 500)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            var result = await _client.FuturesUsdt.Market.GetOpenInterestHistoryAsync(symbol, timeFrame, limit, startTime, endTime);
            try
            {
                IsLocked(result.ResponseStatusCode);
            }
            catch (BinanceTooManyRequestException)
            { throw; }


            if (!result.Success)
                return null;

            var candles = result.Data.Select(c => new ResCandle()
            {
                OpenTime = c.Timestamp.Value.ToUnixTimestamp(),

                Open = c.SumOpenInterestValue,
                High = c.SumOpenInterestValue,
                Low = c.SumOpenInterestValue,
                Close = c.SumOpenInterestValue,

                Volume = 0,
            });

            return candles;
        }

        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<DateTime> GetStartTimeMax(string symbol, KlineInterval timeFrame)
        {
            symbol = symbol.ToUpper();
        o:
            var result = await _client.FuturesUsdt.Market.GetKlinesAsync(symbol, timeFrame, new DateTime(1970, 1, 1), null, 1);
            try
            {
                IsLocked(result.ResponseStatusCode);
            }
            catch (BinanceTooManyRequestException)
            { throw; }

            if (!result.Success)
                goto o;
            return result.Data.Min(c => c.OpenTime);
        }

        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<DateTime> GetEndTimeMax(string symbol, KlineInterval timeFrame)
        {
            symbol = symbol.ToUpper();
            var result = await _client.FuturesUsdt.Market.GetKlinesAsync(symbol, timeFrame, null, null, 1);

            try
            {
                IsLocked(result.ResponseStatusCode);
            }
            catch (BinanceTooManyRequestException)
            { throw; }

            return result.Data.Max(c => c.OpenTime);
        }
        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<DateTime> GetOpenInterestEndTimeMax(string symbol, PeriodInterval timeFrame)
        {
            symbol = symbol.ToUpper();
            var result = await _client.FuturesUsdt.Market.GetOpenInterestHistoryAsync(symbol, timeFrame, 1, null, null);

            try
            {
                IsLocked(result.ResponseStatusCode);
            }
            catch (BinanceTooManyRequestException)
            { throw; }

            return result.Data.Max(c => c.Timestamp.Value);
        }

        /// <exception cref="BinanceTooManyRequestException"/>
        private void IsLocked(System.Net.HttpStatusCode? code)
        {
            if (code != null && code.Value == System.Net.HttpStatusCode.TooManyRequests)
                locked = true;
            if (locked)
                throw new BinanceTooManyRequestException("Too many requests to binance.com");
        }


        public async Task<IEnumerable<BinanceRecentTrade>> GetMarketBookAsync(string symbol, int? limit = 1000)
        {
            var trades = await _client.FuturesUsdt.Market.GetSymbolTradesAsync(symbol, limit);
            if (!trades.Success) return null;

            return (IEnumerable<BinanceRecentTrade>)trades.Data;
        }
        public async Task<WebCallResult<IBinanceKline>> GetOpenCandelAsync(string symbol, KlineInterval timeFrame)
        {
            var result = await _client.FuturesUsdt.Market.GetKlinesAsync(symbol, timeFrame, null, null, 1);
            var openCandle = new WebCallResult<IBinanceKline>(
                result.ResponseStatusCode,
                result.ResponseHeaders,
                result.Data.FirstOrDefault(),
                result.Error);

            return openCandle;
        }
        public async Task<BinanceOrderBook> GetOrderBookAsync(string symbol, int? limit = 1000)
        {
            var orderBook = await _client.FuturesUsdt.Market.GetOrderBookAsync(symbol, limit);
            if (!orderBook.Success) return null;
            return orderBook.Data;
        }
    }
}
