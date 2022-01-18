using System.Collections.Concurrent;
using ServerApplication.Bybit.Models;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class BybitFuturesKlineMessageQueue : ConcurrentQueue<BybitFuturesExtendedCandle> { }
}
