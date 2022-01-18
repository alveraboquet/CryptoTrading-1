using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
namespace DataLayer
{
    public class Heatmap : IDisposable
    {
        public Heatmap()
        { }

        public Heatmap(Mode mode, decimal openPrice)
        {
            this.OpenPrice = openPrice;
            this.Mode = mode;
            this.Range = GetPriceRange(mode, openPrice);


            int blocksCount = (int)(openPrice / this.Range);
            this.Blocks = new List<decimal>();
            for (int i = 0; i < blocksCount * 2; i++)
            {
                this.Blocks.Add(0);
            }
        }

        private decimal GetPriceRange(Mode mode, decimal lastCandleClosePrice)
        {
            switch (mode)
            {
                default:
                case Mode.HD:
                    return 0.01M * lastCandleClosePrice;
                case Mode.FULLHD:
                    return 0.005M * lastCandleClosePrice;
                case Mode.FOURK:
                    return 0.0025M * lastCandleClosePrice;
                case Mode.EightK:
                    return 0.00125M * lastCandleClosePrice;
            }
        }
        [BsonElement("OP")]
        public decimal OpenPrice { get; set; }
        [BsonElement("r")]
        public decimal Range { get; set; }
        [BsonElement("m")]
        public Mode Mode { get; set; }

        public List<decimal> Blocks { get; set; }

        public void Dispose()
        {
            this.Blocks.Clear();
        }
    }

    public enum Mode
    {
        HD = 0,
        FULLHD = 1,
        FOURK = 2,
        EightK = 3,
    }
}
