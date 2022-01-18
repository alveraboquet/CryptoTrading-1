using System;
using System.Runtime.Serialization;

namespace ExchangeModels.BybitFutures
{
    public class BybitInstrumentInfoSnapshot
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }
        [DataMember(Name = "symbol")]
        public string Symbol { get; set; }
        [DataMember(Name = "predicted_funding_rate_e6")]
        public decimal PredictedFundingRate { get; set; }
        [DataMember(Name = "next_funding_time")]
        public DateTime NextFundingTime { get; set; }
    }

    public class BybitInstrumentInfoData
    {
        [DataMember(Name = "update")]
        public BybitInstrumentInfoUpdate[] Update { get; set; }
    }

    public class BybitInstrumentInfoUpdate
    {
        [DataMember(Name = "predicted_funding_rate_e6")]
        public decimal? PredictedFundingRate { get; set; }
        [DataMember(Name = "next_funding_time")]
        public long NextFundingTime { get; set; }
        [DataMember(Name = "symbol")]
        public string Symbol { get; set; }
    }
}