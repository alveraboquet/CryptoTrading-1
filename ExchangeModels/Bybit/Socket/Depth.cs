using System.Runtime.Serialization;

namespace ExchangeModels.Bybit
{
    public class DepthMessage : ByBitSpotMessage
    {
        [DataMember(Name = "data")]
        public DepthDataModel[] Data { get; set; }
    }

    public class DepthDataModel
    {
        [DataMember(Name = "e")]
        public int ExchangeId { get; set; }
        [DataMember(Name = "s")]
        public string Symbol { get; set; }
        [DataMember(Name = "t")]
        public long Timestamp { get; set; }
        [DataMember(Name = "v")]
        public string Version { get; set; }

        //  Bid prices & quantities in descending order (best price first)
        [DataMember(Name = "b")]
        public decimal[][] BidOrders { get; set; }

        // Ask prices & quantities in ascending order (best price first)
        [DataMember(Name = "a")]
        public decimal[][] AskOrders { get; set; } 
    }
}