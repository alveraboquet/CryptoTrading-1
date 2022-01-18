using ExchangeModels.BinanceFutures;
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
    public class BinanceFuturesUsdAllfundsWorker : BackgroundService
    {
        private readonly SubscriberSocket _subBFUsdAllfunds;
        private readonly BinanceZeroMQProperties _options;
        private readonly SocketServer _server;
        private readonly ILog _logger;
        private const string Exchange = ApplicationValues.BinanceUsdName;

        public BinanceFuturesUsdAllfundsWorker(BinanceZeroMQProperties options, SocketServer server)
        {
            _options = options;
            this._server = server;
            _subBFUsdAllfunds = SubPubFactory.NewSubscriber(10000);
            _logger = LogManager.GetLogger(typeof(BinanceFuturesUsdAllfundsWorker));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Started");
            _subBFUsdAllfunds.Connect(WebSocketHelper.GetZeroMQAddress(_options.PublisherIPAddress,
                _options.BinanceFuturesUsdAllfundsPort));

            _subBFUsdAllfunds.SubscribeToAnyTopic();

            return base.StartAsync(cancellationToken);
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    byte[] messageReceived = _subBFUsdAllfunds.ReceiveFrameBytes();

                    List<FundingRateUpdate> fr = BinanceConverter.DeserializeBinanceFuturesUsdFundingRate(messageReceived);
                    int chanId = Extension.GetAllfundsChanId(Exchange);

                    string dataJson = AllfundsSnapshot.GetDataJson(fr);
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
            _subBFUsdAllfunds.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}