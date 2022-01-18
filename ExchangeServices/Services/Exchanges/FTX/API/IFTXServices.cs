using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Enums;
using DataLayer;
using FtxApi.Enums;
using FtxApi.Models.LeveragedTokens;
using FtxApi.Models.Markets;
using FtxApi.Util;

namespace ExchangeServices
{
    public interface IFTXServices
    {
        Task<IEnumerable<Candle>> GetCandelsAsync(string symbol, FtxResolution timeFrame, int limit, DateTime startTime, DateTime endTime);

        Task<IEnumerable<Candle>> GetLastCandelsAsync(string symbol, FtxResolution timeFrame, int limit);

        Task<IEnumerable<string>> GetSymbolsAsync();

        Task<List<FtxApi.Models.Trade>> GetMarketBookAsync(string symbol, int limit, DateTime start, DateTime end); 

        Task<Orderbook> GetOrderBookAsync(string symbol); // symbol example: ETH/BTC or BTC-PERP
    }
}
