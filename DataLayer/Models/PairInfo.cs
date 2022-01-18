using DataLayer.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLayer
{ 
    [BsonIgnoreExtraElements]
    public class PairInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string PairId { get; set; }
        public string Symbol { get; set; }
        public string  Exchange { get; set; }
        public List<TimeFrameOption> TimeFrameOptions { get; set; }

        public int QuoteAssetPrecision { get; set; }
        public string TimezoneDailyCloseFormat { get; set; }
        public bool IsListed { get; set; }
        public bool IsLinechart { get; set; }
        public bool IsAvailableVolume { get; set; }
        public bool IsAvailableHeatmap { get; set; }
        public bool IsAvailableFootprint { get; set; }

        public override string ToString()
        {
            return $"{this.Exchange} | {this.Symbol} | {this.IsListed}";
        }

        public TimeFrameOption GetTimeFrameOrDefault(string timeframe)
        {
            return this.TimeFrameOptions.FirstOrDefault(p => p.TimeFrame == timeframe);
        }
    }
}
