using Microsoft.Extensions.DependencyInjection;
using ServerApplication.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication
{
    public static class WorkerManager
    {
        public static IServiceCollection AddBinanceWorkers(this IServiceCollection services)
        {
            services.AddHostedService<BinanceCandleClosedWorker>();
            services.AddHostedService<BinanceKlineWorker>();
            services.AddHostedService<BinanceTradeWorker>();
            services.AddHostedService<BinanceWorker>();
            services.AddHostedService<BinanceRedisWorker>();
            services.AddHostedService<ApiZeroMqPublisherBinanceWorker>();
            return services;
        }

        public static IServiceCollection AddBinanceFuturesUsdWorkers(this IServiceCollection services)
        {
            services.AddHostedService<BinanceFuturesUsdCandleClosedWorker>();
            services.AddHostedService<BinanceFuturesUsdKlineWorker>();
            services.AddHostedService<BinanceFuturesUsdTradeWorker>();
            services.AddHostedService<BinanceFuturesUsdWorker>();
            services.AddHostedService<BinanceFuturesUsdRedisWorker>();
            services.AddHostedService<ApiZeroMqPublisherBinanceFuturesUsdWorker>();

            return services;
        }
        public static IServiceCollection AddBinanceFuturesUsdLiqFrWorkers(this IServiceCollection services)
        {
            services.AddHostedService<BinanceFuturesUsdFundingRateWorker>();
            services.AddHostedService<BinanceFuturesUsdLiquidationWorker>();
            services.AddHostedService<BinanceFuturesUsdManageStreamingWorker>();
            services.AddHostedService<BinanceFuturesUsdZeroMqLiqFrWorker>();
            services.AddHostedService<ApiLiqFrZeroMqPublisherBinanceFuturesUsdWorker>();

            return services;
        }
    }
}
