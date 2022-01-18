using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace DataLayer
{
    public class ResHeatmap
    {
        [JsonPropertyName("blocks")]
        [DataMember(Name = "blocks")]
        public List<decimal> Blocks { get; set; }

        [JsonPropertyName("openPrice")]
        [DataMember(Name = "openPrice")]
        public decimal OpenPrice { get; set; }

        [JsonPropertyName("openTime")]
        [DataMember(Name = "openTime")]
        public long OpenTime { get; set; }


        private string _json = "";
        /// <summary>
        /// ar first call of this method, creates the json. after changing properties the json wont change. (for better performance)
        /// </summary>
        /// <returns>the json of this object</returns>
        public string GetJson()
        {
            if (_json.Equals(""))
                _json = System.Text.Json.JsonSerializer.Serialize(this);

            return _json;
        }
    }

    public class ResFootPrint
    {
        [JsonPropertyName("above")]
        [DataMember(Name = "above")]
        public List<decimal[]> AboveMarketOrders { get; set; } //ascending

        [JsonPropertyName("below")]
        [DataMember(Name = "below")]
        public List<decimal[]> BelowMarketOrders { get; set; } //descending sort

        [JsonPropertyName("openPrice")]
        [DataMember(Name = "openPrice")]
        public decimal OpenPrice { get; set; }

        [JsonPropertyName("openTime")]
        [DataMember(Name = "openTime")]
        public long OpenTime { get; set; }

        [JsonIgnore]
        private string _json = "";
        /// <summary>
        /// ar first call of this method, creates the json. after changing properties the json wont change. (for better performance)
        /// </summary>
        /// <returns>the json of this object</returns>
        public string GetJson()
        {
            if (_json.Equals(""))
                _json = System.Text.Json.JsonSerializer.Serialize(this);

            return _json;
        }
    }
}
