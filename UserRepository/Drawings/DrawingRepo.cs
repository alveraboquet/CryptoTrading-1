using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserModels;
using Microsoft.EntityFrameworkCore;

namespace UserRepository
{
    public class DrawingRepo : IDrawingRepository
    {
        private UserContext _context;
        public DrawingRepo(UserContext userContext)
        {
            _context = userContext;
        }


        public async Task<long> AddDrawingAsync(Drawing drawing, int userId, long layerId, string exchange, string symbol)
        {
            if (layerId != 0)
            {
                if (!await _context.Layers.AnyAsync(l => l.Id == layerId && l.UserId == userId))
                    return -1;
            }
            else  // its default layer
            {
                if (!_context.Layers.Any(l => l.UserId == userId && l.Exchange == exchange && l.Symbol == symbol))
                {
                    Layer layer = new()
                    {
                        UserId = userId,
                        Exchange = exchange,
                        Symbol = symbol,
                        IsDefault = true,
                        Name = "genesis",
                        Drawings = new List<Drawing>()
                    };
                    layer.Drawings.Add(drawing);

                    await _context.Layers.AddAsync(layer);
                }
                else
                {
                    drawing.LayerId = await (from l in _context.Layers.AsQueryable()
                                             where 
                                                l.UserId == userId && l.IsDefault &&            // for this user
                                                l.Exchange == exchange && l.Symbol == symbol    // and this ex:symbol
                                             select l.Id).FirstOrDefaultAsync();

                    await _context.Drawings.AddAsync(drawing);
                }
            }

            await this.SaveChangesAsync();
            return drawing.Id;
        }

        public async Task<bool> DeleteDrawingAsync(long userId, long drawId)
        {
            try
            {
                var draw = await (from d in _context.Drawings.AsQueryable()
                            where 
                                d.Id == drawId &&           // this draw
                                d.Layer.UserId == userId    // for this user
                            select d).FirstOrDefaultAsync();
                _context.Drawings.Remove(draw);
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task<List<DrawingRes>> GetAllDrawingsAsync(long userId, string exchange, string symbol, long layerId)
        {
            if (layerId == 0) // its default layer
            {
                return (from d in _context.Drawings.AsQueryable()
                        where
                            d.Layer.UserId == userId &&
                            d.Layer.Exchange == exchange &&
                            d.Layer.Symbol == symbol &&
                            d.Layer.IsDefault
                        select new DrawingRes()
                        {
                            Data = d.Data,
                            Id = d.Id,
                            Type = d.Type
                        }).ToListAsync();
            }
            else // by this layerId
            {
                return (from d in _context.Drawings.AsQueryable()
                        where
                            d.Layer.Id == layerId &&
                            d.Layer.UserId == userId
                        select new DrawingRes()
                        {
                            Data = d.Data,
                            Id = d.Id,
                            Type = d.Type
                        }).ToListAsync();
            }
        }

        public async Task<bool> ModifyDrawingAsync(long userId, long drawId, string data)
        {
            try
            {
                var draw = await (from d in _context.Drawings.AsQueryable()
                                    where
                                        d.Id == drawId &&           // this draw
                                        d.Layer.UserId == userId    // for this user
                                    select d).FirstOrDefaultAsync();
                draw.Data = data;
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task SaveChangesAsync() => this._context.SaveChangesAsync();
    }
}
