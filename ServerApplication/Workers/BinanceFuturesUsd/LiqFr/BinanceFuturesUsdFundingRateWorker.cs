using ExchangeModels.BinanceFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Redis;
using ServerApplication.Caching;
using ServerApplication.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ServerApplication.Workers
{
    public class BinanceFuturesUsdFundingRateWorker : BackgroundService
    {
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly BinanceFuturesUsdFundingRateCalculateQueue _fundingRates;
        private readonly BinanceFuturesUsdZeroMqFrCandleQueue _frCandleZeroMq;
        private readonly ICacheService _redisCache;
        private const string Exchange = ApplicationValues.BinanceUsdName;

        private readonly string[] _TimeFrames = {
            "1m", "15m",
            "4H",
            "1D"
        };

        public BinanceFuturesUsdFundingRateWorker(IMemoryCache cache, ICacheService redisCache,
            BinanceFuturesUsdFundingRateCalculateQueue fundingRates,
            BinanceFuturesUsdZeroMqFrCandleQueue frCandleZeroMq)
        {
            _redisCache = redisCache;
            _frCandleZeroMq = frCandleZeroMq;
            _cache = cache;
            this._fundingRates = fundingRates;

            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdFundingRateWorker));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _fundingRates.TryDequeue(out byte[] json))
                    {
                        _redisCache.SetAllFundingRateAsync(Exchange, json);
                        List<FundingRateUpdate> fr = BinanceConverter.DeserializeBinanceFuturesUsdFundingRate(json);
                        StreamFundingRate(fr);
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            return Task.CompletedTask;
        }

        private void StreamFundingRate(List<FundingRateUpdate> frUpdates)
        {
            foreach (var fr in frUpdates)
            {
                foreach (var tf in _TimeFrames)
                {
                    var candle = _cache.TryGetOpenCandle(Exchange, fr.Symbol, tf);
                    if (candle == null || candle.GetCloseTime() < fr.EventTime)
                        continue;

                    if (candle.FundingRate == null)
                        candle.FundingRate = new DataLayer.FundingRate(fr.Rate);

                    // Replace if high updated
                    if (candle.FundingRate.High < fr.Rate)
                        candle.FundingRate.High = fr.Rate;

                    // Replace if low updated
                    if (candle.FundingRate.Low > fr.Rate)
                        candle.FundingRate.Low = fr.Rate;

                    candle.FundingRate.Close = fr.Rate;

                    EnqueueFrCandle(candle);
                }
            }
        }

        /// <summary>
        /// Enqueue to publish in websocket for FR.<symbol>
        /// </summary>
        private void EnqueueFrCandle(DataLayer.Candle candle)
        {
            _frCandleZeroMq.Enqueue(new ZeroMQ.OpenCandle()
            {
                OpenTime = candle.OpenTime,
                Symbol = $"FR.{candle.Symbol}",
                Timeframe = candle.TimeFrame,

                Open = candle.FundingRate.Open,
                High = candle.FundingRate.High,
                Low = candle.FundingRate.Low,
                Close = candle.FundingRate.Close,
                
                Volume = 0,
            });
        }
    }
}