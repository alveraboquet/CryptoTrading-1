using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseRepository
{
    public interface IPairStreamInfoRepository
    {
        Task<IEnumerable<StreamReport>> GetReportsForRange(string exchange, string symbol, string timeframe, long start, long end);
        Task RemoveReportsForRange(string exchange, string symbol, string timeframe, IEnumerable<StreamReport> reports);
        Task Create(PairStreamInfo streamInfo);
        Task CreateMany(IEnumerable<PairStreamInfo> streamInfos);
        Task<List<PairStreamInfo>> GetMany(string exchange);
        Task<PairStreamInfo> GetOne(string exchange, string symbol, string timeframe);
        Task<PairStreamInfo> GetOne(string id);
        Task Update(string id, PairStreamInfo streamInfo);
        Task Upsert(PairStreamInfo streamInfo);
    }
}
