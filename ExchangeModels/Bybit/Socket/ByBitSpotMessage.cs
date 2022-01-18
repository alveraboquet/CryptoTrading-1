using System.Runtime.Serialization;

namespace ExchangeModels.Bybit
{
    public abstract class ByBitSpotMessage
    {
        [DataMember(Name = "symbol")]
        public string Symbol { get; set; }
        [DataMember(Name = "symbolName")]
        public string SymbolName { get; set; }
        [DataMember(Name = "topic")]
        public string Topic { get; set; }
        [DataMember(Name = "params")]
        public ByBitParams Params { get; set; }
        [DataMember(Name = "f")]
        public bool IsFirstMessage { get; set; }
        [DataMember(Name = "sendTime")]
        public long SendTime { get; set; }
        [DataMember(Name = "shared")]
        public bool IsShared { get; set; }
    }
}