using Microsoft.Extensions.Hosting;
using ServerApplication.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ServerApplication.Workers
{
    public class BinanceFuturesUsdZeroMqLiqFrWorker : BackgroundService
    {
        private readonly BinanceFuturesUsdFrLiqPublisher _publisher;

        private readonly BinanceFuturesUsdZeroMqLiquidationQueue _liqTradeZeroMq;
        private readonly BinanceFuturesUsdZeroMqLiqCandleQueue _liqCandleZeroMq;
        private readonly BinanceFuturesUsdZeroMqFrCandleQueue _frCandleZeroMq;

        private readonly BinanceFuturesUsdZeroMqFundingRateQueue _fundingRateZeroMq;

        public BinanceFuturesUsdZeroMqLiqFrWorker(BinanceFuturesUsdFrLiqPublisher publisher,
            BinanceFuturesUsdZeroMqLiquidationQueue liqTradeZeroMq,
            BinanceFuturesUsdZeroMqLiqCandleQueue liqCandleZeroMq,
            BinanceFuturesUsdZeroMqFrCandleQueue frCandleZeroMq,
            BinanceFuturesUsdZeroMqFundingRateQueue fundingRateZeroMq)
        {
            _fundingRateZeroMq = fundingRateZeroMq;
            _frCandleZeroMq = frCandleZeroMq;
            _liqCandleZeroMq = liqCandleZeroMq;
            _liqTradeZeroMq = liqTradeZeroMq;
            _publisher = publisher;
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
                        _publisher.PublishLiqTrade(trade);
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
                        _publisher.PublishLiqCandle(candle);
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
                        _publisher.PublishFrCandle(candle);
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            // allfunds thread
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _fundingRateZeroMq.TryDequeue(out byte[] json))
                    {
                        _publisher.PublishAllfunds(json);
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            return Task.CompletedTask;
        }
    }
}