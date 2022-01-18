using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExchangeModels
{
    public class SDepthUpdate
    {
        [DataMember(Name = "s")]
        public string Symbol { get; set; }

        //[DataMember(Name = "u")]
        //public long FinalUpdateID { get; set; }

        [DataMember(Name = "a")]
        public decimal[][] Asks { get; set; }

        [DataMember(Name = "b")]
        public decimal[][] Bids { get; set; }
    }
}
