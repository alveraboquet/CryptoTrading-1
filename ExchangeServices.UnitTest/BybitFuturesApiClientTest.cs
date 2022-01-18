using System.Linq;
using System.Threading.Tasks;
using ExchangeServices.Services.Exchanges.Bybit.API;
using FluentAssertions;
using Xunit;

namespace ExchangeServices.UnitTest
{
    public class BybitFuturesApiClientTest
    {
        private readonly IBybitFuturesService _service;

        public BybitFuturesApiClientTest()
        {
            _service = new BybitFuturesService();
        }

        [Fact]
        public async Task ShouldSuccessfullyGetSymbols()
        {
            // Act
            var symbols = (await _service.GetSymbolsAsync()).ToList();
            
            // Assertion
            symbols.Should().NotBeNullOrEmpty();
        }
    }
}