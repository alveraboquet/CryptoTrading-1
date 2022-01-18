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
    public class BybitOrderbookWorker : BackgroundService
    {
        // injected via DI
        private readonly SocketServer _server;
        private readonly BybitZeroMQProperties _options;
        // initialized on constructor
        private readonly SubscriberSocket _orderBookSubscriber;
        private readonly ILog _logger;
        private readonly string _exchange = ApplicationValues.BybitName;

        public BybitOrderbookWorker(BybitZeroMQProperties options, SocketServer server)
        {
            _options = options;
            _server = server;
            _orderBookSubscriber = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BybitOrderbookWorker));
        }
        
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _orderBookSubscriber.Connect(
                WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress, _options.BybitOrderbookPort)
                );
            _orderBookSubscriber.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _orderBookSubscriber.ReceiveFrameBytes();
                    var orderBook = Utf8Json.JsonSerializer.Deserialize<ZeroMQ.OrderBook>(messageReceived);

                    int chanId = Extension.GetChanId(_exchange, orderBook.Symbol, "orderbook");
                    List<Guid> ids = _server.GetChannelsIds(Channel.OrderBook, $"{_exchange}.{orderBook.Symbol.ToLower()}");

                    SubsequentResponse<ZeroMQ.OrderBook> response = 
                        new SubsequentResponse<ZeroMQ.OrderBook>(chanId, orderBook);
                    
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
            _orderBookSubscriber.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}