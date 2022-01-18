using System.Threading;
using System.Threading.Tasks;
using DataLayer;
using ExchangeModels;
using ExchangeModels.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Models;
using ServerApplication.Bybit.Queues.BybitFutures;
using Utilities;

namespace ServerApplication.Bybit.Workers.BybitFutures
{
    public class BybitFuturesKlineWorker : BackgroundService
    {
        private IMemoryCache _cache;
        private readonly ILog _logger;
        private string _exchange = ApplicationValues.BybitFuturesName;
        private BybitFuturesKlineMessageQueue _klineQueue;
        private BybitFuturesZeroMqCandleQueue _candleQueue;
        private BybitFuturesRedisSavingDataQueue _redisQueue;

        public BybitFuturesKlineWorker(IMemoryCache cache,
            BybitFuturesKlineMessageQueue klineQueue,
            BybitFuturesZeroMqCandleQueue candleQueue,
            BybitFuturesRedisSavingDataQueue redisQueue)
        {
            _cache = cache;
            _logger = LogManager.GetLogger(typeof(BybitFuturesKlineWorker));
            _klineQueue = klineQueue;
            _candleQueue = candleQueue;
            _redisQueue = redisQueue;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    while (_klineQueue.TryDequeue(out BybitFuturesExtendedCandle kline) && !stoppingToken.IsCancellationRequested)
                    {
                        Candle candle = _cache.TryGetOpenCandle(_exchange, kline.Symbol, kline.Timeframe);
                        bool haveChanged = false;
                        if (candle == null)
                        {
                            // _logger.Info($"**NULL CANDLE** [{kline.Symbol} {kline.Timeframe}]");
                            candle = new Candle(kline.Open, kline.High, kline.Low, kline.Close)
                            {
                                Exchange = _exchange,
                                Symbol = kline.Symbol,
                                TimeFrame = kline.Timeframe,
                                OpenTime = kline.StartAsMilliseconds,
                                Volume = kline.Volume,
                            };
                            _cache.SetOpenCandle(candle.Exchange, candle.Symbol, candle.TimeFrame, candle);
                            haveChanged = true;
                        }
                        else if(candle.OpenTime == kline.StartAsMilliseconds)
                        {
                            if (candle.OpenPrice != kline.Open)
                            {
                                candle.OpenPrice = kline.Open;
                                haveChanged = true;
                            }
                            if (candle.HighPrice != kline.High)
                            {
                                candle.HighPrice = kline.High;
                                haveChanged = true;
                            }
                            if (candle.LowPrice != kline.Low)
                            {
                                candle.LowPrice = kline.Low;
                                haveChanged = true;
                            }
                            if (candle.Volume != kline.Volume)
                            {
                                candle.Volume = kline.Volume;
                                haveChanged = true;
                            }
                            if (candle.ClosePrice != kline.Close)
                            {
                                candle.ClosePrice = kline.Close;
                                haveChanged = true;
                            }
                        }

                        if (haveChanged)
                        {
                            if (candle.TimeFrame.Equals("1m"))
                                _redisQueue.EnqueueCandle(_exchange, candle.Symbol);
                            _candleQueue.EnqueueCandle(candle);
                        }
                    }
                    Thread.Sleep(1);
                }
            }).Start();
            
            return Task.CompletedTask;
        }
    }
}