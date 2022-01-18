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

namespace WebSocket.Workers.BybitFutures
{
    public class BybitFuturesOrderbookWorker : BackgroundService
    {
        private readonly SocketServer _server;
        private readonly BybitZeroMQProperties _options;
        private readonly SubscriberSocket _orderbookSubscriber;
        private readonly ILog _logger;
        private readonly string _exchange = ApplicationValues.BybitFuturesName;

        public BybitFuturesOrderbookWorker(SocketServer server, BybitZeroMQProperties options)
        {
            _server = server;
            _options = options;
            _orderbookSubscriber = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BybitFuturesOrderbookWorker));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _orderbookSubscriber.Connect(
                WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress,
                _options.BybitFuturesOrderbookPort)
                );
            _orderbookSubscriber.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _orderbookSubscriber.ReceiveFrameBytes();
                    var orderBook = Utf8Json.JsonSerializer.Deserialize<ZeroMQ.OrderBook>(messageReceived);

                    int chanId = Extension.GetChanId(_exchange, orderBook.Symbol, "orderbook");
                    List<Guid> ids =
                        _server.GetChannelsIds(Channel.OrderBook, $"{_exchange}.{orderBook.Symbol.ToLower()}");

                    SubsequentResponse<ZeroMQ.OrderBook> response =
                        new SubsequentResponse<ZeroMQ.OrderBook>(chanId, orderBook);
                    string depthMsg = response.ToJson();
                    foreach (var id in ids.ToList())
                    {
                        var session = _server.FindSession(id);
                        ((SocketSession) session)?.SendTextAsync(depthMsg);
                    }
                }
            }).Start();

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Stopped");
            _orderbookSubscriber.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}