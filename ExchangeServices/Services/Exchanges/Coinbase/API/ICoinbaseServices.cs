using Coinbase;
using Coinbase.Models;
using Coinbase.Pro.Models;
using DataLayer;
using ExchangeModels.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeServices
{
    public interface ICoinbaseServices
    {

        Task<IEnumerable<string>> GetSymbolsAsync();

        Task<IEnumerable<DataLayer.Candle>> GetCandlesAsync(string symbol, DateTime startTime, DateTime endTime, CoinbaseTimeFrame timeFrame);

        Task<IEnumerable<Coinbase.Pro.Models.Trade>> GetMarketBookAsync(string symbol, int? limit = null, string before = null, string after = null);

        Task<OrderBook> GetOrderBookAsync(string symbol, CoinbaseOrderBookLimit limit = CoinbaseOrderBookLimit.All);
        Task<IEnumerable<DataLayer.Candle>> GetLastCandlesAsync(string symbol,CoinbaseTimeFrame timeFrame);


    }
}
