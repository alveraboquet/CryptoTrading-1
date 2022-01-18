using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExchangeModels;
using ExchangeModels.BinanceFutures;

namespace Utilities
{
    public static class BinanceConverter
    {
        public static STrade DeserializeBinanceTrade(byte[] data)
        {
            string json = Encoding.ASCII.GetString(data);
            STrade trade = new();
            string[] items = json.Split(',');

            string symbol = items[2];
            string price = items[4];
            string quantity = items[5];
            string time = items[8];
            string isBuyer = items[9];

            trade.Symbol = symbol[5..^1];
            trade.Price = decimal.Parse(price[5..^1]);
            trade.Quantity = decimal.Parse(quantity[5..^1]);
            trade.TradeTime = long.Parse(time[4..]);
            trade.IsBuyer = isBuyer[4..].StartsWith('t');
            return trade;
        }

        public static STrade DeserializeBinanceFuturesUsdTrade(byte[] data)
        {
            string json = Encoding.ASCII.GetString(data);
            STrade trade = new();
            string[] items = json.Split(',');

            string symbol = items[4];
            string price = items[5];
            string quantity = items[6];
            string time = items[9];
            string isBuyer = items[10];

            
            trade.Symbol = symbol[5..^1];
            trade.Price = decimal.Parse(price[5..^1]);
            trade.Quantity = decimal.Parse(quantity[5..^1]);
            trade.TradeTime = long.Parse(time[4..]);
            trade.IsBuyer = isBuyer[4..].StartsWith('t');
            
            return trade;
        }

        public static SKline DeserializeBinanceFuturesUsdKline(byte[] json)
        {
            int count = json.Length;
            
            for (int i = 0; i < count; i++)
            {
                byte bit = json[i];
                if (bit.Equals(0x2c))
                {
                    i += 8;
                    int c = json.Length - (i + 1);
                    byte[] newJson= new byte[c];
                    Buffer.BlockCopy(json, i, newJson, 0, c);
                    return Utf8Json.JsonSerializer.Deserialize<SKline>(newJson);
                }
            }

            return null;
        }

        public static SDepthUpdate DeserializeBinanceFuturesUsdDepth(byte[] json)
        {
            int count = json.Length;

            for (int i = 0; i < count; i++)
            {
                byte bit = json[i];
                if (bit.Equals(0x2c))
                {
                    i += 8;
                    int c = json.Length - (i + 1);
                    byte[] newJson = new byte[c];
                    Buffer.BlockCopy(json, i, newJson, 0, c);
                    return Utf8Json.JsonSerializer.Deserialize<SDepthUpdate>(newJson);
                }
            }

            return null;
        }

        public static List<FundingRateUpdate> DeserializeBinanceFuturesUsdFundingRate(byte[] json)
        {
            int i = 34;
            int c = json.Length - (i + 1);
            byte[] newJson = new byte[c];
            Buffer.BlockCopy(json, i, newJson, 0, c);

            return Utf8Json.JsonSerializer.Deserialize<List<FundingRateUpdate>>(newJson);
        }

        public static LiquidationEvent DeserializeBinanceFuturesUsdLiquidation(byte[] json)
        {
            int i = 35;
            int c = json.Length - (i + 1);
            byte[] newJson = new byte[c];
            Buffer.BlockCopy(json, i, newJson, 0, c);

            return Utf8Json.JsonSerializer.Deserialize<LiquidationEvent>(newJson);
        }
    }
}