using System.Collections.Concurrent;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class ApiBybitFuturesZeroMqCandleQueue : ConcurrentQueue<ZeroMQ.OpenCandle> { }

    public class ApiBybitFuturesZeroMqFootprintQueue : ConcurrentQueue<ZeroMQ.OpenFootprint> { }

    public class ApiBybitFuturesZeroMqHeatmapQueue : ConcurrentQueue<ZeroMQ.OpenHeatmap> { }
}