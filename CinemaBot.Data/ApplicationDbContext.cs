using CinemaBot.Data.Entites;
using Microsoft.EntityFrameworkCore;

namespace CinemaBot.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }

        public DbSet<Url> Urls { get; set; }
        
        /*protected override void OnModelCreating(ModelBuilder builder)
        {
            UpdateStructure(builder);
            base.OnModelCreating(builder);
        }*/

        private void UpdateStructure(ModelBuilder builder)
        {
            builder.Entity<Url>()
                .HasKey(b => new {b.Id});
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=cinemadb;Username=postgres;Password=root");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Url>();
        }
    }
}
