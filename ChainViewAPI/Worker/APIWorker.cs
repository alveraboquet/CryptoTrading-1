using DatabaseRepository;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChainViewAPI.Worker
{
    public class APIWorker : BackgroundService
    {
        private readonly IMemoryCache _cache;
        private readonly IPairInfoRepository _pairInfo;


        public APIWorker(IMemoryCache cache, IPairInfoRepository pairInfo)
        {
            _cache = cache;
            _pairInfo = pairInfo;
        }

        public async override Task StartAsync(CancellationToken cancellationToken)
        {
            var pairs = await _pairInfo.Get();

            string pairsResponse = pairs.SymbolListResponseMessage();

            _cache.SetPairInfoList(pairs);
            _cache.SetPairInfoListResponse(pairsResponse);
        }


        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                var pairs = await _pairInfo.Get();

                string pairsResponse = pairs.SymbolListResponseMessage();

                _cache.SetPairInfoList(pairs);
                _cache.SetPairInfoListResponse(pairsResponse);

                await Task.Delay(3600000, stoppingToken);
            }
        }
    }
}
