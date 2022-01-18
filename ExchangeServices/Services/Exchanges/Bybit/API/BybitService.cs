using System;
using System.Collections.Generic;
using System.Net.Http;
using Utf8Json;
using System.Threading.Tasks;
using ExchangeModels.Bybit.API;

namespace ExchangeServices.Services.Exchanges.Bybit.API
{
    public class BybitService : IBybitService
    {
        private readonly HttpClient _client;

        public BybitService()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://api.bybit.com/");
        }
        
        public async Task<IEnumerable<BybitSpotSymbol>> GetSymbolsAsync()
        {
            var response = await _client.GetAsync("spot/v1/symbols");
            response.EnsureSuccessStatusCode();
            var jsonAsString = await response.Content.ReadAsStringAsync();
            var responseObject = 
                JsonSerializer.Deserialize<BybitApiResponse<IEnumerable<BybitSpotSymbol>>>(jsonAsString);
            return responseObject.Result;
        }
    }
}