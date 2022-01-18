using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChainViewAPI.Models
{
    public class ResSymbolInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("exchange")]
        public string Exchange { get; set; }
        [JsonPropertyName("timeFrames")]
        public List<TimeFrameOption> TimeFrames { get; set; }

        [JsonPropertyName("quoteAssetPrecision")]
        public int QuoteAssetPrecision { get; set; }
        [JsonPropertyName("timezoneDailyCloseFormat")]
        public string TimezoneDailyCloseFormat { get; set; }
        [JsonPropertyName("isListed")]
        public bool IsListed { get; set; }
        [JsonPropertyName("isLinechart")]
        public bool IsLinechart { get; set; }
        [JsonPropertyName("isAvailableVolume")]
        public bool IsAvailableVolume { get; set; }
        [JsonPropertyName("isAvailableHeatmap")]
        public bool IsAvailableHeatmap { get; set; }
        [JsonPropertyName("isAvailableFootprint")]
        public bool IsAvailableFootprint { get; set; }
    }

    public class TimeFrameOption
    {
        [JsonPropertyName("timeFrame")]
        public string TimeFrame { get; set; }
        [JsonPropertyName("startTimeMax")]
        public long StartTimeMax { get; set; }
        [JsonPropertyName("endTimeMax")]
        public long EndTimeMax { get; set; }
    }
}
