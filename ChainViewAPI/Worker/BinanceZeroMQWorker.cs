using ChainViewAPI.Models;
using DataLayer;
using DataLayer.Models;
using log4net;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using ZeroMQ;

namespace ChainViewAPI.Worker
{
    public class BinanceZeroMQWorker : BackgroundService
    {
        private ApiBinanceSubscriber _subscriber;
        private readonly string exchange = ApplicationValues.BinanceName;
        private readonly ILog _logger;

        public BinanceZeroMQWorker(ApiBinanceSubscriber subscriber)
        {
            _subscriber = subscriber;
            _logger = LogManager.GetLogger(typeof(BinanceZeroMQWorker));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"START");
            return base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"STOP");
            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Threads to get data from ServerApplication

            //  candle
            Thread candleThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var candle = _subscriber.GetCandle();

                    var sorted = ChartCachingManager.GetSortedCandles(exchange, candle.Symbol, candle.Timeframe);
                    sorted.Add(new Models.ResCandle((DataLayer.ResCandle)candle));
                }
            });

            //  footprint
            Thread footprintThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var footprint = _subscriber.GetFootprint();

                    var sorted = ChartCachingManager.GetSortedFootprints(exchange, footprint.Symbol, footprint.Timeframe);
                    sorted.Add(new Models.ResFootprint((DataLayer.ResFootPrint)footprint));
                }
            });

            //  heatmap
            Thread heatmapThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var heatmap = _subscriber.GetHeatmap();

                    var sorted = ChartCachingManager.GetSortedHeatmap(exchange, heatmap.Symbol, heatmap.Timeframe);
                    sorted.Add(new Models.ResHeatmap((DataLayer.ResHeatmap)heatmap));
                }
            });

            candleThread.Start();
            footprintThread.Start();
            heatmapThread.Start();

            return Task.CompletedTask;
        }
    }
}