using Microsoft.Extensions.DependencyInjection;
using ServerApplication.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication
{
    public static class DiManager
    {
        public static IServiceCollection AddBinanceQueues(this IServiceCollection services)
        {
            // kline/trade worker
            services.AddSingleton<BinanceKlineCalculate>();
            services.AddSingleton<BinanceTradeCalculate>();

            services.AddSingleton<BinanceMongoDbCandleQueue>();
            services.AddSingleton<BinanceCandleClosedQueue>();

            // redis
            services.AddSingleton<BinanceRedisSavingDataQueue>();

            // zeroMq websocket
            services.AddSingleton<BinanceZeroMQTradeQueue>();
            services.AddSingleton<BinanceZeroMQCandleQueue>();
            services.AddSingleton<BinanceZeroMQDepthQueue>();

            // zeroMq Api
            services.AddSingleton<ApiBinanceZeroMqCandleQueue>();
            services.AddSingleton<ApiBinanceZeroMqFootprintQueue>();
            services.AddSingleton<ApiBinanceZeroMqHeatmapQueue>();

            return services;
        }

        public static IServiceCollection AddBinanceFuturesUsdQueues(this IServiceCollection services)
        {
            // kline/trade worker
            services.AddSingleton<BinanceFuturesUsdKlineCalculate>();
            services.AddSingleton<BinanceFuturesUsdTradeCalculate>();

            services.AddSingleton<BinanceFuturesUsdCandleAndOrderbookQueue>();
            services.AddSingleton<BinanceFuturesUsdMongoDbCandleQueue>();

            // redis
            services.AddSingleton<BinanceFuturesUsdRedisSavingDataQueue>();

            // zeroMq websocket
            services.AddSingleton<BinanceFuturesUsdZeroMqTradeQueue>();
            services.AddSingleton<BinanceFuturesUsdZeroMqCandleQueue>();
            services.AddSingleton<BinanceFuturesUsdZeroMqDepthQueue>();

            // zeroMq Api
            services.AddSingleton<ApiBinanceFuturesUsdZeroMqCandleQueue>();
            services.AddSingleton<ApiBinanceFuturesUsdZeroMqFootprintQueue>();
            services.AddSingleton<ApiBinanceFuturesUsdZeroMqHeatmapQueue>();

            // LiqFr
            services.AddSingleton<BinanceFuturesUsdFundingRateCalculateQueue>();
            services.AddSingleton<BinanceFuturesUsdLiquidationCalculateQueue>();
            services.AddSingleton<BinanceFuturesUsdZeroMqFrCandleQueue>();
            services.AddSingleton<BinanceFuturesUsdZeroMqFundingRateQueue>();
            services.AddSingleton<BinanceFuturesUsdZeroMqLiqCandleQueue>();
            services.AddSingleton<BinanceFuturesUsdZeroMqLiquidationQueue>();

            // LiqFr Api
            services.AddSingleton<ApiFrBinanceFuturesUsdZeroMqCandleQueue>();
            services.AddSingleton<ApiLiqBinanceFuturesUsdZeroMqCandleQueue>();

            return services;
        }

        public static IServiceCollection AddBinanceZeroMqPublishers(this IServiceCollection services)
        {
            services.AddSingleton<ZeroMQ.BinancePublisher>();
            services.AddSingleton<ZeroMQ.ApiBinancePublisher>();

            return services;
        }

        public static IServiceCollection AddBinanceFuturesUsdZeroMqPublishers(this IServiceCollection services)
        {
            services.AddSingleton<ZeroMQ.BinanceFuturesUsdPublisher>();
            services.AddSingleton<ZeroMQ.ApiBinanceFuturesUsdPublisher>();

            // LiqFr
            services.AddSingleton<ZeroMQ.BinanceFuturesUsdFrLiqPublisher>();
            services.AddSingleton<ZeroMQ.ApiBinanceFuturesUsdFrLiqPublisher>();

            return services;
        }
    }
}
