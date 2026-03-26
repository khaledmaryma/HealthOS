using LIS.Api.Models.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Data
{
    public class HISUsersDbContext : DbContext
    {
        public HISUsersDbContext(DbContextOptions<HISUsersDbContext> options) : base(options) { }

        public DbSet<UserDefinition> UserDefinitions { get; set; } = null!;
        public DbSet<ProfileDefinition> ProfileDefinitions { get; set; } = null!;
        public DbSet<PermissionDefinition> PermissionDefinitions { get; set; } = null!;
        public DbSet<ProfilePermission> ProfilePermissions { get; set; } = null!;
        public DbSet<AppDefinition> AppDefinitions { get; set; } = null!;
        public DbSet<ScreenDefinition> ScreenDefinitions { get; set; } = null!;
        public DbSet<UserKpiDefinition> UserKpiDefinitions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PermissionDefinition>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<ProfilePermission>()
                .HasOne(pp => pp.Permission)
                .WithMany(p => p.ProfilePermissions)
                .HasForeignKey(pp => pp.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProfilePermission>()
                .HasOne(pp => pp.Profile)
                .WithMany()
                .HasForeignKey(pp => pp.ProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ScreenDefinition>()
                .HasOne(s => s.Application)
                .WithMany(a => a.Screens)
                .HasForeignKey(s => s.AppId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PermissionDefinition>()
                .HasOne(p => p.Application)
                .WithMany(a => a.Permissions)
                .HasForeignKey(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PermissionDefinition>()
                .HasOne(p => p.Screen)
                .WithMany(s => s.Permissions)
                .HasForeignKey(p => p.ScreenId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserKpiDefinition>()
                .HasIndex(e => new { e.UserId, e.AppKey, e.HomePageId })
                .HasDatabaseName("IX_UserKpi_User_App_Home");
        }
    }
}
