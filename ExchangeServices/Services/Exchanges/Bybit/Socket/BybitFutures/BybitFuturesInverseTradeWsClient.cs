using System;
using DataLayer;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;
using DataLayer.Models;

namespace ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures
{
    public class BybitFuturesInverseTradeWsClient : IDisposable
    {
        private const string WebsocketURL = "wss://stream.bybit.com/realtime";
        private WatsonWsClient _client;

        public WatsonWsClient Client => this._client;

        public BybitFuturesInverseTradeWsClient()
        {
            _client = new WatsonWsClient(new Uri(WebsocketURL))
            {
                EnableStatistics = false
            };

            _client.ConfigureOptions((options) => { options.SetBuffer(400, 400); });
        }

        public Task ConnectAsync() => _client.StartAsync();

        public async Task SubToAllSymbols()
        {
            string subMessage = @"{""op"":""subscribe"",""args"":[""trade""]}";
            // string subMessage = @"{""op"":""subscribe"",""args"":[""trade.BTCUSD""]}";
            await _client.SendAsync(subMessage);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
