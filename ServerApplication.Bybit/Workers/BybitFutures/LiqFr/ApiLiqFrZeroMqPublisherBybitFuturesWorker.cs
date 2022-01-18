using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ.Publishers.BybitFutures;

namespace ServerApplication.Bybit.Workers.BybitFutures.LiqFr
{
    public class ApiLiqFrZeroMqPublisherBybitFuturesWorker : BackgroundService
    {
        private readonly ApiBybitFuturesFrLiqPublisher _publisher;
        private readonly ApiFrBybitFuturesZeroMqCandleQueue _frCandlesQueue;
        private readonly ApiLiqBybitFuturesZeroMqCandleQueue _liqCandleQueue;
        public ApiLiqFrZeroMqPublisherBybitFuturesWorker(ApiLiqBybitFuturesZeroMqCandleQueue liqCandleQueue,
            ApiFrBybitFuturesZeroMqCandleQueue frCandlesQueue, ApiBybitFuturesFrLiqPublisher publisher)
        {
            this._liqCandleQueue = liqCandleQueue;
            this._frCandlesQueue = frCandlesQueue;
            this._publisher = publisher;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Threads to send data to ChainViewAPI

            // FR Candles
            Thread frThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_frCandlesQueue.TryDequeue(out var candle))
                    {
                        _publisher.PublishFrCandle(candle);
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            });

            // LIQ Candles
            Thread liqThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_liqCandleQueue.TryDequeue(out var candle))
                    {
                        _publisher.PublishLiqCandle(candle);
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            });

            frThread.Start();
            liqThread.Start();

            return Task.CompletedTask;
        }
    }
}
