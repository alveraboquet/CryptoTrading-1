using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using DatabaseRepository;
using Microsoft.Extensions.Caching.Memory;
using Utilities;
using ServerApplication.Bybit.StreamingServices;
using ServerApplication.Bybit.Queues;
using DataLayer;
using DataLayer.Models;
using ExchangeModels.Bybit.API;
using ExchangeServices.ExtensionMethods;
using ExchangeServices.Services.Exchanges.Bybit.API;
using ServerApplication.Bybit.Caching;

namespace ServerApplication.Bybit.Workers
{
    public class BybitWorker : BackgroundService
    {
        private readonly ILog _logger;
        private IPairInfoRepository _pairRepo;
        private IBybitService _client;
        private IMemoryCache _cache;
        private IPairStreamInfoRepository _streamInfo;
        private ICandleService _candleRepo;

        private const string Exchange = ApplicationValues.BybitName;

        private readonly BybitTradeMessageQueue _receivedTrade;
        private readonly BybitKlineMessageQueue _receivedKline;

        private readonly BybitZeroMQDepthQueue _pubDepthQueue;
        private readonly BybitRedisSavingDataQueue _redisSavingQueue;
        
        private readonly BybitZeroMQTradeQueue _zeroMqTradeQueue;


        public BybitWorker(IPairInfoRepository pairRepo, IMemoryCache cache,
            IPairStreamInfoRepository streamInfo, ICandleService candleRepo,
            BybitTradeMessageQueue receivedTrade, BybitKlineMessageQueue receivedKline,
            BybitZeroMQDepthQueue pubDepthQueue, BybitRedisSavingDataQueue redisQueue,
            IBybitService client, BybitZeroMQTradeQueue zeroMqTradeQueue)
        {
            _redisSavingQueue = redisQueue;
            _client = client;
            _zeroMqTradeQueue = zeroMqTradeQueue;
            _pubDepthQueue = pubDepthQueue;

            _receivedKline = receivedKline;
            _candleRepo = candleRepo;
            _streamInfo = streamInfo;
            _cache = cache;
            _receivedTrade = receivedTrade;
            _pairRepo = pairRepo;
            _logger = LogManager.GetLogger(typeof(BybitWorker));
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"START");

            IEnumerable<BybitSpotSymbol> symbols = null;
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
                    if (!pair.IsListed || pair.QuoteAssetPrecision != symbol.GetQuoteAssetPrecision())
                    {
                        pair.IsListed = true;
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
                            TimeFrame = "1D",
                             StartTimeMax = null,
                            EndTimeMax = null,
                        }
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
                        IsListed = true
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
                var BTCUSDT = pairInfos.FirstOrDefault(p => p.Symbol == "BTCUSDT");

                long now = DateTime.UtcNow.ToUnixTimestamp();
                foreach (var timeframe in BTCUSDT.TimeFrameOptions)
                {
                    long lastStop = default;
                    try
                    {
                        lastStop = await _candleRepo.GetLastCandleOpenTime(Exchange, BTCUSDT.Symbol, timeframe.TimeFrame);
                    }
                    catch (InvalidOperationException)
                    { }

                    if (lastStop == default) continue;
                    lastStop += 1000; // add 1 second for api call to binance (to do not get duplicate candles)
                    List<PairStreamInfo> createList = new List<PairStreamInfo>();
                    foreach (var pair in pairInfos)
                    {
                        var firstStreamInfo = streamInfos.FirstOrDefault(s =>
                                                s.Exchange == pair.Exchange &&
                                                s.Symbol == pair.Symbol &&
                                                s.TimeFrame == timeframe.TimeFrame);
                        if (firstStreamInfo == default)
                        {
                            firstStreamInfo = new PairStreamInfo(pair.Exchange, pair.Symbol, timeframe.TimeFrame);
                            firstStreamInfo.AddReport(lastStop, now);
                            createList.Add(firstStreamInfo);
                        }
                        else
                        {
                            firstStreamInfo.AddReport(lastStop, now);
                            await _streamInfo.Update(firstStreamInfo.Id, firstStreamInfo);
                        }
                    }
                    if (createList.Any())
                        await _streamInfo.CreateMany(createList);

                    Thread.Sleep(1);
                }
            });
        }


        private BybitTradeStreaming NewTradeStreaming() => new(this._cache, _receivedTrade, _zeroMqTradeQueue);
        private BybitKlineStreaming NewKlineStreaming() => new(this._cache, _receivedKline);
        private BybitDepthStreaming NewDepthStreaming(CancellationToken stoppingToken) => new(stoppingToken, _pubDepthQueue, _redisSavingQueue, this._cache);

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(async () =>
            {
                var pairInfos = (await _pairRepo.GetListed(Exchange)).RemoveCostumePairs();

                _logger.Info($"Logging stop/start streaming.");
                LogStopStartStreaming(pairInfos);

                // Start Trade Streaming
                NewTradeStreaming().Connect(pairInfos);

                // Start Kline Streaming
                NewKlineStreaming().Connect(pairInfos);

                // Start Depth Streaming
                NewDepthStreaming(stoppingToken).Connect(pairInfos);

                Thread.Sleep(30000);

                while (!stoppingToken.IsCancellationRequested)
                {
                    pairInfos = (await _pairRepo.GetListed(Exchange)).RemoveCostumePairs();
                    List<PairInfo> startTradePairs = new();
                    List<PairInfo> startKlinePairs = new();
                    List<PairInfo> startDepthPairs = new();

                    foreach (var pair in pairInfos)
                    {
                        bool symbolTradeIsStreaming = _cache.TryGetTradeSymbolIsStreaming(pair.Exchange, pair.Symbol);
                        if (!symbolTradeIsStreaming)
                            startTradePairs.Add(pair);

                        bool symbolKlineIsStreaming = _cache.TryGetKlineSymbolIsStreaming(pair.Exchange, pair.Symbol);
                        if (!symbolKlineIsStreaming)
                            startKlinePairs.Add(pair);

                        bool symbolDepthIsStreaming = _cache.TryGetDepthSymbolIsStreaming(pair.Exchange, pair.Symbol);
                        if (!symbolDepthIsStreaming)
                            startDepthPairs.Add(pair);
                    }

                    if (startTradePairs.Any())
                        // Start Trade Streaming
                        NewTradeStreaming().Connect(startTradePairs);

                    if (startKlinePairs.Any())
                        // Start Kline Streaming
                        NewKlineStreaming().Connect(startKlinePairs);

                    if (startDepthPairs.Any())
                        // Start Depth Streaming
                        NewDepthStreaming(stoppingToken).Connect(startDepthPairs);

                    Thread.Sleep(30000);
                }
            }).Start();
            return Task.CompletedTask;
        }
    }
}
