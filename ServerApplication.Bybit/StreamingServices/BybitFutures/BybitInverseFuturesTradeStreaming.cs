using System;
using ExchangeModels.BinanceFutures;
using ExchangeModels.BybitFutures;
using ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues.BybitFutures;
using Utilities;
using WatsonWebsocket;
using Utf8Json;

namespace ServerApplication.Bybit.StreamingServices.BybitFutures
{
    public class BybitInverseFuturesTradeStreaming : IDisposable
    {
        private const string Exchange = ApplicationValues.BybitFuturesName;
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        // client
        private readonly BybitFuturesInverseTradeWsClient _inverseClient;
        // queues
        private readonly BybitFuturesZeroMqTradeQueue _zeroMqTrade;
        private readonly BybitFuturesTradeMessageQueue _receivedTrade;

        public BybitInverseFuturesTradeStreaming(IMemoryCache cache, 
            BybitFuturesZeroMqTradeQueue zeroMqTrade, BybitFuturesTradeMessageQueue receivedTrade)
        {
            _cache = cache;
            _logger = LogManager.GetLogger(typeof(BybitInverseFuturesTradeStreaming));
            _inverseClient = new();
            _zeroMqTrade = zeroMqTrade;
            _receivedTrade = receivedTrade;
        }

        public void Connect()
        {
            _inverseClient.Client.MessageReceived += OnMessageReceived;
            _inverseClient.Client.ServerConnected += OnConnected;
            _inverseClient.Client.ServerDisconnected += OnDisconnected;
            _inverseClient.ConnectAsync().Wait();
        }
        
        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Disconnected");
            // BTCUSD represents all other derivatives in inverse exchange here
            _cache.SetTradeSymbolIsStreaming(Exchange, "BTCUSD", false);
            Dispose();
        }
        
        private async void OnConnected(object sender, EventArgs e)
        {
            _logger.Info("Bybit Futures Inverse Client Connected.");
            await _inverseClient.SubToAllSymbols();
            // BTCUSD represents all other derivatives in inverse exchange here
            _cache.SetTradeSymbolIsStreaming(Exchange, "BTCUSD", true);
        }
        
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (!e.Data[2].Equals(0x73)) // s | if it is not the 'success' message
            {
                var trades = JsonSerializer.Deserialize<BybitMessage<BybitFuturesTrade[]>>(e.Data);
                foreach (var trade in trades.Data)
                {
                    if (trade.Symbol.EndsWith("USD"))
                    {
                        _receivedTrade.Enqueue(trade);
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
            _inverseClient?.Dispose();
        }
    }
}