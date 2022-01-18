using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer
{
    public class BaseOrder
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }

    public class AskOrder : BaseOrder
    { }

    public class BidOrder : BaseOrder
    { }
}
