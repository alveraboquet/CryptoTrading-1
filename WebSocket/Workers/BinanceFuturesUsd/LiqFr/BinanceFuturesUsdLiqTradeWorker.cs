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
    public class BinanceFuturesUsdLiqTradeWorker : BackgroundService
    {
        private readonly SubscriberSocket _subLiqTrade;
        private readonly BinanceZeroMQProperties _options;
        private readonly SocketServer _server;
        private readonly ILog _logger;
        private readonly string Exchange = ApplicationValues.BinanceUsdName;

        public BinanceFuturesUsdLiqTradeWorker(BinanceZeroMQProperties options, SocketServer server)
        {
            _options = options;
            _server = server;
            _subLiqTrade = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdTradeWorker));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _subLiqTrade.Connect(WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress,
                _options.BinanceFuturesUsdLiqTradePort));
            _subLiqTrade.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _subLiqTrade.ReceiveFrameBytes();
                    ZeroMQ.Trade trade = Utf8Json.JsonSerializer.Deserialize<ZeroMQ.Trade>(messageReceived);

                    int chanId = Extension.GetChanId(Exchange, trade.Symbol, "trade");
                    List<Guid> ids = _server.GetChannelsIds(Channel.Trades, $"{Exchange}.{trade.Symbol.ToLower()}");

                    SubsequentResponse<ZeroMQ.Trade> response = new(chanId, trade);
                    string tradeMsg = response.ToJson();

                    foreach (Guid id in ids.ToList())
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
            _subLiqTrade.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}