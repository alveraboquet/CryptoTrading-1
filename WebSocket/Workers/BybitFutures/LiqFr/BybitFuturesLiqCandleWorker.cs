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

namespace WebSocket.Workers.BybitFutures.LiqFr
{
    public class BybitFuturesLiqCandleWorker : BackgroundService
    {
        private readonly SocketServer _server;
        private readonly BybitZeroMQProperties _options;
        private readonly SubscriberSocket _liqCandleSubscriber;
        private readonly ILog _logger;
        private const string Exchange = ApplicationValues.BybitFuturesName;

        public BybitFuturesLiqCandleWorker(BybitZeroMQProperties options, SocketServer server)
        {
            _options = options;
            _server = server;
            _liqCandleSubscriber = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BybitFuturesLiqCandleWorker));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _liqCandleSubscriber.Connect(
                WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress,
                _options.BybitFuturesLiqCandlePort)
            );
            _liqCandleSubscriber.SubscribeToAnyTopic();
            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _liqCandleSubscriber.ReceiveFrameBytes();
                    var candle = Utf8Json.JsonSerializer.Deserialize<ZeroMQ.OpenCandle>(messageReceived);

                    int chanId = Extension.GetChanId(Exchange, candle.Symbol, "candle", candle.Timeframe);
                    List<Guid> ids = _server.GetChannelsIds(Channel.Candles, $"{Exchange}.{candle.Symbol.ToLower()}:{candle.Timeframe.ToLower()}");

                    SubsequentResponse<ZeroMQ.OpenCandle> response = new(chanId, candle);
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
            _liqCandleSubscriber.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}