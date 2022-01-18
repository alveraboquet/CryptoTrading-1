using Utf8Json;
using DataLayer;
using ExchangeModels.BybitFutures;
using ExchangeServices.Services;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using ServerApplication.Bybit.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using WatsonWebsocket;
using ServerApplication.Bybit.Queues;

namespace ServerApplication.Bybit.StreamingServices.BybitFutures
{
    public class BybitFuturesFrStreaming
    {
        #region Private Variables

        private IEnumerable<PairInfo> _usdtPairs;
        private const string Exchange = ApplicationValues.BybitFuturesName;
        private readonly IMemoryCache _cache;
        private readonly ILog _logger;
        private readonly BybitFuturesInverseFrWsClient _inverseClient;
        private readonly BybitFuturesUsdtFrWsClient _usdtClient;

        private readonly BybitFuturesFrCalculateQueue _frCalcQueue;

        #endregion

        public BybitFuturesFrStreaming(IMemoryCache cache, BybitFuturesFrCalculateQueue frCalcQueue)
        {
            _frCalcQueue = frCalcQueue;

            _cache = cache;

            _inverseClient = new();
            _usdtClient = new();
            _logger = LogManager.GetLogger(typeof(BybitFuturesFrStreaming));
        }

        public void Connect(IEnumerable<PairInfo> usdtPairs)
        {
            if (!usdtPairs.Any())
                return;

            this._usdtPairs = usdtPairs;

            _inverseClient.Client.MessageReceived += OnInverseMessageReceived;
            _inverseClient.Client.ServerConnected += OnInverseConnected;
            _inverseClient.Client.ServerDisconnected += OnInverseDisconnected;
            _inverseClient.ConnectAsync().Wait();

            _usdtClient.Client.MessageReceived += OnUsdtMessageReceived;
            _usdtClient.Client.ServerConnected += OnUsdtConnected;
            _usdtClient.Client.ServerDisconnected += OnUsdtDisconnected;
            _usdtClient.ConnectAsync().Wait();
        }

        private void OnInverseDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Disconnected");
            _cache.SetBybitFuturesIsFrStreaming(false);
            Dispose();
        }
        
        private void OnUsdtDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Disconnected");
            _cache.SetBybitFuturesIsFrStreaming(false);
            Dispose();
        }

        private async void OnInverseConnected(object sender, EventArgs e)
        {
            _logger.Info("Inverse socket connected.");
            await _inverseClient.SubToAllSymbols();
            if (_usdtClient.Client.Connected)
                _cache.SetBybitFuturesIsFrStreaming(true);
        }

        private async void OnUsdtConnected(object sender, EventArgs e)
        {
            _logger.Info("USDT socket connected.");
            await _usdtClient.SubscribeToSymbolsAsync(_usdtPairs.ToArray());
            if (_inverseClient.Client.Connected)
                _cache.SetBybitFuturesIsFrStreaming(true);
        }

        private void OnUsdtMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            HandleIncomingMessage(e.Data);
        }
        
        private void OnInverseMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            HandleIncomingMessage(e.Data);
        }

        private void HandleIncomingMessage(byte[] data)
        {
            if (!data[2].Equals(0x73)) // s | if it is not the 'success' message
            {
                if (TryDetectMessageIsSnapshot(data, out var isSnapshot))
                {
                    if (isSnapshot)
                    {
                        var fundingRate = JsonSerializer.Deserialize<BybitMessage<BybitInstrumentInfoSnapshot>>(data);
                        if (fundingRate.Data.Symbol.EndsWith("USDT") ||
                            fundingRate.Data.Symbol.EndsWith("USD"))
                        {
                            _frCalcQueue.Enqueue((fundingRate.Data.Symbol,
                                fundingRate.Data.PredictedFundingRate));
                        }
                    }
                    else
                    {
                        var fundingRate = JsonSerializer.Deserialize<BybitMessage<BybitInstrumentInfoData>>(data);
                        if (fundingRate.Data.Update[0].Symbol.EndsWith("USDT") ||
                            fundingRate.Data.Update[0].Symbol.EndsWith("USD"))
                        {
                            if (fundingRate.Data.Update[0].PredictedFundingRate != null)
                            {
                                if (fundingRate.Data.Update[0].Symbol.EndsWith("USD"))
                                    _logger.Info($"{fundingRate.Data.Update[0].Symbol} fr: {fundingRate.Data.Update[0].PredictedFundingRate}");
                                
                                _frCalcQueue.Enqueue(
                                    (fundingRate.Data.Update[0].Symbol,
                                        fundingRate.Data.Update[0].PredictedFundingRate.Value));
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _inverseClient?.Dispose();
            _usdtClient?.Dispose();
        }

        public bool TryDetectMessageIsSnapshot(byte[] data, out bool isSnapshot)
        {
            isSnapshot = true;
            string msg = Encoding.ASCII.GetString(data);
            try
            {
                var splitedMessage = msg.Split(@",""type"":""");

                if (splitedMessage[1].StartsWith("snapshot"))
                {
                    isSnapshot = true;
                    return true;
                }
                else if (splitedMessage[1].StartsWith("delta"))
                {
                    isSnapshot = false;
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return false;
            }

            return false;
        }
    }
}
