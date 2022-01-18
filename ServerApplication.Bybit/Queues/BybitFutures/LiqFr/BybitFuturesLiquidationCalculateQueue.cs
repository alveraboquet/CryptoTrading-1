using System.Collections.Concurrent;
using ExchangeModels.BybitFutures;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class BybitFuturesLiquidationCalculateQueue : ConcurrentQueue<BybitLiquidationData>
    {
        
    }
}