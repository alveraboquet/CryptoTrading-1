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
    public class BybitCandleWorker : BackgroundService
    {
        // injected via DI
        private readonly BybitZeroMQProperties _options;
        private readonly SocketServer _server;
        // initialized on constructor
        private readonly SubscriberSocket _candleSubscriber;
        private readonly ILog _logger;
        private readonly string _exchange = ApplicationValues.BybitName;

        public BybitCandleWorker(BybitZeroMQProperties options, SocketServer server)
        {
            _options = options;
            _server = server;
            _candleSubscriber = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BybitCandleWorker));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _candleSubscriber.Connect(
                WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress, _options.BybitCandlePort)
                );
            _candleSubscriber.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _candleSubscriber.ReceiveFrameBytes();
                    var candle = Utf8Json.JsonSerializer.Deserialize<ZeroMQ.OpenCandle>(messageReceived);

                    int chanId = Extension.GetChanId(_exchange, candle.Symbol, "candle", candle.Timeframe);
                    List<Guid> ids = _server.GetChannelsIds(Channel.Candles, $"{_exchange}.{candle.Symbol.ToLower()}:{candle.Timeframe.ToLower()}");

                    SubsequentResponse<ZeroMQ.OpenCandle> response = 
                        new SubsequentResponse<ZeroMQ.OpenCandle>(chanId, candle);
                    string candleMsg = response.ToJson();

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
            _candleSubscriber.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}