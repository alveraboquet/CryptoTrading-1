using ServerApplication.Queues;
using Binance.Net.Objects.Spot.MarketData;
using DatabaseRepository;
using DataLayer;
using DataLayer.Models;
using ExchangeServices;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using ServerApplication.Caching;
using Redis;
using ServerApplication.StreamingServices;
using System.Collections.Concurrent;
using ExchangeModels;
using System.Diagnostics;

namespace ServerApplication.Workers
{
    public class BinanceWorker : BackgroundService
    {
        private readonly ILog _logger;
        private IPairInfoRepository _pairRepo;
        private IBinanceServices _client;
        private IMemoryCache _cache;
        private IPairStreamInfoRepository _streamInfo;
        private ICandleService _candleRepo;

        private const string Exchange = ApplicationValues.BinanceName;

        private BinanceRedisSavingDataQueue _redisQueue;
        private BinanceZeroMQTradeQueue _pubTradeQueue;
        private BinanceZeroMQDepthQueue _redisDepthQueue;
        private BinanceKlineCalculate _klineQueue;
        private BinanceTradeCalculate _tradeQueue;

        public BinanceWorker(IBinanceServices client, IPairInfoRepository pairRepo, IMemoryCache cache,
            BinanceRedisSavingDataQueue redisQueue, BinanceZeroMQDepthQueue redisDepthQueue,
            BinanceZeroMQTradeQueue pubTradeQueue, BinanceKlineCalculate klineQueue,
            BinanceTradeCalculate tradeQueue, IPairStreamInfoRepository streamInfo, ICandleService candleRepo)
        {
            _candleRepo = candleRepo;
            _streamInfo = streamInfo;
            _tradeQueue = tradeQueue;
            _klineQueue = klineQueue;
            _redisQueue = redisQueue;
            _pubTradeQueue = pubTradeQueue;
            _redisDepthQueue = redisDepthQueue;
            _cache = cache;
            _client = client;
            _pairRepo = pairRepo;
            _logger = LogManager.GetLogger(typeof(BinanceWorker));
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"START");

            IEnumerable<BinanceSymbol> symbols = null;
            try
            {
                o:
                Thread.Sleep(10000);
                symbols = await _client.GetSymbolsAsync();
                if (symbols == null)
                {
                    _logger.Error("Faild to get symbols. Trying again.");
                    goto o;
                }
            }
            catch { }
            int updateCount = 0;
            int createCount = 0;

            _logger.Info($"Creating or updating PairInfos using symbols.");
            foreach (var symbol in symbols)
            {
                var pair = await _pairRepo.Get(Exchange, symbol.Name);
                if (pair != null)
                {
                    // is state or precision changed?
                    if (pair.IsListed != symbol.IsListed() || pair.QuoteAssetPrecision != symbol.GetQuoteAssetPrecision())
                    {
                        pair.IsListed = symbol.IsListed();
                        pair.QuoteAssetPrecision = symbol.GetQuoteAssetPrecision();
                        _pairRepo.Update(pair.PairId, pair);
                        updateCount++;
                    }
                }
                else
                {
                    List<TimeFrameOption> timeFrameOptions = new List<TimeFrameOption>()
                    {
                        new TimeFrameOption()
                        {
                            TimeFrame = "1m",
                            StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "5m",
                            StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "15m",
                            StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "30m",
                             StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "1H",
                          StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "2H",
                             StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "4H",
                             StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "6H",
                           StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "12H",
                            StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "1D",
                             StartTimeMax = null,
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "3D",
                           StartTimeMax = null,
                            EndTimeMax = null,
                        },
                    };

                    var pairInfo = new PairInfo()
                    {
                        Exchange = Exchange,
                        IsAvailableFootprint = true,
                        IsLinechart = false,
                        IsAvailableHeatmap = true,
                        IsAvailableVolume = true,
                        Symbol = symbol.Name,
                        QuoteAssetPrecision = symbol.GetQuoteAssetPrecision(),
                        TimezoneDailyCloseFormat = "UTC 00:00",
                        TimeFrameOptions = timeFrameOptions,
                        IsListed = symbol.IsListed()
                    };
                    _pairRepo.Create(pairInfo);
                    createCount++;
                }
            }

            _logger.Info($"{updateCount} updates and {createCount} new pair for {Exchange} PairInfos.");
            await base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"STOP");
            return base.StopAsync(cancellationToken);
        }

        private void LogStopStartStreaming(List<PairInfo> pairInfos)
        {
            Task.Run(async () =>
            {
                var streamInfos = await _streamInfo.GetMany(Exchange);
                var ETHBTC = pairInfos.FirstOrDefault(p => p.Symbol == "ETHBTC");

                Stopwatch sw = Stopwatch.StartNew();
                long now = DateTime.UtcNow.ToUnixTimestamp();
                foreach (var timeframe in ETHBTC.TimeFrameOptions)
                {
                    long lastStop = default;
                    try
                    {
                        lastStop = await _candleRepo.GetLastCandleOpenTime(Exchange, ETHBTC.Symbol, timeframe.TimeFrame);
                    }
                    catch (InvalidOperationException)
                    { }

                    if (lastStop == default) continue;
                    lastStop += 1000; // add 1 second for api call to binance (to do not get duplicate candles)
                    List<PairStreamInfo> createList = new List<PairStreamInfo>();
                    foreach (var pair in pairInfos)
                    {
                        var si = streamInfos.FirstOrDefault(s =>
                                                s.Exchange == pair.Exchange &&
                                                s.Symbol == pair.Symbol &&
                                                s.TimeFrame == timeframe.TimeFrame);
                        if (si == default)
                        {
                            si = new PairStreamInfo(pair.Exchange, pair.Symbol, timeframe.TimeFrame);
                            si.AddReport(lastStop, now);
                            createList.Add(si);
                        }
                        else
                        {
                            si.AddReport(lastStop, now);
                            await _streamInfo.Update(si.Id, si);
                        }
                    }
                    if (createList.Any())
                        await _streamInfo.CreateMany(createList);
                }
                sw.Stop();
            });
        }


        private BinanceTradeKlineStreaming NewTradeKlineStreaming() =>
            new BinanceTradeKlineStreaming(this._cache, _pubTradeQueue, _klineQueue, _tradeQueue);
        private BinanceDepthStreaming NewDepthStreaming(CancellationToken stoppingToken) =>
            new BinanceDepthStreaming(stoppingToken, this._redisDepthQueue, _redisQueue, this._cache);

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(async () =>
            {
                int tradeRange = 60;
                int depthRange = 100;

                var pairInfos = (await _pairRepo.GetListed(Exchange)).RemoveCostumePairs();
                _logger.Info($"{tradeRange} pairs per WS connection.");
                Console.WriteLine(pairInfos.Count);
                _logger.Info($"Logging stop/start streaming.");
                LogStopStartStreaming(pairInfos);

                #region Start Trade Kline Streaming
                for (int i = 0; i < pairInfos.Count - 1;)
                {
                    int r = Math.Min(tradeRange, pairInfos.Count - i);
                    var symbols = pairInfos.GetRange(i, r).ToArray();

                    var streaming = NewTradeKlineStreaming();
                    streaming.Connect(symbols);
                    i += symbols.Length;
                }
                #endregion

                #region Start Depth Streaming
                for (int i = 0; i < pairInfos.Count - 1;)
                {
                    int r = Math.Min(depthRange, pairInfos.Count - i);
                    var symbols = pairInfos.GetRange(i, r).ToArray();

                    var streaming = NewDepthStreaming(stoppingToken);
                    streaming.Connect(symbols);
                    i += symbols.Length;
                }
                #endregion

                Console.WriteLine(pairInfos.Count);
                Thread.Sleep(30000);

                while (!stoppingToken.IsCancellationRequested)
                {
                    pairInfos = (await _pairRepo.GetListed(Exchange)).RemoveCostumePairs();
                    List<PairInfo> startTradeKlinePairs = new List<PairInfo>();
                    List<PairInfo> startDepthPairs = new List<PairInfo>();

                    foreach (var pair in pairInfos)
                    {
                        bool symbolTradeKlineIsStreaming = _cache.TryGetTradeKlineSymbolIsStreaming(pair.Exchange, pair.Symbol);
                        if (!symbolTradeKlineIsStreaming)
                        {
                            startTradeKlinePairs.Add(pair);
                        }

                        bool symbolDepthIsStreaming = _cache.TryGetDepthSymbolIsStreaming(pair.Exchange, pair.Symbol);
                        if (!symbolDepthIsStreaming)
                        {
                            startDepthPairs.Add(pair);
                        }
                    }

                    #region Restart Depth Streaming
                    for (int i = 0; i < startDepthPairs.Count; i += depthRange)
                    {
                        int r = Math.Min(depthRange, startDepthPairs.Count - i);
                        var symbols = startDepthPairs.GetRange(i, r).ToArray();

                        var streaming = NewDepthStreaming(stoppingToken);
                        streaming.Connect(symbols);
                        i += symbols.Length;
                    }
                    #endregion

                    #region Restart Trade & Kline Streaming
                    for (int i = 0; i < startTradeKlinePairs.Count; i += tradeRange)
                    {
                        int r = Math.Min(tradeRange, startTradeKlinePairs.Count - i);
                        var symbols = startTradeKlinePairs.GetRange(i, r).ToArray();

                        var streaming = NewTradeKlineStreaming();
                        streaming.Connect(symbols);
                        i += symbols.Length;
                    }
                    #endregion

                    Thread.Sleep(30000);
                }
            }).Start();
            return Task.CompletedTask;
        }
    }
}