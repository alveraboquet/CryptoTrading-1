using System;
using System.Collections.Generic;
using DataLayer.Models;
using ExchangeModels.BybitFutures;
using ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Models;
using ServerApplication.Bybit.Queues.BybitFutures;
using Utf8Json;
using Utilities;
using WatsonWebsocket;

namespace ServerApplication.Bybit.StreamingServices.BybitFutures
{
    public class BybitInverseFuturesKlineStreaming : IDisposable
    {
        private const string Exchange = ApplicationValues.BybitFuturesName;
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        private List<TimeFrameOption> _timeframes = new List<TimeFrameOption>();

        private readonly BybitFuturesInverseKlineWsClient _inverseClient;
        private readonly BybitFuturesKlineMessageQueue _receivedKline;

        public BybitInverseFuturesKlineStreaming(IMemoryCache cache, BybitFuturesKlineMessageQueue receivedKline)
        {
            _cache = cache;
            _logger = LogManager.GetLogger(typeof(BybitInverseFuturesKlineStreaming));
            _inverseClient = new();
            _receivedKline = receivedKline;
        }

        public void Connect(List<TimeFrameOption> timeframes)
        {
            _inverseClient.Client.MessageReceived += OnMessageReceived;
            _inverseClient.Client.ServerConnected += OnConnected;
            _inverseClient.Client.ServerDisconnected += OnDisconnected;
            _inverseClient.ConnectAsync().Wait();
        }
        
        private async void OnConnected(object sender, EventArgs e)
        {
            _logger.Info("Bybit Futures Inverse Client Connected.");
            await _inverseClient.SubscribeToSymbolsAsync(_timeframes);
            // BTCUSD represents all other derivatives in inverse exchange here
            _cache.SetKlineSymbolIsStreaming(Exchange, "BTCUSD", true);
        }
        
        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Disconnected");
            // BTCUSD represents all other derivatives in inverse exchange here
            _cache.SetKlineSymbolIsStreaming(Exchange, "BTCUSD", false);
            Dispose();
        }
        
        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            try
            {
                if (!args.Data[2].Equals(0x73)) // s | if it is not the 'success' message
                {
                    var klines = JsonSerializer.Deserialize<BybitMessage<BybitFuturesCandle[]>>(args.Data);
                    // we only need inverse perpetual symbols
                    if (klines.Topic.EndsWith("USD"))
                    {
                        foreach (var kline in klines.Data)
                            _receivedKline.Enqueue(new BybitFuturesExtendedCandle(kline, klines.Topic));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        public void Dispose()
        {
            _inverseClient?.Dispose();
        }
    }
}