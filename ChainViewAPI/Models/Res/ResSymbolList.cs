using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChainViewAPI.Models
{
    public class ResSymbolList
    {
        public string Exchange { get; set; }
        public string SymbolName { get; set; }
        public bool IsListed { get; set; }

        public string ToJson()
        {
            return $"[{Exchange}:{SymbolName}:{IsListed}]";
        }
    }
}
