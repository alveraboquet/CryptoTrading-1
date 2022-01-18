using DataLayer;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;
using Utf8Json;
using WatsonWebsocket;
using ExchangeServices.Services.Exchanges.Bybit.Socket;
using ExchangeModels.Bybit;

namespace ServerApplication.Bybit.StreamingServices
{
    public class BybitKlineStreaming : IDisposable
    {
        #region Private Variables

        private IEnumerable<PairInfo> _pairs;
        private const string Exchange = ApplicationValues.BybitName;
        private IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly BybitKlineClient _client;

        private readonly BybitKlineMessageQueue _receivedKline;

        #endregion

        public BybitKlineStreaming(IMemoryCache cache, BybitKlineMessageQueue klineQueue)
        {
            _receivedKline = klineQueue;

            _cache = cache;

            _client = new();
            _logger = LogManager.GetLogger(typeof(BybitKlineStreaming));
        }

        public void Connect(IEnumerable<PairInfo> pairs)
        {
            if (!pairs.Any())
                return;

            _pairs = pairs;

            _client.Client.MessageReceived += OnMessageReceived;
            _client.Client.ServerConnected += OnConnected;
            _client.Client.ServerDisconnected += OnDisconnected;
            _client.ConnectAsync().Wait();
        }

        #region Event Handlers

        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Disconnected");
            _cache.SetKlineSymbolIsStreaming(Exchange, _pairs.Select(p => p.Symbol).ToArray(), false);
            Dispose();
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _client.SubscribeToSymbolsAsync(_pairs.ToArray()).Wait();
            _logger.Info("Connected");
            _cache.SetKlineSymbolIsStreaming(Exchange, _pairs.Select(p => p.Symbol).ToArray(), true);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            try
            {
                var kline = JsonSerializer.Deserialize<KlineMessage>(args.Data);
                if (kline.Data.Length == 0) return;
                if (kline.Data == null)
                {
                    var json = Encoding.ASCII.GetString(args.Data);
                    _logger.Error(json);
                }
                _receivedKline.Enqueue(kline);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        #endregion

        public void Dispose() => _client?.Dispose();
    }
}