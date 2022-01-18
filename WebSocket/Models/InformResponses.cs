using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebSocket
{
    public class FirstResponse
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        [JsonPropertyName("channel")]
        public string Channel { get; set; }
        [JsonPropertyName("key")]
        public string Key { get; set; }
        [JsonPropertyName("chanId")]
        public int ChanId { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class AuthenticationResponse
    {
        public bool Status { get; set; }

        public static string ToJson(bool status)
        {
            string value = (status) ? "confirmed" : "rejected";
            return $"{{\"status\":\"{value}\"}}";
        }

        public string ToJson()
        {
            return ToJson(this.Status);
        }
    }

    public class ErrorResponse
    {
        public ErrorResponse(int code, string message)
        {
            this.Message = message;
            this.Code = code;
        }

        [JsonPropertyName("event")]
        public string Event { get; } = "error";
        [JsonPropertyName("msg")]
        public string Message { get; set; }
        [JsonPropertyName("code")]
        public int Code { get; set; }
    }
}
