using ExchangeModels.BinanceFutures;
using ExchangeModels.BybitFutures;
using ZeroMQ;

namespace ServerApplication.Bybit.Mapping
{
    public static class MappingExtensions
    {
        public static Trade ToZeroMqTrade(this BybitLiquidationData model)
        {
            return new ZeroMQ.Trade
            {
                Symbol = model.Symbol,
                Price = model.Price.ToString(),
                Amount = model.GetSide() == TradeSide.BUY ? model.Quantity.ToString() : $"-{model.Quantity}",
                TradeTime = model.Timestamp.ToString()
            };
        }
    }
}