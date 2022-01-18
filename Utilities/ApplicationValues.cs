using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    public static class ApplicationValues
    {
        public const string BinanceName = "binance";
        public const string BinanceUsdName = "binancefutures";
        public const string BinanceCoinName = "binance-futures-coin";

        public const string BybitName = "bybit";
        public const string BybitFuturesName = "bybitfutures";

        public const string BitfinexName = "bitfinex";
        public const string BitmexName = "bitmex";
        public const string BitstampName = "bitstamp";
        public const string FTXName = "ftx";
        public const string CoinbaseName = "coinbase";


        public static bool IsValidExchange(string exchange)
        {
            return exchange switch
            {
                BinanceName or BinanceUsdName or BinanceCoinName or BybitName or BybitFuturesName or
                    BitfinexName or BitmexName or BitstampName or FTXName or CoinbaseName => true,
                _ => throw new Exception("Invalid exchange."),
            };
        }

        // if changed change here too. WebSocket/Models/Request.GetChannel()
        public static readonly string OrderBookChannel = "orderbook";
        public static readonly string CandlesChannel = "candles";
        public static readonly string TradesChannel = "trades";
    }
}
