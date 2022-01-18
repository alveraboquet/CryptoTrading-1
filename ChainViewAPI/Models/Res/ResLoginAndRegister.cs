using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChainViewAPI.Models
{
    public class ResLoginAndRegister
    {
        [JsonPropertyName("account-id")]
        public int AccountId { get; set; }

        [JsonPropertyName("account-token")]
        public string AccountToken { get; set; }
    }
}
