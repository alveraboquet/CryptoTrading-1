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
    public class ApiZeroMqPublisherBinanceFuturesUsdWorker : BackgroundService
    {
        private readonly ApiBinanceFuturesUsdZeroMqCandleQueue _binanceCandle;
        private readonly ApiBinanceFuturesUsdZeroMqFootprintQueue _binanceFootprint;
        private readonly ApiBinanceFuturesUsdZeroMqHeatmapQueue _binanceHeatmap;
        private readonly ApiBinanceFuturesUsdPublisher _publisher;

        public ApiZeroMqPublisherBinanceFuturesUsdWorker(ApiBinanceFuturesUsdZeroMqCandleQueue binanceCandle,
            ApiBinanceFuturesUsdZeroMqFootprintQueue binanceFootprint, ApiBinanceFuturesUsdZeroMqHeatmapQueue binanceHeatmap,
            ApiBinanceFuturesUsdPublisher binancePublisher)
        {
            _publisher = binancePublisher;
            _binanceFootprint = binanceFootprint;
            _binanceCandle = binanceCandle;
            _binanceHeatmap = binanceHeatmap;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Threads to send data to ChainViewAPI

            // candle
            Thread candleThread = new Thread(() =>
            {
                OpenCandle candle;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_binanceCandle.TryDequeue(out candle))
                    {
                        _publisher.PublishCandle(candle);
                    }
                }
            });

            //  footprint
            Thread footprintThread = new Thread(() =>
            {
                OpenFootprint footprint;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_binanceFootprint.TryDequeue(out footprint))
                    {
                        _publisher.PublishFootprint(footprint);
                    }
                }
            });

            //  heatmap
            Thread heatmapThread = new Thread(() =>
            {
                OpenHeatmap heatmap;
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_binanceHeatmap.TryDequeue(out heatmap))
                    {
                        _publisher.PublishHeatmap(heatmap);
                    }
                }
            });

            candleThread.Start();
            footprintThread.Start();
            heatmapThread.Start();

            return Task.CompletedTask;
        }
    }
}
