using System;
using System.Threading;
using System.Threading.Tasks;
using DataLayer;
using ExchangeModels;
using ExchangeModels.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues;
using ServerApplication.Bybit.Queues.BybitFutures;
using Utilities;

namespace ServerApplication.Bybit.Workers.BybitFutures
{
    public class BybitFuturesTradeWorker : BackgroundService
    {
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly string _exchange = ApplicationValues.BybitFuturesName;
        
        private readonly ApiFrBybitFuturesZeroMqCandleQueue _frCandlesQueue;
        private readonly ApiLiqBybitFuturesZeroMqCandleQueue _liqCandleQueue;

        private readonly BybitFuturesTradeMessageQueue _tradeQueue;
        private readonly BybitFuturesZeroMqCandleQueue _pubCandleQueue;
        private readonly BybitFuturesRedisSavingDataQueue _redisSavingQueue;
        private readonly BybitFuturesCandleAndOrderbookQueue _heatmapWorkerQueue;

        // queues for Api-Binance-ZeroMQ
        private readonly ApiBybitFuturesZeroMqCandleQueue _apiCandleQueue;
        private readonly ApiBybitFuturesZeroMqFootprintQueue _apiFootprintQueue;
        
        private readonly string[] _timeframes = {
            "1m", "5m", "15m", "30m",
            "1H", "2H", "4H", "6H",
            "1D"
        };

        public BybitFuturesTradeWorker(IMemoryCache cache, 
            ApiFrBybitFuturesZeroMqCandleQueue frCandlesQueue, 
            ApiLiqBybitFuturesZeroMqCandleQueue liqCandleQueue, 
            BybitFuturesTradeMessageQueue tradeQueue, 
            BybitFuturesZeroMqCandleQueue pubCandleQueue, 
            BybitFuturesRedisSavingDataQueue redisSavingQueue, 
            BybitFuturesCandleAndOrderbookQueue heatmapWorkerQueue, 
            ApiBybitFuturesZeroMqCandleQueue apiCandleQueue, 
            ApiBybitFuturesZeroMqFootprintQueue apiFootprintQueue)
        {
            _cache = cache;
            _logger = LogManager.GetLogger(typeof(BybitFuturesTradeWorker));
            _frCandlesQueue = frCandlesQueue;
            _liqCandleQueue = liqCandleQueue;
            _tradeQueue = tradeQueue;
            _pubCandleQueue = pubCandleQueue;
            _redisSavingQueue = redisSavingQueue;
            _heatmapWorkerQueue = heatmapWorkerQueue;
            _apiCandleQueue = apiCandleQueue;
            _apiFootprintQueue = apiFootprintQueue;
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _tradeQueue.TryDequeue(out BybitFuturesTrade trade))
                    {
                        StreamLastCandle(trade);
                        // Thread.Sleep(1);
                    }
                    // Thread.Sleep(1);
                }
            }).Start();

            return Task.CompletedTask;
        }
        
        private void CandleClosed(Candle candle)
        {
            #region enqueue candle and footprint for Api ZeroMQ binance
            _apiCandleQueue.Enqueue((ZeroMQ.OpenCandle)candle);
            _apiFootprintQueue.Enqueue(new ZeroMQ.OpenFootprint()
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
        protected FootPrints UpdateFootprint(FootPrints footPrint, BybitFuturesTrade trade)
        {
            decimal price = trade.Price;
            decimal openPrice = footPrint.OpenPrice;
            decimal diff = Math.Abs(openPrice - price);
            decimal step = footPrint.Range;
            int index = (int)(diff / step);
            int type = (trade.Side != "Buy").ToInt();

            decimal blockQua = trade.Size * trade.Price;

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
        
        protected void StreamLastCandle(BybitFuturesTrade trade)
        {
            int c = 9;
            for (int i = 0; i < c; i++)
            {
                var timeFrame = _timeframes[i];
                var candle = _cache.TryGetOpenCandle(_exchange, trade.Symbol, timeFrame);
                if (candle == null)
                    continue;

                var footprint = _cache.TryGetFootPrints(_exchange, trade.Symbol, timeFrame);

                if (footprint == null)
                    footprint = new FootPrints(candle.OpenPrice);
                
                // _logger.Info($"Candle Before [{candle.Symbol} {candle.TimeFrame}]: {candle.OpenTime}");

                var isCandleOpen = candle.GetCloseTime() > trade.TradeTimeMs;
                // _logger.Info($"Compare: {candle.GetCloseTime()} > {trade.TradeTimeMs} => {isCandleOpen}");

                if (isCandleOpen) // candle is still open
                {
                    // Replace if high price updated
                    if (candle.HighPrice < trade.Price)
                        candle.HighPrice = trade.Price;

                    // Replace if low price updated
                    if (candle.LowPrice > trade.Price)
                        candle.LowPrice = trade.Price;
                    candle.ClosePrice = trade.Price;
                    candle.Volume += trade.Size;
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
                        Volume = trade.Size,
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

                // _logger.Info($"Candle After- [{candle.Symbol} {candle.TimeFrame}]: {candle.OpenTime}");
                _cache.SetOpenCandle(_exchange, trade.Symbol, timeFrame, candle);
                _cache.SetFootPrints(_exchange, trade.Symbol, timeFrame, footprint);
            }
            _redisSavingQueue.EnqueueFootprint(_exchange, trade.Symbol);
            _redisSavingQueue.EnqueueCandle(_exchange, trade.Symbol);
        }
    }
}