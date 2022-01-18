using Redis;
using Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using NetCoreServer;
using DataLayer;
using DatabaseRepository;
using UserRepository;
using log4net;

namespace WebSocket
{
    public class SocketServer: WssServer
    {
        private IUserRepository _users;
        private ICacheService _redis;
        private readonly ILog _logger;

        public SocketServer(SslContext context, IPAddress address, int port,
            ICacheService redis, IUserRepository users)
            : base(context, address, port)
        {
            _logger = LogManager.GetLogger(typeof(SocketServer));
            this._redis = redis;
            this._users = users;
            ConnectionCounter = new ConcurrentDictionary<int, int>();
            CandleChannels = new ConcurrentDictionary<string, List<Guid>>();
            AllfundsChannels = new ConcurrentDictionary<string, List<Guid>>();
            OrderbookChannels = new ConcurrentDictionary<string, List<Guid>>();
            TradeChannels = new ConcurrentDictionary<string, List<Guid>>();
        }

        // Key is account-id, value is counter. counter's max value
        public ConcurrentDictionary<int, int> ConnectionCounter { get; set; }
        public ConcurrentDictionary<string, List<Guid>> CandleChannels { get; set; }
        public ConcurrentDictionary<string, List<Guid>> AllfundsChannels { get; set; }
        public ConcurrentDictionary<string, List<Guid>> TradeChannels { get; set; }
        public ConcurrentDictionary<string, List<Guid>> OrderbookChannels { get; set; }

        protected override SslSession CreateSession()
        {
            return new SocketSession(this, _redis, _users);
        }

        protected override void OnStarted()
        {
            _logger.Info($"WebSocket server started.");
            base.OnStarted();
        }

        protected override void OnStopped()
        {
            _logger.Info($"WebSocket server stopped.");
            base.OnStopped();
        }

        protected override void OnError(SocketError error)
        {
            _logger.Info($"Server caught an error with code: {(int)error} | {error}");
        }

        public List<Guid> GetChannelsIds(Channel channel, string key)
        {
            var list = new List<Guid>();
            switch (channel)
            {
                case Channel.Candles:
                    list = CandleChannels.GetOrMakeNew(key);
                    break;
                case Channel.Trades:
                    list = TradeChannels.GetOrMakeNew(key);
                    break;
                case Channel.OrderBook:
                    list = OrderbookChannels.GetOrMakeNew(key);
                    break;
                case Channel.AllFunds:
                    list = AllfundsChannels.GetOrMakeNew(key);
                    break;
            }

            return list;
        }
    }
}
