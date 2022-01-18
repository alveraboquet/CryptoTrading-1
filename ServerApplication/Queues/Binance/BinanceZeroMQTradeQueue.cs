using ExchangeModels;
using ExchangeServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Queues
{
    public class BinanceZeroMQTradeQueue : ConcurrentQueue<byte[]> { }
}