using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExchangeModels
{
    public class SKline
    {
        [DataMember(Name = "s")]
        public string Symbol { get; set; }
        [DataMember(Name = "k")]
        public CandleStick Candle { get; set; }
    }

    public class CandleStick
    {
        [DataMember(Name = "t")]
        public long OpenTime { get; set; }

        [DataMember(Name = "i")]
        public string Interval
        {
            get => _interval ?? "";

            set
            {
                switch (value)
                {
                    case "1m":
                    case "5m":
                    case "15m":
                    case "30m":
                        this._interval = value;
                        break;
                    default:
                        this._interval = value.ToUpper();
                        break;
                }
            }
        }
        private string _interval;

        [DataMember(Name = "o")]
        public decimal OpenPrice { get; set; }

        [DataMember(Name = "c")]
        public decimal ClosePrice { get; set; }

        [DataMember(Name = "h")]
        public decimal HighPrice { get; set; }

        [DataMember(Name = "l")]
        public decimal LowPrice { get; set; }

        [DataMember(Name = "v")]
        public decimal Volume { get; set; }
    }
}