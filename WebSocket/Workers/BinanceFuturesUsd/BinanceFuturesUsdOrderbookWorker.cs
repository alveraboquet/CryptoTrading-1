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
    public class BinanceFuturesUsdOrderbookWorker : BackgroundService
    {
        private readonly SubscriberSocket _subBinanceOrderbook;
        private readonly BinanceZeroMQProperties _options;
        private readonly SocketServer _server;
        private readonly ILog _logger;
        private readonly string _exchange = ApplicationValues.BinanceUsdName;

        public BinanceFuturesUsdOrderbookWorker(BinanceZeroMQProperties options, SocketServer server)
        {
            _options = options;
            this._server = server;
            _subBinanceOrderbook = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BinanceOrderbookWorker));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _subBinanceOrderbook.Connect(WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress, _options.BinanceFuturesUsdOrderbookPort));
            _subBinanceOrderbook.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _subBinanceOrderbook.ReceiveFrameBytes();
                    var OrderBook = Utf8Json.JsonSerializer.Deserialize<ZeroMQ.OrderBook>(messageReceived);

                    int chanId = Extension.GetChanId(_exchange, OrderBook.Symbol, "orderbook");
                    List<Guid> ids = _server.GetChannelsIds(Channel.OrderBook, $"{_exchange}.{OrderBook.Symbol.ToLower()}");

                    SubsequentResponse<ZeroMQ.OrderBook> response = new SubsequentResponse<ZeroMQ.OrderBook>(chanId, OrderBook);
                    string depthMsg = response.ToJson();
                    foreach (var id in ids.ToList())
                    {
                        var session = _server.FindSession(id);
                        ((SocketSession)session)?.SendTextAsync(depthMsg);
                    }
                }
            }).Start();

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Stopped");
            _subBinanceOrderbook.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}
