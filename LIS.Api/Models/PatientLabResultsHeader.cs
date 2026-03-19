using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("PatientLabResultsHeader")]
    public class PatientLabResultsHeader
    {
        [Key]
        public int ID { get; set; }

        public int CaseNumber { get; set; }

        public int PatientID { get; set; }

        public int MRN { get; set; }

        public int AdmissionNB { get; set; }

        [MaxLength(20)]
        public string? AdmissionNumber { get; set; }

        public int? ResultNB { get; set; }

        public DateTime? RequestDate { get; set; }

        public bool IsApproved { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedDate { get; set; }

        [MaxLength(255)]
        public string? Reason { get; set; }

        [MaxLength(50)]
        public string? Room { get; set; }

        [MaxLength(50)]
        public string? Floor { get; set; }

        [MaxLength(50)]
        public string? Bed { get; set; }

        [MaxLength(1)]
        public string? Gender { get; set; }

        public int? Age { get; set; }

        [MaxLength(50)]
        public string? Department { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsDeleted { get; set; }

        public string? Comment { get; set; }

        public int? PrintedStatus { get; set; }

        public bool? ReportedDelivered { get; set; }

        // Navigation property
        public virtual ICollection<PatientLabResult>? PatientLabResults { get; set; }
    }
}

