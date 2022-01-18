using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Caching;
using ServerApplication.Queues;
using ServerApplication.StreamingServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerApplication.Workers
{
    public class BinanceFuturesUsdManageStreamingWorker : BackgroundService
    {
        private readonly IMemoryCache _cache;
        private readonly BinanceFuturesUsdFundingRateCalculateQueue _fundingRates;
        private readonly BinanceFuturesUsdLiquidationCalculateQueue _liqidation;
        private readonly BinanceFuturesUsdZeroMqFundingRateQueue _fundingRateZeroMq;

        public BinanceFuturesUsdManageStreamingWorker(IMemoryCache cache,
            BinanceFuturesUsdFundingRateCalculateQueue fundingRates,
            BinanceFuturesUsdLiquidationCalculateQueue liqidation,
            BinanceFuturesUsdZeroMqFundingRateQueue fundingRateZeroMq)
        {
            this._cache = cache;

            _fundingRateZeroMq = fundingRateZeroMq;
            _fundingRates = fundingRates;
            _liqidation = liqidation;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!_cache.TryGetBinanceFuturesUsdIsLiqFrStreaming(out bool isStreaming))
                    {
                        var streaming = new BinanceFuturesUsdFrLiqStreaming(_cache, _fundingRates, _liqidation, _fundingRateZeroMq);
                        streaming.Connect();
                    }
                    Thread.Sleep(5000);
                }
            }).Start();

            return Task.CompletedTask;
        }
    }
}