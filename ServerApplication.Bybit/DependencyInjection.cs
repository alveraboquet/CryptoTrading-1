using Microsoft.Extensions.DependencyInjection;
using ServerApplication.Bybit.Queues;
using ServerApplication.Bybit.Queues.BybitFutures;
using ServerApplication.Bybit.Queues.BybitFutures.LiqFr;
using ZeroMQ.Publishers.Bybit;
using ZeroMQ.Publishers.BybitFutures;

namespace ServerApplication.Bybit
{
    public static class DependencyInjection
    {
        public static void AddBybitSpotQueues(this IServiceCollection services)
        {
            // Kline/Trade
            services.AddSingleton<BybitClosedCandleQueue>();
            services.AddSingleton<BybitKlineMessageQueue>();
            services.AddSingleton<BybitTradeMessageQueue>();
            
            // MongoDb
            services.AddSingleton<BybitMongoDbCandleQueue>();
            
            // Redis
            services.AddSingleton<BybitRedisSavingDataQueue>();
            
            // ZeroMQ
            services.AddSingleton<ApiBybitZeroMqHeatmapQueue>();
            services.AddSingleton<ApiBybitZeroMqCandleQueue>();
            services.AddSingleton<ApiBybitZeroMqFootprintQueue>();
            services.AddSingleton<BybitZeroMQDepthQueue>();
            services.AddSingleton<BybitZeroMQKlineQueue>();
            services.AddSingleton<BybitZeroMQTradeQueue>();
        }

        public static void AddBybitLiqFrQueues(this IServiceCollection services)
        {
            services.AddSingleton<BybitFuturesAllfundsQueue>();
            services.AddSingleton<BybitFuturesFrCalculateQueue>();
            services.AddSingleton<BybitFuturesLiquidationCalculateQueue>();
            services.AddSingleton<ApiLiqBybitFuturesZeroMqCandleQueue>();
            services.AddSingleton<ApiFrBybitFuturesZeroMqCandleQueue>();
            services.AddSingleton<BybitFuturesZeroMqFrCandleQueue>();
            services.AddSingleton<BybitFuturesZeroMqLiqCandleQueue>();
            services.AddSingleton<BybitFuturesZeroMqLiquidationQueue>();
        }

        public static void AddBybitFuturesQueue(this IServiceCollection services)
        {
            services.AddSingleton<ApiBybitFuturesZeroMqCandleQueue>();
            services.AddSingleton<ApiBybitFuturesZeroMqFootprintQueue>();
            services.AddSingleton<ApiBybitFuturesZeroMqHeatmapQueue>();
            
            services.AddSingleton<BybitFuturesCandleAndOrderbookQueue>();
            services.AddSingleton<BybitFuturesKlineMessageQueue>();
            services.AddSingleton<BybitFuturesMongoDbCandleQueue>();
            
            services.AddSingleton<BybitFuturesRedisSavingDataQueue>();
            services.AddSingleton<BybitFuturesTradeMessageQueue>();
            services.AddSingleton<BybitFuturesZeroMqCandleQueue>();
            
            services.AddSingleton<BybitFuturesZeroMqDepthQueue>();
            services.AddSingleton<BybitFuturesZeroMqTradeQueue>();
        }

        public static void AddBybitZeroMqPublishers(this IServiceCollection services)
        {
            // Bybit spot
            services.AddSingleton<ApiBybitPublisher>();
            services.AddSingleton<BybitPublisher>();
            
            // Bybit futures
            services.AddSingleton<ApiBybitFuturesFrLiqPublisher>();
            services.AddSingleton<ApiBybitFuturesPublisher>();
            services.AddSingleton<BybitFuturesFrLiqPublisher>();
            services.AddSingleton<BybitFuturesPublisher>();
        }
    }
}