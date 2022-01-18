using System;
using System.Text;
using System.Threading.Tasks;
using DataLayer;
using WatsonWebsocket;

namespace ExchangeServices.Services.Exchanges.Bybit.Socket
{
    public class BybitFuturesUsdtLiqWsClient : IDisposable
    {
        private const string WebsocketURL = "wss://stream.bybit.com/realtime_public";
        private WatsonWsClient _client;
        public WatsonWsClient Client { get => this._client; }

        public BybitFuturesUsdtLiqWsClient()
        {
            _client = new WatsonWsClient(new Uri(WebsocketURL))
            {
                EnableStatistics = false
            };

            _client.ConfigureOptions((options) => { options.SetBuffer(400, 400); });
        }
        
        public Task ConnectAsync() => _client.StartAsync();
        
        public async Task SubscribeToSymbolsAsync(PairInfo[] symbols)
        {
            StringBuilder str = new StringBuilder();

            foreach (PairInfo symbol in symbols)
                str.Append($"\"liquidation.{symbol.Symbol}\",");
            
            // removing extra ',' at the end
            var symbolPayload = str.ToString().Substring(0, str.Length - 1);
            // var symbolPayload = "\"liquidation.BTCUSDT\"";
            
            var subRequest = "{" +
                             "\"op\": \"subscribe\", " +
                             $"\"args\": [{symbolPayload}]" +
                             "}";
            await _client.SendAsync(subRequest);
        }
        
        public void Dispose() => _client.Dispose();
    }
}