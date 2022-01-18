using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserModels;

namespace UserRepository
{
    public interface IDrawingRepository
    {
        Task<long> AddDrawingAsync(Drawing drawing, int userId, long layerId, string exchange, string symbol);

        Task<bool> ModifyDrawingAsync(long userId, long drawId, string data);
        Task<bool> DeleteDrawingAsync(long userId, long drawId);

        Task<List<DrawingRes>> GetAllDrawingsAsync(long userId, string exchange, string symbol, long layerId);

        Task SaveChangesAsync();
    }
}
