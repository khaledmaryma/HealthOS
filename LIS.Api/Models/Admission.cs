using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("Admission", Schema = "dbo")]
    public class Admission
    {
        [Key]
        public int ID { get; set; }

        [MaxLength(20)]
        public string? Number { get; set; }

        public int? AdmissionSite { get; set; }

        public int? ReferralPhysician { get; set; }

        public int? AttendingPhysician { get; set; }

        public int? MainInsurance { get; set; }

        public int? MainInsuranceClass { get; set; }

        public int? Insured { get; set; }

        public int? AuxiliaryInsurance { get; set; }

        public int? AuxiliaryInsuranceClass { get; set; }

        public int? CheckInClass { get; set; }

        [MaxLength(50)]
        public string? Department { get; set; }

        public DateTime? CheckInDate { get; set; }

        public DateTime? CheckOutDate { get; set; }

        public int? Patient { get; set; }

        public int? Type { get; set; }

        public int? IsWorkAccident { get; set; }

        public int? IsExtended { get; set; }

        public int? Group { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}















