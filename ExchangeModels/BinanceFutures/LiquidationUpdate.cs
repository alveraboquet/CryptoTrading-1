using System.Runtime.Serialization;

namespace ExchangeModels.BinanceFutures
{
    public class LiquidationEvent
    {
        //[DataMember(Name = "E")]
        //public long EventTime { get; set; }

        [DataMember(Name = "o")]
        public LiquidationUpdate LiquidationUpdate { get; set; }
    }

    public class LiquidationUpdate
    {
        [DataMember(Name = "s")]
        public string Symbol { get; set; }

        [DataMember(Name = "S")]
        public TradeSide Side { get; set; }

        [DataMember(Name = "q")]
        public decimal Quantity { get; set; }

        [DataMember(Name = "p")]
        public decimal Price { get; set; }

        [DataMember(Name = "T")]
        public long TradeTime { get; set; }
    }

    public enum TradeSide
    {
        SELL,
        BUY
    }
}