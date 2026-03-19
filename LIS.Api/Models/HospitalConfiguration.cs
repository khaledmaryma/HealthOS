using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("HospitalConfiguration", Schema = "dbo")]
    public class HospitalConfiguration
    {
        [Key]
        public int ID { get; set; }

        [MaxLength(300)]
        public string? HospitalName { get; set; }

        [MaxLength(50)]
        public string? HospitalClassification { get; set; }

        [MaxLength(50)]
        public string? MOHAgreementNumber { get; set; }

        public DateTime? MOHAgreementDate { get; set; }

        [MaxLength(50)]
        public string? InventoryDBNameCurrent { get; set; }

        [MaxLength(50)]
        public string? InventoryDBNamePrevious { get; set; }

        [MaxLength(50)]
        public string? CNSSAgreementNumber { get; set; }

        public string? HospitalAddress { get; set; } // nvarchar(max)

        [MaxLength(50)]
        public string? HospitalPhone { get; set; }

        [MaxLength(50)]
        public string? HospitalFax { get; set; }

        [MaxLength(300)]
        public string? HospitalNameArabic { get; set; }

        [MaxLength(10)]
        public string? HospitalNameAbreviation { get; set; }

        public string? HospitalAddressArabic { get; set; } // nvarchar(max)

        [Column(TypeName = "image")]
        public byte[]? HospitalLogo { get; set; } // IMAGE type

        [MaxLength(300)]
        public string? CNSSHospitalNameArabic { get; set; }

        public bool? RequireOTPOrder { get; set; }

        public bool? RequireOTPAdmCancel { get; set; }

        public string? InvoiceFooter1 { get; set; } // nvarchar(max)

        public string? InvoiceFooter2 { get; set; } // nvarchar(max)

        public string? AccountingFooter1 { get; set; } // nvarchar(max)

        public string? AccountingFooter2 { get; set; } // nvarchar(max)

        public string? MedicalFooter1 { get; set; } // nvarchar(max)

        public string? MedicalFooter2 { get; set; } // nvarchar(max)
    }
}

