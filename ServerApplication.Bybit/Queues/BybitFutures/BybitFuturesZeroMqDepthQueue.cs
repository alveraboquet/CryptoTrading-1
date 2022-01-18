using System.Collections.Concurrent;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class BybitFuturesZeroMqDepthQueue : ConcurrentQueue<ZeroMQ.OrderBook>
    {
        
    }
}