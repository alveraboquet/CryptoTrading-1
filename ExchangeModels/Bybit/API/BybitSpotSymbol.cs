using System.Runtime.Serialization;

namespace ExchangeModels.Bybit.API
{
    public class BybitSpotSymbol
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "alias")]
        public string Alias { get; set; }
        [DataMember(Name = "baseCurrency")]
        public string BaseCurrency { get; set; }
        [DataMember(Name = "quoteCurrency")]
        public string QuoteCurrency { get; set; }
        [DataMember(Name = "basePrecision")]
        public decimal BasePrecision { get; set; }
        [DataMember(Name = "quotePrecision")]
        public decimal QuotePrecision { get; set; }
        [DataMember(Name = "minTradeQuantity")]
        public decimal MinTradeQuantity { get; set; }
        [DataMember(Name = "minTradeAmount")]
        public decimal MinTradeAmount { get; set; }
        [DataMember(Name = "maxTradeQuantity")]
        public decimal MaxTradeQuantity { get; set; }
        [DataMember(Name = "maxTradeAmount")]
        public decimal MaxTradeAmount { get; set; }
        [DataMember(Name = "minPricePrecision")]
        public decimal MinPricePrecision { get; set; }
    }
}