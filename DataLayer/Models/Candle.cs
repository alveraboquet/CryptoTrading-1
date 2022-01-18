using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class Candle : IDisposable
    {
        public Candle(long openTime, decimal open, decimal high, decimal low, decimal close, decimal volume)
            : this(open, high, low, close, volume)
        {
            this.OpenTime = openTime;
        }

        public Candle(long openTime, decimal open, decimal high, decimal low, decimal close, decimal volume,
            string timeframe, string symbol) : this(openTime, open, high, low, close, volume)
        {
            this.Symbol = symbol;
            this.TimeFrame = timeframe;
        }

        public Candle(long openTime, decimal open, decimal high, decimal low, decimal close, decimal volume,
            string timeframe, string exchange, string symbol)
            : this(openTime, open, high, low, close, volume, timeframe, symbol)
        {
            this.Exchange = exchange;
        }

        public Candle(decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            this.OpenPrice = open;
            this.HighPrice = high;
            this.LowPrice = low;
            this.ClosePrice = close;
            this.Volume = volume;
        }

        public Candle(decimal openPrice, decimal highPrice, decimal lowPrice, decimal closePrice)
            : this(openPrice, highPrice, lowPrice, closePrice, 0)
        {
            this.FootPrint = new FootPrints(openPrice);

            this.FundingRate = null;
            this.Liquidation = null;
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonIgnore]
        public string Exchange { get; set; }
        [BsonIgnore]
        public string Symbol { get; set; }
        [BsonElement("TF")]
        public string TimeFrame { get; set; }

        [BsonElement("OT")]
        public long OpenTime { get; set; }

        [BsonElement("o")]
        public decimal OpenPrice { get; set; }
        [BsonElement("h")]
        public decimal HighPrice { get; set; }
        [BsonElement("l")]
        public decimal LowPrice { get; set; }
        [BsonElement("c")]
        public decimal ClosePrice { get; set; }
        [BsonElement("v")]
        public decimal Volume { get; set; }
        public Heatmap Heatmap8K { get; set; }
        public FootPrints FootPrint { get; set; }

        public FundingRate FundingRate { get; set; }
        public Liquidation Liquidation { get; set; }

        public Candle Clone() =>
            new Candle(this.OpenPrice, this.HighPrice, this.LowPrice, this.ClosePrice)
            {
                Id = this.Id,
                Exchange = this.Exchange,
                Symbol = this.Symbol,
                TimeFrame = this.TimeFrame,
                OpenTime = this.OpenTime,
                Volume = this.Volume,
                FundingRate = this.FundingRate?.Clone(),
                Liquidation = this.Liquidation?.Clone(),
            };

        public void Dispose()
        {
            this.Heatmap8K?.Dispose();
            this.FootPrint?.Dispose();
        }
    }
}