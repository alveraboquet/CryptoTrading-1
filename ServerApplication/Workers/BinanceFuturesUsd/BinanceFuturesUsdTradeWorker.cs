using DataLayer;
using ExchangeModels;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Caching;
using ServerApplication.Queues;
using System;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using ZeroMQ;

namespace ServerApplication.Workers
{
    public class BinanceFuturesUsdTradeWorker : BackgroundService
    {
        private readonly ApiFrBinanceFuturesUsdZeroMqCandleQueue _frCandlesQueue;
        private readonly ApiLiqBinanceFuturesUsdZeroMqCandleQueue _liqCandleQueue;

        private readonly BinanceFuturesUsdTradeCalculate _tradeQueue;
        private readonly BinanceFuturesUsdZeroMqCandleQueue _pubCandleQueue;
        private readonly BinanceFuturesUsdRedisSavingDataQueue _redisSavingQueue;
        private readonly BinanceFuturesUsdCandleAndOrderbookQueue _heatmapWorkerQueue;

        // queues for Api-Binance-ZeroMQ
        private readonly ApiBinanceFuturesUsdZeroMqCandleQueue _binanceCandle;
        private readonly ApiBinanceFuturesUsdZeroMqFootprintQueue _binanceFootprint;

        private readonly string[] _TimeFrames = {
            "1m", "5m", "15m",
            "1H", "4H",
            "1D", "3D"
        };
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly string exchange = ApplicationValues.BinanceUsdName;

        public BinanceFuturesUsdTradeWorker(IMemoryCache cache, BinanceFuturesUsdTradeCalculate tradeQueue,
            BinanceFuturesUsdZeroMqCandleQueue pubCandleQueue, BinanceFuturesUsdRedisSavingDataQueue redisQueue,
            BinanceFuturesUsdCandleAndOrderbookQueue heatmapWorkerQueue, ApiBinanceFuturesUsdZeroMqFootprintQueue binanceFootprint,
            ApiBinanceFuturesUsdZeroMqCandleQueue binanceCandle,
            ApiFrBinanceFuturesUsdZeroMqCandleQueue frCandlesQueue, ApiLiqBinanceFuturesUsdZeroMqCandleQueue liqCandleQueue)
        {
            _liqCandleQueue = liqCandleQueue;
            _frCandlesQueue = frCandlesQueue;
            _binanceCandle = binanceCandle;
            _binanceFootprint = binanceFootprint;

            _heatmapWorkerQueue = heatmapWorkerQueue;
            _pubCandleQueue = pubCandleQueue;
            _redisSavingQueue = redisQueue;
            _tradeQueue = tradeQueue;
            _cache = cache;

            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdTradeWorker));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _tradeQueue.TryDequeue(out byte[] json))
                    {
                        STrade trade = BinanceConverter.DeserializeBinanceFuturesUsdTrade(json);
                        StreamLastCandle(trade);
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            return Task.CompletedTask;
        }

        private void CandleClosed(Candle candle)
        {
            #region enqueue candle and footprint for Api ZeroMQ binance
            _binanceCandle.Enqueue((ZeroMQ.OpenCandle)candle);
            _binanceFootprint.Enqueue(new OpenFootprint()
            {
                Timeframe = candle.TimeFrame,
                Symbol = candle.Symbol,
                OpenPrice = candle.OpenPrice,
                OpenTime = candle.OpenTime,
                AboveMarketOrders = candle.FootPrint.AboveMarketOrders,
                BelowMarketOrders = candle.FootPrint.BelowMarketOrders
            });
            #endregion

            #region Enqueue Fr and Liq candles for API
            if (candle.FundingRate != null)
            {
                _frCandlesQueue.Enqueue(new ZeroMQ.OpenCandle()
                {
                    OpenTime = candle.OpenTime,
                    Symbol = $"FR.{candle.Symbol}",
                    Timeframe = candle.TimeFrame,

                    Open = candle.FundingRate.Open,
                    High = candle.FundingRate.High,
                    Low = candle.FundingRate.Low,
                    Close = candle.FundingRate.Close,

                    Volume = 0,
                });
            }

            if (candle.Liquidation != null)
            {
                _liqCandleQueue.Enqueue(new ZeroMQ.OpenCandle()
                {
                    Symbol = $"LIQSELL.{candle.Symbol}",
                    Timeframe = candle.TimeFrame,
                    OpenTime = candle.OpenTime,

                    Low = candle.Liquidation.LiqSell,
                    High = candle.Liquidation.LiqSell,
                    Open = candle.Liquidation.LiqSell,
                    Close = candle.Liquidation.LiqSell,

                    Volume = 0
                });
                _liqCandleQueue.Enqueue(new ZeroMQ.OpenCandle()
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
                _liqCandleQueue.Enqueue(new ZeroMQ.OpenCandle()
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
            #endregion

            _heatmapWorkerQueue.Enqueue(candle);
        }

        /// <summary>
        /// algorythem of updating footprint
        /// </summary>
        /// <param name="footPrint">current footprint</param>
        /// <param name="trade">new trade</param>
        protected FootPrints UpdateFootprint(FootPrints footPrint, STrade trade)
        {
            decimal price = trade.Price;
            decimal openPrice = footPrint.OpenPrice;
            decimal diff = Math.Abs(openPrice - price);
            decimal step = footPrint.Range;
            int index = (int)(diff / step);
            int type = (!trade.IsBuyer).ToInt();

            decimal blockQua = trade.Quantity * trade.Price;

            if (price >= openPrice) // its above
            {
                if (index <= footPrint.AboveMarketOrders.Count - 1)  // exist
                {
                    footPrint.AboveMarketOrders[index][type] += blockQua;
                }
                else   // add new one
                {
                    int i = footPrint.AboveMarketOrders.Count;
                    if ((index - i) > 50000) return footPrint;
                    for (; i <= index; i++)
                    {
                        var block = new decimal[2];
                        if (i == index)
                        {
                            block[type] += blockQua;
                        }
                        footPrint.AboveMarketOrders.Add(block);
                    }
                }
            }
            else // its below
            {
                if (index <= footPrint.BelowMarketOrders.Count - 1) // exist
                {
                    footPrint.BelowMarketOrders[index][type] += blockQua;
                }
                else    // add new one
                {
                    int i = footPrint.BelowMarketOrders.Count;
                    if ((index - i) > 50000) return footPrint;
                    for (; i <= index; i++)
                    {
                        var block = new decimal[2];
                        if (i == index)
                        {
                            block[type] += blockQua;
                        }
                        footPrint.BelowMarketOrders.Add(block);
                    }
                }
            }

            return footPrint;
        }

        protected void StreamLastCandle(STrade trade)
        {
            int c = 7;
            for (int i = 0; i < c; i++)
            {
                var timeFrame = _TimeFrames[i];
                var candle = _cache.TryGetOpenCandle(exchange, trade.Symbol, timeFrame);
                if (candle == null)
                    continue;

                var footprint = _cache.TryGetFootPrints(exchange, trade.Symbol, timeFrame);

                if (footprint == null)
                    footprint = new FootPrints(candle.OpenPrice);

                if (candle.GetCloseTime() > trade.TradeTime) // candle is still open
                {
                    // Replace if high price updated
                    if (candle.HighPrice < trade.Price)
                        candle.HighPrice = trade.Price;

                    // Replace if low price updated
                    if (candle.LowPrice > trade.Price)
                        candle.LowPrice = trade.Price;
                    candle.ClosePrice = trade.Price;
                    candle.Volume += trade.Quantity;
                }
                else     // candle is closed
                {
                    if (candle.Symbol.Equals("BTCUSDT"))
                        _logger.Info($"{candle.Symbol} closed at {candle.GetCloseTime().UnixTimeStampToDateTime():hh:mm:ss} : {candle.TimeFrame}");

                    var closedCandle = candle.Clone();
                    closedCandle.FootPrint = footprint.Clone();

                    // calculate heatmap then save in mongodb
                    CandleClosed(closedCandle);

                    var newOpenTime = candle.GetCloseTime();

                    // Initialize new candle
                    candle = new Candle(trade.Price, trade.Price, trade.Price, trade.Price)
                    {
                        Volume = trade.Quantity,
                        OpenTime = newOpenTime,
                        Exchange = candle.Exchange,
                        Symbol = candle.Symbol,
                        TimeFrame = candle.TimeFrame
                    };
                    footprint = new FootPrints(candle.OpenPrice);
                }
                _pubCandleQueue.EnqueueCandle(candle);

                // update footprint
                footprint = this.UpdateFootprint(footprint, trade);

                _cache.SetOpenCandle(exchange, trade.Symbol, timeFrame, candle);
                _cache.SetFootPrints(exchange, trade.Symbol, timeFrame, footprint);
            }
            _redisSavingQueue.EnqueueFootprint(exchange, trade.Symbol);
            _redisSavingQueue.EnqueueCandle(exchange, trade.Symbol);
        }
    }
}