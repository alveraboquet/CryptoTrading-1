using System;
using System.Threading.Tasks;
using ExchangeServices.Services.Exchanges.Bybit.API;
using FluentAssertions;
using Xunit;

namespace ExchangeServices.UnitTest
{
    public class BybitSpotApiClientTest
    {
        private readonly IBybitService _service;

        public BybitSpotApiClientTest()
        {
            _service = new BybitService();
        }
        
        [Fact]
        public async Task ShouldSuccessfullyGetSymbols()
        {
            // Act
            var symbols = await _service.GetSymbolsAsync();
            
            // Assertion
            symbols.Should().NotBeNullOrEmpty();
        }
    }
}