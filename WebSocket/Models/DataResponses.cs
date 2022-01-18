using Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocket
{
    public class SubsequentResponse
    {
        public static string ToJson(int chanId, string json)
        {
            return $"[{chanId},{json}]";
        }
        public static string ToJson<T>(int chanId, T data) where T : ZeroMQ.IToJson
        {
            return ToJson(chanId, data.ToJson());
        }
    }

    public class SubsequentResponse<TData>
        where TData : ZeroMQ.IToJson
    {
        private SubsequentResponse(int chanId)
        {
            this.ChanId = chanId;
        }

        public SubsequentResponse(int chanId, TData data)
            : this(chanId)
        {
            this.Data = data;
        }


        public int ChanId { get; set; }
        public TData Data { get; }

        public string ToJson()
        {
            return SubsequentResponse.ToJson(this.ChanId, this.Data);
        }
    }
}