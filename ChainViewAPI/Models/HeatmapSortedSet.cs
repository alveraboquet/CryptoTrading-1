using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChainViewAPI.Models
{
    public class HeatmapSortedSet
    {
        public HeatmapSortedSet(string exchange, string symbol, string timeframe)
        {
            this.Exchange = exchange;
            this.Symbol = symbol;
            this.Timeframe = timeframe;
            this.Data = new SortedSet<ResHeatmap>(new ComparerHeatmapOpenTime());
            this.IsAllDataExtractedFromMongoDB = false;
        }

        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string Timeframe { get; set; }
        public bool IsAllDataExtractedFromMongoDB { get; set; }
        private SortedSet<ResHeatmap> Data { get; set; }
        private object _lockData = new object();

        #region Public Methods

        public static string CreateJson(List<ResHeatmap> heatmaps, DataLayer.Mode mode)
        {
            StringBuilder str = new StringBuilder();
            str.Append('[');
            int count = heatmaps.Count;
            for (int i = 0; i < count; i++)
            {
                str.Append(heatmaps[i].GetJson(mode));
            }

            // remove the last `,`
            if (heatmaps.Any())
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

        public List<ResHeatmap> GetRange(long start, long end)
        {
            lock (_lockData)
                return (from h in this.Data
                    where h.OpenTime >= start &&
                            h.OpenTime <= end
                    select h).ToList();
        }

        public void AddRange(IEnumerable<ResHeatmap> heatmaps)
        {
            lock (_lockData)
                this.Data.UnionWith(heatmaps);
        }

        public void Add(ResHeatmap heatmap)
        {
            lock (_lockData)
                this.Data.Add(heatmap);
        }

        #endregion

        private class ComparerHeatmapOpenTime : IComparer<ResHeatmap>
        {
            public int Compare(ResHeatmap x, ResHeatmap y) => x.OpenTime.CompareTo(y.OpenTime);
        }
    }
}