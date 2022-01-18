using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class Liquidation
    {
        public decimal LiqBuy { get; set; }
        public decimal LiqSell { get; set; }
        public decimal Liq { get; set; }

        public Liquidation Clone() =>
            new Liquidation()
            {
                Liq = this.Liq,
                LiqBuy = this.LiqBuy,
                LiqSell = this.LiqSell
            };
    }
}