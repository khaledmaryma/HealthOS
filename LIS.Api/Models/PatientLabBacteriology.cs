using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("PatientLabBacteriology")]
    public class PatientLabBacteriology
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("PatientHeader")]
        public int PatientHeader { get; set; }

        [Column("Code")]
        [StringLength(255)]
        public string? Code { get; set; }

        [Column("AntibioticID")]
        public int? AntibioticId { get; set; }

        [Column("AntibioticDescription")]
        [StringLength(255)]
        public string? AntibioticDescription { get; set; }

        [Column("DateTime")]
        public DateTime? DateTime { get; set; }

        [Column("Resistant")]
        public bool Resistant { get; set; }

        [Column("Intermediat")]
        public bool Intermediat { get; set; }

        [Column("Sensible")]
        public bool Sensible { get; set; }

        [Column("Charge")]
        [StringLength(255)]
        public string Charge { get; set; } = string.Empty;

        [Column("Diameter")]
        [StringLength(255)]
        public string Diameter { get; set; } = string.Empty;

        [Column("DisplayOrder")]
        [StringLength(255)]
        public string? DisplayOrder { get; set; }

        [Column("CreatedBy")]
        public int CreatedBy { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("ModifiedBy")]
        public int? ModifiedBy { get; set; }

        [Column("ModifiedDate")]
        public DateTime? ModifiedDate { get; set; }

        [Column("IsDeleted")]
        public bool IsDeleted { get; set; }

        // Navigation property to PatientLabBacteriologyHeader
        [ForeignKey("PatientHeader")]
        public virtual PatientLabBacteriologyHeader? BacteriologyHeader { get; set; }

        // Navigation property to Antibiotic
        [ForeignKey("AntibioticId")]
        public virtual Antibiotic? Antibiotic { get; set; }
    }
}






