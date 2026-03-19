using System.ComponentModel.DataAnnotations;

namespace LIS.Api.Models
{
    public class DeliveryHeader
    {
        [Key]
        public int Id { get; set; }
        public int Type { get; set; }
        public int TypeCounter { get; set; }
        public int PatientType { get; set; }
        public DateTime Date { get; set; }
        public DateTime? PrescriptionDate { get; set; }
        public string? ReferenceNb { get; set; }
        public int Patient { get; set; }
        public int? Insurance { get; set; }
        public int? Doctor { get; set; }
        public int Currency { get; set; }
        public int Warehouse { get; set; }
        public string? Comment { get; set; }
        public decimal? Gross { get; set; }
        public decimal? Discount { get; set; }
        public decimal? Vat { get; set; }
        public decimal? Round { get; set; }
        public decimal? Net { get; set; }
        public int Admission { get; set; }
        public int? DrugInvoiceNumber { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
        public Guid? GUID { get; set; }
        public int? MedicalUnitId { get; set; }
    }
}
