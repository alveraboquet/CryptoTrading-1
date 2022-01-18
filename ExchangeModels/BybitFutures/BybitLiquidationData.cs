using System.Runtime.Serialization;
using ExchangeModels.BinanceFutures;

namespace ExchangeModels.BybitFutures
{
    public class BybitLiquidationData
    {
        [DataMember(Name = "symbol")]
        public string Symbol { get; set; }
        [DataMember(Name = "side")]
        public string Side { get; set; }
        [DataMember(Name = "price")]
        public decimal Price { get; set; }
        [DataMember(Name = "qty")] 
        public decimal Quantity { get; set; }
        [DataMember(Name = "time")] 
        public long Timestamp { get; set; }

        public TradeSide GetSide() => Side switch
        {
            "Sell" => TradeSide.SELL,
            "Buy" => TradeSide.BUY,
            _ => TradeSide.BUY
        };
    }
}