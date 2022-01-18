using Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChainViewAPI.Models
{
    public class ResCandle
    {
        public ResCandle(DataLayer.ResCandle candle)
        {
            this.OpenTime = candle.OpenTime;

            _json = $"[{OpenTime},{candle.Open.G29()},{candle.High.G29()},{candle.Low.G29()},{candle.Close.G29()},{candle.Volume.G29()}],";
        }

        public long OpenTime { get; private set; }

        private string _json;
        public string Json { get => _json; }
    }

    public class ResFootprint
    {
        public ResFootprint(DataLayer.ResFootPrint footprint)
        {
            this.OpenTime = footprint.OpenTime;

            _json = System.Text.Json.JsonSerializer.Serialize(footprint) + ',';
        }

        public long OpenTime { get; private set; }

        private string _json;
        public string Json { get => _json; }
    }

    public class ResHeatmap
    {
        public ResHeatmap(DataLayer.ResHeatmap heatmap)
        {
            this.OpenTime = heatmap.OpenTime;

            this._8kJson = System.Text.Json.JsonSerializer.Serialize(heatmap) + ',';
            this._4kJson = System.Text.Json.JsonSerializer.Serialize(heatmap.Convert8KToHeatmap(DataLayer.Mode.FOURK)) + ',';
            this._fullHdJson = System.Text.Json.JsonSerializer.Serialize(heatmap.Convert8KToHeatmap(DataLayer.Mode.FULLHD)) + ',';
            this._hdJson = System.Text.Json.JsonSerializer.Serialize(heatmap.Convert8KToHeatmap(DataLayer.Mode.HD)) + ',';
        }

        public long OpenTime { get; private set; }

        private string _hdJson;
        private string _fullHdJson;
        private string _4kJson;
        private string _8kJson;
        public string GetJson(DataLayer.Mode mode)
        {
            return mode switch
            {
                DataLayer.Mode.HD => _hdJson,
                DataLayer.Mode.FULLHD => _fullHdJson,
                DataLayer.Mode.FOURK => _4kJson,
                DataLayer.Mode.EightK => _8kJson,
                _ => _hdJson,
            };
        }
    }
}
