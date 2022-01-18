using DataLayer;
using ExchangeModels.BinanceFutures;
using ExchangeModels.BybitFutures;
using ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues.BybitFutures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utf8Json;
using Utilities;
using WatsonWebsocket;

namespace ServerApplication.Bybit.StreamingServices.BybitFutures
{
    public class BybitUsdtFuturesTradeStreaming : IDisposable
    {
        #region Private Variables

        private List<PairInfo> _usdtPairs;
        private const string Exchange = ApplicationValues.BybitFuturesName;
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;

        // clients
        private readonly BybitFuturesUsdtTradeWsClient _usdtClient;

        // queues
        private readonly BybitFuturesZeroMqTradeQueue _zeroMqTrade;
        private readonly BybitFuturesTradeMessageQueue _receivedTrade;

        #endregion

        public BybitUsdtFuturesTradeStreaming(IMemoryCache cache, BybitFuturesZeroMqTradeQueue zeroMqTrade,
            BybitFuturesTradeMessageQueue receivedTrade)
        {
            this._cache = cache;
            _usdtClient = new();
            _logger = LogManager.GetLogger(typeof(BybitUsdtFuturesTradeStreaming));
            _usdtPairs = new List<PairInfo>();
            this._zeroMqTrade = zeroMqTrade;
            this._receivedTrade = receivedTrade;
        }

        public void Connect(List<PairInfo> usdtPairs)
        {
            if (!usdtPairs.Any())
                return;

            this._usdtPairs = usdtPairs;

            _usdtClient.Client.MessageReceived += OnMessageReceived;
            _usdtClient.Client.ServerConnected += OnConnected;
            _usdtClient.Client.ServerDisconnected += OnDisconnected;
            _usdtClient.ConnectAsync().Wait();
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Disconnected");
            _cache.SetTradeSymbolIsStreaming(Exchange, _usdtPairs.Select(p => p.Symbol).ToArray(), false);
            Dispose();
        }

        private async void OnConnected(object sender, EventArgs e)
        {
            _logger.Info("Bybit Futures USDT Client Connected.");
            await _usdtClient.SubscribeToSymbolsAsync(_usdtPairs.ToArray());
            _cache.SetTradeSymbolIsStreaming(Exchange, _usdtPairs.Select(p => p.Symbol).ToArray(), true);
        }
        
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (!e.Data[2].Equals(0x73)) // s | if it is not the 'success' message
            {
                var trades = JsonSerializer.Deserialize<BybitMessage<BybitFuturesUsdtTrade[]>>(e.Data);
                foreach (var trade in trades.Data)
                {
                    if (trade.Symbol.EndsWith("USDT"))
                    {
                        _receivedTrade.Enqueue(trade.GetTrade());
                        _zeroMqTrade.Enqueue(new()
                        {
                            TradeTime = trade.TradeTimeMs.ToString(),
                            Amount = (trade.GetSide() == TradeSide.BUY ? trade.Size : -trade.Size).ToString(),
                            Price = trade.Price.ToString(),
                            Symbol = trade.Symbol
                        });
                    }
                }
            }
        }
        

        public void Dispose()
        {
            _usdtClient?.Dispose();
        }
    }
}