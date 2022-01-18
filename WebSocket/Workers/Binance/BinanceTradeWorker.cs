using log4net;
using Microsoft.Extensions.Hosting;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using ZeroMQ;

namespace WebSocket.Workers
{
    public class BinanceTradeWorker : BackgroundService
    {
        private readonly SubscriberSocket _subBinanceTrade;
        private readonly BinanceZeroMQProperties _options;
        private readonly SocketServer _server;
        private readonly ILog _logger;
        private readonly string _exchange = ApplicationValues.BinanceName;
        public BinanceTradeWorker(BinanceZeroMQProperties options, SocketServer server)
        {
            _options = options;
            this._server = server;
            _subBinanceTrade = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BinanceTradeWorker));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _subBinanceTrade.Connect(WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress, _options.BinanceTradePort));
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

                    SubsequentResponse<ZeroMQ.Trade> response = new SubsequentResponse<ZeroMQ.Trade>(chanId, trade);
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
