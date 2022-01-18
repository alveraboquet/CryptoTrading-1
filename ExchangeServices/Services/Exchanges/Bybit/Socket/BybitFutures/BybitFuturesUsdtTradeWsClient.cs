using System;
using System.Text;
using System.Threading.Tasks;
using DataLayer;
using WatsonWebsocket;

namespace ExchangeServices.Services.Exchanges.Bybit.Socket.BybitFutures
{
    public class BybitFuturesUsdtTradeWsClient
    {
        private const string WebsocketURL = "wss://stream.bybit.com/realtime_public";
        
        private WatsonWsClient _client;
        public WatsonWsClient Client => this._client;

        public BybitFuturesUsdtTradeWsClient()
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
            {
                if (!symbol.Symbol.EndsWith("USD") && !symbol.Symbol.EndsWith("USDT")) continue;
                
                // if (symbol.Symbol != "BTCUSDT") continue;
                
                str.Append($"\"trade.{symbol.Symbol}\",");
            }

            try
            {
                // removing extra ',' at the end
                var symbolPayload = str.ToString().Substring(0, str.Length - 1);

                var subRequest = "{" +
                                 "\"op\": \"subscribe\", " +
                                 $"\"args\": [{symbolPayload}]" +
                                 "}";
                await _client.SendAsync(subRequest);
            }
            catch (Exception e)
            {
                // Console.WriteLine(e.StackTrace);
                // Console.WriteLine(e.Message);
                // Console.WriteLine(str.ToString());
            }
        }
        
        public void Dispose() => _client.Dispose();
    }
}