using Utilities;
using DataLayer;
using DatabaseRepository;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExchangeServices;
using Binance.Net.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Redis;
using ChainViewAPI.Models;

namespace ChainViewAPI.Controllers
{
    [Route("v1/api")]
    [ApiController]
    public class ChartsInfoController : ControllerBase
    {
        private readonly IBinanceCollectCandles _binanceCandleCollect;
        private readonly IPairStreamInfoRepository _pairStreamRepo;
        private readonly IBinanceFuturesUsdCollectCandles _binanceFuturesCandleCollect;
        private readonly ICandleService _candleRepository;
        private readonly IMemoryCache _cache;
        private readonly ICacheService _redis;
        private readonly IBinanceFuturesUsdtServices _BFUsdApi;
        public ChartsInfoController(ICandleService candle, IBinanceCollectCandles binanceCandleCollect,
            IMemoryCache cache, IPairStreamInfoRepository pairStreamRepo, ICacheService redis,
            IBinanceFuturesUsdCollectCandles binanceFuturesCandleCollect, IBinanceFuturesUsdtServices BFUsdApi)
        {
            _BFUsdApi = BFUsdApi;
            _redis = redis;
            _pairStreamRepo = pairStreamRepo;
            _cache = cache;
            _binanceCandleCollect = binanceCandleCollect;
            _candleRepository = candle;
            _binanceFuturesCandleCollect = binanceFuturesCandleCollect;
        }

        /// <param name="exchange" example="binance"></param>
        /// <param name="symbol" example="BTCUSDT">symbol name</param>
        /// <param name="timeframe" example="1m">the timeframe</param>
        /// <param name="start">start time unixtimestamp foramt milliseconds</param>
        /// <param name="end">end time unixtimestamp foramt milliseconds</param>
        /// <param name="last">number of candles you want</param>
        /// <response code="400">exchange/symbol/timeframe is invalid</response>
        /// <response code="400">start is bigger than end.</response>
        /// <response code="408">requested for data after ServerApplication stoped. returns the server stop time (int64)</response>
        /// <response code="200">returns the candles</response>
        /// <response code="429">Too many request to binance.com</response>
        [HttpGet("candle")]
        public async Task<IActionResult> Candle(
            [Required] string exchange,
            [Required] string symbol,
            [Required] string timeframe,
            long? start, long? end, int? last)
        {
            TimeSpan timeFrame_Time;
            PairInfo pairInfo;
            // validate ex.symbol.timeframe
            try
            {
                timeFrame_Time = timeframe.ToBinanceTimeFrame().ToTimeSpan();
                pairInfo = _cache.TryGetPairInfoList().FirstOrDefault(p =>
                        p.Symbol.Equals(symbol) &&
                        p.Exchange.Equals(exchange));

                if (pairInfo == default)
                    return BadRequest("symbol or exchange is invalid");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            // convert /last request to start/end
            if (last != null)
            {
                var now1 = DateTime.UtcNow - timeFrame_Time;

                end = now1.ToUnixTimestamp();
                var ts = timeFrame_Time * last.Value;
                start = (now1 - ts).ToUnixTimestamp();
            }
            else
            {
                var now = DateTime.UtcNow.ToUnixTimestamp();

                if (end == null)
                    end = now;
                else
                    end = Math.Min(end.Value, now);
            }

            if (start > end)
                return BadRequest("start is bigger than end.");

            if (_redis.GetServerApplicationStoped(out long? stopTime) && stopTime.Value < end)
                return StatusCode(408, stopTime);

            switch(exchange)
            {
                case ApplicationValues.BinanceName:
                    return await GetBinanceCandles(exchange, symbol, timeframe, start.Value, end.Value, pairInfo);

                case ApplicationValues.BinanceUsdName:
                    if (symbol.IsLiqFrOiSymbol())
                        return await GetBinanceFuturesUsdLiqFrOiCandle(exchange, symbol, timeframe, start.Value, end.Value, pairInfo);
                    else
                        return await GetBinanceFuturesUsdCandles(exchange, symbol, timeframe, start.Value, end.Value, pairInfo);

                default:
                    return BadRequest("Wrong exchange");
            }
        }

        #region Handle Candle Requests
        [NonAction]
        private async Task<IActionResult> GetBinanceCandles(
            string exchange, string symbol, string timeframe,
            long start, long end,
            PairInfo pairInfo)
        {
            var sorted = ChartCachingManager.GetSortedCandles(exchange, symbol, timeframe);
            if (!sorted.IsAllDataExtractedFromMongoDB) // get all data in MongoDB
            {
                var candles = await _candleRepository.GetAllCandlesAsync(exchange, symbol, timeframe);
                sorted.AddRange(candles.Select(c => new Models.ResCandle(c)));
                sorted.IsAllDataExtractedFromMongoDB = true;
            }

            try
            {
                var klineInterval = timeframe.ToBinanceTimeFrame();
                var candlesFromBinanceAPI = (await _binanceCandleCollect.CollectCandleIfDoesNotExist(
                                                        symbol, klineInterval,
                                                        start, end,
                                                        sorted.MinOpenTime,
                                                        pairInfo)).ToList();
                sorted.AddRange(candlesFromBinanceAPI.Select(c => new Models.ResCandle(c)));
            }
            catch (BinanceTooManyRequestException ex)
            {
                return StatusCode(429, ex.Message);
            }

            var res = sorted.GetRange(start, end);

            string json = CandleSortedSet.CreateJson(res);
            return Ok(json);
        }

        [NonAction]
        private async Task<IActionResult> GetBinanceFuturesUsdCandles(
                string exchange, string symbol, string timeframe,
                long start, long end,
                PairInfo pairInfo)
        {
            CandleSortedSet sorted = ChartCachingManager.GetSortedCandles(exchange, symbol, timeframe);
            if (!sorted.IsAllDataExtractedFromMongoDB) // get all data in MongoDB
            {
                var candles = await _candleRepository.GetAllCandlesAsync(exchange, symbol, timeframe);
                sorted.AddRange(candles.Select(c => new Models.ResCandle(c)));
                sorted.IsAllDataExtractedFromMongoDB = true;
            }

            try
            {
                var klineInterval = timeframe.ToBinanceTimeFrame();
                var candlesFromBinanceAPI = (await _binanceFuturesCandleCollect.CollectCandleIfDoesNotExist(
                                                        symbol, klineInterval,
                                                        start, end,
                                                        sorted.MinOpenTime,
                                                        pairInfo)).ToList();

                sorted.AddRange(candlesFromBinanceAPI.Select(c => new Models.ResCandle(c)));
            }
            catch (BinanceTooManyRequestException ex)
            {
                return StatusCode(429, ex.Message);
            }

            var res = sorted.GetRange(start, end);

            string json = CandleSortedSet.CreateJson(res);
            return Ok(json);
        }

        [NonAction]
        private async Task<IActionResult> GetBinanceFuturesUsdLiqFrOiCandle(
                string exchange, string symbol, string timeframe,
                long start, long end,
                PairInfo pairInfo)
        {
            if (pairInfo.IsOiPair() && start < DateTime.UtcNow.AddDays(-29).ToUnixTimestamp())
                return BadRequest("Only the data of the latest 29 days is available.");

            CandleSortedSet sorted = ChartCachingManager.GetSortedCandles(exchange, symbol, timeframe);
            if (!sorted.IsAllDataExtractedFromMongoDB)
            {
                // get all data in MongoDB and save in cache
                IEnumerable<DataLayer.ResCandle> candles = new List<DataLayer.ResCandle>();
                switch (symbol.GetCustomePairType())
                {
                    case DataLayer.Models.CustomPairType.Fr:
                        candles = await _candleRepository.GetAllFrCandlesAsync(exchange, symbol, timeframe);
                        break;

                    case DataLayer.Models.CustomPairType.Liq:
                        candles = await _candleRepository.GetAllLiqCandlesAsync(exchange, symbol, timeframe);
                        break;

                    case DataLayer.Models.CustomPairType.LiqBuy:
                        candles = await _candleRepository.GetAllLiqBuyCandlesAsync(exchange, symbol, timeframe);
                        break;

                    case DataLayer.Models.CustomPairType.LiqSell:
                        candles = await _candleRepository.GetAllLiqSellCandlesAsync(exchange, symbol, timeframe);
                        break;
                }

                sorted.AddRange(candles.Select(c => new Models.ResCandle(c)));
                sorted.IsAllDataExtractedFromMongoDB = true;
            }

            try
            {
                if (pairInfo.IsOiPair())
                {
                    PeriodInterval period = timeframe.ToBinancePeriodInterval();

                    var candles = (await _binanceFuturesCandleCollect.CollectOpenInterestCandlesIfDoesNotExist(
                                                            pairInfo.GetSymbol(), period,
                                                            start, end,
                                                            pairInfo,
                                                            sorted.MinOpenTime, sorted.MaxOpenTime,
                                                            sorted.AddRange)).ToList();

                    sorted.AddRange(candles.Select(c => new Models.ResCandle(c)));
                }
            }
            catch (BinanceTooManyRequestException ex)
            {
                return StatusCode(429, ex.Message);
            }

            var res = sorted.GetRange(start, end);

            string json = CandleSortedSet.CreateJson(res);
            return Ok(json);
        }
        #endregion



        /// <param name="exchange" example="binance"></param>
        /// <param name="symbol" example="BTCUSDT">symbol name</param>
        /// <param name="timeframe" example="1m">the timeframe string format</param>
        /// <param name="start">start time unixtimestamp foramt milliseconds</param>
        /// <param name="end">end time unixtimestamp foramt milliseconds</param>
        /// <response code="400">exchange/symbol/timeframe is invalid</response>
        /// <response code="400">start is bigger than end.</response>
        /// <response code="400">requested for data after ServerApplication stoped. returns the server stop time (int64)</response>
        /// <response code="200">returns the footprints</response>
        [HttpGet("footprint")]
        public async Task<IActionResult> Footprint(
            [Required] string exchange,
            [Required] string symbol,
            [Required] string timeframe,
            [Required] long start, long? end)
        {
            try
            {
                timeframe.ToBinanceTimeFrame();
                ApplicationValues.IsValidExchange(exchange);
                if (!_cache.TryGetPairInfoList().Any(p => p.Symbol == symbol))
                    return BadRequest("symbol is invalid");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var now = DateTime.UtcNow.ToUnixTimestamp();
            if (end == null) end = now;
            else end = Math.Min(end.Value, now);

            if (start > end)
                return BadRequest("start is bigger than end.");

            if (_redis.GetServerApplicationStoped(out long? stopTime) && stopTime.Value < end)
                return BadRequest(stopTime);

            try
            {
                var sorted = ChartCachingManager.GetSortedFootprints(exchange, symbol, timeframe);
                if (!sorted.IsAllDataExtractedFromMongoDB) // get all data in MongoDB
                {
                    var footprints = await _candleRepository.GetAllFootprintAsync(exchange, symbol, timeframe);
                    sorted.AddRange(footprints.Select(f => new Models.ResFootprint(f)));
                    sorted.IsAllDataExtractedFromMongoDB = true;
                }
                var res = sorted.GetRange(start, end.Value);

                string json = FootprintSortedSet.CreateJson(res);
                return Ok(json);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        /// <param name="exchange" example="binance"></param>
        /// <param name="symbol" example="BTCUSDT">symbol name</param>
        /// <param name="timeframe" example="1m">the timeframe string format</param>
        /// <param name="start">start time unixtimestamp foramt milliseconds</param>
        /// <param name="end">end time unixtimestamp foramt milliseconds</param>
        /// <param name="mode" example="0">mode of heatmap</param>
        /// <response code="400">exchange/symbol/timeframe is invalid</response>
        /// <response code="400">start is bigger than end.</response>
        /// <response code="400">requested for data after ServerApplication stoped. returns the server stop time (int64)</response>
        /// <response code="200">returns heatmaps</response>
        [HttpGet("heatmap")]
        public async Task<IActionResult> Heatmap(
            [Required] string exchange,
            [Required] string symbol,
            [Required] string timeframe,
            [Required] Mode mode,
            [Required] long start, long? end)
        {
            try
            {
                timeframe.ToBinanceTimeFrame();
                ApplicationValues.IsValidExchange(exchange);
                if (!_cache.TryGetPairInfoList().Any(p => p.Symbol == symbol))
                    return BadRequest("symbol is invalid");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var now = DateTime.UtcNow.ToUnixTimestamp();

            if (end == null) end = now;
            else end = Math.Min(end.Value, now);

            if (start > end)
                return BadRequest("start is bigger than end.");


            if (_redis.GetServerApplicationStoped(out long? stopTime) && stopTime.Value < end)
                return BadRequest(stopTime);

            var sorted = ChartCachingManager.GetSortedHeatmap(exchange, symbol, timeframe);
            if (!sorted.IsAllDataExtractedFromMongoDB)
            {
                var heatmaps = await _candleRepository.GetAllHeatmapAsync(exchange, symbol, timeframe);
                sorted.AddRange(heatmaps.Select(h => new Models.ResHeatmap(h)));
                sorted.IsAllDataExtractedFromMongoDB = true;
            }
            var res = sorted.GetRange(start, end.Value);

            string json = HeatmapSortedSet.CreateJson(res, mode);

            return Ok(json);
        }
    }
}