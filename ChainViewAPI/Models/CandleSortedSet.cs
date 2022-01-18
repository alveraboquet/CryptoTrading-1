using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChainViewAPI.Models
{
    public class CandleSortedSet
    {
        public CandleSortedSet(string exchange, string symbol, string timeframe)
        {
            this.Exchange = exchange;
            this.Symbol = symbol;
            this.Timeframe = timeframe;
            this.Data = new SortedSet<ResCandle>(new ComparerCandleOpenTime());

            this.IsAllDataExtractedFromMongoDB = false;
        }

        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string Timeframe { get; set; }
        public bool IsAllDataExtractedFromMongoDB { get; set; }

        private long _minOpenTime = 0;
        public long MinOpenTime
        {
            get => _minOpenTime;
            private set
            {
                if (_minOpenTime is 0)
                    _minOpenTime = value;
                else
                    _minOpenTime = Math.Min(_minOpenTime, value);
            }
        }

        private long _maxOpenTime = 0;
        public long MaxOpenTime
        {
            get => _maxOpenTime;
            private set
            {
                if (value is 0)
                    _maxOpenTime = value;
                else
                    _maxOpenTime = Math.Max(_maxOpenTime, value);
            }
        }
        private SortedSet<ResCandle> Data { get; set; }
        private object _lockData = new object();

        #region Public Methods

        public static string CreateJson(List<ResCandle> candles)
        {
            StringBuilder str = new StringBuilder();
            str.Append('[');
            int count = candles.Count;
            for (int i = 0; i < count; i++)
            {
                str.Append(candles[i].Json);
            }

            // remove the last `,`
            if (candles.Any())
                str = str.Remove(str.Length - 1, 1);

            str.Append(']');
            return str.ToString();
        }

        public void Clear()
        {
            this.MinOpenTime = 0;
            this.MaxOpenTime = 0;

            this.IsAllDataExtractedFromMongoDB = false;
            lock (_lockData)
                this.Data.Clear();
        }

        public List<ResCandle> GetRange(long start, long end)
        {
            lock (_lockData)
                return (from c in this.Data
                        where c.OpenTime >= start &&
                                c.OpenTime <= end
                        select c).ToList();
        }

        public void AddRange(IEnumerable<ResCandle> candles)
        {
            if (candles.Any())
            {
                this.MinOpenTime = candles.Min(c => c.OpenTime);
                this.MaxOpenTime = candles.Max(c => c.OpenTime);
            }

            lock (_lockData)
                this.Data.UnionWith(candles);
        }
        public void AddRange(IEnumerable<DataLayer.ResCandle> candles)
        {
            this.AddRange(candles.Select(c => new Models.ResCandle(c)));
        }

        public void Add(ResCandle candle)
        {
            this.MinOpenTime = candle.OpenTime;
            this.MaxOpenTime = candle.OpenTime;

            lock (_lockData)
                this.Data.Add(candle);
        }

        #endregion

        private class ComparerCandleOpenTime : IComparer<ResCandle>
        {
            public int Compare(ResCandle x, ResCandle y) => x.OpenTime.CompareTo(y.OpenTime);
        }
    }
}