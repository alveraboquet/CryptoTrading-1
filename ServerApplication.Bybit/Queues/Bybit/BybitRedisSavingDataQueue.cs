using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Bybit.Queues
{
    public class BybitRedisSavingDataQueue : ConcurrentQueue<string>
    {
        public void EnqueueCandle(string symbol)
        {
            this.Enqueue($"c:{symbol}");
        }

        public void EnqueueFootprint(string symbol)
        {
            this.Enqueue($"f:{symbol}");
        }

        public void EnqueueOrderbook(string symbol)
        {
            this.Enqueue($"o:{symbol}");
        }
    }
}
