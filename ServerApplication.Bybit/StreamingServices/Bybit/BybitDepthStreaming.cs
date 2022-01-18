using DataLayer;
using DataLayer.Models.Stream;
using ExchangeModels;
using ExchangeModels.Bybit;
using ExchangeServices;
using ExchangeServices.Services.Exchanges.Bybit.Socket;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using ServerApplication.Bybit.Queues;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;
using Utilities;
using WatsonWebsocket;

namespace ServerApplication.Bybit.StreamingServices
{
    public class BybitDepthStreaming
    {
        private readonly string exchange = ApplicationValues.BybitName;

        private readonly ILog _logger;

        private readonly BybitZeroMQDepthQueue _pubDepthQueue;
        private readonly BybitRedisSavingDataQueue _redisSavingQueue;

        private readonly ConcurrentQueue<DepthMessage> _depthQueue;
        private readonly IMemoryCache _cache;
        private readonly BybitDepthWsClient _client;
        private readonly CancellationToken _token;
        private IEnumerable<PairInfo> _pairs;

        public BybitDepthStreaming(CancellationToken workerToken, BybitZeroMQDepthQueue pubDepthQueue,
            BybitRedisSavingDataQueue redisQueue, IMemoryCache cache)
        {
            _token = workerToken;

            _cache = cache;
            _client = new();
            _redisSavingQueue = redisQueue;
            _pubDepthQueue = pubDepthQueue;
            _logger = LogManager.GetLogger(typeof(BybitDepthStreaming));
            _depthQueue = new();
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

        #region Event handlers
        private void OnConnected(object sender, EventArgs e)
        {
            _client.SubToAllSymbols(_pairs.ToArray()).Wait();
            _logger.Info("Depth WS Connected.");
            
            _cache.SetDepthSymbolIsStreaming(this.exchange, _pairs.Select(p => p.Symbol).ToArray(), true);
            Thread orderbookThread = new Thread(OrderbookThread);

            foreach (var item in this._pairs)
                _cache.SetOrderBook(item.Exchange, item.Symbol, null);

            orderbookThread.Start();
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            try
            {
                DepthMessage depth = JsonSerializer.Deserialize<DepthMessage>(args.Data);
                if (!depth.IsFirstMessage)
                {
                    if (depth.Data.Length > 1)
                    {
                        var jsonAsString = Encoding.ASCII.GetString(args.Data);
                        _logger.Error($"Two members for depth. \n {jsonAsString}");
                    }
                    
                    _depthQueue.Enqueue(depth);

                    ZeroMQ.OrderBook pub = new ZeroMQ.OrderBook()
                    {
                        Symbol = depth.Symbol,
                        Asks = depth.Data[0].AskOrders,
                        Bids = depth.Data[0].BidOrders,
                    };

                    _pubDepthQueue.Enqueue(pub);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Depth WS Disconnected.");
            _cache.SetDepthSymbolIsStreaming(this.exchange, _pairs.Select(p => p.Symbol).ToArray(), false);
            Dispose();
        }
        #endregion

        #region Orderbook Thread
        protected void OrderbookThread()
        {
            while (!_token.IsCancellationRequested)
            {
                while (!_token.IsCancellationRequested && _depthQueue.TryDequeue(out var depth))
                {
                    UpdateOrderBook(depth);
                }
                Thread.Sleep(1);
            }
        }

        protected void UpdateOrderBook(DepthMessage update)
        {
            StreamingOrderBook currentOrderBook = _cache.TryGetOrderBook(this.exchange, update.Symbol);
            if (currentOrderBook == null)
            {
                currentOrderBook = new StreamingOrderBook()
                {
                    Asks = new ConcurrentDictionary<decimal, decimal>(),
                    Bids = new ConcurrentDictionary<decimal, decimal>(),
                    Symbol = update.Symbol,
                };
                // _logger.Info($"We set new orderbook.");
                _cache.SetOrderBook(this.exchange, update.Symbol, currentOrderBook);
            }

            List<DataLayer.AskOrder> asks = update.Data[0].AskOrders.Select(a => new DataLayer.AskOrder() { Price = a[0], Quantity = a[1] }).ToList();
            List<DataLayer.BidOrder> bids = update.Data[0].BidOrders.Select(b => new DataLayer.BidOrder() { Price = b[0], Quantity = b[1] }).ToList();

            foreach (AskOrder item in asks)
            {
                try
                {
                    if (item.Quantity == 0)
                    {
                        currentOrderBook.Asks.TryRemove(item.Price, out _);
                    }
                    else
                    {
                        currentOrderBook.Asks[item.Price] = item.Quantity;
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                { _logger.Error(ex.Message, ex); }
                catch (Exception ex)
                { _logger.Error(ex.Message, ex); }
            }

            foreach (BidOrder item in bids)
            {
                try
                {
                    if (item.Quantity == 0) // remove it
                    {
                        currentOrderBook.Bids.TryRemove(item.Price, out _);
                    }
                    else
                    {
                        currentOrderBook.Bids[item.Price] = item.Quantity;
                    }
                }
                catch (Exception ex)
                { _logger.Error(ex.Message, ex); }
            }

            _redisSavingQueue.EnqueueOrderbook(update.Symbol);
        }
        #endregion

        public void Dispose() => _client?.Dispose();
    }
}