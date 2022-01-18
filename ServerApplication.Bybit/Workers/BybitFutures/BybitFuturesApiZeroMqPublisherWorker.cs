using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Queues.BybitFutures;
using ZeroMQ.Publishers.Bybit;

namespace ServerApplication.Bybit.Workers.BybitFutures
{
    public class BybitFuturesApiZeroMqPublisherWorker : BackgroundService
    {
        private readonly ApiBybitFuturesZeroMqCandleQueue _candleQueue;
        private readonly ApiBybitFuturesZeroMqFootprintQueue _footprintQueue;
        private readonly ApiBybitFuturesZeroMqHeatmapQueue _heatmapQueue;
        private readonly ApiBybitPublisher _publisher;

        public BybitFuturesApiZeroMqPublisherWorker(ApiBybitPublisher publisher,
            ApiBybitFuturesZeroMqFootprintQueue footprintQueue, ApiBybitFuturesZeroMqHeatmapQueue heatmapQueue,
            ApiBybitFuturesZeroMqCandleQueue candleQueue)
        {
            _candleQueue = candleQueue;
            _footprintQueue = footprintQueue;
            _heatmapQueue = heatmapQueue;
            _publisher = publisher;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Threads to send data to ChainViewAPI

            // candle
            Thread candleThread = new Thread(() =>
            {
                ZeroMQ.OpenCandle candle;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_candleQueue.TryDequeue(out candle))
                    {
                        _publisher.PublishCandle(candle);
                        // Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            });

            //  footprint
            Thread footprintThread = new Thread(() =>
            {
                ZeroMQ.OpenFootprint footprint;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_footprintQueue.TryDequeue(out footprint))
                    {
                        _publisher.PublishFootprint(footprint);
                        // Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            });

            //  heatmap
            Thread heatmapThread = new Thread(() =>
            {
                ZeroMQ.OpenHeatmap heatmap;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_heatmapQueue.TryDequeue(out heatmap))
                    {
                        _publisher.PublishHeatmap(heatmap);
                        // Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            });

            candleThread.Start();
            footprintThread.Start();
            heatmapThread.Start();

            return Task.CompletedTask;
        }
    }
}