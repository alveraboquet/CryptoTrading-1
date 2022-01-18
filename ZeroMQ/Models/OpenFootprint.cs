using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utf8Json;
using System.Runtime.Serialization;

namespace ZeroMQ
{
    public class OpenFootprint
    {
        // info
        [DataMember(Name = "s")]
        public string Symbol { get; set; }
        [DataMember(Name = "t")]
        public string Timeframe { get; set; }

        // data
        [DataMember(Name = "above")]
        public List<decimal[]> AboveMarketOrders { get; set; } //ascending

        [DataMember(Name = "below")]
        public List<decimal[]> BelowMarketOrders { get; set; } //descending sort

        [DataMember(Name = "openPrice")]
        public decimal OpenPrice { get; set; }

        [DataMember(Name = "openTime")]
        public long OpenTime { get; set; }

        public static explicit operator DataLayer.ResFootPrint(OpenFootprint footprint)
        {
            return new DataLayer.ResFootPrint()
            {
                OpenTime = footprint.OpenTime,
                OpenPrice = footprint.OpenPrice,
                AboveMarketOrders = footprint.AboveMarketOrders,
                BelowMarketOrders = footprint.BelowMarketOrders
            };
        }
    }
}
