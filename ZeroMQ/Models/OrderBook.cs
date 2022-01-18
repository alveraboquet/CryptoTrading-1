using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Utilities;

namespace ZeroMQ
{
    //public class Order : IToJson
    //{
    //    [DataMember(Name = "p")]
    //    public decimal Price { get; set; }
    //    [DataMember(Name = "b")]
    //    public bool BuyerIsMaker { get; set; }
    //    [DataMember(Name = "a")]
    //    public decimal Amount { get; set; }
    //    public string ToJson()
    //    {
    //        return $"[{Price.G29()},{Amount.G29()}]";
    //    }
    //}

    public class OrderBook : IToJson
    {
        public OrderBook()
        {
        }
        // info
        //[DataMember(Name = "e")]
        //public string Exchange { get; set; }
        [DataMember(Name = "s")]
        public string Symbol { get; set; }

        // data
        [DataMember(Name = "a")]
        public decimal[][] Asks { get; set; }

        [DataMember(Name = "b")]
        public decimal[][] Bids { get; set; }

        public string ToJson()
        {
            StringBuilder json = new StringBuilder("[[");
            for (int i = 0; i < Bids.Length; i++)
            {
                json.Append($"[{Bids[i][0].G29()},{Bids[i][1].G29()}],");
            }
            if (json.ToString().EndsWith(','))
                json = json.Remove(json.Length - 1, 1);

            json.Append("],[");

            for (int i = 0; i < Asks.Length; i++)
            {
                json.Append($"[{Asks[i][0].G29()},{Asks[i][1].G29()}],");
            }
            if (json.ToString().EndsWith(','))
                json = json.Remove(json.Length - 1, 1);

            json.Append("]]");
            return json.ToString();
        }
    }
}
