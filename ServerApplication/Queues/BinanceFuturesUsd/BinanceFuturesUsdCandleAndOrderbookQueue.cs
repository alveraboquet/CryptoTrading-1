using System.Collections.Concurrent;

namespace ServerApplication.Queues
{
    public class BinanceFuturesUsdCandleAndOrderbookQueue : ConcurrentQueue<DataLayer.Candle> { }
}