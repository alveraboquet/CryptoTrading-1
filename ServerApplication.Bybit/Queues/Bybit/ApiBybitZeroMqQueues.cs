using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Bybit.Queues
{
    public class ApiBybitZeroMqCandleQueue : ConcurrentQueue<ZeroMQ.OpenCandle>
    {
    }

    public class ApiBybitZeroMqFootprintQueue : ConcurrentQueue<ZeroMQ.OpenFootprint>
    {
    }

    public class ApiBybitZeroMqHeatmapQueue : ConcurrentQueue<ZeroMQ.OpenHeatmap>
    {
    }
}
