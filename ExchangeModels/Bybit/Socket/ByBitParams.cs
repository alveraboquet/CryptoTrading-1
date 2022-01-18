using System.Runtime.Serialization;

namespace ExchangeModels.Bybit
{
    public class ByBitParams
    {
        [DataMember(Name = "realtimeInterval")]
        public string RealtimeInterval { get; set; }
        [DataMember(Name = "klineType")]
        public string KlineType { get; set; }
    }
}