using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeModels.BinanceFutures
{
    public class FundingRateUpdate
    {
        [DataMember(Name = "s")]
        public string Symbol { get; set; }

        [DataMember(Name = "E")]
        public long EventTime { get; set; }

        [DataMember(Name = "r")]
        public decimal Rate { get; set; }
    }
}
