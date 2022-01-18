using DataLayer;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseRepository
{
    public class PairStreamInfoService : IPairStreamInfoRepository
    {
        private readonly IMongoCollection<DataLayer.PairStreamInfo> coll;
        public PairStreamInfoService(IChartDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            coll = database.GetCollection<DataLayer.PairStreamInfo>(settings.StreamInfoCollectionName);
        }

        public Task Create(PairStreamInfo streamInfo)
        {
            return coll.InsertOneAsync(streamInfo);
        }

        public Task CreateMany(IEnumerable<PairStreamInfo> streamInfos)
        {
            return coll.InsertManyAsync(streamInfos);
        }

        public Task<List<PairStreamInfo>> GetMany(string exchange)
        {
            return (from s in coll.AsQueryable()
                    where s.Exchange == exchange
                    select s).ToListAsync();
        }

        public Task<PairStreamInfo> GetOne(string exchange, string symbol, string timeframe)
        {
            return (from s in coll.AsQueryable()
                    where s.Exchange == exchange && s.Symbol == symbol && s.TimeFrame == timeframe
                    select s).FirstOrDefaultAsync();
        }

        public Task<PairStreamInfo> GetOne(string id)
        {
            return (from s in coll.AsQueryable()
                    where s.Id == id
                    select s).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<StreamReport>> GetReportsForRange(string exchange, string symbol, string timeframe, long start, long end)
        {
            var report = await (from s in coll.AsQueryable()
                    where s.Exchange == exchange && s.Symbol == symbol && s.TimeFrame == timeframe
                    select s.StreamReports).FirstOrDefaultAsync();
            if (report == default)
                return null;
            
            return report.Where(r => 
                        start <= r.Stop && r.Stop <= end
                            ||
                        start <= r.Start && r.Start <= end
                        ).ToList();
        }

        public Task RemoveReportsForRange(string exchange, string symbol, string timeframe, IEnumerable<StreamReport> reports)
        {
            throw new NotImplementedException();
        }

        public Task Update(string id, PairStreamInfo streamInfo)
        {
            var filter = Builders<PairStreamInfo>.Filter.Eq(s => s.Id, id);
            return coll.ReplaceOneAsync(filter, streamInfo);
        }

        public Task Upsert(PairStreamInfo streamInfo)
        {
            throw new NotImplementedException();
        }
    }
}
