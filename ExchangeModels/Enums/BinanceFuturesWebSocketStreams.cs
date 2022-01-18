using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeModels.Enums
{
    public enum BinanceFuturesUsdWebSocketStreams
    {
        /// <summary>
        /// Force order for all symbols '!forceOrder@arr'
        /// </summary>
        AllLiquidation,
        /// <summary>
        /// Mark Price for all symbols '!markPrice@arr'
        /// </summary>
        AllFundingRates,

        Trade,
        Kline,
        Depth
    }
}