using System.Collections.Concurrent;
using ZeroMQ;

namespace ServerApplication.Queues
{
    public class BinanceFuturesUsdZeroMqFrCandleQueue : ConcurrentQueue<OpenCandle> { }
}