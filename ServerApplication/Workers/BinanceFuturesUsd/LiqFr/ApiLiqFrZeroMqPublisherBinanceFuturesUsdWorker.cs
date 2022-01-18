using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using ServerApplication.Queues;
using ZeroMQ;

namespace ServerApplication.Workers
{
    public class ApiLiqFrZeroMqPublisherBinanceFuturesUsdWorker : BackgroundService
    {
        private readonly ApiBinanceFuturesUsdFrLiqPublisher _publisher;
        private readonly ApiFrBinanceFuturesUsdZeroMqCandleQueue _frCandlesQueue;
        private readonly ApiLiqBinanceFuturesUsdZeroMqCandleQueue _liqCandleQueue;
        public ApiLiqFrZeroMqPublisherBinanceFuturesUsdWorker(ApiBinanceFuturesUsdFrLiqPublisher publisher,
            ApiFrBinanceFuturesUsdZeroMqCandleQueue frCandlesQueue, ApiLiqBinanceFuturesUsdZeroMqCandleQueue liqCandleQueue)
        {
            _liqCandleQueue = liqCandleQueue;
            _frCandlesQueue = frCandlesQueue;
            _publisher = publisher;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Threads to send data to ChainViewAPI

            // FR Candles
            Thread frThread = new Thread(() =>
            {
                OpenCandle candle;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_frCandlesQueue.TryDequeue(out candle))
                    {
                        _publisher.PublishFrCandle(candle);
                    }
                }
            });

            // LIQ Candles
            Thread liqThread = new Thread(() =>
            {
                OpenCandle candle;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_liqCandleQueue.TryDequeue(out candle))
                    {
                        _publisher.PublishLiqCandle(candle);
                    }
                }
            });

            frThread.Start();
            liqThread.Start();

            return Task.CompletedTask;
        }
    }
}
