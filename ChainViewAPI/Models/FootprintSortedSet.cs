using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChainViewAPI.Models
{
    public class FootprintSortedSet
    {
        public FootprintSortedSet(string exchange, string symbol, string timeframe)
        {
            this.Exchange = exchange;
            this.Symbol = symbol;
            this.Timeframe = timeframe;
            this.Data = new SortedSet<ResFootprint>(new ComparerFooprintOpenTime());
            this.IsAllDataExtractedFromMongoDB = false;
        }

        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string Timeframe { get; set; }
        public bool IsAllDataExtractedFromMongoDB { get; set; }
        private SortedSet<ResFootprint> Data { get; set; }

        private object _lockData = new object();

        #region Public Methods

        public static string CreateJson(List<ResFootprint> footprints)
        {
            StringBuilder str = new StringBuilder();
            str.Append('[');
            int count = footprints.Count;
            for (int i = 0; i < count; i++)
            {
                str.Append(footprints[i].Json);
            }

            // remove the last ','
            if (footprints.Any())
                str = str.Remove(str.Length - 1, 1);

            str.Append(']');
            return str.ToString();
        }

        public void Clear()
        {
            this.IsAllDataExtractedFromMongoDB = false;
            lock (_lockData)
                this.Data.Clear();
        }

        public List<ResFootprint> GetRange(long start, long end)
        {
            lock (_lockData)
                return (from f in this.Data
                    where f.OpenTime >= start &&
                            f.OpenTime <= end
                    select f).ToList();
        }

        public void AddRange(IEnumerable<ResFootprint> footprints)
        {
            lock (_lockData)
                this.Data.UnionWith(footprints);
        }

        public void Add(ResFootprint footprint)
        {
            lock (_lockData)
                this.Data.Add(footprint);
        }

        #endregion

        private class ComparerFooprintOpenTime : IComparer<ResFootprint>
        {
            public int Compare(ResFootprint x, ResFootprint y) => x.OpenTime.CompareTo(y.OpenTime);
        }
    }
}