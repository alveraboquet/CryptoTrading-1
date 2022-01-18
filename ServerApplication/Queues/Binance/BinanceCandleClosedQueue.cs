using DataLayer.Models;
using DataLayer.Models.Stream;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Queues
{
    public class BinanceCandleClosedQueue : ConcurrentQueue<DataLayer.Candle> { }
}
