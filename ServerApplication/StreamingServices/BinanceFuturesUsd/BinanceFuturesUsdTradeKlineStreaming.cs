using DataLayer;
using DataLayer.Models.Stream;
using ExchangeServices;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Caching;
using ServerApplication.Queues;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Utilities;
using Utf8Json;
using WatsonWebsocket;
using System.Text;
using ExchangeModels;
using ExchangeModels.Enums;

namespace ServerApplication.StreamingServices
{
    public class BinanceFuturesUsdTradeKlineStreaming : IDisposable
    {
        private IMemoryCache _cache;
        private IEnumerable<PairInfo> _pairs;
        private BinanceFuturesUsdTradeKlineWSClient _client;
        private readonly ILog _logger;
        private readonly string exchange = ApplicationValues.BinanceUsdName;
        private readonly BinanceFuturesUsdKlineCalculate _receivedKline;
        private readonly BinanceFuturesUsdTradeCalculate _receivedTrade;
        private readonly BinanceFuturesUsdZeroMqTradeQueue _zmqTrade;
        public BinanceFuturesUsdTradeKlineStreaming(IMemoryCache cache,
            BinanceFuturesUsdKlineCalculate receivedKline, BinanceFuturesUsdTradeCalculate receivedTrade,
            BinanceFuturesUsdZeroMqTradeQueue zmqTrade)
        {
            _zmqTrade = zmqTrade;
            _receivedKline = receivedKline;
            _receivedTrade = receivedTrade;
            this._cache = cache;
            _client = new BinanceFuturesUsdTradeKlineWSClient();
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdTradeKlineStreaming));
        }

        public void Connect(IEnumerable<PairInfo> pairs)
        {
            if (!pairs.Any())
                return;

            _client.Init(pairs.ToArray());
            _pairs = pairs;

            _client.Client.MessageReceived += OnMessageReceived;
            _client.Client.ServerConnected += OnConnected;
            _client.Client.ServerDisconnected += OnDisconnected;
            _client.Connect().Wait();
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Trade & Kline Disconnected");
            _cache.SetTradeKlineSymbolIsStreaming(this.exchange, _pairs.Select(p => p.Symbol).ToArray(), false);
            Dispose();
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _logger.Info("Trade & Kline Connected");
            _cache.SetTradeKlineSymbolIsStreaming(this.exchange, _pairs.Select(p => p.Symbol).ToArray(), true);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            if (BinanceFuturesUsdTradeKlineDetector.TryDetectMessageType(args.Data, out var type))
            {
                switch (type.Value)
                {
                    case BinanceFuturesUsdWebSocketStreams.Kline:
                        _receivedKline.Enqueue(args.Data);
                        break;
                    case BinanceFuturesUsdWebSocketStreams.Trade:
                        _receivedTrade.Enqueue(args.Data);
                        _zmqTrade.Enqueue(args.Data);
                        break;
                }
            }
        }

        public void Dispose() => _client?.Dispose();
    }

    public static class BinanceFuturesUsdTradeKlineDetector
    {
        public static bool TryDetectMessageType(byte[] msg, out BinanceFuturesUsdWebSocketStreams? type)
        {
            type = null;
            var typeBin = msg.Skip(11).Take(40).ToArray();
            for (int i = 0; i < typeBin.Length; i++)
            {
                byte bin = typeBin[i];
                if (bin.Equals(0x40)) // @
                {
                    // first character after `@`
                    var firstChar = typeBin[i + 1];
                    if (firstChar.Equals(0x6b)) // k
                    {
                        type = BinanceFuturesUsdWebSocketStreams.Kline;
                        return true;
                    }
                    else if (firstChar.Equals(0x61)) // a
                    {
                        type = BinanceFuturesUsdWebSocketStreams.Trade;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}