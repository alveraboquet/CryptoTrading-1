using System.Collections.Concurrent;
using ZeroMQ;

namespace ServerApplication.Bybit.Queues.BybitFutures.LiqFr
{
    public class BybitFuturesZeroMqLiqCandleQueue : ConcurrentQueue<ZeroMQ.OpenCandle>
    {
        
    }
}