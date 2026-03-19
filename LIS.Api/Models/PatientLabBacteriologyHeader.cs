using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("PatientLabBacteriologyHeader")]
    public class PatientLabBacteriologyHeader
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("PatientLabResultID")]
        public int PatientLabResultId { get; set; }

        [Column("GermsID")]
        public int? GermsId { get; set; }

        [Column("Germ")]
        public string? Germ { get; set; }

        [Column("DateTime")]
        public DateTime? DateTime { get; set; }

        [Column("PrelevementID")]
        public int? PrelevementId { get; set; }

        [Column("Prelevement")]
        public string? Prelevement { get; set; }

        [Column("BacterieID")]
        public int? BacterieId { get; set; }

        [Column("Bacteria")]
        public string? Bacteria { get; set; }

        [Column("Number")]
        public string? Number { get; set; }

        [Column("Comments")]
        public string? Comments { get; set; }

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

        // Navigation property to PatientLabResult
        [ForeignKey("PatientLabResultId")]
        public virtual PatientLabResult? PatientLabResult { get; set; }
    }
}






