using Coinbase;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Coinbase.Pro;
using DataLayer;
using ExchangeModels.Enums;
using Utilities;

namespace ExchangeServices
{
    public class CoinbaseServices : ICoinbaseServices
    {
        private CoinbaseProClient _client;
        public CoinbaseServices(CoinbaseProClient client)
        {
            _client = client;
        }

        public async Task<IEnumerable<Candle>> GetCandlesAsync(string symbol, DateTime startTime, DateTime endTime, CoinbaseTimeFrame timeFrame)
        {
            try
            {
                var result = await _client.MarketData.GetHistoricRatesAsync(symbol, startTime, endTime, timeFrame.GetSeconds());
                var candes = result.Select(c => new Candle(
                    openTime: c.Time.ToUnixTimeSeconds(),
                    open: c.Open.Value,
                    high: c.High.Value,
                    low: c.Low.Value,
                    close: c.Close.Value,
                    volume: c.Volume.Value,
                    timeframe: timeFrame.ToStringFormat(),
                    exchange: ApplicationValues.CoinbaseName,
                    symbol: symbol));

                return candes;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<Candle>> GetLastCandlesAsync(string symbol, CoinbaseTimeFrame timeFrame)
        {
            try
            {
                var result = await _client.MarketData.GetHistoricRatesAsync(symbol, DateTime.Now, DateTime.Now, timeFrame.GetSeconds());
                var candes = result.Select(c => new Candle(
                    openTime: c.Time.ToUnixTimeSeconds(),
                    open: c.Open.Value,
                    high: c.High.Value,
                    low: c.Low.Value,
                    close: c.Close.Value,
                    volume: c.Volume.Value,
                    timeframe: timeFrame.ToStringFormat(),
                    exchange: ApplicationValues.CoinbaseName,
                    symbol: symbol));

                return candes;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<Coinbase.Pro.Models.Trade>> GetMarketBookAsync(string symbol, int? limit = null, string before = null, string after = null)
        {
            try
            {
                // if enter grater than 100 returns null
                limit = Math.Min(limit.Value, 100);

                var result = await _client.MarketData.GetTradesAsync(symbol, limit, before, after);
                
                if (!result.Data.Any()) return null;

                return result.Data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Coinbase.Pro.Models.OrderBook> GetOrderBookAsync(string symbol, CoinbaseOrderBookLimit limit = CoinbaseOrderBookLimit.All)
        {
            try
            {
                var result = await _client.MarketData.GetOrderBookAsync(symbol, (int)limit);

                return result;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<string>> GetSymbolsAsync()
        {
            try
            {
                var result = await _client.MarketData.GetProductsAsync();
                if (!result.Any()) return null;

                return result.Select(s => s.Id);
            }
            catch
            {
                return null;
            }
        }
    }
}
