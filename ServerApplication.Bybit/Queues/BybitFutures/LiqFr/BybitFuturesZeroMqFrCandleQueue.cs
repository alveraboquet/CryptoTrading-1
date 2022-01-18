using System.Collections.Concurrent;
using ZeroMQ;

namespace ServerApplication.Bybit.Queues
{
    public class BybitFuturesZeroMqFrCandleQueue : ConcurrentQueue<OpenCandle> { }
}
