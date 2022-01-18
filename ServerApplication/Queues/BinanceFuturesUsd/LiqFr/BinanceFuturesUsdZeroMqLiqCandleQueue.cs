using System.Collections.Concurrent;
using ZeroMQ;

namespace ServerApplication.Queues
{
    public class BinanceFuturesUsdZeroMqLiqCandleQueue : ConcurrentQueue<OpenCandle> { }
}