using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UserModels
{
    public class User
    {
        private const char SplitChar = ';';
        public User() { }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string StripeMid { get; set; }
        public int Plan { get; set; }
        public long PlanExpiration { get; set; }
        public bool AutoPlanRenew { get; set; }

        public string ChartSettings { get; set; }
        public string WatchList { get; internal set; }


        public string[] AddWatchList(string exchange, string symbol)
        {
            StringBuilder str = new(this.WatchList);
            var val = $"{exchange}:{symbol}";

            if (!(WatchList ?? "").Split(SplitChar, StringSplitOptions.RemoveEmptyEntries).Contains(val))
            {
                if (str.Length > 2) // its not empty
                {
                    str.Append($"{SplitChar}{val}");
                }
                else // its empty
                {
                    str.Append(val);
                }
                this.WatchList = str.ToString();
            }

            return WatchList.Split(SplitChar, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] RemoveWatchList(string exchange, string symbol)
        {
            if (string.IsNullOrWhiteSpace(this.WatchList))
                return Array.Empty<string>();
            var val = $"{exchange}:{symbol}";

            var wl = this.WatchList.Split(SplitChar, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (wl.Any(s => s == val))
            {
                wl.Remove(val);
                this.WatchList = string.Join(SplitChar, wl);
                return wl.ToArray();
            }

            return WatchList.Split(SplitChar, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetWatchList()
        {
            return GetWatchList(WatchList);
        }
        public static string[] GetWatchList(string watchList)
        {
            if (string.IsNullOrWhiteSpace(watchList))
                return Array.Empty<string>();
            return watchList.Split(SplitChar, StringSplitOptions.RemoveEmptyEntries);
        }


        #region Relations
        public virtual ICollection<UserSession> Sessions { get; set; }

        public virtual ICollection<Layer> Layers { get; set; }
        #endregion
    }

    public class UserSession
    {
        public UserSession()
        { }

        public long Id { get; set; }
        public string AccessToken { get; set; }
        public string Type { get; set; }
        public string UserAgent { get; set; }
        public string IP { get; set; }
        public long LastLogin { get; set; }

        #region Relations
        // User
        public int UserId { get; set; }
        public virtual User User { get; set; }


        #endregion
    }
}
