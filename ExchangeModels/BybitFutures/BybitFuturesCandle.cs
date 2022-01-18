using System.Runtime.Serialization;

namespace ExchangeModels.BybitFutures
{
    public class BybitFuturesCandle
    {
        [DataMember(Name = "start")]
        public long Start { get; set; }
        
        [DataMember(Name = "end")]
        public long End { get; set; }
        
        [DataMember(Name = "open")]
        public decimal Open { get; set; }
        
        [DataMember(Name = "close")]
        public decimal Close { get; set; }
        
        [DataMember(Name = "high")]
        public decimal High { get; set; }
        
        [DataMember(Name = "low")]
        public decimal Low { get; set; }
        
        [DataMember(Name = "volume")]
        public decimal Volume { get; set; }
        
        [DataMember(Name = "turnover")]
        public decimal Turnover { get; set; }
        
        [DataMember(Name = "confirm")]
        public bool Confirm { get; set; }
        
        [DataMember(Name = "cross_seq")]
        public long CrossSeq { get; set; }
        
        [DataMember(Name = "timestamp")]
        public long Timestamp { get; set; }
    }
}