using DataLayer;
using ExchangeModels;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Caching;
using ServerApplication.Queues;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using ZeroMQ;

namespace ServerApplication.Workers
{
    public class BinanceTradeWorker : BackgroundService
    {
        private BinanceTradeCalculate _tradeQueue;
        private IMemoryCache _cache;
        private BinanceZeroMQCandleQueue _pubCandleQueue;
        private BinanceRedisSavingDataQueue _redisSavingQueue;
        public const string Exchange = ApplicationValues.BinanceName;
        private BinanceCandleClosedQueue _heatmapWorkerQueue;
        private readonly ILog _logger;
        private readonly string[] _TimeFrames = { 
            "1m", "5m", "15m", "30m",
            "1H", "2H", "4H", "6H", "12H",
            "1D", "3D"
        };

        // queues for Api-Binance-ZeroMQ
        private readonly ApiBinanceZeroMqCandleQueue _binanceCandle;
        private readonly ApiBinanceZeroMqFootprintQueue _binanceFootprint;

        public BinanceTradeWorker(BinanceTradeCalculate tradeQueue, IMemoryCache cache,
            BinanceZeroMQCandleQueue pubCandleQueue, BinanceRedisSavingDataQueue redisQueue,
            BinanceCandleClosedQueue heatmapWorkerQueue, ApiBinanceZeroMqFootprintQueue binanceFootprint,
            ApiBinanceZeroMqCandleQueue binanceCandle)
        {
            _binanceCandle = binanceCandle;
            _binanceFootprint = binanceFootprint;

            _heatmapWorkerQueue = heatmapWorkerQueue;
            _pubCandleQueue = pubCandleQueue;
            _redisSavingQueue = redisQueue;
            _tradeQueue = tradeQueue;
            _cache = cache;

            _logger = LogManager.GetLogger(typeof(BinanceTradeWorker));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _tradeQueue.TryDequeue(out byte[] json))
                    {
                        STrade trade = BinanceConverter.DeserializeBinanceTrade(json);
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

            _heatmapWorkerQueue.Enqueue(candle);
        }

        /// <summary>
        /// algorithem of updating footprint
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

            if (price >= openPrice) // its above
            {
                if (index <= footPrint.AboveMarketOrders.Count - 1)  // exist
                {
                    footPrint.AboveMarketOrders[index][type] += trade.Quantity;
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
                            block[type] += trade.Quantity;
                        }
                        footPrint.AboveMarketOrders.Add(block);
                    }
                }
            }
            else // its below
            {
                if (index <= footPrint.BelowMarketOrders.Count - 1) // exist
                {
                    footPrint.BelowMarketOrders[index][type] += trade.Quantity;
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
                            block[type] += trade.Quantity;
                        }
                        footPrint.BelowMarketOrders.Add(block);
                    }
                }
            }

            return footPrint;
        }

        protected void StreamLastCandle(STrade trade)
        {
            int c = 11;
            for (int i = 0; i < c; i++)
            {
                var timeFrame = _TimeFrames[i];
                var candle = _cache.TryGetOpenCandle(Exchange, trade.Symbol, timeFrame);
                if (candle == null)
                    continue;

                var footprint = _cache.TryGetFootPrints(Exchange, trade.Symbol, timeFrame);
                if (footprint == null)
                    footprint = new FootPrints(candle.OpenPrice);

                long closeTime = candle.GetCloseTime();

                if (closeTime > trade.TradeTime) // candle is still open
                {
                    candle.ClosePrice = trade.Price;
                    candle.Volume += trade.Quantity;

                    // Replace if high price updated
                    if (candle.HighPrice < trade.Price)
                        candle.HighPrice = trade.Price;

                    // Replace if low price updated
                    if (candle.LowPrice > trade.Price)
                        candle.LowPrice = trade.Price;
                }
                else     // candle is closed
                {
                    if (candle.Symbol.Equals("ETHBTC"))
                        _logger.Info($"{candle.Symbol} closed at {closeTime.UnixTimeStampToDateTime():hh:mm:ss} : {candle.TimeFrame}");

                    var closedCandle = candle.Clone();
                    closedCandle.FootPrint = footprint.Clone();

                    CandleClosed(closedCandle);    // calculate heatmap then save in mongodb

                    // Initialize new candle
                    candle = new Candle(trade.Price, trade.Price, trade.Price, trade.Price)
                    {
                        Volume = trade.Quantity,
                        OpenTime = closeTime,
                        Exchange = candle.Exchange,
                        Symbol = candle.Symbol,
                        TimeFrame = candle.TimeFrame
                    };
                    footprint = new FootPrints(candle.OpenPrice);
                }
                _pubCandleQueue.Enqueue(candle);

                // update footprint
                footprint = this.UpdateFootprint(footprint, trade);

                _cache.SetOpenCandle(Exchange, trade.Symbol, timeFrame, candle);
                _cache.SetFootPrints(Exchange, trade.Symbol, timeFrame, footprint);
            }
            _redisSavingQueue.EnqueueFootprint(trade.Symbol);
            _redisSavingQueue.EnqueueCandle(trade.Symbol);
        }
    }
}