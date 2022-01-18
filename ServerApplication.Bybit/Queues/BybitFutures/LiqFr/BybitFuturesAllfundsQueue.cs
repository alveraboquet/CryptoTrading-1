using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Bybit.Queues
{
    /// <summary>
    /// Symbol Name, Current FundingRate
    /// </summary>
    public class BybitFuturesAllfundsQueue : ConcurrentQueue<Dictionary<string, decimal>> { }
}
