using ExchangeModels.Bybit.API;
using Utilities;

namespace ExchangeServices.ExtensionMethods
{
    public static class BybitExtensions
    {
        // 1m (1), 5m (5), 15m (15), 30m (30), 1h (60), 2h (120), 4h (240), 6h (360), 1D (D)
        public static string ToBybitPerpetualTimeframe(this string timeframe) =>
            timeframe switch
            {
                "1m" => "1",
                "5m" => "5",
                "15m" => "15",
                "30m" => "30",
                "1H" => "60",
                "2H" => "120",
                "4H" => "240",
                "6H" => "360",
                "1D" => "D",
                _ => null
            };

        public static string ToStandardTimeframe(this string timeframe) =>
            timeframe switch
            {
                "1" => "1m",
                "5" => "5m",
                "15" => "15m",
                "30" => "30m",
                "60" => "1H",
                "120" => "2H",
                "240" => "4H",
                "360" => "6H",
                "D" => "1D",
                _ => null
            };

        public static int GetQuoteAssetPrecision(this BybitSpotSymbol symbol)
        {
            var arr = symbol.MinPricePrecision.G29().Split('.');
            var precision = (arr.Length > 1) ? arr[1].Length : 0;
            return precision;
        }
        
        public static string GetFundingRateSymbolName(this BybitFuturesSymbol symbol) => $"FR.{symbol.Name}";
        
        public static void GetLiquidationSymbolNames(this BybitFuturesSymbol symbol, out string name, out string sellName, out string buyName)
        {
            name = $"LIQ.{symbol.Name}";
            sellName = $"LIQSELL.{symbol.Name}";
            buyName = $"LIQBUY.{symbol.Name}";
        }
        public static string GetOpenIntersetSymbolName(this BybitFuturesSymbol symbol) => $"OI.{symbol.Name}";
    }
}