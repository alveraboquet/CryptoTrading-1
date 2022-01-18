using System.Collections.Concurrent;

namespace ServerApplication.Queues
{
    public class ApiBinanceFuturesUsdZeroMqCandleQueue : ConcurrentQueue<ZeroMQ.OpenCandle> { }

    public class ApiBinanceFuturesUsdZeroMqFootprintQueue : ConcurrentQueue<ZeroMQ.OpenFootprint> { }

    public class ApiBinanceFuturesUsdZeroMqHeatmapQueue : ConcurrentQueue<ZeroMQ.OpenHeatmap> { }
}