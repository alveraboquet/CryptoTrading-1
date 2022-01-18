using System;
using System.Runtime.Serialization;
using ExchangeModels.BinanceFutures;

namespace ExchangeModels.BybitFutures
{
    public class BybitFuturesTrade
    {
        [DataMember(Name = "symbol")] public string Symbol { get; set; }

        [DataMember(Name = "tick_direction")] public string TickDirection { get; set; }

        [DataMember(Name = "price")] public decimal Price { get; set; }

        [DataMember(Name = "size")] public decimal Size { get; set; }

        [DataMember(Name = "timestamp")] public DateTime Timestamp { get; set; }

        [DataMember(Name = "trade_time_ms")] public long TradeTimeMs { get; set; }

        [DataMember(Name = "side")] public string Side { get; set; }

        [DataMember(Name = "trade_id")] public string TradeId { get; set; }

        public TradeSide GetSide() => Side switch
        {
            "Sell" => TradeSide.SELL,
            "Buy" => TradeSide.BUY,
            _ => TradeSide.BUY
        };
    }

    public class BybitFuturesUsdtTrade
    {
        [DataMember(Name = "symbol")] public string Symbol { get; set; }
        [DataMember(Name = "tick_direction")] public string TickDirection { get; set; }
        [DataMember(Name = "price")] public decimal Price { get; set; }
        [DataMember(Name = "size")] public decimal Size { get; set; }
        [DataMember(Name = "timestamp")] public DateTime Timestamp { get; set; }
        [DataMember(Name = "trade_time_ms")] public string TradeTimeMs { get; set; }
        [DataMember(Name = "side")] public string Side { get; set; }
        [DataMember(Name = "trade_id")] public string TradeId { get; set; }

        public BybitFuturesTrade GetTrade() => new BybitFuturesTrade
        {
            Symbol = Symbol,
            TickDirection = TickDirection,
            Price = Price,
            Size = Size,
            Timestamp = Timestamp,
            TradeTimeMs = Convert.ToInt64(TradeTimeMs),
            Side = Side,
            TradeId = TradeId
        };
        
        public TradeSide GetSide() => Side switch
        {
            "Sell" => TradeSide.SELL,
            "Buy" => TradeSide.BUY,
            _ => TradeSide.BUY
        };
    }
}