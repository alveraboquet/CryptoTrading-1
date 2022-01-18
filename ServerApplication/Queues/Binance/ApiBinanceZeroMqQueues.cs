using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Queues
{
    public class ApiBinanceZeroMqCandleQueue : ConcurrentQueue<ZeroMQ.OpenCandle>
    {
    }

    public class ApiBinanceZeroMqFootprintQueue : ConcurrentQueue<ZeroMQ.OpenFootprint>
    {
    }

    public class ApiBinanceZeroMqHeatmapQueue : ConcurrentQueue<ZeroMQ.OpenHeatmap>
    {
    }
}
