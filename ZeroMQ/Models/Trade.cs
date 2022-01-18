using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Utilities;
using ExchangeModels.BinanceFutures;

namespace ZeroMQ
{
    public class Trade : IToJson
    {
        [DataMember(Name = "s")]
        public string Symbol { get; set; }

        // data
        [DataMember(Name = "T")]
        public string TradeTime { get; set; }
        [DataMember(Name = "p")]
        public string Price { get; set; }
        [DataMember(Name = "a")]
        public string Amount { get; set; }

        public string ToJson()
        {
            return $"[{TradeTime},{Price},{Amount}]";
        }


        public static explicit operator Trade(LiquidationUpdate liq)
        {
            if (liq == null)
                return null;

            var trade = new Trade()
            {
                Symbol = liq.Symbol,
                Price = liq.Price.ToString(),
                TradeTime = liq.TradeTime.ToString()
            };

            trade.Amount = (liq.Side) switch
            {
                TradeSide.BUY => liq.Quantity.ToString(),
                _ or TradeSide.SELL => $"-{liq.Quantity}"
            };

            return trade;
        }

        public static ZeroMQ.Trade DeserializeBinanceTrade(byte[] data)
        {
            string json = Encoding.ASCII.GetString(data);
            Trade trade = new();
            string[] items = json.Split(',');

            string symbol = items[2];
            string price = items[4];
            string quantity = items[5];
            string time = items[8];
            string isBuyer = items[9];
            trade.Symbol = symbol.Substring(5, symbol.Length - 6);
            trade.Price = price.Substring(5, price.Length - 6);
            trade.TradeTime = time.Substring(4, time.Length - 4);
            trade.Amount = isBuyer.Substring(4, isBuyer.Length - 4).StartsWith('t') ?
                $"-{quantity.Substring(5, quantity.Length - 6)}" :
                quantity.Substring(5, quantity.Length - 6);

            return trade;
        }

        public static ZeroMQ.Trade DeserializeBinanceFuturesUsdTrade(byte[] data)
        {
            string json = Encoding.ASCII.GetString(data);
            Trade trade = new();
            string[] items = json.Split(',');

            string symbol = items[4];
            string price = items[5];
            string quantity = items[6];
            string time = items[9];
            string isBuyer = items[10];

            trade.Symbol = symbol.Substring(5, symbol.Length - 6);
            trade.Price = price.Substring(5, price.Length - 6);
            trade.TradeTime = time.Substring(4, time.Length - 4);
            trade.Amount = isBuyer.Substring(4, isBuyer.Length - 4).StartsWith('t') ?
                $"-{quantity.Substring(5, quantity.Length - 6)}" :
                quantity.Substring(5, quantity.Length - 6);

            return trade;
        }
    }
}