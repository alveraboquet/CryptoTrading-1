using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace DataLayer
{
    public class FootPrints : IDisposable
    {
        private FootPrints()
        {  }

        public FootPrints(decimal openPrice)
        {
            this.Range = openPrice * 0.0003M;

            this.OpenPrice = openPrice;

            BelowMarketOrders = new List<decimal[]>();
            AboveMarketOrders = new List<decimal[]>();
            BelowMarketOrders.Add(new decimal[] {0, 0});
            AboveMarketOrders.Add(new decimal[] {0, 0});
        }

        [JsonPropertyName("OP")]
        [BsonElement("OP")]
        public decimal OpenPrice { get; set; }

        [JsonPropertyName("r")]
        [BsonElement("r")]
        public decimal Range { get; set; }

        [JsonPropertyName("above")]
        [BsonElement("above")]
        public List<decimal[]> AboveMarketOrders { get; set; }//ascending

        [JsonPropertyName("below")]
        [BsonElement("below")]
        public List<decimal[]> BelowMarketOrders { get; set; }//descending sort

        public FootPrints Clone()
        {
            return new FootPrints
            {
                Range = this.Range,
                OpenPrice = this.OpenPrice,
                AboveMarketOrders = new List<decimal[]>(this.AboveMarketOrders.ToList()),
                BelowMarketOrders = new List<decimal[]>(this.BelowMarketOrders.ToList()),
            };
        }

        public void Dispose()
        {
            this.AboveMarketOrders.Clear();
            this.BelowMarketOrders.Clear();
        }
    }
}
