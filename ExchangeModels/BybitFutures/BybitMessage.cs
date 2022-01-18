using System.Runtime.Serialization;

namespace ExchangeModels.BybitFutures
{
    public class BybitMessage<TData>
    {
        [DataMember(Name = "topic")]
        public string Topic { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; } // may be null, use it carefully
        [DataMember(Name = "data")]
        public TData Data { get; set; }
    }
}