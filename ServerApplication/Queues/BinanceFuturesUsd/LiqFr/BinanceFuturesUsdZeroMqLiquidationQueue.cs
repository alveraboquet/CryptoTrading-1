using System.Collections.Concurrent;
using ZeroMQ;

namespace ServerApplication.Queues
{
    public class BinanceFuturesUsdZeroMqLiquidationQueue : ConcurrentQueue<Trade> { }
}