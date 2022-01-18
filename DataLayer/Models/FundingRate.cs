using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class FundingRate
    {
        private FundingRate() { }
        public FundingRate(decimal open)
        {
            this.Open = this.High = this.Low = this.Close = open;
        }

        [BsonElement("o")]
        public decimal Open { get; set; }

        [BsonElement("h")]
        public decimal High { get; set; }
        [BsonElement("l")]
        public decimal Low { get; set; }

        [BsonElement("c")]
        public decimal Close { get; set; }

        public FundingRate Clone() =>
            new FundingRate()
            {
                Open = this.Open,
                High = this.High,
                Low = this.Low,
                Close = this.Close,
            };
    }
}