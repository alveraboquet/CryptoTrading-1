using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ChainViewAPI.Worker
{
    public class ClearCacheWorker : BackgroundService
    {
        private const string Exchange = ApplicationValues.BinanceName;
        private IMemoryCache _cache;
        private readonly string[] _TimeFrames = {
            "1m", "5m", "15m", "30m",
            "1H", "2H", "4H", "6H", "12H",
            "1D", "3D"
        };

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("START");
            return base.StartAsync(cancellationToken);
        }

        public ClearCacheWorker(IMemoryCache cache)
        {
            _cache = cache;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Thread heatmapTheard = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var pairs = _cache.TryGetPairInfoList();
                    foreach (var pair in pairs)
                    {
                        foreach (var timeframe in _TimeFrames)
                        {
                            ChartCachingManager.GetSortedHeatmap(Exchange, pair.Symbol, timeframe).Clear();
                        }
                    }
                    Thread.Sleep(7200 * 1000);
                }
            });

            Thread footprintTheard = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var pairs = _cache.TryGetPairInfoList();
                    foreach (var pair in pairs)
                    {
                        foreach (var timeframe in _TimeFrames)
                        {
                            ChartCachingManager.GetSortedFootprints(Exchange, pair.Symbol, timeframe).Clear();
                        }
                    }
                    Thread.Sleep(7200 * 1000);
                }
            });

            Thread candleTheard = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var pairs = _cache.TryGetPairInfoList();
                    foreach (var pair in pairs)
                    {
                        foreach (var timeframe in _TimeFrames)
                        {
                            ChartCachingManager.GetSortedCandles(Exchange, pair.Symbol, timeframe).Clear();
                        }
                    }
                    Thread.Sleep(7200 * 1000);
                }
            });

            footprintTheard.Start();
            heatmapTheard.Start();
            candleTheard.Start();
            return Task.CompletedTask;
        }
    }
}
