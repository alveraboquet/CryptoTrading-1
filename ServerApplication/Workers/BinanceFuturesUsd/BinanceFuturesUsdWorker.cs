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
using Binance.Net.Objects.Futures.MarketData;

namespace ServerApplication.Workers
{
    public class BinanceFuturesUsdWorker : BackgroundService
    {
        private readonly ILog _logger;
        private IBinanceFuturesUsdtServices _client;
        private IPairInfoRepository _pairRepo;
        private IMemoryCache _cache;
        private readonly ICandleService _candleRepo;
        private const string Exchange = ApplicationValues.BinanceUsdName;
        private readonly IPairStreamInfoRepository _streamInfo;

        private readonly BinanceFuturesUsdKlineCalculate _receivedKline;
        private readonly BinanceFuturesUsdTradeCalculate _receivedTrade;
        private readonly BinanceFuturesUsdZeroMqTradeQueue _zmqTrade;
        private readonly BinanceFuturesUsdZeroMqDepthQueue _pubDepthQueue;
        private readonly BinanceFuturesUsdRedisSavingDataQueue _redisQueue;
        public BinanceFuturesUsdWorker(IBinanceFuturesUsdtServices client, IPairInfoRepository pairRepo,
            BinanceFuturesUsdKlineCalculate receivedKline, BinanceFuturesUsdTradeCalculate receivedTrade,
            BinanceFuturesUsdZeroMqTradeQueue zmqTrade, BinanceFuturesUsdZeroMqDepthQueue pubDepthQueue,
            BinanceFuturesUsdRedisSavingDataQueue redisQueue, IMemoryCache cache,
            IPairStreamInfoRepository streamInfo, ICandleService candleRepo)
        {
            _candleRepo = candleRepo;
            _streamInfo = streamInfo;
            _cache = cache;
            _redisQueue = redisQueue;
            _zmqTrade = zmqTrade;
            _pubDepthQueue = pubDepthQueue;
            _receivedKline = receivedKline;
            _receivedTrade = receivedTrade;
            _pairRepo = pairRepo;
            _client = client;
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdWorker));
        }

        private async Task UpdateCandleSymbols(IEnumerable<BinanceFuturesUsdtSymbol> symbols)
        {
            int updateCount = 0;
            int createCount = 0;
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
                            TimeFrame = "1H",
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
                        },
                        new TimeFrameOption()
                        {
                            TimeFrame = "3D",
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
                        IsListed = symbol.IsListed()
                    };
                    _pairRepo.Create(pairInfo);
                    createCount++;
                }
            }

            _logger.Info($"{updateCount} updates and {createCount} new candle pair(s) for {Exchange}.");
        }

        private async Task UpdateFundingRateSymbols(IEnumerable<BinanceFuturesUsdtSymbol> symbols)
        {
            int updateCount = 0;
            int createCount = 0;
            DateTime now = DateTime.Now;
            foreach (var symbol in symbols)
            {
                string name = symbol.GetFundingRateSymbolName();
                var pair = await _pairRepo.Get(Exchange, name);
                if (pair != null)
                {
                    if (pair.IsListed != symbol.IsListed()) // if state was changed
                    {
                        pair.IsListed = symbol.IsListed();
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
                    _pairRepo.Create(pairInfo);
                    createCount++;
                }
            }

            _logger.Info($"{updateCount} updates and {createCount} new funding rate pair(s) for {Exchange}.");
        }

        private async Task UpdateOpenIntersetSymbol(IEnumerable<BinanceFuturesUsdtSymbol> symbols)
        {
            int updateCount = 0;
            int createCount = 0;
            foreach (var symbol in symbols)
            {
                string name = symbol.GetOpenIntersetSymbolName();
                var pair = await _pairRepo.Get(Exchange, name);
                if (pair != null)
                {
                    if (pair.IsListed != symbol.IsListed()) // if state was changed
                    {
                        pair.IsListed = symbol.IsListed();
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
                    _pairRepo.Create(pairInfo);
                    createCount++;
                }
            }

            _logger.Info($"{updateCount} updates and {createCount} new open interset pair(s) for {Exchange}.");
        }

        private async Task UpdateLiquidationSymbols(IEnumerable<BinanceFuturesUsdtSymbol> symbols)
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

            async Task UpdateSymbols(string name, BinanceFuturesUsdtSymbol symbol)
            {
                var pair = await _pairRepo.Get(Exchange, name);
                if (pair != null)
                {
                    if (pair.IsListed != symbol.IsListed()) // if state was changed
                    {
                        pair.IsListed = symbol.IsListed();
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
                    _pairRepo.Create(pairInfo);
                    createCount++;
                }
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"STARTED");

            IEnumerable<BinanceFuturesUsdtSymbol> symbols = null;
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

            _logger.Info($"Creating or updating PairInfos using symbols.");

            await UpdateCandleSymbols(symbols);
            await UpdateFundingRateSymbols(symbols);
            await UpdateOpenIntersetSymbol(symbols);
            await UpdateLiquidationSymbols(symbols);

            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"STOPED");
            return base.StopAsync(cancellationToken);
        }

        private void LogStopStartStreaming(List<PairInfo> pairInfos)
        {
            Task.Run(async () =>
            {
                var streamInfos = await _streamInfo.GetMany(Exchange);
                var symbol = pairInfos.FirstOrDefault(p => p.Symbol == "BTCUSDT");
                //var symbol = pairInfos.FirstOrDefault(p => p.Symbol == "ETHUSDT");
                long now = DateTime.UtcNow.ToUnixTimestamp();
                foreach (var timeframe in symbol.TimeFrameOptions)
                {
                    long lastStop = default;
                    try
                    {
                        lastStop = await _candleRepo.GetLastCandleOpenTime(Exchange, symbol.Symbol, timeframe.TimeFrame);
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
            });
        }


        private BinanceFuturesUsdTradeKlineStreaming NewTradeKlineStreaming() =>
            new BinanceFuturesUsdTradeKlineStreaming(this._cache, _receivedKline, _receivedTrade, _zmqTrade);
        private BinanceFuturesUsdDepthStreaming NewDepthStreaming(CancellationToken stoppingToken) =>
            new BinanceFuturesUsdDepthStreaming(stoppingToken, this._pubDepthQueue,
                this._redisQueue, this._cache);

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(async () =>
            {
                int tradeRange = 20;
                int depthRange = 50;
                var pairInfos = (await _pairRepo.GetListed(Exchange)).RemoveCostumePairs();
                _logger.Info($"{pairInfos.Count} pairs and {tradeRange} pairs per WS connection.");
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
