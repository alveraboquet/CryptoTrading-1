using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Utilities;

namespace WebSocket
{
    public class Request
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        // subscribe & unsubscribe
        [JsonPropertyName("channel")]
        public string Channel { get; set; }
        [JsonPropertyName("key")]
        public string Key { get; set; }


        // authentication
        [JsonPropertyName("account-Id")]
        public int AccountId { get; set; }
        [JsonPropertyName("account-Token")]
        public string AccountToken { get; set; }

        public Event GetEvent()
        {
            if (string.IsNullOrWhiteSpace(this.Event)) 
                throw new Exception("Enter event");

            return this.Event switch
            {
                "auth" => WebSocket.Event.Auth,
                "subscribe" => WebSocket.Event.Subscribe,
                "unsubscribe" => WebSocket.Event.Unsubscribe,
                _ => throw new Exception("Invalid event"),
            };
        }

        public Channel GetChannel()
        {
            if (string.IsNullOrWhiteSpace(this.Channel)) throw new Exception("Enter channel");

            return this.Channel switch
            {
                "candle" => WebSocket.Channel.Candles,
                "trade" => WebSocket.Channel.Trades,
                "orderbook" => WebSocket.Channel.OrderBook,
                "allfunds" => WebSocket.Channel.AllFunds,
                _ => throw new Exception("Invalid channel")
            };
        }

        public static bool IsFrOrLiqPair(string pair)
        {
            return pair.StartsWith("FR.") ||
                   pair.StartsWith("LIQ.") ||
                   pair.StartsWith("LIQBUY.") ||
                   pair.StartsWith("LIQSELL.");
        }

        public (string ex, string pair, string timeFrame) GetKey()
        {
            var channel = GetChannel();
            string exchange;
            string symbol = null;
            string timeFrame = null;

            if (channel == WebSocket.Channel.AllFunds)
            {
                exchange = Key;
            }
            else
            {
                try
                {
                    var keyParts = this.Key.Split('.').ToList();
                    exchange = keyParts[0];

                    if (keyParts.Count > 2)
                        symbol = string.Join('.', keyParts.GetRange(1, 2)); // maximum 2 symbols ?
                    else
                        symbol = this.Key.Split('.')[1];

                    var symbolParts = symbol.Split(':');

                    if (symbolParts.Length > 2) throw new Exception("Invalid key");

                    if (channel == WebSocket.Channel.Candles)
                    {
                        try
                        {
                            symbol = symbolParts[0];
                            timeFrame = symbolParts[1];
                            timeFrame.ToBinanceAvailableTimeFrame();
                        }
                        catch
                        {
                            throw new Exception("Enter timeframe");
                        }
                    }
                    else
                    {
                        if (this.Key.Contains(':'))
                            throw new Exception("Invalid key");
                    }
                }
                catch
                {
                    throw new Exception("Invalid key");
                }
            }

            try
            {
                ApplicationValues.IsValidExchange(exchange);
            }
            catch (Exception)
            { throw; }

            return (exchange, symbol, timeFrame);
        }
    }
}
