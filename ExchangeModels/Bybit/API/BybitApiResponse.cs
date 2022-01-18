using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExchangeModels.Bybit.API
{
    public class BybitApiResponse<T>
    {
        [DataMember(Name = "ret_msg")]
        public string ReturnMessage { get; set; }
        [DataMember(Name = "result")]
        public T Result { get; set; }
    }
}