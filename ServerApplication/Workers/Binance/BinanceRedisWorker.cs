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
    public class BinanceRedisWorker : BackgroundService
    {
        private readonly ILog _logger;
        private ICacheService _redis;
        private BinancePublisher _publisher;
        private BinanceZeroMQTradeQueue _trade;
        private BinanceZeroMQCandleQueue _candle;
        private BinanceZeroMQDepthQueue _depth;
        private BinanceRedisSavingDataQueue _redisQueue;
        private IMemoryCache _cache;
        private readonly string[] _TimeFrames = {
            "1m", "5m", "15m", "30m",
            "1H", "2H", "4H", "6H", "12H",
            "1D", "3D"
        };
        const string Exchange = ApplicationValues.BinanceName;
        public BinanceRedisWorker(BinanceRedisSavingDataQueue redisQueue, ZeroMQ.BinancePublisher publisher,
            ICacheService redisCache, BinanceZeroMQTradeQueue trade, BinanceZeroMQDepthQueue depth,
            BinanceZeroMQCandleQueue candle, IMemoryCache cache)
        {
            _cache = cache;
            _candle = candle;
            _redis = redisCache;
            _trade = trade;
            _depth = depth;
            _publisher = publisher;
            _redisQueue = redisQueue;
            _logger = LogManager.GetLogger(typeof(BinanceRedisWorker));
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
                                for (int i = 0; i < 11; i++)
                                {
                                    timeframe = _TimeFrames[i];
                                    footprint = _cache.TryGetFootPrints(Exchange, symbol, timeframe);
                                    if (footprint != null)
                                        await _redis.SetFootPrintsAsync(Exchange, symbol, timeframe, footprint);
                                }
                                break;

                            case "c":
                                symbol = info[1];
                                for (int i = 0; i < 11; i++)
                                {
                                    timeframe = _TimeFrames[i];
                                    candle = _cache.TryGetOpenCandle(Exchange, symbol, timeframe);
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
                while (!stoppingToken.IsCancellationRequested)
                    while (_candle.TryDequeue(out res) && !stoppingToken.IsCancellationRequested)
                    {
                        candle = _cache.TryGetOpenCandle(Exchange, res.Symbol, res.TimeFrame);
                        if (candle != null)
                            _publisher.PublishCandle(candle);
                    }

            }).Start();

            // publish trade thread
            new Thread(() =>
            {
                ZeroMQ.Trade trade;
                byte[] tradeBins;
                while (!stoppingToken.IsCancellationRequested)
                    while (_trade.TryDequeue(out tradeBins) && !stoppingToken.IsCancellationRequested)
                    {
                        trade = ZeroMQ.Trade.DeserializeBinanceTrade(tradeBins);
                        if (trade != null)
                            _publisher.PublishTrade(trade);
                    }

            }).Start();

            // publish depth thread
            new Thread(() =>
            {
                ZeroMQ.OrderBook orderBook;
                while (!stoppingToken.IsCancellationRequested)
                    while (_depth.TryDequeue(out orderBook) && !stoppingToken.IsCancellationRequested)
                    {
                        if (orderBook != null)
                            _publisher.PublishOrderbook(orderBook);
                    }

            }).Start();
            return Task.CompletedTask;
        }
    }
}
