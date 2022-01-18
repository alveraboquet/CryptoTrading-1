using DataLayer;
using ExchangeModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Caching;
using ServerApplication.Queues;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ServerApplication.Workers
{
    public class BinanceFuturesUsdKlineWorker : BackgroundService
    {
        private IMemoryCache _cache;
        private BinanceFuturesUsdKlineCalculate _klineQueue;
        private string exchange = ApplicationValues.BinanceUsdName;
        private BinanceFuturesUsdZeroMqCandleQueue _pubCandleQueue;
        private BinanceFuturesUsdRedisSavingDataQueue _redisSavingQueue;

        public BinanceFuturesUsdKlineWorker(IMemoryCache cache, BinanceFuturesUsdKlineCalculate klineQueue,
            BinanceFuturesUsdZeroMqCandleQueue pubCandleQueue, BinanceFuturesUsdRedisSavingDataQueue redisQueue)
        {
            _redisSavingQueue = redisQueue;
            _pubCandleQueue = pubCandleQueue;
            _cache = cache;
            _klineQueue = klineQueue;
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_klineQueue.TryDequeue(out byte[] json) && !stoppingToken.IsCancellationRequested)
                    {
                        SKline kline = BinanceConverter.DeserializeBinanceFuturesUsdKline(json);
                        Candle candle = _cache.TryGetOpenCandle(this.exchange, kline.Symbol, kline.Candle.Interval);
                        bool haveChanged = false;
                        if (candle == null)
                        {
                            candle = new Candle(kline.Candle.OpenPrice, kline.Candle.HighPrice, kline.Candle.LowPrice, kline.Candle.ClosePrice)
                            {
                                Exchange = this.exchange,
                                Symbol = kline.Symbol,
                                TimeFrame = kline.Candle.Interval,
                                OpenTime = kline.Candle.OpenTime,
                                Volume = kline.Candle.Volume,
                            };
                            _cache.SetOpenCandle(candle.Exchange, candle.Symbol, candle.TimeFrame, candle);
                            haveChanged = true;
                        }
                        else if(candle.OpenTime == kline.Candle.OpenTime)
                        {
                            if (candle.OpenPrice != kline.Candle.OpenPrice)
                            {
                                candle.OpenPrice = kline.Candle.OpenPrice;
                                haveChanged = true;
                            }
                            if (candle.HighPrice != kline.Candle.HighPrice)
                            {
                                candle.HighPrice = kline.Candle.HighPrice;
                                haveChanged = true;
                            }
                            if (candle.LowPrice != kline.Candle.LowPrice)
                            {
                                candle.LowPrice = kline.Candle.LowPrice;
                                haveChanged = true;
                            }
                            if (candle.Volume != kline.Candle.Volume)
                            {
                                candle.Volume = kline.Candle.Volume;
                                haveChanged = true;
                            }
                            if (candle.ClosePrice != kline.Candle.ClosePrice)
                            {
                                candle.ClosePrice = kline.Candle.ClosePrice;
                                haveChanged = true;
                            }
                        }

                        if (haveChanged)
                        {
                            if (candle.TimeFrame.Equals("1m"))
                                _redisSavingQueue.EnqueueCandle(exchange, candle.Symbol);
                            _pubCandleQueue.EnqueueCandle(candle);
                        }
                    }
                }
            }).Start();

            return Task.CompletedTask;
        }
    }
}
