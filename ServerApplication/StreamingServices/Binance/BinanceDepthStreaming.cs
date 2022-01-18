using DataLayer;
using DataLayer.Models.Stream;
using ExchangeModels;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;
using Utilities;
using WatsonWebsocket;

namespace ServerApplication.StreamingServices
{
    public class BinanceDepthStreaming
    {
        private const byte depthBin = 0x64; 
        private readonly string exchange = ApplicationValues.BinanceName;

        private readonly ILog _logger;
        private BinanceZeroMQDepthQueue _pubDepthQueue;
        private ConcurrentQueue<SDepthUpdate> _depthQueue;
        private BinanceRedisSavingDataQueue _redisSavingQueue;
        private IMemoryCache _cache;
        private BinanceDepthWSClient _client;
        private CancellationToken _token;
        private IEnumerable<PairInfo> _pairs;

        public BinanceDepthStreaming(CancellationToken workerToken, BinanceZeroMQDepthQueue pubDepthQueue,
            BinanceRedisSavingDataQueue redisQueue, IMemoryCache cache)
        {
            _token = workerToken;
            _cache = cache;
            _client = new BinanceDepthWSClient();
            _redisSavingQueue = redisQueue;
            _pubDepthQueue = pubDepthQueue;
            _logger = LogManager.GetLogger(typeof(BinanceDepthStreaming));
            _depthQueue = new ConcurrentQueue<SDepthUpdate>();
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

        #region Event handlers
        private void OnConnected(object sender, EventArgs e)
        {
            _logger.Info("Depth WS Connected.");
            _cache.SetDepthSymbolIsStreaming(this.exchange, _pairs.Select(p => p.Symbol).ToArray(), true);
            Thread orderbookThread = new Thread(OrderbookThread);
            foreach (var item in this._pairs)
            {
                _cache.SetOrderBook(item.Exchange, item.Symbol, null);
            }
            orderbookThread.Start();
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            if (args.Data[6].Equals(depthBin))
            {
                SDepthUpdate depth = JsonSerializer.Deserialize<SDepthUpdate>(args.Data);
                _depthQueue.Enqueue(depth);

                ZeroMQ.OrderBook pub = new ZeroMQ.OrderBook()
                {
                    Symbol = depth.Symbol,
                    Asks = depth.Asks,
                    Bids = depth.Bids,
                };

                _pubDepthQueue.Enqueue(pub);
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
                while (!_token.IsCancellationRequested && _depthQueue.TryDequeue(out SDepthUpdate depth))
                {
                    UpdateOrderBook(depth);
                }
                Thread.Sleep(1);
            }
        }
        protected void UpdateOrderBook(SDepthUpdate update)
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
                _cache.SetOrderBook(this.exchange, update.Symbol, currentOrderBook);
            }

            List<DataLayer.AskOrder> asks = update.Asks.Select(a => new DataLayer.AskOrder() { Price = a[0], Quantity = a[1] }).ToList();
            List<DataLayer.BidOrder> bids = update.Bids.Select(b => new DataLayer.BidOrder() { Price = b[0], Quantity = b[1] }).ToList();

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