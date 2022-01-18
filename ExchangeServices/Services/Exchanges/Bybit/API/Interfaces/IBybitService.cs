using System.Collections.Generic;
using System.Threading.Tasks;
using Binance.Net.Objects.Spot.MarketData;
using ExchangeModels.Bybit.API;

namespace ExchangeServices.Services.Exchanges.Bybit.API
{
    public interface IBybitService
    {
        Task<IEnumerable<BybitSpotSymbol>> GetSymbolsAsync();
    }
}