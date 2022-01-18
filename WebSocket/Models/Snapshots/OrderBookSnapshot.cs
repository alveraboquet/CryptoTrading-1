using DataLayer;
using DataLayer.Models.Stream;
using ExchangeModels.BinanceFutures;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace WebSocket
{
    public class OrderBookSnapshot
    {
        public OrderBookSnapshot(int chanId, StreamingOrderBook data)
        {
            this.ChanId = chanId;
            Data = data;
        }

        public string Event { get; } = "snapshot";
        public int ChanId { get; set; }
        public StreamingOrderBook Data { get; set; }

        public string ToJson()
        {
            return $"{{\"event\":\"snapshot\",\"chanId\":{ChanId},\"data\":{GetDataJson()}}}";
        }

        private string GetDataJson()
        {
            StringBuilder json = new("[[");

            // Bids
            foreach (KeyValuePair<decimal, decimal> entry in Data.Bids)
            {
                json.Append($"[{entry.Key.G29()},{entry.Value.G29()}],");
            }
            json = json.Remove(json.Length - 1, 1);

            json.Append("],[");

            // Asks
            foreach (KeyValuePair<decimal, decimal> entry in Data.Asks)
            {
                json.Append($"[{entry.Key.G29()},{entry.Value.G29()}],");
            }
            json = json.Remove(json.Length - 1, 1);
            json.Append("]]");

            return json.ToString();
        }
    }
}