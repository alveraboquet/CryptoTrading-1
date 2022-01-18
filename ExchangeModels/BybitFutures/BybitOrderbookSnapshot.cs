using System.Runtime.Serialization;

namespace ExchangeModels.BybitFutures
{
    public class BybitOrderbookSnapshot
    {
        [DataMember(Name = "order_book")]
        public BybitFuturesOrder[] Orderbook { get; set; }
    }
}