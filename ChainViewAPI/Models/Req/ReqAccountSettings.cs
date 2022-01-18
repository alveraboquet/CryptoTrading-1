using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChainViewAPI.Models
{
    public class ReqChangePassword
    {
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
    public class ReqChangeEmail
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }
    }

    public class ReqChangeUserName
    {
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
    }

    public abstract class ReqAccountSettings
    {
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
