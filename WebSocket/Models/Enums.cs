using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocket
{
    public enum Channel
    {
        Trades,
        OrderBook,
        Candles,
        AllFunds
    }

    public enum Event
    {
        Subscribe = 1,
        Auth = 2,
        Unsubscribe = 3
    }
}
