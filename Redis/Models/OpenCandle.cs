using System.Text.Json.Serialization;

namespace Redis
{
    public class OpenCandle
    {
        // info
        [JsonPropertyName("e")]
        public string Exchange { get; set; }
        [JsonPropertyName("s")]
        public string Symbol { get; set; }
        [JsonPropertyName("t")]
        public string Timeframe { get; set; }

        // data
        [JsonPropertyName("ot")]
        public long OpenTime { get; set; }
        [JsonPropertyName("o")]
        public decimal Open { get; set; }
        [JsonPropertyName("h")]
        public decimal High { get; set; }
        [JsonPropertyName("l")]
        public decimal Low { get; set; }
        [JsonPropertyName("c")]
        public decimal Close { get; set; }
        [JsonPropertyName("v")]
        public decimal Volume { get; set; }

        public static explicit operator OpenCandle(DataLayer.Candle candle)
        {
            return new OpenCandle()
            {
                OpenTime = candle.OpenTime,
                Open = candle.OpenPrice,
                High = candle.HighPrice,
                Low = candle.LowPrice,
                Close = candle.ClosePrice,
                Volume = candle.Volume,
                Exchange = candle.Exchange,
                Symbol = candle.Symbol,
                Timeframe = candle.TimeFrame,
            };
        }
    }
}
