using log4net;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using ZeroMQ.Subscribers;

namespace ChainViewAPI.Worker
{
    public class BinanceFuturesUsdZeroMqWorker : BackgroundService
    {
        private ApiBinanceFuturesUsdSubscriber _subscriber;
        private readonly string Exchange = ApplicationValues.BinanceUsdName;
        private readonly ILog _logger;

        public BinanceFuturesUsdZeroMqWorker(ApiBinanceFuturesUsdSubscriber subscriber)
        {
            _subscriber = subscriber;
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdZeroMqWorker));
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

                    var sorted = ChartCachingManager.GetSortedCandles(Exchange, candle.Symbol, candle.Timeframe);
                    sorted.Add(new Models.ResCandle((DataLayer.ResCandle)candle));
                }
            });

            //  footprint
            Thread footprintThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var footprint = _subscriber.GetFootprint();

                    var sorted = ChartCachingManager.GetSortedFootprints(Exchange, footprint.Symbol, footprint.Timeframe);
                    sorted.Add(new Models.ResFootprint((DataLayer.ResFootPrint)footprint));
                }
            });

            //  heatmap
            Thread heatmapThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var heatmap = _subscriber.GetHeatmap();

                    var sorted = ChartCachingManager.GetSortedHeatmap(Exchange, heatmap.Symbol, heatmap.Timeframe);
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
