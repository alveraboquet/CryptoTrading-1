using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DataLayer.Models.Stream;
using ExchangeModels.BinanceFutures;
using ExchangeModels.BybitFutures;
using ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues.BybitFutures;
using Utilities;
using Utf8Json;
using WatsonWebsocket;

namespace ServerApplication.Bybit.StreamingServices.BybitFutures
{
    public class BybitInverseFuturesDepthStreaming : IDisposable
    {
        private readonly string _exchange = ApplicationValues.BybitFuturesName;
        private readonly ILog _logger;
        private CancellationToken _token;

        private IMemoryCache _cache;

        // Client
        private BybitFuturesInverseDepthWsClient _inverseClient;

        // Queues
        private ConcurrentQueue<BybitOrderbookUpdate> _depthQueue;
        private BybitFuturesRedisSavingDataQueue _redisSavingQueue;
        private BybitFuturesZeroMqDepthQueue _pubDepthQueue;

        public BybitInverseFuturesDepthStreaming(BybitFuturesZeroMqDepthQueue pubDepthQueue,
            BybitFuturesRedisSavingDataQueue redisSavingQueue,
            IMemoryCache cache,
            CancellationToken workerToken)
        {
            _inverseClient = new();
            _logger = LogManager.GetLogger(typeof(BybitInverseFuturesDepthStreaming));
            _depthQueue = new();
            _token = workerToken;
            _redisSavingQueue = redisSavingQueue;
            _pubDepthQueue = pubDepthQueue;
            _cache = cache;
        }

        public void Connect()
        {
            _inverseClient.Client.ServerConnected += OnInverseConnected;
            _inverseClient.Client.MessageReceived += OnMessageReceived;
            _inverseClient.Client.ServerDisconnected += OnDisconnected;
            _inverseClient.ConnectAsync().Wait();
        }

        #region Event Handlers
        
        private async void OnInverseConnected(object sender, EventArgs e)
        {
            _logger.Info("Depth Inverse WS Connected.");
            await _inverseClient.SubToAllSymbols();
            // BTCUSD represents all other derivatives in inverse exchange here
            _cache.SetDepthSymbolIsStreaming(_exchange, "BTCUSD", true);
            Thread orderbookThread = new Thread(OrderbookThread);
            orderbookThread.Start();
        }
        
        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Depth WS Disconnected.");
            // BTCUSD represents all other derivatives in inverse exchange here
            _cache.SetDepthSymbolIsStreaming(_exchange, "BTCUSD", false);
            Dispose();
        }
        
        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            if (!args.Data[2].Equals(0x73)) // s | if it is not the 'success' message
            {
                OnMessageReceived(args.Data);
            }
        }

        private void OnMessageReceived(byte[] data)
        {
            var jsonString = Encoding.ASCII.GetString(data);
            if (jsonString.Contains("\"type\":\"snapshot\",")) return;
            if (!jsonString.Contains("USDT\",") && !jsonString.Contains("USD\",")) return;
            var depth = JsonSerializer.Deserialize<BybitMessage<BybitOrderbookUpdate>>(data);
            _depthQueue.Enqueue(depth.Data);

            var asks = new List<decimal[]>();
            var bids = new List<decimal[]>();
            string symbol = depth.Topic.Split('.')[1];

            foreach (var order in depth.Data.Update)
            {
                if (order.GetSide() == TradeSide.BUY)
                    bids.Add(new [] { order.Price, order.Size});
                else
                    asks.Add(new [] { order.Price, order.Size});
            }
            
            foreach (var order in depth.Data.Insert)
            {
                if (order.GetSide() == TradeSide.BUY)
                    bids.Add(new [] { order.Price, order.Size});
                else
                    asks.Add(new [] { order.Price, order.Size});
            }

            foreach (var order in depth.Data.Delete)
            {
                if (order.GetSide() == TradeSide.BUY)
                    bids.Add(new[] { order.Price, 0 });
                else
                    asks.Add(new[] { order.Price, 0 });
            }

            ZeroMQ.OrderBook pub = new ZeroMQ.OrderBook()
            {
                Symbol = symbol,
                Asks = asks.ToArray(),
                Bids = bids.ToArray(),
            };

            _pubDepthQueue.Enqueue(pub);
        }
        
        #endregion

        #region Orderbook Thread

        protected void OrderbookThread()
        {
            while (!_token.IsCancellationRequested)
            {
                while (!_token.IsCancellationRequested && _depthQueue.TryDequeue(out BybitOrderbookUpdate depth))
                {
                    UpdateOrderBook(depth);
                }

                Thread.Sleep(1);
            }
        }

        protected void UpdateOrderBook(BybitOrderbookUpdate update)
        {
            var symbol = DetectSymbol(update);
            StreamingOrderBook currentOrderBook = _cache.TryGetOrderBook(_exchange, symbol);
            if (currentOrderBook == null)
            {
                currentOrderBook = new StreamingOrderBook()
                {
                    Asks = new ConcurrentDictionary<decimal, decimal>(),
                    Bids = new ConcurrentDictionary<decimal, decimal>(),
                    Symbol = symbol,
                };
                _cache.SetOrderBook(_exchange, symbol, currentOrderBook);
            }

            foreach (var order in update.Delete)
            {
                try
                {
                    if (order.GetSide() == TradeSide.BUY)
                        currentOrderBook.Asks.TryRemove(order.Price, out _);
                    else
                        currentOrderBook.Bids.TryRemove(order.Price, out _);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message, ex);
                }
            }

            foreach (var order in update.Insert)
            {
                try
                {
                    if (order.GetSide() == TradeSide.BUY)
                        currentOrderBook.Asks[order.Price] = order.Size;
                    else
                        currentOrderBook.Bids[order.Price] = order.Size;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message, ex);
                }
            }

            foreach (var order in update.Update)
            {
                try
                {
                    if (order.GetSide() == TradeSide.BUY)
                    {
                        currentOrderBook.Asks[order.Price] = order.Size;
                    }
                    else
                    {
                        currentOrderBook.Bids[order.Price] = order.Size;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message, ex);
                }
            }

            // _logger.Info($"Orderbook [{currentOrderBook.Symbol}]: asks {currentOrderBook.Asks.Count} - bids {currentOrderBook.Bids.Count}");
            _redisSavingQueue.EnqueueOrderbook(_exchange, symbol);
        }

        private string DetectSymbol(BybitOrderbookUpdate update)
        {
            if (update.Delete.Length > 0) return update.Delete[0].Symbol;
            else if (update.Update.Length > 0) return update.Update[0].Symbol;
            else if (update.Insert.Length > 0) return update.Insert[0].Symbol;
            return null;
        }

        #endregion

        public void Dispose()
        {
            _inverseClient?.Dispose();
        }
    }
}