using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("PatientLabResult")]
    public class PatientLabResult
    {
        [Key]
        public int ID { get; set; }

        public int PatientHeaderID { get; set; }

        public int? LabTestID { get; set; }

        public string LabTestDescription { get; set; } = string.Empty;

        public int MedicalClass { get; set; }

        public string MedicalClassDesc { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Paragraph { get; set; }

        [MaxLength(25)]
        public string? Min { get; set; }

        [MaxLength(25)]
        public string? Max { get; set; }

        [MaxLength(10)]
        public string? Prefix { get; set; }

        [MaxLength(10)]
        public string? Suffix { get; set; }

        [MaxLength(25)]
        public string? ErrorMin { get; set; }

        [MaxLength(25)]
        public string? ErrorMax { get; set; }

        public int? UOM { get; set; }

        [MaxLength(50)]
        public string? UOMDescription { get; set; }

        public string? Result { get; set; }

        public string? Last { get; set; }

        public DateTime? LastResultDate { get; set; }

        public string? DefaultTextResult { get; set; }

        public string? Comments { get; set; }

        [MaxLength(150)]
        public string? DisplayOrder { get; set; }

        public int? StatusID { get; set; }

        public bool IsResultok { get; set; }

        public Guid? GUID { get; set; }

        public DateTime? ResultDate { get; set; }

        public string? Ref_Range { get; set; }

        public int? TempHelperID { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsDeleted { get; set; }

        public int? PreInvoiceDetailID { get; set; }

        public int? PreInvoiceDetailSequence { get; set; }

        public decimal? LowPanicIndex { get; set; }

        public decimal? HighPanicIndex { get; set; }

        public bool IsPanic { get; set; }

        public bool IsNotified { get; set; }

        public DateTime? PanicDate { get; set; }

        public string? PanicComment { get; set; }

        public bool? Printed { get; set; }

        // Navigation property
        [ForeignKey("PatientHeaderID")]
        public virtual PatientLabResultsHeader? PatientLabResultsHeader { get; set; }

        // Navigation property
        public virtual ICollection<PatientLabSub>? PatientLabSubs { get; set; }
    }
}

