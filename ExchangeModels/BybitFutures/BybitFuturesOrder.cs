using System.Runtime.Serialization;
using ExchangeModels.BinanceFutures;

namespace ExchangeModels.BybitFutures
{
    public class BybitFuturesOrder
    {
        [DataMember(Name = "price")]
        public decimal Price { get; set; }
        
        [DataMember(Name = "symbol")]
        public string Symbol { get; set; }
        
        // We dont need it, uncommenting Id property will cuz errors
        // the reason is on inverse perpetual its long and on USDT perpetual its string
        // [DataMember(Name = "id")]
        // public string Id { get; set; }
        
        [DataMember(Name = "side")]
        public string Side { get; set; }
        
        [DataMember(Name = "size")]
        public decimal Size { get; set; }
        
        
        public TradeSide GetSide() => Side switch
                {
                    "Sell" => TradeSide.SELL,
                    "Buy" => TradeSide.BUY,
                    _ => TradeSide.BUY
                };
    }
}