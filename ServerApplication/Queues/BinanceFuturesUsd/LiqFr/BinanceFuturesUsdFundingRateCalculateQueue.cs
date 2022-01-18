using System.Collections.Concurrent;

namespace ServerApplication.Queues
{
    public class BinanceFuturesUsdFundingRateCalculateQueue : ConcurrentQueue<byte[]> { }
}