using System.Runtime.Serialization;

namespace ExchangeModels.BybitFutures
{
    public class BybitOrderbookUpdate
    {
        [DataMember(Name = "delete")]
        public BybitFuturesOrder[] Delete { get; set; }

        [DataMember(Name = "update")]
        public BybitFuturesOrder[] Update { get; set; }

        [DataMember(Name = "insert")]
        public BybitFuturesOrder[] Insert { get; set; }
    }
}