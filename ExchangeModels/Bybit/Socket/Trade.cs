using System;
using System.Runtime.Serialization;

namespace ExchangeModels.Bybit
{
    public class TradeMessage : ByBitSpotMessage
    {
        [DataMember(Name = "data")]
        public TradeDataModel[] Data { get; set; }
        public TradeDataModel Trade => this.Data[0];
    }

    public class TradeDataModel
    {
        [DataMember(Name = "v")]
        public string TransactionId { get; set; }
        [DataMember(Name = "t")]
        public long Timestamp { get; set; }
        [DataMember(Name = "p")]
        public decimal Price { get; set; }
        [DataMember(Name = "q")]
        public decimal Quantity { get; set; }
        // [DataMember(Name = "m")]
        // public string IsBuyAsString { get; set; } // True indicates buy order, false indicates sell order
        // public bool IsBuy => Convert.ToBoolean(IsBuyAsString);
        
        [DataMember(Name = "m")]
        public bool IsBuy { get; set; } // True indicates buy order, false indicates sell order

        public string Symbol { get; set; }
    }
}