namespace ZeroMQ
{
    public class BinanceZeroMQProperties : ZeroMQProperties
    {
        // websocket Binance
        public int BinanceTradePort { get; set; }
        public int BinanceOrderbookPort { get; set; }
        public int BinanceCandlePort { get; set; }

        // websocket Binance Futures Usd
        public int BinanceFuturesUsdTradePort { get; set; }
        public int BinanceFuturesUsdOrderbookPort { get; set; }
        public int BinanceFuturesUsdCandlePort { get; set; }

        // websocket Binance Futures Usd Fr-Liq
        public int BinanceFuturesUsdLiqTradePort { get; set; }
        public int BinanceFuturesUsdLiqCandlePort { get; set; }
        public int BinanceFuturesUsdFrCandlePort { get; set; }
        public int BinanceFuturesUsdAllfundsPort { get; set; }

        // Api Binance
        public int BinanceCandleApiPort { get; set; }
        public int BinanceHeatmapApiPort { get; set; }
        public int BinanceFootprintApiPort { get; set; }

        // Api Binance Futures Usd
        public int BinanceFuturesUsdCandleApiPort { get; set; }
        public int BinanceFuturesUsdHeatmapApiPort { get; set; }
        public int BinanceFuturesUsdFootprintApiPort { get; set; }

        // Api Binance Futures Usd Fr Liq
        public int BinanceFuturesUsdFrCandlesApiPort { get; set; }
        public int BinanceFuturesUsdLiqCandlesApiPort { get; set; }
        
    }
}