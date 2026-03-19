using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("PatientLabSub")]
    public class PatientLabSub
    {
        [Key]
        public int ID { get; set; }

        public int PatientLabTestID { get; set; }

        public string LabTestDescription { get; set; } = string.Empty;

        public int LabTestSubID { get; set; }

        public string LabTestSubDescription { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Paragraph { get; set; }

        [MaxLength(25)]
        public string? Min { get; set; }

        [MaxLength(25)]
        public string? Max { get; set; }

        public int UOM { get; set; }

        [MaxLength(50)]
        public string UOMDescription { get; set; } = string.Empty;

        public string? Result { get; set; }

        public string? Comments { get; set; }

        [MaxLength(150)]
        public string? DisplayOrder { get; set; }

        [MaxLength(50)]
        public string? Percentage { get; set; }

        public string? LastResult { get; set; }

        public DateTime? LastResultDate { get; set; }

        public int? StatusID { get; set; }

        [MaxLength(10)]
        public string? Prefix { get; set; }

        [MaxLength(10)]
        public string? Suffix { get; set; }

        [MaxLength(25)]
        public string? ErrorMin { get; set; }

        [MaxLength(25)]
        public string? ErrorMax { get; set; }

        public string? Ref_Range { get; set; }

        public decimal? LowPanicIndex { get; set; }

        public decimal? HighPanicIndex { get; set; }

        public bool IsPanic { get; set; }

        public bool IsNotified { get; set; }

        public DateTime? PanicDate { get; set; }

        public string? PanicComment { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation property
        [ForeignKey("PatientLabTestID")]
        public virtual PatientLabResult? PatientLabResult { get; set; }
    }
}

