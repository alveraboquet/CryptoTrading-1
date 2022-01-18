using System.Threading;
using System.Threading.Tasks;
using ExchangeModels.BinanceFutures;
using ExchangeModels.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Mapping;
using ServerApplication.Bybit.Queues.BybitFutures;
using ServerApplication.Bybit.Queues.BybitFutures.LiqFr;
using Utilities;

namespace ServerApplication.Bybit.Workers.BybitFutures
{
    public class BybitFuturesLiquidationWorker : BackgroundService
    {
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly BybitFuturesLiquidationCalculateQueue _liquidationCalculateQueue;
        private readonly BybitFuturesZeroMqLiquidationQueue _liqTradeZeroMqQueue;
        private readonly BybitFuturesZeroMqLiqCandleQueue _liqCandleZeroMqQueue;

        private const string Exchange = ApplicationValues.BybitFuturesName;

        private readonly string[] _timeFrames =
        {
            "15m",
            "4H",
            "1D"
        };

        public BybitFuturesLiquidationWorker(IMemoryCache cache,
            BybitFuturesLiquidationCalculateQueue liquidationCalculateQueue,
            BybitFuturesZeroMqLiquidationQueue liqTradeZeroMqQueue,
            BybitFuturesZeroMqLiqCandleQueue liqCandleZeroMqQueue)
        {
            _cache = cache;
            _logger = LogManager.GetLogger(typeof(BybitFuturesLiquidationWorker));
            _liquidationCalculateQueue = liquidationCalculateQueue;
            _liqTradeZeroMqQueue = liqTradeZeroMqQueue;
            _liqCandleZeroMqQueue = liqCandleZeroMqQueue;
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        while (!stoppingToken.IsCancellationRequested
                               && _liquidationCalculateQueue.TryDequeue(out BybitLiquidationData liquidation))
                        {
                            #region Enquqe liq Trades

                            switch (liquidation.GetSide())
                            {
                                case TradeSide.BUY:
                                    ZeroMQ.Trade liqBuyTrade = liquidation.ToZeroMqTrade();
                                    liqBuyTrade.Symbol = $"LIQBUY.{liqBuyTrade.Symbol}";
                                    _liqTradeZeroMqQueue.Enqueue(liqBuyTrade);
                                    break;
                                
                                case TradeSide.SELL:
                                    ZeroMQ.Trade liqSellTrade = liquidation.ToZeroMqTrade();
                                    liqSellTrade.Symbol = $"LIQSELL.{liqSellTrade.Symbol}";
                                    _liqTradeZeroMqQueue.Enqueue(liqSellTrade);
                                    break;
                            }
                            
                            ZeroMQ.Trade liqTrade = liquidation.ToZeroMqTrade();
                            liqTrade.Symbol = $"LIQ.{liqTrade.Symbol}";
                            _liqTradeZeroMqQueue.Enqueue(liqTrade);
                            
                            #endregion

                            StreamLiquidation(liquidation);
                        }
                    }
                })
                .Start();

            return Task.CompletedTask;
        }

        private void StreamLiquidation(BybitLiquidationData liquidation)
        {
            foreach (var timeframe in _timeFrames)
            {
                var candle = _cache.TryGetOpenCandle(Exchange, liquidation.Symbol, timeframe);
                if (candle == null) continue;
                
                candle.Liquidation ??= new DataLayer.Liquidation();
                
                decimal q = liquidation.Price * liquidation.Quantity;
                
                switch (liquidation.GetSide())
                {
                    case TradeSide.SELL:
                        candle.Liquidation.LiqSell += q;

                        _liqCandleZeroMqQueue.Enqueue(new ZeroMQ.OpenCandle()
                        {
                            Symbol = $"LIQSELL.{candle.Symbol}",
                            Timeframe = candle.TimeFrame,

                            OpenTime = candle.OpenTime,

                            Open = candle.Liquidation.LiqSell,
                            High = candle.Liquidation.LiqSell,
                            Low = candle.Liquidation.LiqSell,
                            Close = candle.Liquidation.LiqSell,

                            Volume = 0
                        });
                        break;

                    case TradeSide.BUY:
                        candle.Liquidation.LiqBuy += q;

                        _liqCandleZeroMqQueue.Enqueue(new ZeroMQ.OpenCandle()
                        {
                            Symbol = $"LIQBUY.{candle.Symbol}",
                            Timeframe = candle.TimeFrame,

                            OpenTime = candle.OpenTime,

                            Open = candle.Liquidation.LiqBuy,
                            High = candle.Liquidation.LiqBuy,
                            Low = candle.Liquidation.LiqBuy,
                            Close = candle.Liquidation.LiqBuy,

                            Volume = 0
                        });
                        break;
                }

                candle.Liquidation.Liq += q;

                _liqCandleZeroMqQueue.Enqueue(new ZeroMQ.OpenCandle()
                {
                    Symbol = $"LIQ.{candle.Symbol}",
                    Timeframe = candle.TimeFrame,

                    OpenTime = candle.OpenTime,

                    Open = candle.Liquidation.Liq,
                    High = candle.Liquidation.Liq,
                    Low = candle.Liquidation.Liq,
                    Close = candle.Liquidation.Liq,

                    Volume = 0
                });
                Thread.Sleep(1);
            }
        }
    }
}