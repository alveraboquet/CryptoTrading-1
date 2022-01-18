using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ServerApplication.Bybit.Workers
{
    public class BybitFuturesFundingRateWorker : BackgroundService
    {
        private readonly Dictionary<string, decimal> _allfundsData;
        private readonly IMemoryCache _cache;
        private readonly BybitFuturesAllfundsQueue _allfundsQueue;
        private readonly BybitFuturesFrCalculateQueue _frCalcQueue;
        private readonly BybitFuturesZeroMqFrCandleQueue _frCandleZeroMq;
        private const string Exchange = ApplicationValues.BybitFuturesName;

        private readonly string[] _TimeFrames = {
            "1m", "15m",
            "4H",
            "1D"
        };

        public BybitFuturesFundingRateWorker(IMemoryCache cache, BybitFuturesAllfundsQueue allfundsQueue,
            BybitFuturesFrCalculateQueue frCalcQueue, BybitFuturesZeroMqFrCandleQueue frCandleZeroMq)
        {
            _allfundsData = new();
            this._cache = cache;
            this._allfundsQueue = allfundsQueue;
            this._frCalcQueue = frCalcQueue;
            this._frCandleZeroMq = frCandleZeroMq;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var allfundsThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _allfundsQueue.Enqueue(_allfundsData);

                    Thread.Sleep(3000);
                }
            });

            var fundingRateThread = new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (!stoppingToken.IsCancellationRequested && _frCalcQueue.TryDequeue(out var frUpdate))
                    {
                        frUpdate.FundingRate /= 1000000;

                        // update allfunds list
                        _allfundsData[frUpdate.Symbol] = frUpdate.FundingRate;
                        StreamFundingRate(frUpdate.Symbol, frUpdate.FundingRate);
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
            });

            allfundsThread.Start();
            fundingRateThread.Start();

            return Task.CompletedTask;
        }


        private void StreamFundingRate(string Symbol, decimal fundingRate)
        {
            foreach (var tf in _TimeFrames)
            {
                var candle = _cache.TryGetOpenCandle(Exchange, Symbol, tf);

                if (candle == null) return;

                if (candle.FundingRate == null)
                    candle.FundingRate = new DataLayer.FundingRate(fundingRate);

                // Replace if high updated
                if (candle.FundingRate.High < fundingRate)
                    candle.FundingRate.High = fundingRate;

                // Replace if low updated
                if (candle.FundingRate.Low > fundingRate)
                    candle.FundingRate.Low = fundingRate;

                candle.FundingRate.Close = fundingRate;

                EnqueueFrCandle(candle);
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Enqueue to publish in websocket for FR.<symbol>
        /// </summary>
        private void EnqueueFrCandle(DataLayer.Candle candle)
        {
            _frCandleZeroMq.Enqueue(new ZeroMQ.OpenCandle()
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
    }
}
