using System.Collections.Concurrent;

namespace ServerApplication.Queues
{
    public class ApiFrBinanceFuturesUsdZeroMqCandleQueue : ConcurrentQueue<ZeroMQ.OpenCandle> { }
    public class ApiLiqBinanceFuturesUsdZeroMqCandleQueue : ConcurrentQueue<ZeroMQ.OpenCandle> { }
}