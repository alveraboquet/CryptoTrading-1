using ExchangeModels.BybitFutures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class BybitFuturesTradeMessageQueue : ConcurrentQueue<BybitFuturesTrade> { }
}
