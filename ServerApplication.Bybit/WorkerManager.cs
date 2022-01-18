using Microsoft.Extensions.DependencyInjection;
using ServerApplication.Bybit.Workers;
using ServerApplication.Bybit.Workers.BybitFutures;
using ServerApplication.Bybit.Workers.BybitFutures.LiqFr;

namespace ServerApplication.Bybit
{
    public static class WorkerManager
    {
        public static void AddBybitSpotWorkers(this IServiceCollection services)
        {
            services.AddHostedService<ApiZeroMqPublisherBybitWorker>();
            services.AddHostedService<BybitCandleClosedWorker>();
            services.AddHostedService<BybitKlineWorker>();
            services.AddHostedService<BybitRedisWorker>();
            services.AddHostedService<BybitTradeWorker>();
            services.AddHostedService<BybitWorker>();
        }

        public static void AddBybitFuturesWorkers(this IServiceCollection services)
        {
            // Kline & Trade & Depth
            services.AddHostedService<BybitFuturesWorker>();
            services.AddHostedService<BybitFuturesKlineWorker>();
            services.AddHostedService<BybitFuturesRedisWorker>();
            services.AddHostedService<BybitFuturesTradeWorker>();
            services.AddHostedService<BybitFuturesCandleClosedWorker>();
            services.AddHostedService<BybitFuturesApiZeroMqPublisherWorker>();

            // Liquidation & Funding Rate
            services.AddHostedService<BybitFuturesLiquidationWorker>();
            services.AddHostedService<BybitFuturesFundingRateWorker>();
            services.AddHostedService<BybitFuturesManageStreamingWorker>();
            services.AddHostedService<BybitFuturesZeroMqLiqFrWorker>();
            services.AddHostedService<ApiLiqFrZeroMqPublisherBybitFuturesWorker>();
        }
    }
}