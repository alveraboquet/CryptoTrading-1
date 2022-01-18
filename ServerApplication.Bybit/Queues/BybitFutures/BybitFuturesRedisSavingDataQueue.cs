using System.Collections.Concurrent;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class BybitFuturesRedisSavingDataQueue : ConcurrentQueue<string>
    {
        public void EnqueueCandle(string exchange, string symbol)
        {
            this.Enqueue($"c:{exchange}:{symbol}");
        }
        
        public void EnqueueFootprint(string exchange, string symbol)
        {
            this.Enqueue($"f:{exchange}:{symbol}");
        }
        
        public void EnqueueOrderbook(string exchange, string symbol)
        {
            this.Enqueue($"o:{exchange}:{symbol}");
        }
        
        public (string exchange, string symbol) GetOrderbookInfo(string val)
        {
            string[] res = val.Split(':');
            return (res[1], res[2]);
        }

        public (string exchange, string symbol, string timeframe) GetInfo(string val)
        {
            string[] res = val.Split(':');
            return (res[1], res[2], res[3]);
        }
    }
}