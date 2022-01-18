using DataLayer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Queues
{
    public class BinanceZeroMQCandleQueue : ConcurrentQueue<string>
    {
        public void Enqueue(string symbol, string timeFrame)
        {
            this.Enqueue($"{symbol}:{timeFrame}");
        }

        public void Enqueue(DataLayer.Candle candle)
        {
            this.Enqueue(candle.Symbol, candle.TimeFrame);
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
