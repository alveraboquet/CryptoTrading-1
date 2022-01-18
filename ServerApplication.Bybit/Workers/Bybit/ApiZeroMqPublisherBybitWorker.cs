using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Queues;
using ZeroMQ;
using ZeroMQ.Publishers.Bybit;

namespace ServerApplication.Bybit.Workers
{
    public class ApiZeroMqPublisherBybitWorker : BackgroundService
    {
        private readonly ApiBybitZeroMqCandleQueue _bybitCandle;
        private readonly ApiBybitZeroMqFootprintQueue _bybitFootprint;
        private readonly ApiBybitZeroMqHeatmapQueue _bybitHeatmap;
        private readonly ApiBybitPublisher _bybitPublisher;

        public ApiZeroMqPublisherBybitWorker(ApiBybitZeroMqCandleQueue bybitCandle,
            ApiBybitZeroMqFootprintQueue bybitFootprint, ApiBybitZeroMqHeatmapQueue bybitHeatmap,
            ApiBybitPublisher bybitPublisher)
        {
            _bybitCandle = bybitCandle;
            _bybitFootprint = bybitFootprint;
            _bybitHeatmap = bybitHeatmap;
            _bybitPublisher = bybitPublisher;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Threads to send data to ChainViewAPI

            // candle
            Thread candleThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_bybitCandle.TryDequeue(out OpenCandle candle))
                    {
                        _bybitPublisher.PublishCandle(candle);
                    }

                    Thread.Sleep(1);
                }
            });

            //  footprint
            Thread footprintThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_bybitFootprint.TryDequeue(out OpenFootprint footprint))
                    {
                        _bybitPublisher.PublishFootprint(footprint);
                    }
                    Thread.Sleep(1);
                }
            });

            //  heatmap
            Thread heatmapThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_bybitHeatmap.TryDequeue(out OpenHeatmap heatmap))
                    {
                        _bybitPublisher.PublishHeatmap(heatmap);
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