using System.ComponentModel.DataAnnotations;

namespace LIS.Api.Models
{
    public class BillingInvoiceDetail
    {
        [Key]
        public int Id { get; set; }
        public DateTime? PrescriptionDate { get; set; }
        public int? PrescribedBy { get; set; }
        public int MedicationUnit { get; set; }
        public string MedicationUnitDescription { get; set; } = string.Empty;
        public int Admission { get; set; }
        public int Patient { get; set; }
        public int Denomination { get; set; }
        public string DenominationCode { get; set; } = string.Empty;
        public string DenominationDescription { get; set; } = string.Empty;
        public string DenominationCoeffCode { get; set; } = string.Empty;
        public decimal DenominationCoeffValue { get; set; }
        public decimal DenominationCoeffPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal NetPrice { get; set; }
        public decimal NetUnitPrice { get; set; }
        public decimal DifferenceAmount { get; set; }
        public decimal DeniedAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal LumpSum { get; set; }
        public decimal ComplementaryAmount { get; set; }
        public decimal ComplementaryAmountOtherCurrency { get; set; }
        public decimal ComplementaryDifferenceOtherCurrency { get; set; }
        public int OperatingPhysician { get; set; }
        public int? IsMedicalResultOk { get; set; }
        public DateTime? MedicalResultDate { get; set; }
        public int RequireApproval { get; set; }
        public string? ApprovalReference { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public int IsDenied { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ExecutionDate { get; set; }
        public int InvoiceHeader { get; set; }
        public int ReferralPhysician { get; set; }
        public int CostCenter { get; set; }
        public int ProfitCenter { get; set; }
        public int? PacIndex { get; set; }
        public int? PreInvoiceDetail { get; set; }
        public DateTime DetailDate { get; set; }
        public int? MainDetailId { get; set; }
        public int CopyFlag { get; set; }
        public DateTime? DetailDateHelper { get; set; }
        public int IsDoubtfull { get; set; }
        public string? Procedure { get; set; }
        public int IsDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? PreviousDetailId { get; set; }
        public int OrderDetailSequenceNumber { get; set; }
        public string? Source { get; set; }
        public int IsCanceled { get; set; }
        public string? CancelComment { get; set; }
        public int? OldOrderDetailSequenceNumber { get; set; }
        public int? IsApproved { get; set; }
        public int? InvoiceNumber { get; set; }
        public decimal? PatientAmount { get; set; }
    }
}