using Bitfinex.Net;
using Bitfinex.Net.Objects;
using CryptoExchange.Net.Objects;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ExchangeServices
{
    public class BitfinexServices : IBitfinexServices
    {
        private BitfinexClient _client;
        public BitfinexServices(BitfinexClient client)
        {
            _client = client;
        }

        public async Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, TimeFrame timeFrame, DateTime startTime, DateTime endTime, int? limit = null, Sorting sorting = Sorting.OldFirst)
        {
            var result = await _client.GetKlinesAsync(
                timeFrame: timeFrame,
                symbol: symbol,
                limit: limit,
                startTime: startTime,
                endTime: endTime,
                sorting: Sorting.OldFirst);
 
            if (!result.Success) return null;

            var candles = result.Data.Select(c => new Candle(
                openTime: c.Timestamp.ToUnixTimestamp(),
                open: c.Open,
                high: c.High,
                low: c.Low,
                close: c.Close,
                volume: c.Volume,
                timeframe: timeFrame.ToStringFormat(),
                exchange: ApplicationValues.BitfinexName,
                symbol: symbol));

            return candles;
        }

        public async Task<WebCallResult<BitfinexKline>> GetLastKline(TimeFrame timeFrame, string symbol, string fundingPeriod = null, CancellationToken ct = default)
        {
            var result = await _client.GetLastKlineAsync(timeFrame, symbol, null, ct);

            var openCandle = new WebCallResult<BitfinexKline>(
                result.ResponseStatusCode,
                result.ResponseHeaders,
                result.Data,
                result.Error);

            return openCandle;
        }

        public async Task<IEnumerable<BitfinexTradeSimple>> GetMarketBookAsync(string symbol, DateTime startTime, DateTime endTime, Sorting sorting = Sorting.OldFirst, int? limit = null)
        {
            var result = await _client.GetTradesAsync(symbol, limit, startTime, endTime, sorting);
            if (!result.Success) return null;

            return result.Data;
        }

        public async Task<IEnumerable<BitfinexOrderBookEntry>> GetOrderBookAsync(string symbol, Precision precision, int? limit = null)
        {
            var result = await _client.GetOrderBookAsync(symbol, precision, limit);
            if (!result.Success) return null;

            return result.Data;
        }

        public async Task<DateTime> GetStartTimeMax(string symbol, TimeFrame timeFrame)
        {
            symbol = symbol.ToUpper();
            o:
            var result = await _client.GetKlinesAsync(timeFrame, symbol, null, null);
            if (!result.Success) goto o;
            return result.Data.First().Timestamp;
        }

        public async Task<IEnumerable<string>> GetSymbolsAsync()
        {
            var result = await _client.GetSymbolsAsync();
            if (!result.Success) return null;
            return result.Data;
        }
    
    }
}
