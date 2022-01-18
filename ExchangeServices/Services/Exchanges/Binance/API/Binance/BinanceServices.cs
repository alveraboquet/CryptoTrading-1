using Binance.Net;
using Binance.Net.Enums;
using DataLayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using CryptoExchange.Net.ExchangeInterfaces;
using Binance.Net.Objects.Spot.MarketData;
using FtxApi.Util;
using Binance.Net.Objects.Spot;
using Utilities;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Objects;
using System.Threading;

namespace ExchangeServices
{
    [Serializable]
    public class BinanceTooManyRequestException : Exception
    {
        public BinanceTooManyRequestException() { }
        public BinanceTooManyRequestException(string message) : base(message) { }
        public BinanceTooManyRequestException(string message, Exception inner) : base(message, inner) { }
        protected BinanceTooManyRequestException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class BinanceServices : IBinanceServices
    {
        private BinanceClient _client;
        private bool locked;
        private Timer timer;
        public BinanceServices(BinanceClient binanceClient)
        {
            DateTime now = DateTime.Now;
            int startIn = 61000 - ((now.Second * 1000) + now.Millisecond);

            timer = new Timer((o) => locked = false, null, startIn, 60000);

            locked = false;
            _client = binanceClient;
        }


        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, KlineInterval timeFrame, DateTime? startTime, DateTime? endTime, int? limit = 1000)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            var result = await _client.Spot.Market.GetKlinesAsync(symbol, timeFrame, startTime, endTime, limit);
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
                exchange: ApplicationValues.BinanceName,
                symbol: symbol));

            return candles;
        }

        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<DateTime> GetStartTimeMax(string symbol, KlineInterval timeFrame)
        {
            symbol = symbol.ToUpper();
        o:
            var result = await _client.Spot.Market.GetKlinesAsync(symbol, timeFrame, new DateTime(1970, 1, 1), null, 1);
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
        public async Task<IEnumerable<BinanceSymbol>> GetSymbolsAsync()
        {
            var result = await _client.Spot.System.GetExchangeInfoAsync();
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
        public async Task<DateTime> GetEndTimeMax(string symbol, KlineInterval timeFrame)
        {
            symbol = symbol.ToUpper();
            var result = await _client.Spot.Market.GetKlinesAsync(symbol, timeFrame, null, null, 1);
            try
            {
                IsLocked(result.ResponseStatusCode);
            }
            catch (BinanceTooManyRequestException)
            { throw; }

            return result.Data.Max(c => c.OpenTime);
        }


        private void IsLocked(System.Net.HttpStatusCode? code)
        {
            if (code != null && code.Value == System.Net.HttpStatusCode.TooManyRequests)
                locked = true;
            if (locked)
                throw new BinanceTooManyRequestException("Too many requests to binance.com");
        }
    }
}
