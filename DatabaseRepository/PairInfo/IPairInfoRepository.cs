using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DataLayer;

namespace DatabaseRepository
{
    public interface IPairInfoRepository
    {
        //Task<IEnumerable<DataLayer.PairInfo>> Get();
        Task<List<DataLayer.PairInfo>> Get();
        Task<DataLayer.PairInfo> Get(string exchange, string symbol);
        Task<List<DataLayer.PairInfo>> Get(string exchange);
        Task<IEnumerable<DataLayer.PairInfo>> GetListed(string exchange);
        Task<IEnumerable<DataLayer.PairInfo>> Search(string exchange, string text);
        Task<IEnumerable<DataLayer.PairInfo>> Search(string text);

        DataLayer.PairInfo Create(DataLayer.PairInfo PairInfo);
        bool IsExist(string exchange, string symbol);
        public void Update(string id, DataLayer.PairInfo PairInfoIn);
        public void Remove(DataLayer.PairInfo PairInfoIn);
        public void Remove(string id);
    }
}
