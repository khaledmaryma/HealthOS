using Microsoft.EntityFrameworkCore;
using LIS.Api.Models;

namespace LIS.Api.Data
{
    public class HospitalDefinitionDbContext : DbContext
    {
        public HospitalDefinitionDbContext(DbContextOptions<HospitalDefinitionDbContext> options) : base(options) { }

        public DbSet<HospitalDenomination> Denominations { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure HospitalDenomination entity
            modelBuilder.Entity<HospitalDenomination>(entity =>
            {
                entity.ToTable("Denomination", "dbo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.SmallDescription).HasColumnName("SmallDescription");
                entity.Property(e => e.LongDescription).HasColumnName("LongDescription");
                entity.Property(e => e.Code).HasColumnName("Code");
                entity.Property(e => e.Abreviation).HasColumnName("Abreviation");
                entity.Property(e => e.HasOperatingPhysician).HasColumnName("HasOperatingPhysician").IsRequired(false);
                entity.Property(e => e.HasAnesthesiaPhysician).HasColumnName("HasAnesthesiaPhysician").IsRequired(false);
                entity.Property(e => e.HasOperatingRoom).HasColumnName("HasOperatingRoom").IsRequired(false);
                entity.Property(e => e.IsHonoraryExcluded).HasColumnName("IsHonoraryExcluded").IsRequired(false);
                entity.Property(e => e.IsResidenceRelated).HasColumnName("IsResidenceRelated").IsRequired(false);
                entity.Property(e => e.HasMedicalResult).HasColumnName("HasMedicalResult").IsRequired(false);
                entity.Property(e => e.App).HasColumnName("App").IsRequired(false);
                entity.Property(e => e.OperatingRoom).HasColumnName("OperatingRoom");
                entity.Property(e => e.CoefficientCode).HasColumnName("CoefficientCode").IsRequired(false);
                entity.Property(e => e.CoefficientValue).HasColumnName("CoefficientValue").IsRequired(false);
                entity.Property(e => e.CashPriceUsd).HasColumnName("CashPriceUsd").IsRequired(false);
                entity.Property(e => e.CashPriceLlbp).HasColumnName("CashPriceLlbp").IsRequired(false);
                entity.Property(e => e.Status).HasColumnName("Status").IsRequired(false);
                entity.Property(e => e.DisplayOrder).HasColumnName("DisplayOrder").IsRequired(false);
                entity.Property(e => e.CostCenter).HasColumnName("CostCenter").IsRequired(false);
                entity.Property(e => e.ExpectedResidenceDays).HasColumnName("ExpectedResidenceDays").IsRequired(false);
                entity.Property(e => e.IsSubItem).HasColumnName("IsSubItem").IsRequired(false);
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").IsRequired(false);
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy").IsRequired(false);
                entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy");
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
                entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
                entity.Property(e => e.StartDate).HasColumnName("StartDate");
                entity.Property(e => e.StartDateLabel).HasColumnName("StartDateLabel");
                entity.Property(e => e.EndDate).HasColumnName("EndDate");
                entity.Property(e => e.EndDateLabel).HasColumnName("EndDateLabel");
                entity.Property(e => e.IsSelectedOrNot).HasColumnName("IsSelectedOrNot").IsRequired(false);
                entity.Property(e => e.SeverityId).HasColumnName("SeverityID").IsRequired(false);
                entity.Property(e => e.StatusId).HasColumnName("StatusID").IsRequired(false);
                entity.Property(e => e.Comments).HasColumnName("Comments").IsRequired(false);
                entity.Property(e => e.InCrAppCode).HasColumnName("InCrAppCode").IsRequired(false);
                entity.Property(e => e.InCaAppCode).HasColumnName("InCaAppCode").IsRequired(false);
                entity.Property(e => e.OutCrAppCode).HasColumnName("OutCrAppCode").IsRequired(false);
                entity.Property(e => e.OutCaAppCode).HasColumnName("OutCaAppCode").IsRequired(false);
                entity.Property(e => e.DenominationDefaultTime).HasColumnName("DenominationDefaultTime").IsRequired(false);
                entity.Property(e => e.Rate).HasColumnName("Rate").IsRequired(false);
                entity.Property(e => e.HasVideo).HasColumnName("HasVideo").IsRequired(false);
                entity.Property(e => e.IsOpenHeart).HasColumnName("IsOpenHeart").IsRequired(false);
                entity.Property(e => e.IsReferralShare).HasColumnName("IsReferralShare");
                entity.Property(e => e.ReferralAmount).HasColumnName("ReferralAmount");
                entity.Property(e => e.DenominationGroupId).HasColumnName("DenominationGroupID");
                entity.Property(e => e.IsClassRelated).HasColumnName("IsClassRelated");
                entity.Property(e => e.CreditDiscount).HasColumnName("CreditDiscount");
                entity.Property(e => e.CashDiscount).HasColumnName("CashDiscount");
                entity.Property(e => e.IsPrintable).HasColumnName("IsPrintable").IsRequired(false);
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate").IsRequired(false);
                entity.Property(e => e.SmallDescription).HasColumnName("SmallDescription").IsRequired(false);
                entity.Property(e => e.LongDescription).HasColumnName("LongDescription").IsRequired(false);
                entity.Property(e => e.Code).HasColumnName("Code").IsRequired(false);
            });

            // Configure Department entity
            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("Departments", "dbo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.DepartmentName).HasColumnName("DepartmentName").IsRequired(false);
                entity.Property(e => e.Code).HasColumnName("Code").IsRequired(false);
                entity.Property(e => e.IsActive).HasColumnName("IsActive").IsRequired(false);
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").IsRequired(false);
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy").IsRequired(false);
                entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy").IsRequired(false);
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate").IsRequired(false);
                entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate").IsRequired(false);
            });
        }
    }
}

