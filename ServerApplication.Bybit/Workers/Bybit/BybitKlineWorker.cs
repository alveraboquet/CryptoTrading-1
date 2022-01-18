using DataLayer;
using ExchangeModels;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using ZeroMQ;

namespace ServerApplication.Bybit.Workers
{
    public class BybitKlineWorker : BackgroundService
    {
        private BybitKlineMessageQueue _klineQueue;
        private string exchange = ApplicationValues.BybitName;
        private IMemoryCache _cache;
        private BybitRedisSavingDataQueue _redisSavingQueue;
        private readonly ILog _logger;
        private BybitZeroMQKlineQueue _pubCandleQueue;

        private BybitClosedCandleQueue _closedCandleQueue;

        // queues for Api-Bybit-ZeroMQ
        private readonly ApiBybitZeroMqCandleQueue _bybitCandle;
        private readonly ApiBybitZeroMqFootprintQueue _bybitFootprint;

        public BybitKlineWorker(BybitKlineMessageQueue klineQueue, IMemoryCache cache,
            BybitZeroMQKlineQueue pubCandleQueue, BybitRedisSavingDataQueue redisQueue,
            BybitClosedCandleQueue closedCandleQueue, ApiBybitZeroMqFootprintQueue bybitFootprint,
            ApiBybitZeroMqCandleQueue bybitCandle)
        {
            _bybitCandle = bybitCandle;
            _bybitFootprint = bybitFootprint;

            _closedCandleQueue = closedCandleQueue;
            _redisSavingQueue = redisQueue;
            _pubCandleQueue = pubCandleQueue;
            _cache = cache;
            _klineQueue = klineQueue;
            _logger = LogManager.GetLogger(typeof(BybitKlineWorker));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_klineQueue.TryDequeue(out var kline) && !stoppingToken.IsCancellationRequested)
                    {
                        Candle candle = _cache.TryGetOpenCandle(this.exchange, kline.Symbol, kline.Params.KlineType);
                        if (candle == null) // it is the first message
                        {
                            try
                            {
                                candle = new Candle(kline.Candle.OpenPrice, kline.Candle.HighPrice, kline.Candle.LowPrice, kline.Candle.ClosePrice)
                                {
                                    Exchange = this.exchange,
                                    Symbol = kline.Symbol,
                                    TimeFrame = kline.Params.KlineType,
                                    OpenTime = kline.Candle.OpenTime,
                                    Volume = kline.Candle.Volume,
                                };
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e.ToString());
                                _logger.Error(e.StackTrace.ToString());
                            }

                            // initialize candle and footprint in cache
                            _cache.SetFootPrints(candle.Exchange, candle.Symbol, candle.TimeFrame, new FootPrints(candle.OpenPrice));
                            _cache.SetOpenCandle(candle.Exchange, candle.Symbol, candle.TimeFrame, candle);
                        }
                        else if (candle.OpenTime != kline.Candle.OpenTime) // candle closed
                        {
                            if (candle.Symbol.Equals("BTCUSDT")) // log just one symbol they close at the same time
                                _logger.Info($"{candle.Symbol} closed at {kline.Candle.OpenTime.UnixTimeStampToDateTime():hh:mm:ss} : {candle.TimeFrame}");

                            var closedCandle = candle.Clone();
                            var footprint = _cache.TryGetFootPrints(exchange, candle.Symbol, candle.TimeFrame);
                            closedCandle.FootPrint = footprint?.Clone();
                            CandleClosed(closedCandle);

                            // Initialize new candle
                            candle = new Candle(kline.Candle.OpenPrice,
                                kline.Candle.HighPrice,
                                kline.Candle.LowPrice,
                                kline.Candle.ClosePrice)
                            {
                                Volume = kline.Candle.Volume,
                                OpenTime = kline.Candle.OpenTime,
                                Exchange = candle.Exchange,
                                Symbol = candle.Symbol,
                                TimeFrame = candle.TimeFrame
                            };

                            footprint = new FootPrints(candle.OpenPrice);

                            _cache.SetOpenCandle(exchange, candle.Symbol, candle.TimeFrame, candle);
                            _cache.SetFootPrints(exchange, candle.Symbol, candle.TimeFrame, footprint);
                        }
                        else // update candle
                        {
                            candle.OpenPrice = kline.Candle.OpenPrice;
                            candle.HighPrice = kline.Candle.HighPrice;
                            candle.LowPrice = kline.Candle.LowPrice;
                            candle.ClosePrice = kline.Candle.ClosePrice;
                            candle.Volume = kline.Candle.Volume;
                        }

                        if (candle.TimeFrame.Equals("1m"))
                            _redisSavingQueue.EnqueueCandle(candle.Symbol);
                        _pubCandleQueue.Enqueue((ZeroMQ.OpenCandle)candle);
                    }

                    Thread.Sleep(1);
                }
            }).Start();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Enqueues closed candle to other threads
        /// </summary>
        private void CandleClosed(Candle candle)
        {
            #region enqueue candle and footprint for Api ZeroMQ binance
            _bybitCandle.Enqueue((ZeroMQ.OpenCandle)candle);

            if (candle.FootPrint is not null)
            {
                _bybitFootprint.Enqueue(new ZeroMQ.OpenFootprint()
                {
                    Timeframe = candle.TimeFrame,
                    Symbol = candle.Symbol,
                    OpenPrice = candle.OpenPrice,
                    OpenTime = candle.OpenTime,
                    AboveMarketOrders = candle.FootPrint.AboveMarketOrders,
                    BelowMarketOrders = candle.FootPrint.BelowMarketOrders
                });
            }
            #endregion

            _closedCandleQueue.Enqueue(candle);
        }
    }
}
