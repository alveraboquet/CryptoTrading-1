using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Linq;
using NetCoreServer;
using System.Dynamic;
using Utilities;
using Redis;
using DataLayer;
using DataLayer.Models.Stream;
using DatabaseRepository;
using UserRepository;
using log4net;
using ExchangeModels.BinanceFutures;

namespace WebSocket
{
    public class SocketSession : WssSession
    {
        private readonly List<string> _candleChannels;
        private readonly List<string> _allfundsChannels;
        private readonly List<string> _tradeChannels;
        private readonly List<string> _orderbookChannels;
        private readonly ICacheService _redis;
        private readonly IUserRepository _users;
        private SocketServer _server;

        private readonly ILog _logger;

        public SocketSession(SocketServer server,
            ICacheService redis, IUserRepository users)
            : base(server)
        {
            _logger = LogManager.GetLogger(typeof(SocketSession));
            _users = users;
            _redis = redis;
            _candleChannels = new List<string>();
            _tradeChannels = new List<string>();
            _orderbookChannels = new List<string>();
            _allfundsChannels = new List<string>();
            this.IsAuthorized = false;
            _server = server;
        }

        public bool IsAuthorized { get; private set; }
        public int AccountId { get; set; }

        public override void OnWsConnected(HttpRequest request)
        {
            _logger.Info($"Connected: {Id}");
            base.OnWsConnected(request);
        }

        public override void OnWsDisconnected()
        {
            foreach (var item in _candleChannels)
            {
                if (_server.CandleChannels.TryGetValue(item, out var guids))
                {
                    guids.RemoveAll(a => a == this.Id);
                }
            }

            foreach (var item in _orderbookChannels)
            {
                if (_server.OrderbookChannels.TryGetValue(item, out var guids))
                {
                    guids.RemoveAll(a => a == this.Id);
                }
            }

            foreach (var item in _tradeChannels)
            {
                if (_server.TradeChannels.TryGetValue(item, out var guids))
                {
                    guids.RemoveAll(a => a == this.Id);
                }
            }

            // TODO: Check if value is 1 remove otherwise decrease value
            _server.ConnectionCounter.Remove(this.AccountId, out int val);

            _logger.Info($"Disconnected: {Id}");
            base.OnWsDisconnected();
        }

        protected override void OnError(SocketError error)
        {
            _logger.Info($"Session caught an error with code: {(int)error} | {error}");
        }

        public override async void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string json = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Request request;
            try
            {
                // deserializing to general request model
                request = JsonSerializer.Deserialize<WebSocket.Request>(json);
            }
            catch (Exception)
            {
                this.ReturnError300($"Invalid Json");
                return;
            }

            // events can be auth, subscribe, unsubscribe
            Event @event;
            try
            {
                if (request == null) throw new Exception("Invalid json");
                // converts event from string to ENUM
                @event = request.GetEvent();
            }
            catch (Exception ex)
            {
                ReturnError300(ex.Message);
                return;
            }

            if (@event == Event.Auth)
            {
                if (IsAuthorized)
                {
                    ReturnError102();
                }
                else
                {
                    if (request.AccountId == default)
                    { ReturnError100("Enter account-Id"); return; }

                    if (string.IsNullOrWhiteSpace(request.AccountToken))
                    { ReturnError100("Enter account-Token"); return; }

                    AuthenticationReq(request.AccountId, request.AccountToken);
                }
            }
            else
            {
                #region Validate channel and convert channel
                
                if (string.IsNullOrWhiteSpace(request.Channel))
                {
                    ReturnError300("Enter channel");
                    return;
                }
                // channels can be candle, trade, orderbook, allfunds
                Channel channel;
                try
                {
                    // converting channel from string to ENUM
                    channel = request.GetChannel();
                }
                catch (Exception ex)
                {
                    ReturnError300(ex.Message);
                    return;
                }
                
                #endregion

                #region Validate Key
                
                // Key format: 
                //      for allfunds: exchange
                //      for orderbook and trade: exchange.symbol
                //      for candle: exchange.symbol:timeframe
                string key;
                
                if (!string.IsNullOrWhiteSpace(request.Key))
                    key = request.Key.ToLower();
                else
                {
                    ReturnError300("Enter key");
                    return;
                }

                (string ex, string pair, string timeFrame) keyTuple;
                try
                {
                    // converting key to a tuple
                    keyTuple = request.GetKey();
                }
                catch (Exception ex)
                {
                    ReturnError300(ex.Message);
                    return;
                }
                #endregion

                if (@event == Event.Subscribe)
                {
                    // TODO: Enable authentication
                    // if (IsAuthorized)
                    if (true)
                    {
                        // check is channel already subscribed?
                        switch (channel)
                        {
                            default:
                            case Channel.Trades:
                                if (_tradeChannels.Contains(key))
                                {
                                    ReturnError301();
                                    return;
                                }
                                break;
                            case Channel.OrderBook:
                                if (_orderbookChannels.Contains(key))
                                {
                                    ReturnError301();
                                    return;
                                }
                                break;
                            case Channel.Candles:
                                if (_candleChannels.Contains(key))
                                {
                                    ReturnError301();
                                    return;
                                }
                                break;
                            case Channel.AllFunds:
                                if (_allfundsChannels.Contains(key))
                                {
                                    ReturnError301();
                                    return;
                                }
                                break;
                        }

                        switch (channel)
                        {
                            case Channel.Candles:
                                {
                                    int chanId = Extension.GetChanId(keyTuple.ex, keyTuple.pair, "candle", keyTuple.timeFrame);
                                    FootPrints footprint = null;
                                    OpenCandle candle = null;
                                    if (!WebSocket.Request.IsFrOrLiqPair(keyTuple.pair))
                                    {
                                        footprint = await _redis.TryGetFootPrints(keyTuple.ex, keyTuple.pair, keyTuple.timeFrame);
                                        candle = await _redis.TryGetOpenCandle(keyTuple.ex, keyTuple.pair, keyTuple.timeFrame);

                                        if (candle == null)
                                        {
                                            ReturnError300("Can not stream or invalid key");
                                            return;
                                        }
                                    }

                                    ReturnSubscribed(chanId, request.Channel, request.Key);
                                    _candleChannels.Add(key);

                                    // ignore this possible null reference exception
                                    // it will never happen
                                    if (!WebSocket.Request.IsFrOrLiqPair(keyTuple.pair))
                                        SendFootprintSnapshot(chanId, footprint, new ZeroMQ.OpenCandle()
                                        {
                                            Volume = candle.Volume,
                                            Close = candle.Close,
                                            //Exchange = candle.Exchange,
                                            Low = candle.Low,
                                            High = candle.High,
                                            Open = candle.Open,
                                            OpenTime = candle.OpenTime,
                                            Symbol = candle.Symbol,
                                            Timeframe = candle.Timeframe,
                                        });

                                    _server.CandleChannels.GetOrMakeNew(key).Add(this.Id);
                                    break;
                                }

                            case Channel.Trades:
                                {
                                    _tradeChannels.Add(key);
                                    int chanId = Extension.GetChanId(keyTuple.ex, keyTuple.pair, "trade");
                                    ReturnSubscribed(chanId, request.Channel, request.Key);
                                    // Id is sessionId
                                    _server.TradeChannels.GetOrMakeNew(key).Add(this.Id);
                                    break;
                                }

                            case Channel.OrderBook:
                                {
                                    _orderbookChannels.Add(key);
                                    try
                                    {
                                        string exchange = request.Key.Split('.')[0];
                                        string symbol = request.Key.Split('.')[1];
                                        var res = _redis.TryGetOrderBook(exchange, symbol).Result;
                                        int chanId = Extension.GetChanId(keyTuple.ex, keyTuple.pair, "orderbook");
                                        ReturnSubscribed(chanId, request.Channel, request.Key);
                                        SendOrderBookSnapshot(chanId, res);
                                    }
                                    catch
                                    {
                                        ReturnError300("Invalid key.");
                                        return;
                                    }
                                    _server.OrderbookChannels.GetOrMakeNew(key).Add(this.Id);
                                    break;
                                }

                            case Channel.AllFunds:
                                {
                                    int chanId = Extension.GetAllfundsChanId(keyTuple.ex);
                                    List<FundingRateUpdate> data = await _redis.GetAllFundingRateAsync(keyTuple.ex);

                                    ReturnSubscribed(chanId, request.Channel, request.Key);
                                    _allfundsChannels.Add(key);

                                    if (data is not null)
                                        SendAllfundsSnapshot(chanId, data);

                                    _server.AllfundsChannels.GetOrMakeNew(key).Add(this.Id);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        ReturnError101();
                    }
                }
                else if (@event == Event.Unsubscribe)
                {
                    // TODO: Enable authentication
                    // if (IsAuthorized)
                    if (true)
                    {
                        int chanId = 1111;
                        if (!(_candleChannels.Contains(key) ||
                            _orderbookChannels.Contains(key) ||
                            _tradeChannels.Contains(key) ||
                            _allfundsChannels.Contains(key)))
                        {
                            ReturnError401();
                            return;
                        }
                        else
                        {
                            switch (channel)
                            {
                                case Channel.Candles:
                                    {
                                        _candleChannels.Remove(key);
                                        if (_server.CandleChannels.TryGetValue(key, out var guids))
                                        {
                                            guids.Remove(this.Id);
                                        }
                                        chanId = Extension.GetChanId(keyTuple.ex, keyTuple.pair, "candle", keyTuple.timeFrame);
                                        break;
                                    }
                                case Channel.Trades:
                                    {
                                        _tradeChannels.Remove(key);
                                        if (_server.TradeChannels.TryGetValue(key, out var guids))
                                        {
                                            guids.Remove(this.Id);
                                        }
                                        chanId = Extension.GetChanId(keyTuple.ex, keyTuple.pair, "trade");
                                        break;
                                    }
                                case Channel.OrderBook:
                                    {
                                        _orderbookChannels.Remove(key);
                                        if (_server.OrderbookChannels.TryGetValue(key, out var guids))
                                        {
                                            guids.Remove(this.Id);
                                        }
                                        chanId = Extension.GetChanId(keyTuple.ex, keyTuple.pair, "orderbook");
                                        break;
                                    }
                                case Channel.AllFunds:
                                    {
                                        _allfundsChannels.Remove(key);
                                        if (_server.AllfundsChannels.TryGetValue(key, out var guids))
                                        {
                                            guids.Remove(this.Id);
                                        }
                                        chanId = Extension.GetAllfundsChanId(keyTuple.ex);
                                        break;
                                    }
                            }
                        }

                        ReturnUnsubscribed(chanId, request.Channel, request.Key);
                    }
                    else
                    {
                        ReturnError101();
                    }
                }
            }

            base.OnWsReceived(buffer, offset, size);
        }

        #region Handle requests

        public void AuthenticationReq(int accountId, string accountToken)
        {
            /*
                > account-Id is valid (db)
                > account-Token is valid (db)
                > if there are no more than 25 connections per account-Id (cache)
             */

            try
            {
                bool isValidDB = _users.IsExistSession(accountId, accountToken).Result;

                bool isExist = _server.ConnectionCounter.TryGetValue(accountId, out int connCount);
                bool isValidConn = (connCount < 25); //R

                this.IsAuthorized = isValidConn && isValidDB;

                if (isExist)
                    _server.ConnectionCounter[accountId]++;
                else
                    _server.ConnectionCounter[accountId] = 1;
            }
            catch (AggregateException)
            { this.IsAuthorized = false; }

            if (IsAuthorized)
                this.AccountId = accountId;
            else
                this.AccountId = 0;

            ReturnAuthorized(IsAuthorized);
            if (!IsAuthorized)
            {
                this.Close(0);
            }
        }

        #endregion

        #region Send Snapshot
        public void SendFootprintSnapshot(int chanId, FootPrints footprint, ZeroMQ.OpenCandle candle)
        {
            var response = new CandleSnapshot(chanId, footprint, candle);
            this.SendTextAsync(response.ToJson());
        }
        public void SendOrderBookSnapshot(int chanId, StreamingOrderBook data)
        {
            if (data == null)
                return;

            var response = new OrderBookSnapshot(chanId, data);
            this.SendTextAsync(response.ToJson());
        }
        public void SendAllfundsSnapshot(int chanId, List<FundingRateUpdate> data)
        {
            if (data == null)
                return;

            var response = new AllfundsSnapshot(chanId, data);
            this.SendTextAsync(response.ToJson());
        }
        #endregion

        #region Return Informs
        private void ReturnAuthorized(bool isAuthorized)
        {
            this.SendTextAsync(AuthenticationResponse.ToJson(isAuthorized));
        }

        private void ReturnSubscribed(int chanId, string channel, string key)
        {
            WebSocket.FirstResponse firstResponse = new FirstResponse()
            {
                Event = "subscribed",
                ChanId = chanId,
                Channel = channel,
                Key = key
            };
            this.SendTextAsync(firstResponse.ToJson());
        }
        private void ReturnUnsubscribed(int chanId, string channel, string key)
        {
            WebSocket.FirstResponse firstResponse = new FirstResponse()
            {
                Event = "unsubscribe",
                ChanId = chanId,
                Channel = channel,
                Key = key
            };
            this.SendTextAsync(firstResponse.ToJson());
        }

        #endregion

        #region Return Errors
        /// <summary>
        /// generic error for subscribe
        /// </summary>
        private void ReturnError100(string message)
        {
            var error = new ErrorResponse(100, message);
            this.SendTextAsync(JsonSerializer.Serialize(error));
        }

        /// <summary>
        /// Not authorized yet
        /// </summary>
        private void ReturnError101()
        {
            var error = new ErrorResponse(101, "Not authorized yet");
            this.SendTextAsync(JsonSerializer.Serialize(error));
        }

        /// <summary>
        /// Already authorized
        /// </summary>
        private void ReturnError102()
        {
            var error = new ErrorResponse(102, "Already authorized");
            this.SendTextAsync(JsonSerializer.Serialize(error));
        }

        /// <summary>
        /// generic error for subscribe
        /// </summary>
        private void ReturnError300(string message)
        {
            var error = new ErrorResponse(300, message);
            this.SendTextAsync(JsonSerializer.Serialize(error));
        }

        /// <summary>
        ///  generic error for unsubscribe
        /// </summary>
        private void ReturnError400(string message)
        {
            var error = new ErrorResponse(400, message);
            this.SendTextAsync(JsonSerializer.Serialize(message));
        }

        /// <summary>
        /// already subscribed
        /// </summary>
        private void ReturnError301()
        {
            var error = new ErrorResponse(301, "Already Subscribed");
            this.SendTextAsync(JsonSerializer.Serialize(error));
        }

        /// <summary>
        /// unkown channel
        /// </summary>
        private void ReturnError302(string channel)
        {
            var error = new ErrorResponse(302, $"Unkown channel '{channel}'");
            this.SendTextAsync(JsonSerializer.Serialize(error));
        }

        /// <summary>
        /// not subscribed
        /// </summary>
        private void ReturnError401()
        {
            var error = new ErrorResponse(401, "Not subscribed");
            this.SendTextAsync(JsonSerializer.Serialize(error));
        }
        #endregion
    }
}
