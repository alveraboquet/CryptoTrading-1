using System.Runtime.Serialization;

namespace ExchangeModels.Bybit.API
{
    public class BybitFuturesSymbol
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "alias")] public string Alias { get; set; }
        [DataMember(Name = "status")] public string Status { get; set; }
        [DataMember(Name = "base_currency")] public string BaseCurrency { get; set; }
        [DataMember(Name = "quote_currency")] public string QuoteCurrency { get; set; }
        [DataMember(Name = "price_scale")] public int QuoteAssetPrecision { get; set; }
        [DataMember(Name = "taker_fee")] public decimal TakerFee { get; set; }
        [DataMember(Name = "maker_fee")] public decimal MakerFee { get; set; }
        [DataMember(Name = "leverage_filter")] public BybitFuturesLeverageFilter LeverageFilter { get; set; }
        [DataMember(Name = "price_filter")] public BybitFuturesPriceFilter PriceFilter { get; set; }
        [DataMember(Name = "lot_size_filter")] public BybitFuturesLotSizeFilter LotSizeFilter { get; set; }

        public bool IsListed() => Status == "Trading";
    }

    public class BybitFuturesLeverageFilter
    {
        [DataMember(Name = "min_leverage")] public int MinLeverage { get; set; }
        [DataMember(Name = "max_leverage")] public int MaxLeverage { get; set; }
        [DataMember(Name = "leverage_step")] public decimal LeverageStep { get; set; }
    }

    public class BybitFuturesPriceFilter
    {
        [DataMember(Name = "min_price")] public decimal MinPrice { get; set; }
        [DataMember(Name = "max_price")] public decimal MaxPrice { get; set; }
        [DataMember(Name = "tick_size")] public decimal TickSize { get; set; }
    }

    public class BybitFuturesLotSizeFilter
    {
        [DataMember(Name = "max_trading_qty")] public decimal MaxTradingQuantity { get; set; }
        [DataMember(Name = "min_trading_qty")] public decimal MinTradingQuantity { get; set; }
        [DataMember(Name = "qty_step")] public decimal QuantityStep { get; set; }
    }
}