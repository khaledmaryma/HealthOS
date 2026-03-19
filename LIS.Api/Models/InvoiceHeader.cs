namespace LIS.Api.Models
{
    public class InvoiceHeader
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? PatientId { get; set; }
        public string? MRN { get; set; }
        public string? AdmissionNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? BalanceAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public string? Notes { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
    }

    public class CreateInvoiceHeaderRequest
    {
        public int? PatientId { get; set; }
        public string? MRN { get; set; }
        public string? AdmissionNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? Notes { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class UpdateInvoiceHeaderRequest
    {
        public int Id { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? BalanceAmount { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
    }
}














