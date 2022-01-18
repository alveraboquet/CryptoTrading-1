using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace DataLayer.Models.Stream
{
    public class StreamingOrderBook : IDisposable
    {
        [JsonPropertyName("s")]
        public string Symbol { get; set; }

        /// <summary>
        /// key: price | value: quantity
        /// </summary>
        [JsonPropertyName("a")]
        public ConcurrentDictionary<decimal, decimal> Asks {get; set; }

        /// <summary>
        /// key: price | value: quantity
        /// </summary>
        [JsonPropertyName("b")]
        public ConcurrentDictionary<decimal, decimal> Bids { get; set; }


        public StreamingOrderBook Clone()
        {
            return new StreamingOrderBook
            {
                Symbol = this.Symbol,
                Asks =  new ConcurrentDictionary<decimal, decimal>(this.Asks.ToArray()),
                Bids = new ConcurrentDictionary<decimal, decimal>(this.Bids.ToArray()),
            };
        }

        public void Dispose()
        {
            this.Bids.Clear();

            this.Asks.Clear();
        }
    }
}
