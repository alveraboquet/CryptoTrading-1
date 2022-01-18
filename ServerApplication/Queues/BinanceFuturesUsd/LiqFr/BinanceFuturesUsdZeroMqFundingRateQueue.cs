using System.Collections.Concurrent;

namespace ServerApplication.Queues
{
    public class BinanceFuturesUsdZeroMqFundingRateQueue : ConcurrentQueue<byte[]> { }
}