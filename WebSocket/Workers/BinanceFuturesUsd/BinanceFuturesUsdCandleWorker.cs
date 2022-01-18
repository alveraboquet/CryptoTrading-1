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
    public class BinanceFuturesUsdCandleWorker : BackgroundService
    {
        private readonly SubscriberSocket _subBinanceCandle;
        private readonly BinanceZeroMQProperties _options;
        private readonly SocketServer _server;
        private readonly ILog _logger;
        private readonly string _exchange = ApplicationValues.BinanceUsdName;

        public BinanceFuturesUsdCandleWorker(BinanceZeroMQProperties options, SocketServer server)
        {
            _options = options;
            this._server = server;
            _subBinanceCandle = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdCandleWorker));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _subBinanceCandle.Connect(WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress, _options.BinanceFuturesUsdCandlePort));
            _subBinanceCandle.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _subBinanceCandle.ReceiveFrameBytes();
                    var candle = Utf8Json.JsonSerializer.Deserialize<ZeroMQ.OpenCandle>(messageReceived);

                    int chanId = Extension.GetChanId(_exchange, candle.Symbol, "candle", candle.Timeframe);
                    List<Guid> ids = _server.GetChannelsIds(Channel.Candles, $"{_exchange}.{candle.Symbol.ToLower()}:{candle.Timeframe.ToLower()}");

                    SubsequentResponse<ZeroMQ.OpenCandle> response = new SubsequentResponse<ZeroMQ.OpenCandle>(chanId, candle);
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
            _subBinanceCandle.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}
