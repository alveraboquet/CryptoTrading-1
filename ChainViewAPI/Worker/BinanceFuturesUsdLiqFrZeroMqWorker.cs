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
    public class BinanceFuturesUsdLiqFrZeroMqWorker : BackgroundService
    {
        private readonly ApiLiqFrBinanceFuturesUsdSubscriber _subscriber;
        private readonly string Exchange = ApplicationValues.BinanceUsdName;
        private readonly ILog _logger;

        public BinanceFuturesUsdLiqFrZeroMqWorker(ApiLiqFrBinanceFuturesUsdSubscriber subscriber)
        {
            _subscriber = subscriber;
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdLiqFrZeroMqWorker));
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

            Thread frThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var candle = _subscriber.GetFrCandle();

                    var sorted = ChartCachingManager.GetSortedCandles(Exchange, candle.Symbol, candle.Timeframe);
                    sorted.Add(new Models.ResCandle((DataLayer.ResCandle)candle));
                }
            });

            Thread liqThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var candle = _subscriber.GetLiqCandle();

                    var sorted = ChartCachingManager.GetSortedCandles(Exchange, candle.Symbol, candle.Timeframe);
                    sorted.Add(new Models.ResCandle((DataLayer.ResCandle)candle));
                }
            });

            frThread.Start();
            liqThread.Start();

            return Task.CompletedTask;
        }
    }
}
