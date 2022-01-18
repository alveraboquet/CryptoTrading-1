using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Bybit.Queues
{
    public class BybitFuturesFrCalculateQueue : ConcurrentQueue<(string Symbol, decimal FundingRate)> { }
}
