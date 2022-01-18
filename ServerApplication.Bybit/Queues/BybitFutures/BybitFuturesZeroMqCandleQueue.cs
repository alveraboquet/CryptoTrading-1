using System.Collections.Concurrent;

namespace ServerApplication.Bybit.Queues.BybitFutures
{
    public class BybitFuturesZeroMqCandleQueue : ConcurrentQueue<string>
    {
        public void EnqueueCandle(string symbol, string timeFrame)
        {
            this.Enqueue($"{symbol}:{timeFrame}");
        }

        public void EnqueueCandle(DataLayer.Candle candle)
        {
            this.EnqueueCandle(candle.Symbol, candle.TimeFrame);
        }

        public bool TryDequeue(out (string Symbol, string TimeFrame) result)
        {
            bool isExist = this.TryDequeue(out string res);

            if (isExist)
            {
                string[] val = res.Split(':');
                result.Symbol = val[0];
                result.TimeFrame = val[1];
            }
            else
            {
                result.Symbol = null;
                result.TimeFrame = null;
            }

            return isExist;
        }
    }
}