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

namespace ExchangeServices
{
    public class BinanceFuturesCoinServices : IBinanceFuturesCoinServices
    {
        private BinanceClient _client;

        public BinanceFuturesCoinServices(BinanceClient binanceClient)
        {
            _client = binanceClient;
        }

        public async Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, KlineInterval timeFrame, DateTime? startTime, DateTime? endTime, int? limit = 1000)
        {
            if (symbol == null) throw new ArgumentNullException(nameof(symbol));

            symbol = symbol.ToUpper();

            var result = await _client.FuturesCoin.Market.GetKlinesAsync(symbol, timeFrame, startTime, endTime, limit);
            if (!result.Success) return null;

            var candles = result.Data.Select(c => new Candle(
                openTime: c.OpenTime.ToUnixTimestamp(),
                open: c.Open,
                high: c.High,
                low: c.Low,
                close: c.Close,
                volume: c.BaseVolume,
                timeframe: timeFrame.ToStringFormat(),
                exchange: ApplicationValues.BinanceCoinName,
                symbol: symbol));

            return candles;
        }

        public async Task<IEnumerable<BinanceRecentTrade>> GetMarketBookAsync(string symbol, int? limit = 1000)
        {
            var trades = await _client.FuturesCoin.Market.GetSymbolTradesAsync(symbol, limit);
            if (!trades.Success) return null;

            return (IEnumerable<BinanceRecentTrade>)trades.Data;
        }

        public async Task<WebCallResult<IBinanceKline>> GetOpenCandelAsync(string symbol, KlineInterval timeFrame)
        {
            var result = await _client.FuturesCoin.Market.GetKlinesAsync(symbol, timeFrame, null, null, 1);
            var openCandle = new WebCallResult<IBinanceKline>(
                result.ResponseStatusCode,
                result.ResponseHeaders,
                result.Data.FirstOrDefault(),
                result.Error);

            return openCandle;
        }

        public async Task<BinanceOrderBook> GetOrderBookAsync(string symbol, int? limit = 1000)
        {
            var orderBook = await _client.FuturesCoin.Market.GetOrderBookAsync(symbol, limit);
            if (!orderBook.Success) return null;
            return orderBook.Data;
        }

        public async Task<DateTime> GetStartTimeMax(string symbol, KlineInterval timeFrame)
        {
            symbol = symbol.ToUpper();
        o:
            var result = await _client.FuturesCoin.Market.GetKlinesAsync(symbol, timeFrame, new DateTime(1970, 1, 1), null, 1);
            if (!result.Success) goto o;
            return result.Data.First().OpenTime;
        }

        public async Task<DateTime> GetEndTimeMax(string symbol, KlineInterval timeFrame)
        {
            symbol = symbol.ToUpper();
            o:
            var result = await _client.FuturesCoin.Market.GetKlinesAsync(symbol, timeFrame, null, null, 1);
            if (!result.Success) goto o;
            return result.Data.Max(c => c.OpenTime);
        }

        public async Task<IEnumerable<BinanceFuturesCoinSymbol>> GetSymbolsAsync()
        {
            var res = await _client.FuturesCoin.System.GetExchangeInfoAsync();
            if (!res.Success) return null;
            return res.Data.Symbols;
        }
    }
}
