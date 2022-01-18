using ExchangeModels.Enums;
using ExchangeServices;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Caching;
using ServerApplication.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using WatsonWebsocket;

namespace ServerApplication.StreamingServices
{
    public class BinanceFuturesUsdFrLiqStreaming : IDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly BinanceFuturesUsdFrLiqWSClient _client;
        private readonly BinanceFuturesUsdFundingRateCalculateQueue _fundingRates;
        private readonly BinanceFuturesUsdLiquidationCalculateQueue _liqidation;
        private readonly BinanceFuturesUsdZeroMqFundingRateQueue _fundingRateZeroMq;

        public BinanceFuturesUsdFrLiqStreaming(IMemoryCache cache,
            BinanceFuturesUsdFundingRateCalculateQueue fundingRates,
            BinanceFuturesUsdLiquidationCalculateQueue liqidation,
            BinanceFuturesUsdZeroMqFundingRateQueue fundingRateZeroMq)
        {
            _fundingRateZeroMq = fundingRateZeroMq;
            _fundingRates = fundingRates;
            _liqidation = liqidation;
            this._cache = cache;
            _client = new BinanceFuturesUsdFrLiqWSClient();
            this._logger = LogManager.GetLogger(typeof(BinanceFuturesUsdFrLiqStreaming));
        }

        public void Connect()
        {
            _client.Init();

            _client.Client.MessageReceived += OnMessageReceived;
            _client.Client.ServerConnected += OnConnected;
            _client.Client.ServerDisconnected += OnDisconnected;
            _client.Connect().Wait();
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Liq-Fr Disconnected");
            _cache.SetBinanceFuturesUsdIsLiqFrStreaming(false);
            Dispose();
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _logger.Info("Liq-Fr Connected");
            _cache.SetBinanceFuturesUsdIsLiqFrStreaming(true);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (BinanceFuturesUsdFrLiqDetector.TryDetectMessageType(e.Data, out var type))
            {
                switch (type.Value)
                {
                    case BinanceFuturesUsdWebSocketStreams.AllLiquidation:
                        _liqidation.Enqueue(e.Data);
                        break;

                    case BinanceFuturesUsdWebSocketStreams.AllFundingRates:
                        _fundingRates.Enqueue(e.Data);
                        _fundingRateZeroMq.Enqueue(e.Data);
                        break;
                }
            }
        }

        public void Dispose() => _client?.Dispose();
    }

    public static class BinanceFuturesUsdFrLiqDetector
    {
        public static bool TryDetectMessageType(byte[] msg, out BinanceFuturesUsdWebSocketStreams? type)
        {
            type = null;
            if (msg[12].Equals(0x66)) // f
            {
                type = BinanceFuturesUsdWebSocketStreams.AllLiquidation;
                return true;
            }
            else if (msg[12].Equals(0x6d)) // m
            {
                type = BinanceFuturesUsdWebSocketStreams.AllFundingRates;
                return true;
            }
            return false;
        }
    }
}