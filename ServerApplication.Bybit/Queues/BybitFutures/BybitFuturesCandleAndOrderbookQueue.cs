using System.Collections.Concurrent;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class BybitFuturesCandleAndOrderbookQueue : ConcurrentQueue<DataLayer.Candle>
    {
        
    }
}