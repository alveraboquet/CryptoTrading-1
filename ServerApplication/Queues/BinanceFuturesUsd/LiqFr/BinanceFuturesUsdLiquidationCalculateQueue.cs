using System.Collections.Concurrent;

namespace ServerApplication.Queues
{
    public class BinanceFuturesUsdLiquidationCalculateQueue : ConcurrentQueue<byte[]> { }
}