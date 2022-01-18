using System;
using System.Threading;
using System.Threading.Tasks;
using DataLayer;
using DataLayer.Models.Stream;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Redis;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues.BybitFutures;
using Utilities;
using ZeroMQ.Publishers.BybitFutures;

namespace ServerApplication.Bybit.Workers.BybitFutures
{
    public class BybitFuturesRedisWorker : BackgroundService
    {
        private IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly ICacheService _redis;
        private string _exchange = ApplicationValues.BybitName;
        private readonly BybitFuturesPublisher _publisher;
        // Queues
        private readonly BybitFuturesZeroMqCandleQueue _candleQueue;
        private readonly BybitFuturesZeroMqTradeQueue _tradeQueue;
        private readonly BybitFuturesZeroMqDepthQueue _depthQueue;
        private readonly BybitFuturesRedisSavingDataQueue _redisQueue;
        
        private readonly string[] _timeframes = {
            "1m", "5m", "15m", "30m",
            "1H", "2H", "4H", "6H",
            "1D"
        };

        public BybitFuturesRedisWorker(IMemoryCache cache, ICacheService redis,
            BybitFuturesZeroMqCandleQueue candleQueue, BybitFuturesZeroMqTradeQueue tradeQueue,
            BybitFuturesPublisher publisher, BybitFuturesZeroMqDepthQueue depthQueue, 
            BybitFuturesRedisSavingDataQueue redisQueue)
        {
            _cache = cache;
            _candleQueue = candleQueue;
            _tradeQueue = tradeQueue;
            _publisher = publisher;
            _depthQueue = depthQueue;
            _redisQueue = redisQueue;
            _redis = redis;
            _logger = LogManager.GetLogger(typeof(BybitFuturesRedisWorker));
        }
        
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info("START");
            return base.StartAsync(cancellationToken);
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("STOP");
            return base.StopAsync(cancellationToken);
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // saving thread
            new Thread(async () =>
            {
                string exchange, symbol, timeframe, element;
                string[] info;
                StreamingOrderBook orderbook;
                FootPrints footprint;
                DataLayer.Candle candle;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_redisQueue.TryDequeue(out element) && !stoppingToken.IsCancellationRequested)
                    {
                        info = element.Split(':');
                        switch (info[0])
                        {
                            // orderbook
                            case "o":
                                (exchange, symbol) = (info[1], info[2]);
                                orderbook = _cache.TryGetOrderBook(exchange, symbol);
                                if (orderbook != null)
                                    await _redis.SetOrderBookAsync(exchange, orderbook.Symbol, orderbook);
                                else
                                    _logger.Info($"orderbook in cache is null.");
                                break;

                            // footprint
                            case "f":
                                (exchange, symbol) = (info[1], info[2]);
                                for (int i = 0; i < _timeframes.Length; i++)
                                {
                                    timeframe = _timeframes[i];
                                    footprint = _cache.TryGetFootPrints(exchange, symbol, timeframe);
                                    if (footprint != null)
                                        await _redis.SetFootPrintsAsync(exchange, symbol, timeframe, footprint);
                                }
                                break;

                            // candle
                            case "c":
                                (exchange, symbol) = (info[1], info[2]);
                                for (int i = 0; i < _timeframes.Length; i++)
                                {
                                    timeframe = _timeframes[i];
                                    candle = _cache.TryGetOpenCandle(exchange, symbol, timeframe);
                                    if (candle != null)
                                        await _redis.SetOpenCandleAsync(candle);
                                }
                                break;
                        }
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            }).Start();
            
            // publish candle thread
            new Thread(() =>
            {
                // previous candle
                (string Symbol, string TimeFrame) res;
                DataLayer.Candle candle;
                string lastUpdate = "";
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_candleQueue.TryDequeue(out res) && !stoppingToken.IsCancellationRequested)
                    {
                        candle = _cache.TryGetOpenCandle(_exchange, res.Symbol, res.TimeFrame);
                        if (candle != null)
                        {
                            string update = $"{candle.Volume}{candle.ClosePrice}";
                            if (!lastUpdate.Equals(update))
                            {
                                lastUpdate = update;
                                _publisher.PublishCandle(candle);
                            }
                        }
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            }).Start();
            
            // publish trade thread
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_tradeQueue.TryDequeue(out ZeroMQ.Trade trade) && !stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            _publisher.PublishTrade(trade);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex.Message, ex);
                        }
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            }).Start();
            
            // publish depth thread
            new Thread(() =>
            {
                ZeroMQ.OrderBook orderBook;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_depthQueue.TryDequeue(out orderBook) && !stoppingToken.IsCancellationRequested)
                    {
                        if (orderBook != null)
                            _publisher.PublishOrderbook(orderBook);
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            }).Start();
            
            return Task.CompletedTask;
        }
    }
}