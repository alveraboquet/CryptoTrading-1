using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DataLayer;

namespace DatabaseRepository
{
    public interface ICandleService
    {
        Task<Candle> CreateOrUpdateByOpenTimeAsync(Candle candle);
        Candle CreateOrUpdateByOpenTime(Candle candle);

        Task<IEnumerable<Candle>> Get(string exchange, string symbol, string timeframe, long start, long end);
        Task<IEnumerable<ResCandle>> GetCandlesForAPIAsync(string exchange, string symbol, string timeframe, long start, long end);
        Task<IEnumerable<ResFootPrint>> GetFootprint(string exchange, string symbol, string timeframe, long start, long end);
        Task CreateMany(string exchange, string symbol, string timeframe, IEnumerable<Candle> candles);
        Task<long> GetLastCandleOpenTime(string exchange, string symbol, string timeframe);
        Task<IEnumerable<Candle>> GetLast(string exchange, string symbol, string timeframe, int take);
        Task<IEnumerable<ResHeatmap>> GetHeatmap8K(string exchange, string symbol, string timeframe, long start, long end);
        Task<IEnumerable<ResCandle>> GetAllCandlesAsync(string exchange, string symbol, string timeframe);

        Task<IEnumerable<ResCandle>> GetAllFrCandlesAsync(string exchange, string symbol, string timeframe);
        Task<IEnumerable<ResCandle>> GetAllLiqCandlesAsync(string exchange, string symbol, string timeframe);
        Task<IEnumerable<ResCandle>> GetAllLiqBuyCandlesAsync(string exchange, string symbol, string timeframe);
        Task<IEnumerable<ResCandle>> GetAllLiqSellCandlesAsync(string exchange, string symbol, string timeframe);

        Task<IEnumerable<ResFootPrint>> GetAllFootprintAsync(string exchange, string symbol, string timeframe);
        Task<IEnumerable<ResHeatmap>> GetAllHeatmapAsync(string exchange, string symbol, string timeframe);
    }
}
