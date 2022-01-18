using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserModels;

namespace UserRepository
{
    public class LayerRepo : ILayerRepository
    {
        private UserContext _context;
        public LayerRepo(UserContext userContext)
        {
            _context = userContext;
        }

        public async Task<Layer> AddLayerAsync(Layer layer)
        {
            await _context.Layers.AddAsync(layer);
            await this.SaveChangesAsync();
            return layer;
        }

        public Task<int> GetLayersCountAsync(int userId, string exchange, string symbol)
        {
            return (from l in _context.Layers.AsQueryable()
                    where l.UserId == userId && l.Exchange == exchange && l.Symbol == symbol
                    select l).CountAsync();
        }

        public async Task<bool> SetLayerAsDefualtAsync(int userId, string exchange, string symbol, long layerId)
        {
            // set other false
            var layers = await (from l in _context.Layers.AsQueryable()
                          where l.UserId == userId &&
                          l.Exchange == exchange &&
                          l.Symbol == symbol && l.IsDefault
                          select l).ToListAsync();
            foreach (var item in layers)
            {
                item.IsDefault = false;
            }

            // set this as default
            var layer = await (from l in _context.Layers.AsQueryable()
                         where l.UserId == userId && l.Id == layerId
                         select l).FirstOrDefaultAsync();

            if (layer == default)
                return false;

            layer.IsDefault = true;
            await this.SaveChangesAsync();

            return true;
        }

        public Task<List<LayerRes>> GetUserLayersAsync(int userId, string exchange, string symbol)
        {
            return (from l in _context.Layers.AsQueryable()
                    where l.UserId == userId &&
                        l.Exchange == exchange &&
                        l.Symbol == symbol
                    orderby l.IsDefault descending
                    select new LayerRes
                    {
                        Id = l.Id,
                        Name = l.Name
                    }).ToListAsync();
        }

        public async Task<List<LayerRes>> DeleteLayerAndHandleAsync(int userId, string exchange, string symbol, long layerId)
        {
            var layers = await (from l in _context.Layers.AsQueryable()
                                where 
                                    l.UserId == userId &&
                                    l.Exchange == exchange &&
                                    l.Symbol == symbol
                                select l).ToListAsync();

            var layer = layers.FirstOrDefault(l => l.Id == layerId);
            if (layer == default)
                throw new Exception("wrong layer id.");
            else
            {
                layers.Remove(layer);
                _context.Remove(layer);
            }

            if (!layers.Any())
            {
                Layer l = new Layer()
                {
                    UserId = userId,
                    Exchange = exchange,
                    Symbol = symbol,
                    IsDefault = true,
                    Name = "genesis"
                };
                layers.Add(l);
                await _context.AddAsync(l);
            }

            if (layer.IsDefault)
                layers[0].IsDefault = true;

            await this.SaveChangesAsync();
            return layers
                .OrderByDescending(l => l.IsDefault).Select(l => new LayerRes()
            {
                Id = l.Id,
                Name = l.Name
            }).ToList();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
