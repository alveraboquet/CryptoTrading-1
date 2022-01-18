using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExchangeModels.BinanceFutures;
using log4net;
using Microsoft.Extensions.Hosting;
using NetMQ;
using NetMQ.Sockets;
using Utf8Json;
using Utilities;
using ZeroMQ;

namespace WebSocket.Workers.BybitFutures.LiqFr
{
    public class BybitFuturesAllfundsWorker : BackgroundService
    {
        private readonly SocketServer _server;
        private readonly BybitZeroMQProperties _options;
        private readonly SubscriberSocket _allfundsSubscriber;
        private readonly ILog _logger;

        public BybitFuturesAllfundsWorker(BybitZeroMQProperties options, SocketServer server)
        {
            _options = options;
            _server = server;
            _allfundsSubscriber = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BybitFuturesAllfundsWorker));
        }

        private const string Exchange = ApplicationValues.BybitFuturesName;
        
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _allfundsSubscriber.Connect(
                WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress, _options.BybitFuturesAllfundsPort)
            );

            _allfundsSubscriber.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _allfundsSubscriber.ReceiveFrameBytes();
                    
                    var frDict = JsonSerializer.Deserialize<Dictionary<string, decimal>>(messageReceived);
                    int chanId = Extension.GetAllfundsChanId(Exchange);

                    string dataJson = AllfundsSnapshot.GetDataJson(frDict);
                    string candleMsg = SubsequentResponse.ToJson(chanId, dataJson);

                    List<Guid> ids = _server.GetChannelsIds(Channel.AllFunds, Exchange);
                    foreach (var id in ids.ToList())
                    {
                        var session = _server.FindSession(id);
                        ((SocketSession)session)?.SendTextAsync(candleMsg);
                    }
                }
            }).Start();

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Stopped");
            _allfundsSubscriber.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}