using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace DataLayer
{
    public class ResCandle
    {
        [DataMember(Name = "ot")]
        public long OpenTime { get; set; }
        [DataMember(Name = "o")]
        public decimal Open { get; set; }
        [DataMember(Name = "h")]
        public decimal High { get; set; }
        [DataMember(Name = "l")]
        public decimal Low { get; set; }
        [DataMember(Name = "c")]
        public decimal Close { get; set; }
        [DataMember(Name = "v")]
        public decimal Volume { get; set; }

        private string _json = "";
        /// <summary>
        /// ar first call of this method, creates the json. after changing properties the json wont change. (for better performance)
        /// </summary>
        /// <returns>the json of this object</returns>
        public string GetJson()
        {
            if (_json.Equals(""))
                _json = $"[{OpenTime},{G29(Open)},{G29(High)},{G29(Low)},{G29(Close)},{G29(Volume)}]";

            return _json;
        }

        private string G29(decimal num)
        {
            string val = num.ToString("0.##############################");
            return val.Replace(',', '.');
        }
    }
}