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
    public class ApiZeroMqPublisherBinanceWorker : BackgroundService
    {
        private readonly ApiBinanceZeroMqCandleQueue _binanceCandle;
        private readonly ApiBinanceZeroMqFootprintQueue _binanceFootprint;
        private readonly ApiBinanceZeroMqHeatmapQueue _binanceHeatmap;
        private readonly ApiBinancePublisher _binancePublisher;
        public ApiZeroMqPublisherBinanceWorker(ApiBinanceZeroMqCandleQueue binanceCandle,
            ApiBinanceZeroMqFootprintQueue binanceFootprint, ApiBinanceZeroMqHeatmapQueue binanceHeatmap,
            ApiBinancePublisher binancePublisher)
        {
            _binancePublisher = binancePublisher;
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
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_binanceCandle.TryDequeue(out OpenCandle candle))
                    {
                        _binancePublisher.PublishCandle(candle);
                    }
                }
            });

            //  footprint
            Thread footprintThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_binanceFootprint.TryDequeue(out OpenFootprint footprint))
                    {
                        _binancePublisher.PublishFootprint(footprint);
                    }
                }
            });

            //  heatmap
            Thread heatmapThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_binanceHeatmap.TryDequeue(out OpenHeatmap heatmap))
                    {
                        _binancePublisher.PublishHeatmap(heatmap);
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