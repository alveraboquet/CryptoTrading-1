using DataLayer;
using ExchangeModels.Bybit;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Queues;
using ServerApplication.Bybit.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ServerApplication.Bybit.Workers
{
    public class BybitTradeWorker : BackgroundService
    {
        private IMemoryCache _cache;
        private BybitZeroMQKlineQueue _pubCandleQueue;
        private BybitRedisSavingDataQueue _redisSavingQueue;

        private BybitTradeMessageQueue _tradeQueue;

        public const string Exchange = ApplicationValues.BybitName;

        private BybitClosedCandleQueue _closedCandleQueue;

        private readonly ILog _logger;
        private readonly string[] _TimeFrames = {
            "1m", "5m", "15m", "30m",
            "1H", "2H", "4H", "6H", "12H",
            "1D"
        };


        public BybitTradeWorker(BybitTradeMessageQueue tradeQueue, IMemoryCache cache,
            BybitZeroMQKlineQueue pubCandleQueue, BybitRedisSavingDataQueue redisQueue,
            BybitClosedCandleQueue closedCandleQueue)
        {
            _closedCandleQueue = closedCandleQueue;
            _pubCandleQueue = pubCandleQueue;
            _redisSavingQueue = redisQueue;
            _tradeQueue = tradeQueue;
            _cache = cache;

            _logger = LogManager.GetLogger(typeof(BybitTradeWorker));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _tradeQueue.TryDequeue(out var trade))
                    {
                        foreach (var timeFrame in _TimeFrames)
                        {
                            var footprint = _cache.TryGetFootPrints(Exchange, trade.Symbol, timeFrame);

                            if (footprint is not null)
                                footprint = UpdateFootprint(footprint, trade);
                        }
                    }
                    Thread.Sleep(1);
                }
            }).Start();

            return Task.CompletedTask;
        }


        /// <summary>
        /// algorithem of updating footprint
        /// </summary>
        /// <param name="footPrint">current footprint</param>
        /// <param name="trade">new trade</param>
        protected FootPrints UpdateFootprint(FootPrints footPrint, TradeDataModel trade)
        {
            decimal price = trade.Price;
            decimal openPrice = footPrint.OpenPrice;
            decimal diff = Math.Abs(openPrice - price);
            decimal step = footPrint.Range;
            int index = (int)(diff / step);
            int type = (!trade.IsBuy).ToInt();

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
    }
}
