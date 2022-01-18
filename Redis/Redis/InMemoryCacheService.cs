using DataLayer;
using DataLayer.Models.Stream;
using ExchangeModels.BinanceFutures;
using Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Utilities;

namespace Redis
{
    public class InMemoryCacheService : ICacheService
    {
        private const string isServerOFF = "isServerOFF";
        private const string orderBookFormat = "orderBook.{0}.{1}"; //  ex.symbol
        private const string openCandleFormat = "openCandle.{0}.{1}.{2}"; //  ex.symbol.timeFrame
        private const string footprintFormat = "footprint.{0}.{1}.{2}"; //  ex.symbol.timeframe

        private const string allFundingRateFormat = "allFundingRate.{0}"; //  ex

        private ConnectionMultiplexer _connection;
        private IDatabase db;
        private IDatabase _orderbookDb;
        public InMemoryCacheService(ConnectionFactory conn)
        {
            _connection = conn.GetRedisCache();
            var orderbookCon = conn.GetRedisOrderbookCache();

            db = _connection.GetDatabase();
            _orderbookDb = orderbookCon.GetDatabase(1);
        }

        #region Funding Rate
        private static string GetAllFundingRateCacheKey(string exchange)
        {
            exchange = exchange.ToLower();
            return string.Format(allFundingRateFormat, exchange);
        }

        public Task<bool> SetAllFundingRateAsync(string exchange, byte[] fundingRateJson)
        {
            return db.StringSetAsync(GetAllFundingRateCacheKey(exchange), Encoding.ASCII.GetString(fundingRateJson));
        }

        public async Task<List<FundingRateUpdate>> GetAllFundingRateAsync(string exchange)
        {
            string value = await db.StringGetAsync(GetAllFundingRateCacheKey(exchange));

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return BinanceConverter.DeserializeBinanceFuturesUsdFundingRate(Encoding.ASCII.GetBytes(value));
        }

        #endregion

        #region Open Candle

        private static string GetOpenCandleCacheKey(string exchange, string symbol, string timeFrame)
        {
            exchange = exchange.ToLower();
            symbol = symbol.ToLower();
            timeFrame = timeFrame.ToLower();
            return string.Format(openCandleFormat, exchange, symbol, timeFrame);
        }
        public async Task<Redis.OpenCandle> TryGetOpenCandle(string exchange, string symbol, string timeFrame)
        {
            OpenCandle candle = null;
            string value = await db.StringGetAsync(GetOpenCandleCacheKey(exchange, symbol, timeFrame));
            if (string.IsNullOrWhiteSpace(value)) return candle;
            try
            {
                candle = JsonSerializer.Deserialize<OpenCandle>(value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return candle;
            }
            return candle;
        }
        public Task<bool> SetOpenCandleAsync(Redis.OpenCandle openCandle)
        {
            string value = JsonSerializer.Serialize(openCandle);
            return db.StringSetAsync(GetOpenCandleCacheKey(openCandle.Exchange, openCandle.Symbol, openCandle.Timeframe), value);
        }

        public Task<bool> SetOpenCandleAsync(DataLayer.Candle candle)
        {
            var openCandle = new OpenCandle()
            {
                OpenTime = candle.OpenTime,
                Open = candle.OpenPrice,
                High = candle.HighPrice,
                Low = candle.LowPrice,
                Close = candle.ClosePrice,
                Volume = candle.Volume,
                Exchange = candle.Exchange,
                Symbol = candle.Symbol,
                Timeframe = candle.TimeFrame,
            };

            return this.SetOpenCandleAsync(openCandle);
        }

        public bool SetOpenCandle(OpenCandle openCandle)
        {
            string value = JsonSerializer.Serialize(openCandle);
            return db.StringSet(GetOpenCandleCacheKey(openCandle.Exchange, openCandle.Symbol, openCandle.Timeframe), value);
        }

        public bool SetOpenCandle(Candle candle)
        {
            var openCandle = new OpenCandle()
            {
                OpenTime = candle.OpenTime,
                Open = candle.OpenPrice,
                High = candle.HighPrice,
                Low = candle.LowPrice,
                Close = candle.ClosePrice,
                Volume = candle.Volume,
                Exchange = candle.Exchange,
                Symbol = candle.Symbol,
                Timeframe = candle.TimeFrame,
            };

            return this.SetOpenCandle(openCandle);
        }

        #endregion

        #region Footprint
        public static string GetFootprintsCacheKey(string exchange, string symbol, string timeframe)
        {
            exchange = exchange.ToLower();
            symbol = symbol.ToLower();
            timeframe = timeframe.ToLower();
            return string.Format(footprintFormat, exchange, symbol, timeframe);
        }

        public async Task<bool> SetFootPrintsAsync(string exchange, string symbol, string timeframe, FootPrints footPrints)
        {
            string value = JsonSerializer.Serialize(footPrints);
            return await db.StringSetAsync(GetFootprintsCacheKey(exchange, symbol, timeframe), value);
        }
        public bool SetFootPrints(string exchange, string symbol, string timeframe, FootPrints footPrints)
        {
            string value = JsonSerializer.Serialize(footPrints);
            return db.StringSet(GetFootprintsCacheKey(exchange, symbol, timeframe), value);
        }
        public async Task<FootPrints> TryGetFootPrints(string exchange, string symbol, string timeframe)
        {
            FootPrints footPrints = null;

            string value = await db.StringGetAsync(GetFootprintsCacheKey(exchange, symbol, timeframe));
            if (string.IsNullOrWhiteSpace(value)) return footPrints;

            footPrints = JsonSerializer.Deserialize<FootPrints>(value);
            return footPrints;
        }
        #endregion

        #region Order Book

        private static string GetOrderBookCacheKey(string exchange, string symbol)
        {
            exchange = exchange.ToLower();
            symbol = symbol.ToLower();
            return string.Format(orderBookFormat, exchange, symbol);
        }

        public async Task<StreamingOrderBook> TryGetOrderBook(string exchange, string symbol)
        {
            StreamingOrderBook orderBook = null;

            string value = await _orderbookDb.StringGetAsync(GetOrderBookCacheKey(exchange, symbol));
            if (string.IsNullOrWhiteSpace(value)) return orderBook;
            orderBook = JsonSerializer.Deserialize<StreamingOrderBook>(value);
            return orderBook;
        }
        public Task<bool> SetOrderBookAsync(string exchange, string symbol, StreamingOrderBook orderBook)
        {
            string value = JsonSerializer.Serialize(orderBook);
            return _orderbookDb.StringSetAsync(GetOrderBookCacheKey(exchange, symbol), value);
        }
        #endregion

        public bool GetServerApplicationStoped(out long? stopTime)
        {
            stopTime = null;
            string res = db.StringGet(isServerOFF);
            if (string.IsNullOrWhiteSpace(res))
                return false;
            var values = res.Split(':');
            stopTime = long.Parse(values[1]);
            return bool.Parse(values[0]);
        }

        public Task<bool> SetServerApplicationStoped(bool status, long stopTime)
        {
            return db.StringSetAsync(isServerOFF, $"{status}:{stopTime}");
        }
    }
}
