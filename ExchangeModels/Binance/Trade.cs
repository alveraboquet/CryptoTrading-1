using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExchangeModels
{
    public class STrade
    {
        [DataMember(Name = "s")]
        public string Symbol { get; set; }

        [DataMember(Name = "p")]
        public decimal Price { get; set; }

        [DataMember(Name = "q")]
        public decimal Quantity { get; set; }

        [DataMember(Name = "m")]
        public bool IsBuyer { get; set; }

        [DataMember(Name = "T")]
        public long TradeTime { get; set; }
    }
}
