using System.Collections.Concurrent;

namespace ServerApplication.Bybit.Queues
{
    public class BybitMongoDbCandleQueue : ConcurrentQueue<DataLayer.Candle>
    {
        
    }
}