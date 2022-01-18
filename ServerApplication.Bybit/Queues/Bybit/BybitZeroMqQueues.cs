using System.Collections.Concurrent;

namespace ServerApplication.Bybit.Queues
{
    public class BybitZeroMQDepthQueue : ConcurrentQueue<ZeroMQ.OrderBook> { }

    public class BybitZeroMQKlineQueue : ConcurrentQueue<ZeroMQ.OpenCandle> { }

    public class BybitZeroMQTradeQueue : ConcurrentQueue<ZeroMQ.Trade> { }
}