using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserModels;

namespace UserRepository
{
    public interface ILayerRepository
    {
        Task<Layer> AddLayerAsync(Layer layer);
        Task<int> GetLayersCountAsync(int userId, string exchange, string symbol);
        Task<List<LayerRes>> GetUserLayersAsync(int userId, string exchange, string symbol);

        Task<bool> SetLayerAsDefualtAsync(int userId, string exchange, string symbol, long layerId);

        Task<List<LayerRes>> DeleteLayerAndHandleAsync(int userId, string exchange, string symbol, long layerId);

        Task SaveChangesAsync();
        //Task<Layer> AddLayerAsync();
    }
}
