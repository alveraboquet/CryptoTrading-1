using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DataLayer;
using DataLayer.Models.Stream;
using ExchangeModels.BinanceFutures;
using ExchangeModels.BybitFutures;
using ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues.BybitFutures;
using Utf8Json;
using Utilities;
using WatsonWebsocket;

namespace ServerApplication.Bybit.StreamingServices.BybitFutures
{
    public class BybitUsdtFuturesDepthStreaming : IDisposable
    {
        private readonly string _exchange = ApplicationValues.BybitFuturesName;
        
        private readonly ILog _logger;
        private BybitFuturesZeroMqDepthQueue _pubDepthQueue;
        private ConcurrentQueue<BybitOrderbookUpdate> _depthQueue;
        private BybitFuturesRedisSavingDataQueue _redisSavingQueue;
        private IMemoryCache _cache;
        private BybitFuturesUsdtOrderbookWsClient _usdtClient;
        private CancellationToken _token;
        private List<PairInfo> _pairs;

        public BybitUsdtFuturesDepthStreaming(BybitFuturesZeroMqDepthQueue pubDepthQueue, 
            BybitFuturesRedisSavingDataQueue redisSavingQueue, 
            IMemoryCache cache, 
            CancellationToken workerToken)
        {
            _logger = LogManager.GetLogger(typeof(BybitUsdtFuturesDepthStreaming));
            _pairs = new List<PairInfo>();
            _pubDepthQueue = pubDepthQueue;
            _depthQueue = new();
            _redisSavingQueue = redisSavingQueue;
            _cache = cache;
            _usdtClient = new();
            _token = workerToken;
        }

        public void Connect(List<PairInfo> pairs)
        {
            if (!pairs.Any())
                return;

            // _client.Init(pairs.Select(p => p.Symbol).ToArray());
            _pairs = pairs.ToList();
            
            _usdtClient.Client.ServerConnected += OnUsdtConnected;
            _usdtClient.Client.MessageReceived += OnMessageReceived;
            _usdtClient.Client.ServerDisconnected += OnDisconnected;
            
            _usdtClient.ConnectAsync().Wait();
        }
        
        #region Event Handlers
        
        private async void OnUsdtConnected(object sender, EventArgs e)
        {
            _logger.Info("Depth WS Connected.");
            await _usdtClient.SubscribeToSymbolsAsync(_pairs.ToArray());
            _cache.SetDepthSymbolIsStreaming(_exchange, _pairs.Select(p => p.Symbol).ToArray(), true);
            Thread orderbookThread = new Thread(OrderbookThread);
            orderbookThread.Start();
        }
        
        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Depth WS Disconnected.");
            _cache.SetDepthSymbolIsStreaming(_exchange, _pairs.Select(p => p.Symbol).ToArray(), false);
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
                { _logger.Error(ex.Message, ex); }
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
                { _logger.Error(ex.Message, ex); }
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
                { _logger.Error(ex.Message, ex); }
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
            _usdtClient?.Dispose();
        }
    }
}