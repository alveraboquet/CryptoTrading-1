using ExchangeModels.BinanceFutures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace WebSocket
{
    public class AllfundsSnapshot
    {
        public AllfundsSnapshot(int chanId, List<FundingRateUpdate> data)
        {
            this.ChanId = chanId;
            Data = data;
        }
        public string Event { get; } = "snapshot";
        public int ChanId { get; set; }
        public List<FundingRateUpdate> Data { get; set; }

        public string ToJson()
        {
            return $"{{\"event\":\"snapshot\",\"chanId\":{ChanId},\"data\":{GetDataJson()}}}";
        }

        private string GetDataJson()
        {
            return GetDataJson(this.Data);
        }

        public static string GetDataJson(List<FundingRateUpdate> data)
        {
            StringBuilder json = new("[");

            foreach (var entry in data)
            {
                json.Append($"[\"{entry.Symbol}\",{entry.Rate.G29()}],");
            }

            if (data.Any())
                json = json.Remove(json.Length - 1, 1); // remove the last ','

            json.Append(']');

            return json.ToString();
        }

        public static string GetDataJson(Dictionary<string, decimal> data)
        {
            StringBuilder json = new("[");

            foreach (var entry in data)
                json.Append($"[\"{entry.Key}\",{entry.Value.G29()}],");

            if (data.Any())
                json = json.Remove(json.Length - 1, 1); // remove the last ','

            json.Append(']');

            return json.ToString();
        }
    }
}
