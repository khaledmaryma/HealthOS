namespace LIS.Api.Models
{
    public class InvoiceDetail
    {
        public int Id { get; set; }
        public int InvoiceHeaderId { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int? ServiceId { get; set; }
        public int? LabTestId { get; set; }
        public int? DenominationId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public int? Sequence { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateInvoiceDetailRequest
    {
        public int InvoiceHeaderId { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int? ServiceId { get; set; }
        public int? LabTestId { get; set; }
        public int? DenominationId { get; set; }
        public int? Sequence { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateInvoiceDetailRequest
    {
        public int Id { get; set; }
        public string? ItemDescription { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? ServiceId { get; set; }
        public int? LabTestId { get; set; }
        public int? DenominationId { get; set; }
        public int? Sequence { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public string? Notes { get; set; }
    }

    public class InvoiceTotals
    {
        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }
}














