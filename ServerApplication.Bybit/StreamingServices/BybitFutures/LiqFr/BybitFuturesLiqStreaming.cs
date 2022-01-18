using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataLayer;
using ExchangeModels.BinanceFutures;
using ExchangeModels.BybitFutures;
using ExchangeServices.Services.Exchanges.Bybit.Socket;
using ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues.BybitFutures;
using Utf8Json;
using WatsonWebsocket;

namespace ServerApplication.Bybit.StreamingServices.BybitFutures
{
    public class BybitFuturesLiqStreaming : IDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        private IEnumerable<PairInfo> _pairs;
        
        // clients
        private readonly BybitFuturesInverseLiqWsClient _inverseClient;
        private readonly BybitFuturesUsdtLiqWsClient _usdClient;
        
        // queues
        private readonly BybitFuturesLiquidationCalculateQueue _queue;

        public BybitFuturesLiqStreaming(IMemoryCache cache, BybitFuturesLiquidationCalculateQueue queue)
        {
            _cache = cache;
            _queue = queue;
            _inverseClient = new();
            _usdClient = new();
            _logger = LogManager.GetLogger(typeof(BybitFuturesLiqStreaming));
        }

        public void Connect(IEnumerable<PairInfo> pairs)
        {
            _pairs = pairs.ToList();
            
            if (!_pairs.Any())
                return;

            _usdClient.Client.ServerConnected += OnUsdtConnected;
            _usdClient.Client.ServerDisconnected += OnDisconnected;
            _usdClient.Client.MessageReceived += OnMessageReceived;

            _inverseClient.Client.ServerConnected += OnInverseConnected;
            _inverseClient.Client.ServerDisconnected += OnDisconnected;
            _inverseClient.Client.MessageReceived += OnMessageReceived;

            _inverseClient.ConnectAsync().Wait();
            _usdClient.ConnectAsync().Wait();
        }
        
        #region Event Handlers

        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Liq Disconnected");
            _cache.SetBybitFuturesIsLiqStreaming(false);
            // will dispose both websocket clients
            Dispose();
        }

        private async void OnUsdtConnected(object sender, EventArgs e)
        {
            // subscribing to all symbols
            await _usdClient.SubscribeToSymbolsAsync(_pairs.ToArray());
            _logger.Info("Bybit USDT Liquidation Connected.");
            if (_inverseClient.Client.Connected)
                _cache.SetBybitFuturesIsLiqStreaming(true);
        }
        
        private async void OnInverseConnected(object sender, EventArgs e)
        {
            // subscribing to all symbols
            await _inverseClient.SubToAllSymbols();
            _logger.Info("Bybit Inverse Liquidation Connected.");
            if (_usdClient.Client.Connected)
                _cache.SetBybitFuturesIsLiqStreaming(true);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (!e.Data[2].Equals(0x73))
            {
                try
                {
                    var message = JsonSerializer.Deserialize<BybitMessage<BybitLiquidationData>>(e.Data);
                    _logger.Info($"Liquidation {message.Data.Symbol}");
                    if (message.Data.Symbol.EndsWith("USD") || message.Data.Symbol.EndsWith("USDT"))
                        _queue.Enqueue(message.Data);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception);
                }
            }
        }
        
        #endregion

        public void Dispose()
        {
            _inverseClient?.Dispose();
            _usdClient?.Dispose();
        }
    }
}