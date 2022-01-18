using DataLayer;
using DataLayer.Models.Stream;
using ExchangeModels.BinanceFutures;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace WebSocket
{
    public class CandleSnapshot
    {
        public CandleSnapshot(int chanId, FootPrints footprint, ZeroMQ.OpenCandle candle)
        {
            this.ChanId = chanId;
            this.Candle = candle;
            this.FootPrint = footprint;
        }

        public string Event { get; } = "snapshot";
        public int ChanId { get; set; }

        public DataLayer.FootPrints FootPrint { get; set; }
        public ZeroMQ.OpenCandle Candle { get; set; }

        public string ToJson()
        {
            return $"{{\"event\":\"snapshot\",\"chanId\":{ChanId},\"candle\":{Candle.ToJson()}, \"footprint\":{GetDataJson()}}}";
        }

        private string GetDataJson()
        {
            if (FootPrint == null) return "null";

            StringBuilder json = new StringBuilder($"[{FootPrint.OpenPrice.G29()},{FootPrint.Range.G29()},[");
            // Above
            for (int i = 0; i < FootPrint.AboveMarketOrders.Count; i++)
            {
                var item = FootPrint.AboveMarketOrders[i];
                json.Append($"[{item[0].G29()},{item[1].G29()}],");
            }

            json = json.Remove(json.Length - 1, 1);

            json.Append("],[");

            // Below
            for (int i = 0; i < FootPrint.BelowMarketOrders.Count; i++)
            {
                var item = FootPrint.BelowMarketOrders[i];
                json.Append($"[{item[0].G29()},{item[1].G29()}],");
            }
            json = json.Remove(json.Length - 1, 1);
            json.Append("]]");

            return json.ToString();
        }
    }
}