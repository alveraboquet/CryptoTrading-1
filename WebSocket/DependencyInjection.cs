using Microsoft.Extensions.DependencyInjection;
using WebSocket.Workers.Bybit;
using WebSocket.Workers.BybitFutures;
using WebSocket.Workers.BybitFutures.LiqFr;

namespace WebSocket
{
    public static class DependencyInjection
    {
        public static void AddBybitWorkers(this IServiceCollection services)
        {
            // Add socket publishers
            services.AddHostedService<BybitTradeWorker>();
            services.AddHostedService<BybitOrderbookWorker>();
            services.AddHostedService<BybitCandleWorker>();
            
            services.AddHostedService<BybitFuturesTradeWorker>();
            services.AddHostedService<BybitFuturesOrderbookWorker>();
            services.AddHostedService<BybitFuturesCandleWorker>();
            services.AddHostedService<BybitFuturesAllfundsWorker>();
            services.AddHostedService<BybitFuturesFrCandleWorker>();
            services.AddHostedService<BybitFuturesLiqTradeWorker>();
            services.AddHostedService<BybitFuturesLiqCandleWorker>();
        }
    }
}