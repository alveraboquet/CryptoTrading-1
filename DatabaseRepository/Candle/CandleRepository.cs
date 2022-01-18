using DataLayer;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;
using DataLayer.Models;

namespace DatabaseRepository
{
    public class CandleRepository : ICandleService
    {
        private readonly ConcurrentDictionary<string, IMongoCollection<Candle>> collections;
        private readonly IMongoDatabase database;
        private readonly IMongoDatabase adminDB;
        private readonly IChartDatabaseSettings _settings;
        public CandleRepository(IChartDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            adminDB = client.GetDatabase("admin");
            database = client.GetDatabase(settings.DatabaseName);
            _settings = settings;
            collections = new ConcurrentDictionary<string, IMongoCollection<Candle>>();
        }

        #region Private Methods

        private Task<IMongoCollection<Candle>> GetCollection(Candle candle) => GetCollection(candle.Exchange, candle.Symbol);

        private async Task<IMongoCollection<Candle>> GetCollection(string exchange, string symbol)
        {
            if (string.IsNullOrWhiteSpace(exchange)) throw new ArgumentNullException(nameof(exchange), "Exchange is not specified.");
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentNullException(nameof(symbol), "Symbol is not specified.");

            string collectionName = CollectionNameProvider.CandleExSy(exchange, symbol);
            if (!collections.TryGetValue(collectionName, out var coll))
            {
                coll = database.GetCollection<Candle>(collectionName);

                #region Create OpenTime:TimeFrame Index
                // befor this was just OpenTime because we had ex.symbol.timeframe data structure in MongoDB
                var key = Builders<Candle>.IndexKeys.Ascending(c => c.OpenTime).Ascending(c => c.TimeFrame);
                await coll.Indexes.CreateOneAsync(
                    new CreateIndexModel<Candle>(key, new CreateIndexOptions()
                    {
                        Unique = true
                    }));
                #endregion

                /*#region Shard Collection

                var bCommand = new BsonDocument
                {
                    {"shardCollection", $"{_settings.DatabaseName}.{collectionName}"},
                    {"key", new BsonDocument{ {"OT", 1} }}
                };
                var res = await adminDB.RunCommandAsync<BsonDocument>(bCommand);

                #endregion*/

                collections[collectionName] = coll;
            }

            return coll;
        }

        #endregion

        public async Task CreateMany(string exchange, string symbol, string timeframe, IEnumerable<Candle> candles)
        {
            var collection = await this.GetCollection(exchange, symbol);
            await collection.InsertManyAsync(candles, new InsertManyOptions()
            {
                IsOrdered = false
            });
        }

        public Candle CreateOrUpdateByOpenTime(Candle candle)
        {
            var collection = this.GetCollection(candle).Result;

            try
            {
                collection.InsertOne(candle);
            }
            catch (MongoWriteException)
            {
                var filterOT = Builders<Candle>.Filter.Eq(c => c.OpenTime, candle.OpenTime);
                var filterId = Builders<Candle>.Filter.Eq(c => c.Id, candle.Id);
                var filter = Builders<Candle>.Filter.And(filterId, filterOT);

                collection.ReplaceOne(filter, candle);
            }
            return candle;
        }

        public async Task<Candle> CreateOrUpdateByOpenTimeAsync(Candle candle)
        {
            var collection = await this.GetCollection(candle);

            try
            {
                await collection.InsertOneAsync(candle);
            }
            catch (MongoWriteException)
            {
                var filterOT = Builders<Candle>.Filter.Eq(c => c.OpenTime, candle.OpenTime);
                var filterId = Builders<Candle>.Filter.Eq(c => c.Id, candle.Id);
                var filter = Builders<Candle>.Filter.And(filterId, filterOT);

                await collection.ReplaceOneAsync(filter, candle);
            }

            return candle;
        }

        public async Task<IEnumerable<Candle>> Get(string exchange, string symbol, string timeframe, long start, long end)
        {
            var collection = await this.GetCollection(exchange, symbol);
            var query = (from c in collection.AsQueryable()
                         where
                            c.TimeFrame == timeframe &&
                            c.OpenTime >= start && c.OpenTime <= end
                         orderby c.OpenTime
                         select c);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ResCandle>> GetCandlesForAPIAsync(string exchange, string symbol, string timeframe, long start, long end)
        {
            var collection = await this.GetCollection(exchange, symbol);
            var query = (from c in collection.AsQueryable()
                         where
                            c.TimeFrame == timeframe &&
                            c.OpenTime >= start && c.OpenTime <= end
                         orderby c.OpenTime
                         select new ResCandle()
                         {
                             Close = c.ClosePrice,
                             High= c.HighPrice,
                             Low = c.LowPrice,
                             Open = c.OpenPrice,
                             OpenTime = c.OpenTime,
                             Volume = c.Volume
                         });

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ResFootPrint>> GetFootprint(string exchange, string symbol, string timeframe, long start, long end)
        {
            var collection = await this.GetCollection(exchange, symbol);
            var query = (from c in collection.AsQueryable()
                         where c.FootPrint != null && c.TimeFrame == timeframe
                            && c.OpenTime >= start && c.OpenTime <= end
                         orderby c.OpenTime
                         select new ResFootPrint()
                         {
                             AboveMarketOrders = c.FootPrint.AboveMarketOrders,
                             BelowMarketOrders = c.FootPrint.BelowMarketOrders,
                             OpenPrice = c.OpenPrice,
                             OpenTime = c.OpenTime
                         });

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ResHeatmap>> GetHeatmap8K(string exchange, string symbol, string timeframe, long start, long end)
        {
            var collection = await this.GetCollection(exchange, symbol);
            var query = (from c in collection.AsQueryable()
                         where c.Heatmap8K != null && c.TimeFrame == timeframe
                            && c.OpenTime >= start && c.OpenTime <= end
                         orderby c.OpenTime
                         select new ResHeatmap()
                         {
                             Blocks = c.Heatmap8K.Blocks,
                             OpenPrice = c.OpenPrice,
                             OpenTime = c.OpenTime,
                         });
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Candle>> GetLast(string exchange, string symbol, string timeframe, int take)
        {
            var collection = await this.GetCollection(exchange, symbol);

            var filter = Builders<Candle>.Filter.Eq(c => c.TimeFrame, timeframe);
            var sort = Builders<Candle>.Sort.Descending(c => c.OpenTime);

            return collection.Find(_ => true).Sort(sort).Limit(take).ToList();
        }

        public async Task<long> GetLastCandleOpenTime(string exchange, string symbol, string timeframe)
        {
            var collection = await this.GetCollection(exchange, symbol);

            return (from c in collection.AsQueryable()
                    where c.TimeFrame == timeframe
                    select c.OpenTime).Max();
        }

        public async Task<IEnumerable<ResFootPrint>> GetAllFootprintAsync(string exchange, string symbol, string timeframe)
        {
            var collection = await this.GetCollection(exchange, symbol);

            var query = (from c in collection.AsQueryable()
                         where c.FootPrint != null && c.TimeFrame == timeframe

                         orderby c.OpenTime
                         select new ResFootPrint()
                         {
                             AboveMarketOrders = c.FootPrint.AboveMarketOrders,
                             BelowMarketOrders = c.FootPrint.BelowMarketOrders,
                             OpenPrice = c.OpenPrice,
                             OpenTime = c.OpenTime
                         });

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ResHeatmap>> GetAllHeatmapAsync(string exchange, string symbol, string timeframe)
        {
            var collection = await this.GetCollection(exchange, symbol);
            var query = (from c in collection.AsQueryable()
                         where c.Heatmap8K != null && c.TimeFrame == timeframe

                         orderby c.OpenTime
                         select new ResHeatmap()
                         {
                             Blocks = c.Heatmap8K.Blocks,
                             OpenPrice = c.OpenPrice,
                             OpenTime = c.OpenTime,
                         });

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ResCandle>> GetAllCandlesAsync(string exchange, string symbol, string timeframe)
        {
            var collection = await this.GetCollection(exchange, symbol);
            var query = (from c in collection.AsQueryable()
                         where c.TimeFrame == timeframe

                         orderby c.OpenTime
                         select new ResCandle()
                         {
                             Close = c.ClosePrice,
                             High = c.HighPrice,
                             Low = c.LowPrice,
                             Open = c.OpenPrice,
                             OpenTime = c.OpenTime,
                             Volume = c.Volume
                         });

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ResCandle>> GetAllFrCandlesAsync(string exchange, string symbol, string timeframe)
        {
            var collection = await this.GetCollection(exchange, symbol.Split('.')[1]);

            var query = (from c in collection.AsQueryable()
                         where c.TimeFrame == timeframe && c.FundingRate != null

                         orderby c.OpenTime
                         select new ResCandle()
                         {
                             OpenTime = c.OpenTime,

                             Open = c.FundingRate.Open,
                             High = c.FundingRate.High,
                             Low = c.FundingRate.Low,
                             Close = c.FundingRate.Close,

                             //Volume = 0
                         });

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ResCandle>> GetAllLiqCandlesAsync(string exchange, string symbol, string timeframe)
        {
            var collection = await this.GetCollection(exchange, symbol.Split('.')[1]);

            var query = (from c in collection.AsQueryable()
                         where c.TimeFrame == timeframe && c.Liquidation != null

                         orderby c.OpenTime
                         select new ResCandle()
                         {
                             OpenTime = c.OpenTime,

                             Open = c.Liquidation.Liq,
                             High = c.Liquidation.Liq,
                             Low = c.Liquidation.Liq,
                             Close = c.Liquidation.Liq,
                             
                             //Volume = 0
                         });

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ResCandle>> GetAllLiqBuyCandlesAsync(string exchange, string symbol, string timeframe)
        {
            var collection = await this.GetCollection(exchange, symbol.Split('.')[1]);

            var query = (from c in collection.AsQueryable()
                         where c.TimeFrame == timeframe && c.Liquidation != null

                         orderby c.OpenTime
                         select new ResCandle()
                         {
                             OpenTime = c.OpenTime,

                             Open = c.Liquidation.LiqBuy,
                             High = c.Liquidation.LiqBuy,
                             Low = c.Liquidation.LiqBuy,
                             Close = c.Liquidation.LiqBuy,

                             //Volume = 0
                         });

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ResCandle>> GetAllLiqSellCandlesAsync(string exchange, string symbol, string timeframe)
        {
            var collection = await this.GetCollection(exchange, symbol.Split('.')[1]);

            var query = (from c in collection.AsQueryable()
                         where c.TimeFrame == timeframe && c.Liquidation != null

                         orderby c.OpenTime
                         select new ResCandle()
                         {
                             OpenTime = c.OpenTime,

                             Open = c.Liquidation.LiqSell,
                             High = c.Liquidation.LiqSell,
                             Low = c.Liquidation.LiqSell,
                             Close = c.Liquidation.LiqSell,

                             //Volume = 0
                         });

            return await query.ToListAsync();
        }
    }
}
