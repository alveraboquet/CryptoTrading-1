using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Queues;
using ServerApplication.Bybit.Queues.BybitFutures;
using ServerApplication.Bybit.Queues.BybitFutures.LiqFr;
using ZeroMQ.Publishers.BybitFutures;

namespace ServerApplication.Bybit.Workers.BybitFutures.LiqFr
{
    public class BybitFuturesZeroMqLiqFrWorker : BackgroundService
    {
        private readonly BybitFuturesFrLiqPublisher _publisher;

        private readonly BybitFuturesZeroMqLiquidationQueue _liqTradeZeroMq;
        private readonly BybitFuturesZeroMqLiqCandleQueue _liqCandleZeroMq;
        private readonly BybitFuturesZeroMqFrCandleQueue _frCandleZeroMq;
        private readonly BybitFuturesAllfundsQueue _allfundsQueue;
        
        private readonly ILog _logger;

        public BybitFuturesZeroMqLiqFrWorker(BybitFuturesFrLiqPublisher publisher,
            BybitFuturesZeroMqLiquidationQueue liqTradeZeroMq, BybitFuturesZeroMqLiqCandleQueue liqCandleZeroMq,
            BybitFuturesZeroMqFrCandleQueue frCandleZeroMq, BybitFuturesAllfundsQueue allfundsQueue)
        {
            _logger = LogManager.GetLogger(typeof(BybitFuturesZeroMqLiqFrWorker));
            _publisher = publisher;
            _liqTradeZeroMq = liqTradeZeroMq;
            _liqCandleZeroMq = liqCandleZeroMq;
            _frCandleZeroMq = frCandleZeroMq;
            _allfundsQueue = allfundsQueue;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Liq trade thread
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _liqTradeZeroMq.TryDequeue(out var trade))
                    {
                        // _logger.Info($"Publishing liq trade: {trade.Symbol}-{trade.Amount}-{trade.Price}");
                        _publisher.PublishLiqTrade(trade);
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            // Liq candle thread
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _liqCandleZeroMq.TryDequeue(out var candle))
                    {
                        // _logger.Info($"Publishing liq candle: {candle.Symbol}[{candle.Timeframe}]-{candle.Open}->{candle.Close}");
                        _publisher.PublishLiqCandle(candle);
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            // Fr candle thread
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _frCandleZeroMq.TryDequeue(out var candle))
                    {
                        // _logger.Info($"Publishing fr candle: {candle.Symbol}[{candle.Timeframe}]-{candle.Open}->{candle.Close}");
                        _publisher.PublishFrCandle(candle);
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            // allfunds thread
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested 
                           && _allfundsQueue.TryDequeue(out Dictionary<string, decimal> json))
                    {
                        // monitoring publishing process
                        // _logger.Info($"Publishing allfunds:");
                        // foreach (var item in json)
                        //     _logger.Info($"{item.Key}: {item.Value}");
                        _publisher.PublishAllfunds(json);
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            return Task.CompletedTask;
        }
    }
}