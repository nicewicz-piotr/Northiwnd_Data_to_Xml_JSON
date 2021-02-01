using Microsoft.EntityFrameworkCore;

namespace CS7
{
    public class Northwind : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                @"Data Source=(localdb)\v11.0;" + 
                "Initial Catalog=Northwind;" + 
                "Integrated Security=true;" + 
                "MultipleActiveResultSets=true;"
            );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().Property(category => category.CategoryName).IsRequired().HasMaxLength(40);
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.Discontinued);
        }
    }
}