namespace ZeroMQ
{
    public class BybitZeroMQProperties : ZeroMQProperties
    {
        // websocket Bybit
        public int BybitTradePort { get; set; }
        public int BybitOrderbookPort { get; set; }
        public int BybitCandlePort { get; set; }
        
        // websocket Bybit Futures Fr-Liq
        public int BybitFuturesLiqTradePort { get; set; }
        public int BybitFuturesLiqCandlePort { get; set; }
        public int BybitFuturesFrCandlePort { get; set; }
        public int BybitFuturesAllfundsPort { get; set; }
        
        // websocket Bybit Futures
        public int BybitFuturesTradePort { get; set; }
        public int BybitFuturesOrderbookPort { get; set; }
        public int BybitFuturesCandlePort { get; set; }
        
        // Api Bybit
        public int BybitCandleApiPort { get; set; }
        public int BybitHeatmapApiPort { get; set; }
        public int BybitFootprintApiPort { get; set; }
        
        // Api Bybit Futures
        public int BybitFuturesCandleApiPort { get; set; }
        public int BybitFuturesHeatmapApiPort { get; set; }
        public int BybitFuturesFootprintApiPort { get; set; }

        // Api Bybit Futures Fr Liq
        public int BybitFuturesFrCandlesApiPort { get; set; }
        public int BybitFuturesLiqCandlesApiPort { get; set; }
    }
}