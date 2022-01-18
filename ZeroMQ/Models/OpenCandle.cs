using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utf8Json;
using System.Runtime.Serialization;
using DataLayer;

namespace ZeroMQ
{
    public class OpenCandle : IToJson
    {
        // info
        [DataMember(Name = "s")]
        public string Symbol { get; set; }
        [DataMember(Name = "t")]
        public string Timeframe { get; set; }

        // data
        [DataMember(Name = "ot")]
        public long OpenTime { get; set; }
        [DataMember(Name = "o")]
        public decimal Open { get; set; }
        [DataMember(Name = "h")]
        public decimal High { get; set; }
        [DataMember(Name = "l")]
        public decimal Low { get; set; }
        [DataMember(Name = "c")]
        public decimal Close { get; set; }
        [DataMember(Name = "v")]
        public decimal Volume { get; set; }

        public string ToJson()
        {
            return $"[{OpenTime},{Open.G29()},{High.G29()},{Low.G29()},{Close.G29()},{Volume.G29()}]";
        }

        public static explicit operator OpenCandle(DataLayer.Candle candle)
        {
            if (candle == null)
                return null;
            return new OpenCandle()
            {
                OpenTime = candle.OpenTime,
                Open = candle.OpenPrice,
                High = candle.HighPrice,
                Low = candle.LowPrice,
                Close = candle.ClosePrice,
                Volume = candle.Volume,
                //Exchange = candle.Exchange,
                Symbol = candle.Symbol,
                Timeframe = candle.TimeFrame,
            };
        }

        public static explicit operator DataLayer.Candle(OpenCandle candle)
        {
            if (candle == null)
                return null;

            return new DataLayer.Candle(
                openTime: candle.OpenTime,
                open: candle.Open,
                high: candle.High,
                low: candle.Low,
                close: candle.Close,
                volume: candle.Volume,
                timeframe: candle.Timeframe,
                symbol: candle.Symbol);
        }

        public static explicit operator ResCandle(OpenCandle candle)
        {
            if (candle == null)
                return null;
            return new ResCandle()
            {
                OpenTime = candle.OpenTime,
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume
            };
        }
    }
}
