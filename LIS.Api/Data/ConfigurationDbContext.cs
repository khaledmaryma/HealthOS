using Microsoft.EntityFrameworkCore;
using LIS.Api.Models;

namespace LIS.Api.Data
{
    public class ConfigurationDbContext : DbContext
    {
        public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options) : base(options) { }

        public DbSet<HospitalConfiguration> HospitalConfigurations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // HospitalConfiguration is already configured in the model with [Table("HospitalConfiguration", Schema = "dbo")]
        }
    }
}

