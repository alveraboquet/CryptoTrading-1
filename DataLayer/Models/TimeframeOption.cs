using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer.Models
{
    [BsonIgnoreExtraElements]
    public class TimeFrameOption
    {
        public string TimeFrame { get; set; }
        public long? StartTimeMax { get; set; }
        public long? EndTimeMax { get; set; }
        public int GetSeconds()
        {
            return this.TimeFrame switch
            {
                "1m" => 60,
                "5m" => 300,
                "15m" => 900,
                "30m" => 1800,
                "1H" => 3600,
                "2H" => 7200,
                "4H" => 14400,
                "6H" => 21600,
                "12H" => 43200,
                "1D" => 86400,
                "3D" => 259200,
                _ => 60,
            };
        }

        public int GetMinutes()
        {
            return this.GetSeconds() / 60;
        }

        public int GetMiliseconds()
        {
            return this.GetSeconds() * 1000;
        }
    }
}
