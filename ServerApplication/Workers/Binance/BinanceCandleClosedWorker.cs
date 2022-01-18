using DatabaseRepository;
using DataLayer;
using DataLayer.Models.Stream;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Redis;
using ServerApplication.Caching;
using ServerApplication.Queues;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using ZeroMQ;

namespace ServerApplication.Workers
{
    public class BinanceCandleClosedWorker : BackgroundService
    {
        private BinanceMongoDbCandleQueue _mongoQueue;
        private BinanceCandleClosedQueue _heatmapQueue;
        private ICandleService _candleRepo;
        private readonly ILog _logger;
        private BinanceRedisSavingDataQueue _redisQueue;
        private IMemoryCache _cache;
        private ICacheService _redis;


        // queues for Api-Binance-ZeroMq
        private readonly ApiBinanceZeroMqHeatmapQueue _binanceHeatmap;

        public BinanceCandleClosedWorker(ICandleService candleRepo, BinanceMongoDbCandleQueue mongoQueue,
            BinanceRedisSavingDataQueue redisQueue, IMemoryCache cache, BinanceCandleClosedQueue heatmapQueue,
            ICacheService redis, ApiBinanceZeroMqHeatmapQueue binanceHeatmap)
        {
            _binanceHeatmap = binanceHeatmap;

            _redis = redis;
            _cache = cache;
            _redisQueue  = redisQueue;
            _mongoQueue = mongoQueue;
            _candleRepo  = candleRepo;
            _heatmapQueue = heatmapQueue;
            _logger = LogManager.GetLogger(typeof(BinanceCandleClosedWorker));
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await _redis.SetServerApplicationStoped(false, DateTime.UtcNow.ToUnixTimestamp());
            _logger.Info("START");
            await base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("STOP");
            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool isHeatmapEmpty = true;
            bool isMongoDBEmpty = true;
            // heatmap thread
            new Thread(() =>
            {
                DataLayer.Candle candle;
                StreamingOrderBook cOrderbook, orderBook;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_heatmapQueue.TryDequeue(out candle))
                    {
                        isHeatmapEmpty = isMongoDBEmpty = false;
                        orderBook = _cache.TryGetOrderBook(candle.Exchange, candle.Symbol);

                        if (orderBook != null)
                        {
                            cOrderbook = orderBook.Clone();
                            candle.Heatmap8K = new Heatmap(Mode.EightK, candle.OpenPrice);
                            candle.Heatmap8K = CalculateHeatmap(cOrderbook, candle.Heatmap8K);

                            // enqueue heatmap if exist
                            _binanceHeatmap.Enqueue(new OpenHeatmap()
                            {
                                Timeframe = candle.TimeFrame,
                                Symbol = candle.Symbol,
                                OpenPrice = candle.OpenPrice,
                                OpenTime = candle.OpenTime,
                                Blocks = candle.Heatmap8K.Blocks
                            });
                        }
                        else
                            candle.Heatmap8K = null;

                        _mongoQueue.Enqueue(candle);
                    }
                    isHeatmapEmpty = true;
                    Thread.Sleep(1);
                }
                isHeatmapEmpty = true;
            }).Start();
            // heatmap thread
            new Thread(() =>
            {
                DataLayer.Candle candle;
                StreamingOrderBook cOrderbook, orderBook;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_heatmapQueue.TryDequeue(out candle))
                    {
                        isHeatmapEmpty = isMongoDBEmpty = false;
                        orderBook = _cache.TryGetOrderBook(candle.Exchange, candle.Symbol);

                        if (orderBook != null)
                        {
                            cOrderbook = orderBook.Clone();
                            candle.Heatmap8K = new Heatmap(Mode.EightK, candle.OpenPrice);
                            candle.Heatmap8K = CalculateHeatmap(cOrderbook, candle.Heatmap8K);

                            // enqueue heatmap if exist
                            _binanceHeatmap.Enqueue(new OpenHeatmap()
                            {
                                Timeframe = candle.TimeFrame,
                                Symbol = candle.Symbol,
                                OpenPrice = candle.OpenPrice,
                                OpenTime = candle.OpenTime,
                                Blocks = candle.Heatmap8K.Blocks
                            });
                        }
                        else
                            candle.Heatmap8K = null;

                        _mongoQueue.Enqueue(candle);
                    }
                    isHeatmapEmpty = true;
                    Thread.Sleep(1);
                }
                isHeatmapEmpty = true;
            }).Start();

            // mongodb thread
            new Thread(async () =>
            {
                int counter = 0;
                DataLayer.Candle candle;
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (counter >= 1000)
                    {
                        counter = 0;
                        _redisQueue.Clear(); // clearing redis queue, because redis is slow for this, and queue gets bigger
                    }
                    counter++;

                    while (_mongoQueue.TryDequeue(out candle))
                    {
                        isMongoDBEmpty = false;
                        await _candleRepo.CreateOrUpdateByOpenTimeAsync(candle);
                    }
                    isMongoDBEmpty = true;
                    Thread.Sleep(500);
                }
                isMongoDBEmpty = true;
            }).Start();

            
            AppDomain.CurrentDomain.UnhandledException += async (sender, ev) =>
            {
                // set is server off or not, and its time
                await _redis.SetServerApplicationStoped(true, DateTime.UtcNow.ToUnixTimestamp());
                _logger.Error(ev.ExceptionObject.GetType(), (Exception) ev.ExceptionObject);
                _logger.Error("An unhandled exception occurred. Waiting for heatmap and mongodb to finish.");
                while (!isHeatmapEmpty || !isMongoDBEmpty)
                {
                    if (!isHeatmapEmpty)
                    {
                        Console.WriteLine("heatmap is not done yet");
                    }
                    else
                    {
                        Console.WriteLine("MongoDB is not done yet");
                    }
                }
                _logger.Info("Done.");
                Console.WriteLine("==============================================================================");
            };
            AppDomain.CurrentDomain.ProcessExit += async (s, e) =>
            {
                // set is server off or not, and its time, needed in API
                await _redis.SetServerApplicationStoped(true, DateTime.UtcNow.ToUnixTimestamp());
            };

            return Task.CompletedTask;
        }

        protected DataLayer.Heatmap CalculateHeatmap(StreamingOrderBook orderBook, Heatmap heatmap)
        {
            List<KeyValuePair<decimal, decimal>> bids = orderBook.Bids.ToList();

            List<KeyValuePair<decimal, decimal>> asks = orderBook.Asks.ToList();

            decimal step = heatmap.Range;
            decimal minRange = 0;
            decimal maxRange = step;

            for (int i = 0; i < heatmap.Blocks.Count; i++)
            {
                heatmap.Blocks[i] = 0;
                foreach (var order in bids)
                {
                    if (minRange <= order.Key && order.Key < maxRange)
                    {
                        heatmap.Blocks[i] += order.Value;
                    }
                }

                foreach (var order in asks)
                {
                    if (minRange <= order.Key && order.Key < maxRange)
                    {
                        heatmap.Blocks[i] += order.Value;
                    }
                }
                minRange = maxRange;
                maxRange += step;
            }

            return heatmap;
        }
    }
}