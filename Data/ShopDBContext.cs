using Microsoft.EntityFrameworkCore;
using DatabaseConsumer.Models;

namespace DatabaseConsumer.Data
{
    public class ShopDBContext : DbContext
    {
        public ShopDBContext(DbContextOptions<ShopDBContext> options) : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
    }
}
