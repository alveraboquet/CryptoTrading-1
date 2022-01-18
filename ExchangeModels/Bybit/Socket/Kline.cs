using System.Runtime.Serialization;

namespace ExchangeModels.Bybit
{
    public class KlineMessage : ByBitSpotMessage
    {
        [DataMember(Name = "data")]
        public Candle[] Data { get; set; }
        public Candle Candle => Data[0];
    }

    public class Candle
    {
        [DataMember(Name = "t")]
        public long OpenTime { get; set; }
        [DataMember(Name = "s")]
        public string Symbol { get; set; }
        [DataMember(Name = "sn")]
        public string SymbolName { get; set; }
        [DataMember(Name = "c")]
        public decimal ClosePrice { get; set; }
        [DataMember(Name = "h")]
        public decimal HighPrice { get; set; }
        [DataMember(Name = "l")]
        public decimal LowPrice { get; set; }
        [DataMember(Name = "o")]
        public decimal OpenPrice { get; set; }
        [DataMember(Name = "v")]
        public decimal Volume { get; set; }
    }
}