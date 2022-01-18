using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserModels
{
    public class ResUserSession
    {
        public long SessionId { get; set; }
        public string Type { get; set; }
        public string UserAgent { get; set; }
        public string IP { get; set; }
        public long LastLogin { get; set; }
    }

    public class ResUserInfo
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string StripeMid { get; set; }
        public int Plan { get; set; }
        public long PlanExpiration { get; set; }
        public bool AutoPlanRenew { get; set; }
    }
}
