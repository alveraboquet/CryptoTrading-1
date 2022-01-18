using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Objects.Futures.MarketData;
using DatabaseRepository;
using DataLayer;
using DataLayer.Models;
using ExchangeModels.Bybit.API;
using ExchangeServices.ExtensionMethods;
using ExchangeServices.Services.Exchanges.Bybit.API;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues.BybitFutures;
using ServerApplication.Bybit.StreamingServices.BybitFutures;
using Utilities;

namespace ServerApplication.Bybit.Workers.BybitFutures
{
    public class BybitFuturesWorker : BackgroundService
    {
        private readonly ILog _logger;
        private readonly IBybitFuturesService _bybitApiClient;
        private readonly IPairInfoRepository _pairService;
        private readonly IMemoryCache _cache;
        private readonly ICandleService _candleService;
        private const string Exchange = ApplicationValues.BybitFuturesName;
        private readonly IPairStreamInfoRepository _streamInfoService;

        private readonly BybitFuturesKlineMessageQueue _receivedKlineQueue;
        private readonly BybitFuturesTradeMessageQueue _receivedTradeQueue;
        private readonly BybitFuturesZeroMqTradeQueue _zmqTradeQueue;
        private readonly BybitFuturesZeroMqDepthQueue _pubDepthQueue;
        private readonly BybitFuturesRedisSavingDataQueue _redisQueue;

        public BybitFuturesWorker(IBybitFuturesService bybitApiClient, IPairInfoRepository pairService,
            IMemoryCache cache, ICandleService candleService, IPairStreamInfoRepository streamInfoService,
            BybitFuturesKlineMessageQueue receivedKlineQueue, BybitFuturesTradeMessageQueue receivedTradeQueue,
            BybitFuturesZeroMqTradeQueue zmqTradeQueue, BybitFuturesZeroMqDepthQueue pubDepthQueue,
            BybitFuturesRedisSavingDataQueue redisQueue)
        {
            _bybitApiClient = bybitApiClient;
            _pairService = pairService;
            _cache = cache;
            _candleService = candleService;
            _streamInfoService = streamInfoService;
            _receivedKlineQueue = receivedKlineQueue;
            _receivedTradeQueue = receivedTradeQueue;
            _zmqTradeQueue = zmqTradeQueue;
            _pubDepthQueue = pubDepthQueue;
            _redisQueue = redisQueue;
            _logger = LogManager.GetLogger(typeof(BybitFuturesWorker));
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"STARTED");

            IEnumerable<BybitFuturesSymbol> symbols = null;
            try
            {
                o:
                Thread.Sleep(10000);
                symbols = await _bybitApiClient.GetSymbolsAsync();
                if (symbols == null)
                {
                    _logger.Error("Faild to get symbols. Trying again.");
                    goto o;
                }
            }
            catch { }

            _logger.Info($"Creating or updating PairInfos using symbols.");
            var symbolsList = symbols.ToList();

            await UpdateCandleSymbols(symbolsList);
            await UpdateFundingRateSymbols(symbolsList);
            await UpdateOpenIntersetSymbol(symbolsList);
            await UpdateLiquidationSymbols(symbolsList);

            await base.StartAsync(cancellationToken);
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"STOP");
            return base.StopAsync(cancellationToken);
        }
        
        private async Task UpdateCandleSymbols(List<BybitFuturesSymbol> symbols)
        {
            int updateCount = 0;
            int createCount = 0;
            foreach (var symbol in symbols)
            {
                var pair = await _pairService.Get(Exchange, symbol.Name);
                if (pair != null)
                {
                    // is state or precision changed?
                    if (pair.IsListed != symbol.IsListed() || pair.QuoteAssetPrecision != symbol.QuoteAssetPrecision)
                    {
                        pair.IsListed = symbol.IsListed();
                        pair.QuoteAssetPrecision = symbol.QuoteAssetPrecision;
                        _pairService.Update(pair.PairId, pair);
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
                        QuoteAssetPrecision = symbol.QuoteAssetPrecision,
                        TimezoneDailyCloseFormat = "UTC 00:00",
                        TimeFrameOptions = timeFrameOptions,
                        IsListed = symbol.IsListed()
                    };
                    _pairService.Create(pairInfo);
                    createCount++;
                }
            }

            _logger.Info($"{updateCount} updates and {createCount} new candle pair(s) for {Exchange}.");
        }
        
        private async Task UpdateFundingRateSymbols(IEnumerable<BybitFuturesSymbol> symbols)
        {
            int updateCount = 0;
            int createCount = 0;
            DateTime now = DateTime.Now;
            foreach (var symbol in symbols)
            {
                string name = symbol.GetFundingRateSymbolName();
                var pair = await _pairService.Get(Exchange, name);
                if (pair != null)
                {
                    if (pair.IsListed != symbol.IsListed()) // if state was changed
                    {
                        pair.IsListed = symbol.IsListed();
                        _pairService.Update(pair.PairId, pair);
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
                            StartTimeMax = now.ToUnixTimestamp(),
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "15m",
                            StartTimeMax = now.ToUnixTimestamp(),
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "4H",
                            StartTimeMax = now.ToUnixTimestamp(),
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "1D",
                            StartTimeMax = now.ToUnixTimestamp(),
                            EndTimeMax = null,
                        }
                    };

                    var pairInfo = new PairInfo()
                    {
                        Exchange = Exchange,
                        IsAvailableFootprint = false,
                        IsLinechart = false,
                        IsAvailableHeatmap = false,
                        IsAvailableVolume = false,
                        Symbol = name,
                        QuoteAssetPrecision = 8,
                        TimezoneDailyCloseFormat = "UTC 00:00",
                        TimeFrameOptions = timeFrameOptions,
                        IsListed = symbol.IsListed()
                    };
                    _pairService.Create(pairInfo);
                    createCount++;
                }
            }

            _logger.Info($"{updateCount} updates and {createCount} new funding rate pair(s) for {Exchange}.");
        }

        private async Task UpdateOpenIntersetSymbol(IEnumerable<BybitFuturesSymbol> symbols)
        {
            int updateCount = 0;
            int createCount = 0;
            foreach (var symbol in symbols)
            {
                string name = symbol.GetOpenIntersetSymbolName();
                var pair = await _pairService.Get(Exchange, name);
                if (pair != null)
                {
                    if (pair.IsListed != symbol.IsListed()) // if state was changed
                    {
                        pair.IsListed = symbol.IsListed();
                        _pairService.Update(pair.PairId, pair);
                        updateCount++;
                    }
                }
                else
                {
                    List<TimeFrameOption> timeFrameOptions = new List<TimeFrameOption>()
                    {
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
                            TimeFrame = "1D",
                            StartTimeMax = null,
                            EndTimeMax = null,
                        }
                    };

                    var pairInfo = new PairInfo()
                    {
                        Exchange = Exchange,
                        IsAvailableFootprint = false,
                        IsLinechart = true,
                        IsAvailableHeatmap = false,
                        IsAvailableVolume = false,
                        Symbol = name,
                        QuoteAssetPrecision = 8,
                        TimezoneDailyCloseFormat = "UTC 00:00",
                        TimeFrameOptions = timeFrameOptions,
                        IsListed = symbol.IsListed()
                    };
                    _pairService.Create(pairInfo);
                    createCount++;
                }
            }

            _logger.Info($"{updateCount} updates and {createCount} new open interset pair(s) for {Exchange}.");
        }

        private async Task UpdateLiquidationSymbols(IEnumerable<BybitFuturesSymbol> symbols)
        {
            int updateCount = 0;
            int createCount = 0;
            DateTime now = DateTime.Now;
            foreach (var symbol in symbols)
            {
                symbol.GetLiquidationSymbolNames(out string name, out string sellName, out string buyName);
                await UpdateSymbols(name, symbol);
                await UpdateSymbols(sellName, symbol);
                await UpdateSymbols(buyName, symbol);
            }

            _logger.Info($"{updateCount} updates and {createCount} new open interset pair(s) for {Exchange}.");

            async Task UpdateSymbols(string name, BybitFuturesSymbol symbol)
            {
                var pair = await _pairService.Get(Exchange, name);
                if (pair != null)
                {
                    if (pair.IsListed != symbol.IsListed()) // if state was changed
                    {
                        pair.IsListed = symbol.IsListed();
                        _pairService.Update(pair.PairId, pair);
                        updateCount++;
                    }
                }
                else
                {
                    List<TimeFrameOption> timeFrameOptions = new List<TimeFrameOption>()
                    {
                        new TimeFrameOption()
                        {
                            TimeFrame = "15m",
                            StartTimeMax = now.ToUnixTimestamp(),
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "4H",
                            StartTimeMax = now.ToUnixTimestamp(),
                            EndTimeMax = null,
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "1D",
                            StartTimeMax = now.ToUnixTimestamp(),
                            EndTimeMax = null,
                        }
                    };

                    var pairInfo = new PairInfo()
                    {
                        Exchange = Exchange,
                        IsAvailableFootprint = false,
                        IsLinechart = true,
                        IsAvailableHeatmap = false,
                        IsAvailableVolume = false,
                        Symbol = name,
                        QuoteAssetPrecision = 8,
                        TimezoneDailyCloseFormat = "UTC 00:00",
                        TimeFrameOptions = timeFrameOptions,
                        IsListed = symbol.IsListed()
                    };
                    _pairService.Create(pairInfo);
                    createCount++;
                }
            }
        }

        
        private void LogStopStartStreaming(List<PairInfo> pairInfos)
        {
            Task.Run(async () =>
            {
                var streamInfos = await _streamInfoService.GetMany(Exchange);
                var BTCUSDT = pairInfos.FirstOrDefault(p => p.Symbol == "BTCUSDT");

                long now = DateTime.UtcNow.ToUnixTimestamp();
                foreach (var timeframe in BTCUSDT.TimeFrameOptions)
                {
                    long lastStop = default;
                    try
                    {
                        lastStop = await _candleService.GetLastCandleOpenTime(Exchange, BTCUSDT.Symbol, timeframe.TimeFrame);
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
                            await _streamInfoService.Update(si.Id, si);
                        }
                    }
                    if (createList.Any())
                        await _streamInfoService.CreateMany(createList);
                }
            });
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(async () =>
            {
                int tradeRange = 20;
                int depthRange = 50;
                var pairInfos = (await _pairService.GetListed(Exchange)).RemoveCostumePairs();
                pairInfos = pairInfos.Where(p => p.Symbol.EndsWith("USDT")).ToList();
                _logger.Info($"{pairInfos.Count} pairs and {tradeRange} pairs per WS connection.");
                _logger.Info($"Logging stop/start streaming.");
                LogStopStartStreaming(pairInfos);

                #region Start Trade Kline Streaming
                
                var inverseKlineStreaming = NewInverseKlineStreaming();
                inverseKlineStreaming.Connect(pairInfos[0].TimeFrameOptions);
                var inverseTradeStreaming = NewInverseTradeStreaming();
                inverseTradeStreaming.Connect();
                for (int i = 0; i < pairInfos.Count - 1;)
                {
                    int r = Math.Min(tradeRange, pairInfos.Count - i);
                    var symbols = pairInfos.GetRange(i, r).ToList();

                    var streaming = NewUsdtKlineStreaming();
                    var tradeStreaming = NewUsdtTradeStreaming();
                    streaming.Connect(symbols);
                    tradeStreaming.Connect(symbols);
                    i += symbols.Count;
                }
                
                #endregion

                #region Start Depth Streaming
                
                var inverseDepthStreaming = NewInverseDepthStreaming(stoppingToken);
                inverseDepthStreaming.Connect();
                for (int i = 0; i < pairInfos.Count - 1;)
                {
                    int r = Math.Min(depthRange, pairInfos.Count - i);
                    var symbols = pairInfos.GetRange(i, r).ToList();

                    var streaming = NewUsdtDepthStreaming(stoppingToken);
                    streaming.Connect(symbols);
                    i += symbols.Count;
                }
                
                #endregion

                Thread.Sleep(30000);

                // Restarting offline WebSocket clients
                while (!stoppingToken.IsCancellationRequested)
                {
                    pairInfos = (await _pairService.GetListed(Exchange)).RemoveCostumePairs();
                    List<PairInfo> startKlinePairs = new List<PairInfo>();
                    List<PairInfo> startTradePairs = new List<PairInfo>();
                    List<PairInfo> startDepthPairs = new List<PairInfo>();
                    pairInfos = pairInfos.Where(p => p.Symbol.EndsWith("USDT") || p.Symbol == "BTCUSD").ToList();

                    foreach (var pair in pairInfos)
                    {
                        bool symbolKlineIsStreaming = _cache.TryGetKlineSymbolIsStreaming(pair.Exchange, pair.Symbol);
                        if (!symbolKlineIsStreaming)
                        {
                            startKlinePairs.Add(pair);
                        }
                        
                        bool symbolTradeIsStreaming = _cache.TryGetTradeSymbolIsStreaming(pair.Exchange, pair.Symbol);
                        if (!symbolTradeIsStreaming)
                        {
                            startTradePairs.Add(pair);
                        }

                        bool symbolDepthIsStreaming = _cache.TryGetDepthSymbolIsStreaming(pair.Exchange, pair.Symbol);
                        if (!symbolDepthIsStreaming)
                        {
                            startDepthPairs.Add(pair);
                        }
                    }


                    #region Restart Depth Streaming
                    
                    var inverseDepthPresenter = startDepthPairs.FirstOrDefault(p => p.Symbol == "BTCUSD");
                    if (inverseDepthPresenter != null)
                    {
                        _logger.Info("Restarting Inverse depth streaming.");
                        var streaming = NewInverseDepthStreaming(stoppingToken);
                        streaming.Connect();
                        startDepthPairs.Remove(inverseDepthPresenter);
                    }
                    
                    for (int i = 0; i < startDepthPairs.Count; i += depthRange)
                    {
                        int r = Math.Min(depthRange, startDepthPairs.Count - i);
                        var symbols = startDepthPairs.GetRange(i, r).ToList();
                        _logger.Info($"Restarting {String.Join(", ", symbols.Select(s => s.Symbol).ToArray())} depth streaming.");
                        var streaming = NewUsdtDepthStreaming(stoppingToken);
                        streaming.Connect(symbols);
                        i += symbols.Count;
                    }
                    
                    #endregion

                    #region Restart Kline Streaming
                    
                    var inverseKlinePresenter = startKlinePairs.FirstOrDefault(p => p.Symbol == "BTCUSD");
                    if (inverseKlinePresenter != null)
                    {
                        _logger.Info("Restarting Inverse kline streaming.");
                        var streaming = NewInverseKlineStreaming();
                        streaming.Connect(pairInfos[0].TimeFrameOptions);
                        startKlinePairs.Remove(inverseKlinePresenter);
                    }
                    
                    for (int i = 0; i < startKlinePairs.Count; i += tradeRange)
                    {
                        int r = Math.Min(tradeRange, startKlinePairs.Count - i);
                        var symbols = startKlinePairs.GetRange(i, r).ToList();
                        _logger.Info($"Restarting {String.Join(", ", symbols.Select(s => s.Symbol).ToArray())} kline streaming.");
                        var streaming = NewUsdtKlineStreaming();
                        streaming.Connect(symbols);
                        i += symbols.Count;
                    }
                    
                    #endregion
                    
                    #region Restart Trade Streaming
                    
                    var inverseTradePresenter = startTradePairs.FirstOrDefault(p => p.Symbol == "BTCUSD");
                    if (inverseTradePresenter != null)
                    {
                        _logger.Info("Restarting Inverse trade streaming.");
                        var streaming = NewInverseTradeStreaming();
                        streaming.Connect();
                        startTradePairs.Remove(inverseTradePresenter);
                    }
                    
                    for (int i = 0; i < startTradePairs.Count; i += tradeRange)
                    {
                        int r = Math.Min(tradeRange, startTradePairs.Count - i);
                        var symbols = startTradePairs.GetRange(i, r).ToList();
                        _logger.Info($"Restarting {String.Join(", ", symbols.Select(s => s.Symbol).ToArray())} trade streaming.");
                        var tradeStreaming = NewUsdtTradeStreaming();
                        tradeStreaming.Connect(symbols);
                        i += symbols.Count;
                    }
                    
                    #endregion

                    Thread.Sleep(30000);
                }
            }).Start();

            return Task.CompletedTask;
        }
        
        // Kline streaming instances
        private BybitUsdtFuturesKlineStreaming NewUsdtKlineStreaming() =>
            new(_cache, _receivedKlineQueue);
        private BybitInverseFuturesKlineStreaming NewInverseKlineStreaming() =>
            new(_cache, _receivedKlineQueue);
        
        // Trade streaming instances
        private BybitUsdtFuturesTradeStreaming NewUsdtTradeStreaming() =>
            new(_cache, _zmqTradeQueue, _receivedTradeQueue);
        private BybitInverseFuturesTradeStreaming NewInverseTradeStreaming() =>
            new(_cache, _zmqTradeQueue, _receivedTradeQueue);
        
        // Depth streaming instances
        private BybitUsdtFuturesDepthStreaming NewUsdtDepthStreaming(CancellationToken stoppingToken) =>
            new(_pubDepthQueue, _redisQueue, _cache, stoppingToken);
        private BybitInverseFuturesDepthStreaming NewInverseDepthStreaming(CancellationToken stoppingToken) =>
            new(_pubDepthQueue, _redisQueue, _cache, stoppingToken);
    }
}