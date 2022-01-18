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

namespace ServerApplication.StreamingServices
{
    public class BinanceTradeKlineStreaming : IDisposable
    {
        #region Private Variables

        private IEnumerable<PairInfo> _pairs;
        private readonly string exchange = ApplicationValues.BinanceName;
        private IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly BinanceTradeKlineWSClient _client;

        public const byte TradeBin = 0x61;
        public const byte KlineBin = 0x6B;

        private readonly BinanceTradeCalculate _recievedTrade;
        private readonly BinanceKlineCalculate _receivedKline;

        // Redis Queues
        private readonly BinanceZeroMQTradeQueue _zeroMQTradeQueue;
        #endregion

        public BinanceTradeKlineStreaming(IMemoryCache cache, BinanceZeroMQTradeQueue tradequeue,
            BinanceKlineCalculate klineQueue, BinanceTradeCalculate tradeQueue)
        {
            _recievedTrade = tradeQueue;
            _receivedKline = klineQueue;

            _zeroMQTradeQueue = tradequeue;
            _cache = cache;

            _client = new BinanceTradeKlineWSClient();
            _logger = LogManager.GetLogger(typeof(BinanceTradeKlineStreaming));
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

        #region Event Handlers

        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Disconnected");
            _cache.SetTradeKlineSymbolIsStreaming(this.exchange, _pairs.Select(p => p.Symbol).ToArray(), false);
            Dispose();
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _logger.Info("Connected");
            _cache.SetTradeKlineSymbolIsStreaming(this.exchange, _pairs.Select(p => p.Symbol).ToArray(), true);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            switch (args.Data[6])
            {
                case KlineBin:
                    _receivedKline.Enqueue(args.Data);
                    break;

                case TradeBin:
                    _recievedTrade.Enqueue(args.Data);
                    _zeroMQTradeQueue.Enqueue(args.Data);
                    break;
            }
        }

        #endregion

        public void Dispose() => _client?.Dispose();
    }
}