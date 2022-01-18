using System.Collections.Concurrent;

namespace ServerApplication.Queues
{
    public class BinanceFuturesUsdZeroMqDepthQueue : ConcurrentQueue<ZeroMQ.OrderBook> { }
}