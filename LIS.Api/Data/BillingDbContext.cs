using Microsoft.EntityFrameworkCore;
using LIS.Api.Models;

namespace LIS.Api.Data
{
    public class BillingDbContext : DbContext
    {
        public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

        // Note: Add billing entities here when models are created
        // public DbSet<BillingInvoiceHeader> InvoiceHeaders { get; set; } = null!;
        // public DbSet<BillingInvoiceDetail> InvoiceDetails { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure billing entities here
        }
    }
}

