using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Data
{
    public class LISDbContext : DbContext
    {
        public LISDbContext(DbContextOptions<LISDbContext> options) : base(options) { }

        public DbSet<LIS.Api.Models.Patient> Patients { get; set; } = null!;
        public DbSet<LIS.Api.Models.LabTest> LabTests { get; set; } = null!;
        public DbSet<LIS.Api.Models.ResultType> ResultTypes { get; set; } = null!;
        public DbSet<LIS.Api.Models.UnitOfMeasure> UnitOfMeasures { get; set; } = null!;
        public DbSet<LIS.Api.Models.LabTestAge> LabTestAges { get; set; } = null!;
        public DbSet<LIS.Api.Models.LabTestGyneco> LabTestGynecos { get; set; } = null!;
        public DbSet<LIS.Api.Models.LabTestSub> LabTestSubs { get; set; } = null!;
        public DbSet<LIS.Api.Models.ResidentPatient> ResidentPatients { get; set; } = null!;
        public DbSet<LIS.Api.Models.PatientLabResultsHeader> PatientLabResultsHeaders { get; set; } = null!;
        public DbSet<LIS.Api.Models.PatientLabResult> PatientLabResults { get; set; } = null!;
        public DbSet<LIS.Api.Models.PatientLabSub> PatientLabSubs { get; set; } = null!;

        // EMR entities (queried via cross-database SQL)
        public DbSet<LIS.Api.Models.Bilan> Bilans { get; set; } = null!;
        public DbSet<LIS.Api.Models.BilanDetail> BilanDetails { get; set; } = null!;
        
        // Bacteriology entities
        public DbSet<LIS.Api.Models.Germs> Germs { get; set; } = null!;
        public DbSet<LIS.Api.Models.GermAntibiotic> GermAntibiotics { get; set; } = null!;
        public DbSet<LIS.Api.Models.Bacteria> Bacterias { get; set; } = null!;
        public DbSet<LIS.Api.Models.Antibiotic> Antibiotics { get; set; } = null!;
        public DbSet<LIS.Api.Models.AntibFamily> AntibFamilies { get; set; } = null!;
        public DbSet<LIS.Api.Models.PatientLabBacteriologyHeader> PatientLabBacteriologyHeaders { get; set; } = null!;
        public DbSet<LIS.Api.Models.PatientLabBacteriology> PatientLabBacteriologies { get; set; } = null!;
        
        // Configuration entities
        public DbSet<LIS.Api.Models.HospitalConfiguration> HospitalConfigurations { get; set; } = null!;
        public DbSet<LIS.Api.Models.Denomination> Denominations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure UnitOfMeasure - queried from EMR database via cross-database SQL
            // The table configuration is handled in the controller via FromSqlRaw

            // Configure ResidentPatient - queried from Admission database via cross-database SQL
            // The table configuration is handled in the controller via FromSqlRaw

            // Configure HospitalConfiguration - queried from Configuration database via cross-database SQL
            // The table configuration is handled in the controller via FromSqlRaw

            // Configure Denomination - queried from HospitalDefinition database via cross-database SQL
            // The table configuration is handled in the controller via FromSqlRaw

            // Configure LabTest -> ResultType relationship
            modelBuilder.Entity<LIS.Api.Models.LabTest>()
                .HasOne(lt => lt.ResultTypeNavigation)
                .WithMany(rt => rt.LabTests)
                .HasForeignKey(lt => lt.ResultType)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PatientLabResultsHeader -> PatientLabResult relationship
            modelBuilder.Entity<LIS.Api.Models.PatientLabResult>()
                .HasOne(plr => plr.PatientLabResultsHeader)
                .WithMany(plrh => plrh.PatientLabResults)
                .HasForeignKey(plr => plr.PatientHeaderID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PatientLabResult to handle database triggers
            // The table has triggers, so we need to disable the OUTPUT clause
            modelBuilder.Entity<LIS.Api.Models.PatientLabResult>()
                .ToTable(tb => tb.HasTrigger("TR_PatientLabResult"));

            // Configure PatientLabResult -> PatientLabSub relationship
            modelBuilder.Entity<LIS.Api.Models.PatientLabSub>()
                .HasOne(pls => pls.PatientLabResult)
                .WithMany(plr => plr.PatientLabSubs)
                .HasForeignKey(pls => pls.PatientLabTestID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PatientLabSub to handle database triggers
            // The table might have triggers, so we need to disable the OUTPUT clause
            modelBuilder.Entity<LIS.Api.Models.PatientLabSub>()
                .ToTable(tb => tb.HasTrigger("TR_PatientLabSub"));

            // Configure Bacteriology relationships
            
            // Configure GermAntibiotic -> Germs relationship
            modelBuilder.Entity<LIS.Api.Models.GermAntibiotic>()
                .HasOne(ga => ga.Germ)
                .WithMany()
                .HasForeignKey(ga => ga.GermId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure GermAntibiotic -> Antibiotic relationship
            modelBuilder.Entity<LIS.Api.Models.GermAntibiotic>()
                .HasOne(ga => ga.Antibiotic)
                .WithMany()
                .HasForeignKey(ga => ga.AntibioticId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PatientLabBacteriology -> PatientLabBacteriologyHeader relationship
            modelBuilder.Entity<LIS.Api.Models.PatientLabBacteriology>()
                .HasOne(plb => plb.BacteriologyHeader)
                .WithMany()
                .HasForeignKey(plb => plb.PatientHeader)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PatientLabBacteriology -> Antibiotic relationship
            modelBuilder.Entity<LIS.Api.Models.PatientLabBacteriology>()
                .HasOne(plb => plb.Antibiotic)
                .WithMany()
                .HasForeignKey(plb => plb.AntibioticId)
                .OnDelete(DeleteBehavior.Restrict);

            // Note: UnitOfMeasure and ResidentPatient relationships are not enforced by foreign key 
            // because they are in different databases (EMR and Admission respectively)
        }
    }
}
