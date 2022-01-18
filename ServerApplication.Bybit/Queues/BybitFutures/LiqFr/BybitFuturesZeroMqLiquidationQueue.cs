using System.Collections.Concurrent;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class BybitFuturesZeroMqLiquidationQueue : ConcurrentQueue<ZeroMQ.Trade>
    {
        
    }
}