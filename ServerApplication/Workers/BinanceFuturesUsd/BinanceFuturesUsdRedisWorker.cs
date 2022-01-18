using DataLayer.Models.Stream;
using log4net;
using Microsoft.Extensions.Hosting;
using Redis;
using ServerApplication.Queues;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using System.Diagnostics;
using DataLayer;
using ZeroMQ;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Caching;

namespace ServerApplication.Workers
{
    public class BinanceFuturesUsdRedisWorker : BackgroundService
    {
        private readonly ILog _logger;
        private readonly ICacheService _redis;
        private readonly BinanceFuturesUsdPublisher _publisher;
        private readonly BinanceFuturesUsdZeroMqTradeQueue _trade;
        private readonly BinanceFuturesUsdZeroMqCandleQueue _candle;
        private readonly BinanceFuturesUsdZeroMqDepthQueue _depth;
        private readonly BinanceFuturesUsdRedisSavingDataQueue _redisQueue;
        private readonly IMemoryCache _cache;
        private const string Exchange = ApplicationValues.BinanceUsdName;

        private readonly string[] _TimeFrames = {
            "1m", "5m", "15m",
            "1H", "4H",
            "1D", "3D"
        };

        public BinanceFuturesUsdRedisWorker(IMemoryCache cache, BinanceFuturesUsdRedisSavingDataQueue redisQueue,
            ZeroMQ.BinanceFuturesUsdPublisher publisher, ICacheService redisCache,
            BinanceFuturesUsdZeroMqTradeQueue trade, BinanceFuturesUsdZeroMqDepthQueue depth,
            BinanceFuturesUsdZeroMqCandleQueue candle)
        {
            _cache = cache;
            _redis = redisCache;

            _candle = candle;
            _trade = trade;
            _depth = depth;
            _publisher = publisher;
            _redisQueue = redisQueue;
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdRedisWorker));
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
                            case "o":
                                (exchange, symbol) = (info[1], info[2]);
                                orderbook = _cache.TryGetOrderBook(exchange, symbol);
                                if (orderbook != null)
                                    await _redis.SetOrderBookAsync(exchange, orderbook.Symbol, orderbook);
                                break;

                            case "f":
                                (exchange, symbol) = (info[1], info[2]);
                                for (int i = 0; i < 7; i++)
                                {
                                    timeframe = _TimeFrames[i];
                                    footprint = _cache.TryGetFootPrints(exchange, symbol, timeframe);
                                    if (footprint != null)
                                        await _redis.SetFootPrintsAsync(exchange, symbol, timeframe, footprint);
                                }
                                break;

                            case "c":
                                (exchange, symbol) = (info[1], info[2]);
                                for (int i = 0; i < 7; i++)
                                {
                                    timeframe = _TimeFrames[i];
                                    candle = _cache.TryGetOpenCandle(exchange, symbol, timeframe);
                                    if (candle != null)
                                        await _redis.SetOpenCandleAsync(candle);
                                }
                                break;
                        }
                    }
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
                    while (_candle.TryDequeue(out res) && !stoppingToken.IsCancellationRequested)
                    {
                        candle = _cache.TryGetOpenCandle(Exchange, res.Symbol, res.TimeFrame);
                        if (candle != null)
                        {
                            string update = $"{candle.Volume}{candle.ClosePrice}";
                            if (!lastUpdate.Equals(update))
                            {
                                lastUpdate = update;
                                _publisher.PublishCandle(candle);
                            }
                        }
                    }
                }
            }).Start();

            // publish trade thread
            new Thread(() =>
            {
                byte[] tradeBin;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_trade.TryDequeue(out tradeBin) && !stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            _publisher.PublishTrade(ZeroMQ.Trade.DeserializeBinanceFuturesUsdTrade(tradeBin));
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex.Message, ex);
                        }
                    }
                }
            }).Start();

            // publish depth thread
            new Thread(() =>
            {
                ZeroMQ.OrderBook orderBook;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_depth.TryDequeue(out orderBook) && !stoppingToken.IsCancellationRequested)
                    {
                        if (orderBook != null)
                            _publisher.PublishOrderbook(orderBook);
                    }
                }
            }).Start();

            return Task.CompletedTask;
        }
    }
}