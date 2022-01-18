using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserModels;

namespace UserRepository
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) 
            : base(options)
        {  }

        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Layer> Layers { get; set; }
        public DbSet<Drawing> Drawings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Email);
                entity.HasKey(u => u.UserName);
                entity.HasKey(u => u.Id);
                entity.HasMany(u => u.Sessions)
                    .WithOne(s => s.User)
                    .HasForeignKey(s => s.UserId);
            });

            builder.Entity<Layer>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.HasOne(l => l.User)
                    .WithMany(u => u.Layers)
                    .HasForeignKey(l => l.UserId);
            });

            builder.Entity<Drawing>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasOne(d => d.Layer)
                    .WithMany(l => l.Drawings)
                    .HasForeignKey(d => d.LayerId);
            });

            base.OnModelCreating(builder);
        }
    }
}