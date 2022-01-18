using ExchangeModels.BinanceFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Caching;
using ServerApplication.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ServerApplication.Workers
{
    public class BinanceFuturesUsdLiquidationWorker : BackgroundService
    {
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly BinanceFuturesUsdLiquidationCalculateQueue _liqidation;
        private readonly BinanceFuturesUsdZeroMqLiquidationQueue _liqTradeZeroMq;
        private readonly BinanceFuturesUsdZeroMqLiqCandleQueue _liqCandleZeroMq;

        private const string Exchange = ApplicationValues.BinanceUsdName;
        private readonly string[] _TimeFrames = {
            "15m",
            "4H",
            "1D"
        };

        public BinanceFuturesUsdLiquidationWorker(IMemoryCache cache,
            BinanceFuturesUsdLiquidationCalculateQueue liqidation,
            BinanceFuturesUsdZeroMqLiquidationQueue liqTradeZeroMq,
            BinanceFuturesUsdZeroMqLiqCandleQueue liqCandleZeroMq)
        {
            _liqCandleZeroMq = liqCandleZeroMq;
            _liqTradeZeroMq = liqTradeZeroMq;
            _cache = cache;
            _liqidation = liqidation;
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdLiquidationWorker));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _liqidation.TryDequeue(out byte[] json))
                    {
                        LiquidationEvent liq = BinanceConverter.DeserializeBinanceFuturesUsdLiquidation(json);

                        #region Enquqe liq Trades
                        switch (liq.LiquidationUpdate.Side)
                        {
                            case TradeSide.BUY:
                                var liqBuyTrade = (ZeroMQ.Trade)liq.LiquidationUpdate;
                                liqBuyTrade.Symbol = $"LIQBUY.{liqBuyTrade.Symbol}";
                                _liqTradeZeroMq.Enqueue(liqBuyTrade);
                                break;

                            case TradeSide.SELL:
                                var liqSellTrade = (ZeroMQ.Trade)liq.LiquidationUpdate;
                                liqSellTrade.Symbol = $"LIQSELL.{liqSellTrade.Symbol}";
                                _liqTradeZeroMq.Enqueue(liqSellTrade);
                                break;
                        }

                        var liqTrade = (ZeroMQ.Trade)liq.LiquidationUpdate;
                        liqTrade.Symbol = $"LIQ.{liqTrade.Symbol}";
                        _liqTradeZeroMq.Enqueue(liqTrade);
                        #endregion

                        StreamLiquidation(liq);
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            return Task.CompletedTask;
        }

        private void StreamLiquidation(LiquidationEvent liq)
        {
            foreach (var tf in _TimeFrames)
            {
                var candle = _cache.TryGetOpenCandle(Exchange, liq.LiquidationUpdate.Symbol, tf);
                if (candle == null 
                    //|| candle.GetCloseTime() < liq.LiquidationUpdate.TradeTime
                    )
                    continue;

                if (candle.Liquidation == null)
                    candle.Liquidation = new DataLayer.Liquidation();


                decimal q = liq.LiquidationUpdate.Price * liq.LiquidationUpdate.Quantity;

                switch (liq.LiquidationUpdate.Side)
                {
                    case TradeSide.SELL:
                        candle.Liquidation.LiqSell += q;

                        _liqCandleZeroMq.Enqueue(new ZeroMQ.OpenCandle()
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

                        _liqCandleZeroMq.Enqueue(new ZeroMQ.OpenCandle()
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

                _liqCandleZeroMq.Enqueue(new ZeroMQ.OpenCandle()
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
            }
        }
    }
}