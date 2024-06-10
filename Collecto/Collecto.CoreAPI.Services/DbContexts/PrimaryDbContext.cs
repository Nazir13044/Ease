using Microsoft.EntityFrameworkCore;

namespace Collecto.CoreAPI.Services.DbContexts
{
    public class PrimaryDbContext : DbContext
    {
        public PrimaryDbContext(DbContextOptions<PrimaryDbContext> options) : base(options)
        {
        }
        //public DbSet<DivisionDbSet> Divisions { get; set; }

        /*
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          modelBuilder.Entity<Category>().HasData(
              new Category
              {
                Id = 1,
                Name = "Electronics",
                Description = "Electronic Items",
              },
              new Category
              {
                Id = 2,
                Name = "Clothes",
                Description = "Dresses",
              },
              new Category
              {
                Id = 3,
                Name = "Grocery",
                Description = "Grocery Items",
              }
          );
        }
        */

    }
}
