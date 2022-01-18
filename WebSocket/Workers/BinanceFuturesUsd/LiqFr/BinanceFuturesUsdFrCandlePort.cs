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
    public class BinanceFuturesUsdFrCandlePort : BackgroundService
    {
        private readonly SubscriberSocket _subBFUsdFrCandle;
        private readonly BinanceZeroMQProperties _options;
        private readonly SocketServer _server;
        private readonly ILog _logger;
        private const string Exchange = ApplicationValues.BinanceUsdName;

        public BinanceFuturesUsdFrCandlePort(BinanceZeroMQProperties options, SocketServer server)
        {
            _options = options;
            _server = server;
            _subBFUsdFrCandle = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdFrCandlePort));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _subBFUsdFrCandle.Connect(WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress,
                _options.BinanceFuturesUsdFrCandlePort));
            _subBFUsdFrCandle.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _subBFUsdFrCandle.ReceiveFrameBytes();
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
            _subBFUsdFrCandle.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}