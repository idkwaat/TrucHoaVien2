using Microsoft.EntityFrameworkCore;
using ProjectApi.Api.Models;
using ProjectApi.Models;

namespace ProjectApi.Data
{
    public class FurnitureDbContext : DbContext
    {
        public FurnitureDbContext(DbContextOptions<FurnitureDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // nếu cần cấu hình thêm (cascade, index...) đặt ở đây
        }

    }
}
