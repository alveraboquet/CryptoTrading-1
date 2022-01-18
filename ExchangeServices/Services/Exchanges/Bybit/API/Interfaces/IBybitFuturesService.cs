using System.Collections.Generic;
using System.Threading.Tasks;
using Binance.Net.Objects.Futures.MarketData;
using ExchangeModels.Bybit.API;

namespace ExchangeServices.Services.Exchanges.Bybit.API
{
    public interface IBybitFuturesService
    {
        // returns both inverse perpetual and USDT perpetual symbols
        Task<IEnumerable<BybitFuturesSymbol>> GetSymbolsAsync();
    }
}