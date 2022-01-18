using ServerApplication.Bybit.StreamingServices.BybitFutures;
using ServerApplication.Bybit.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServerApplication.Bybit.Queues;
using ServerApplication.Bybit.Queues.BybitFutures;
using Utilities;
using DatabaseRepository;

namespace ServerApplication.Bybit.Workers.BybitFutures.LiqFr
{
    public class BybitFuturesManageStreamingWorker : BackgroundService
    {
        private readonly IMemoryCache _cache;
        private readonly BybitFuturesFrCalculateQueue _frCalcQueue;
        private readonly BybitFuturesLiquidationCalculateQueue _queue;
        private readonly IPairInfoRepository _pairRepo;

        private const string Exchange = ApplicationValues.BybitFuturesName;

        public BybitFuturesManageStreamingWorker(IMemoryCache cache,
            BybitFuturesFrCalculateQueue frCalcQueue,
            BybitFuturesLiquidationCalculateQueue queue, IPairInfoRepository pairRepo)
        {
            _cache = cache;
            _frCalcQueue = frCalcQueue;
            _queue = queue;
            _pairRepo = pairRepo;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {

                    var pairInfos = (await _pairRepo.GetListed(Exchange)).RemoveCostumePairs();

                    pairInfos = pairInfos.Where(p => p.Symbol.EndsWith("USDT")).ToList();

                    // run funding rate streaming
                    _cache.TryGetBybitFuturesIsFrStreaming(out bool isFrStreaming);
                    if (!isFrStreaming)
                    {
                        var streaming = new BybitFuturesFrStreaming(_cache, _frCalcQueue);
                        streaming.Connect(pairInfos);
                    }

                    // run liquidation streaming
                    _cache.TryGetBybitFuturesIsLiqStreaming(out bool isLiqStreaming);
                    if (!isLiqStreaming)
                    {
                        var streaming = new BybitFuturesLiqStreaming(_cache, _queue);
                        streaming.Connect(pairInfos);
                    }

                    Thread.Sleep(5000);
                }
            }).Start();

            return Task.CompletedTask;
        }
    }
}
