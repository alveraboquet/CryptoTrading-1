using System;
using System.Collections.Generic;
using System.Net.Http;
using Utf8Json;
using System.Threading.Tasks;
using ExchangeModels.Bybit.API;

namespace ExchangeServices.Services.Exchanges.Bybit.API
{
    public class BybitFuturesService : IBybitFuturesService
    {
        private readonly HttpClient _client;

        public BybitFuturesService()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://api.bybit.com/");
        }
        
        public async Task<IEnumerable<BybitFuturesSymbol>> GetSymbolsAsync()
        {
            var response = await _client.GetAsync("v2/public/symbols");
            response.EnsureSuccessStatusCode();
            var jsonAsString = await response.Content.ReadAsStringAsync();
            var responseObject = 
                JsonSerializer.Deserialize<BybitApiResponse<IEnumerable<BybitFuturesSymbol>>>(jsonAsString);
            return responseObject.Result;
        }
    }
}