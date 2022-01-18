using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Hosting;
using NetMQ;
using NetMQ.Sockets;
using Utilities;
using ZeroMQ;

namespace WebSocket.Workers.Bybit
{
    public class BybitTradeWorker : BackgroundService
    {
        // injected via DI
        private readonly SocketServer _server;
        private readonly BybitZeroMQProperties _options;
        // initialized on constructor
        private readonly SubscriberSocket _subBinanceTrade;
        private readonly ILog _logger;
        private readonly string _exchange = ApplicationValues.BybitName;

        public BybitTradeWorker(SocketServer server, BybitZeroMQProperties options)
        {
            _server = server;
            _options = options;
            _subBinanceTrade = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BybitTradeWorker));
        }
        
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _subBinanceTrade.Connect(
                WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress, _options.BybitTradePort)
                );
            _subBinanceTrade.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _subBinanceTrade.ReceiveFrameBytes();
                    ZeroMQ.Trade trade = Utf8Json.JsonSerializer.Deserialize<ZeroMQ.Trade>(messageReceived);

                    int chanId = Extension.GetChanId(_exchange, trade.Symbol, "trade");
                    List<Guid> ids = _server.GetChannelsIds(Channel.Trades, $"{_exchange}.{trade.Symbol.ToLower()}");

                    SubsequentResponse<ZeroMQ.Trade> response = 
                        new SubsequentResponse<ZeroMQ.Trade>(chanId, trade);
                    string tradeMsg = response.ToJson();

                    foreach (var id in ids.ToList())
                    {
                        var session = _server.FindSession(id);
                        ((SocketSession)session)?.SendTextAsync(tradeMsg);
                    }
                }
            }).Start();

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Stopped");
            _subBinanceTrade.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}