using System.Threading;
using System.Threading.Tasks;
using DataLayer;
using DataLayer.Models.Stream;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Redis;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues;
using Utilities;
using ZeroMQ.Publishers.Bybit;

namespace ServerApplication.Bybit.Workers
{
    public class BybitRedisWorker : BackgroundService
    {
        private readonly ILog _logger;
        private ICacheService _redis;
        private BybitPublisher _publisher;
        private BybitZeroMQTradeQueue _tradeQueue;
        private BybitZeroMQKlineQueue _candleQueue;
        private BybitZeroMQDepthQueue _depthQueue;
        private BybitRedisSavingDataQueue _redisQueue;
        private IMemoryCache _cache;
        private readonly string[] _timeFrames = {
            "1m", "5m", "15m", "30m",
            "1H", "2H", "4H", "6H", "12H",
            "1D"
        };
        const string Exchange = ApplicationValues.BybitName;

        public BybitRedisWorker(ICacheService redis, BybitPublisher publisher,
            BybitZeroMQTradeQueue tradeQueue, BybitZeroMQKlineQueue candleQueue, 
            BybitZeroMQDepthQueue depthQueue,
            BybitRedisSavingDataQueue redisQueue, IMemoryCache cache)
        {
            _logger = LogManager.GetLogger(typeof(BybitRedisWorker));
            _redis = redis;
            _publisher = publisher;
            _tradeQueue = tradeQueue;
            _candleQueue = candleQueue;
            _depthQueue = depthQueue;
            _redisQueue = redisQueue;
            _cache = cache;
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
                string symbol, timeframe, element;
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
                            case "o":
                                symbol = info[1];
                                orderbook = _cache.TryGetOrderBook(Exchange, symbol);
                                if (orderbook != null)
                                    await _redis.SetOrderBookAsync(Exchange, orderbook.Symbol, orderbook);
                                break;

                            case "f":
                                symbol = info[1];
                                for (int i = 0; i < _timeFrames.Length; i++)
                                {
                                    timeframe = _timeFrames[i];
                                    footprint = _cache.TryGetFootPrints(Exchange, symbol, timeframe);
                                    if (footprint != null)
                                        await _redis.SetFootPrintsAsync(Exchange, symbol, timeframe, footprint);
                                }
                                break;

                            case "c":
                                symbol = info[1];
                                for (int i = 0; i < _timeFrames.Length; i++)
                                {
                                    timeframe = _timeFrames[i];
                                    candle = _cache.TryGetOpenCandle(Exchange, symbol, timeframe);
                                    if (candle != null)
                                        await _redis.SetOpenCandleAsync(candle);
                                }
                                break;
                        }
                    }

                    Thread.Sleep(1);
                }
            }).Start();
            
            // publish candle thread
            new Thread(() =>
            {
                // previous candle
                ZeroMQ.OpenCandle res;
                DataLayer.Candle candle;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_candleQueue.TryDequeue(out res) && !stoppingToken.IsCancellationRequested)
                    {
                        candle = _cache.TryGetOpenCandle(Exchange, res.Symbol, res.Timeframe);
                        if (candle != null)
                            _publisher.PublishCandle(candle);
                    }

                    Thread.Sleep(1);
                }
            }).Start();
            
            // publish trade thread
            new Thread(() =>
            {
                ZeroMQ.Trade trade;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_tradeQueue.TryDequeue(out trade) && !stoppingToken.IsCancellationRequested)
                    {
                        if (trade != null)
                            _publisher.PublishTrade(trade);
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
                    }

                    Thread.Sleep(1);
                }

            }).Start();
            
            return Task.CompletedTask;
        }
    }
}