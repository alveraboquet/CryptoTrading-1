using System.Collections.Concurrent;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class BybitFuturesMongoDbCandleQueue : ConcurrentQueue<DataLayer.Candle> { }
}