using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ZeroMQ
{
    public class OpenHeatmap
    {
        // info
        [DataMember(Name = "s")]
        public string Symbol { get; set; }
        [DataMember(Name = "t")]
        public string Timeframe { get; set; }

        // data
        [DataMember(Name = "blocks")]
        public List<decimal> Blocks { get; set; }

        [DataMember(Name = "openPrice")]
        public decimal OpenPrice { get; set; }

        [DataMember(Name = "openTime")]
        public long OpenTime { get; set; }

        public static explicit operator DataLayer.ResHeatmap(OpenHeatmap heatmap)
        {
            return new DataLayer.ResHeatmap()
            {
                OpenTime = heatmap.OpenTime,
                OpenPrice = heatmap.OpenPrice,
                Blocks = heatmap.Blocks
            };
        }
    }
}
