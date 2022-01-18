using DataLayer;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using Utf8Json;
using WatsonWebsocket;
using ExchangeServices;
using ExchangeModels.Bybit;

namespace ServerApplication.Bybit.StreamingServices
{
    public class BybitTradeStreaming : IDisposable
    {
        #region Private Variables

        private IEnumerable<PairInfo> _pairs;
        private const string Exchange = ApplicationValues.BybitName;
        private IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly BybitTradeWsClient _client;

        private readonly BybitTradeMessageQueue _receivedTrade;
        private readonly BybitZeroMQTradeQueue _zeroMqTrade;

        #endregion

        public BybitTradeStreaming(IMemoryCache cache, BybitTradeMessageQueue receivedTrade,
            BybitZeroMQTradeQueue zeroMqTrade)
        {
            _receivedTrade = receivedTrade;
            _zeroMqTrade = zeroMqTrade;

            _cache = cache;

            _client = new BybitTradeWsClient();
            _logger = LogManager.GetLogger(typeof(BybitTradeStreaming));
        }

        public void Connect(IEnumerable<PairInfo> pairs)
        {
            if (!pairs.Any())
                return;

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
            _cache.SetTradeSymbolIsStreaming(Exchange, _pairs.Select(p => p.Symbol).ToArray(), false);
            Dispose();
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _client.SubToAllSymbols(_pairs.ToArray()).Wait();
            _logger.Info("Connected");
            _cache.SetTradeSymbolIsStreaming(Exchange, _pairs.Select(p => p.Symbol).ToArray(), true);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            try
            {
                var trades = JsonSerializer.Deserialize<TradeMessage>(args.Data);
                if (!trades.IsFirstMessage)
                {
                    foreach (var trade in trades.Data)
                    {
                        trade.Symbol = trades.Symbol;
                        _receivedTrade.Enqueue(trade);
                        _zeroMqTrade.Enqueue(new ZeroMQ.Trade()
                        {
                            TradeTime = trade.Timestamp.ToString(),
                            Amount = (trade.IsBuy ? trade.Quantity : - trade.Quantity).ToString(),
                            Price = trade.Price.ToString(),
                            Symbol = trade.Symbol
                        });
                    }
                }
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
