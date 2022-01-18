using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace ExchangeServices.Services
{
    public class BybitFuturesInverseFrWsClient
    {
        private const string WebsocketURL = "wss://stream.bybit.com/realtime";
        private WatsonWsClient _client;

        public WatsonWsClient Client => this._client;

        public BybitFuturesInverseFrWsClient()
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
            string subMessage = @"{""op"":""subscribe"",""args"":[""instrument_info.100ms.*""]}";
            // string subMessage = @"{""op"":""subscribe"",""args"":[""instrument_info.100ms.BTCUSD|XRPUSD""]}";
            await this._client.SendAsync(subMessage);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
