using DataLayer;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseRepository
{
    public class PairInfoService : IPairInfoRepository
    {
        private readonly IMongoCollection<DataLayer.PairInfo> _pairinfo;

        public PairInfoService(IChartDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _pairinfo = database.GetCollection<DataLayer.PairInfo>(settings.PairInfoCollectionName);
        }

        public async Task<List<DataLayer.PairInfo>> Get()
        {
            return await (await _pairinfo.FindAsync(_ => true)).ToListAsync();
        }

        public async Task<DataLayer.PairInfo> Get(string exchange, string symbol)
        {
            return await (await _pairinfo.FindAsync(pair => pair.Exchange == exchange
                                       && pair.Symbol == symbol)).FirstOrDefaultAsync();
        }

        public DataLayer.PairInfo Create(DataLayer.PairInfo PairInfo)
        {
            _pairinfo.InsertOne(PairInfo);
            return PairInfo;
        }

        public void Remove(DataLayer.PairInfo PairInfoIn)
        {
            _pairinfo.DeleteOne(pair => pair.PairId == PairInfoIn.PairId);
        }

        public void Remove(string id)
        {
            _pairinfo.DeleteOne(pair => pair.PairId == id);
        }

        public void Update(string id, DataLayer.PairInfo PairInfoIn)
        {
            _pairinfo.ReplaceOne(pair => pair.PairId == id, PairInfoIn);
        }

        public bool IsExist(string exchange, string symbol)
        {
            var res = this.Get(exchange, symbol);
            return (res == null);
        }

        public Task<List<DataLayer.PairInfo>> Get(string exchange)
        {
            return _pairinfo.Find<DataLayer.PairInfo>(pair => pair.Exchange == exchange).ToListAsync();
        }

        public async Task<IEnumerable<DataLayer.PairInfo>> GetListed(string exchange)
        {
            return await _pairinfo.Find<DataLayer.PairInfo>(pair => pair.Exchange == exchange &&
                                                              pair.IsListed).ToListAsync();
        }

        public async Task<IEnumerable<PairInfo>> Search(string exchange, string text)
        {
            var query = (from p in _pairinfo.AsQueryable()
                         where p.Exchange == exchange && p.Symbol.Contains(text)
                         select p);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<PairInfo>> Search(string text)
        {
            var filter = Builders<PairInfo>.Filter.Regex(p => p.Symbol, new BsonRegularExpression(text));
            var query = (from p in _pairinfo.AsQueryable()
                         where p.Symbol.Contains(text)
                         select p);

            //return (_pairinfo.Find(filter)).ToList();
            return await query.ToListAsync();
        }
    }
}
