using System.ComponentModel.DataAnnotations;

namespace LIS.Api.Models
{
    public class BillingInvoiceHeader
    {
        [Key]
        public int Id { get; set; }
        public int SequenceNumber { get; set; }
        public string Type { get; set; } = string.Empty;
        public int CounterTypeId { get; set; }
        public int Counter { get; set; }
        public DateTime Date { get; set; }
        public int Admission { get; set; }
        public string? Comment { get; set; }
        public decimal HospitalAmount { get; set; }
        public decimal PhysicianAmount { get; set; }
        public decimal MedicamentAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal DeniedAmount { get; set; }
        public decimal LumpSum { get; set; }
        public int AccountId { get; set; }
        public string AccountDescription { get; set; } = string.Empty;
        public int? ComplementaryAccountId { get; set; }
        public string? ComplementaryAccountDescription { get; set; }
        public int CurrencyId { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal ExchangeRate { get; set; }
        public int CheckInClassId { get; set; }
        public string CheckInClass { get; set; } = string.Empty;
        public int CoverageClassId { get; set; }
        public string CoverageClass { get; set; } = string.Empty;
        public decimal CoverageRate { get; set; }
        public int ReferralPhysicianId { get; set; }
        public string ReferralPhysician { get; set; } = string.Empty;
        public int? AttendingPhysicianId { get; set; }
        public string? AttendingPhysician { get; set; }
        public string? Reference { get; set; }
        public int? MainInvoice { get; set; }
        public int? OldMainInvoice { get; set; }
        public decimal Net { get; set; }
        public decimal Gross { get; set; }
        public decimal NetGross { get; set; }
        public decimal Complementary { get; set; }
        public decimal ComplementaryOtherCurrency { get; set; }
        public decimal ComplementaryDifferenceOtherCurrency { get; set; }
        public string MRN { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; }
        public int DepartmentId { get; set; }
        public string Department { get; set; } = string.Empty;
        public DateTime? DischargeDate { get; set; }
        public int ContextPriceId { get; set; }
        public string ContextPrice { get; set; } = string.Empty;
        public string? ReceiptNumber { get; set; }
        public decimal? ReceiptAmount { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public decimal? RoundedAmount { get; set; }
        public int Insurance { get; set; }
        public int AdmissionInsuranceCoverage { get; set; }
        public int IsDRG { get; set; }
        public int? CollectionScheduleId { get; set; }
        public string? CollectionScheduleNumber { get; set; }
        public DateTime? CollectionScheduleDate { get; set; }
        public decimal? ReceivedLBP { get; set; }
        public decimal? ReceivedUSD { get; set; }
        public decimal? Difference { get; set; }
        public DateTime? ReceivingDate { get; set; }
        public string? VoucherNumber { get; set; }
        public int SplitedInvoice { get; set; }
        public string? PrimaryDischargeDiagnostic { get; set; }
        public string? SecondaryDischargeDiagnostic { get; set; }
        public int RequireRegenerate { get; set; }
        public int? LockedBy { get; set; }
        public string? LockedByName { get; set; }
        public DateTime? LockedDate { get; set; }
        public int? AlternateInvoiceId { get; set; }
        public decimal GlobalDiscount { get; set; }
        public decimal? DifferenceAdjust { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int IsDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? Group { get; set; }
        public string? AgreementNumber { get; set; }
        public int? ComplementaryDifferenceCalculationState { get; set; }
        public int IsReversed { get; set; }
        public int? CreditNoteNumber { get; set; }
        public DateTime? CreditNoteDate { get; set; }
        public decimal? CreditNotePaidAmount { get; set; }
        public decimal? CreditNoteDiscount { get; set; }
        public string? EmployeeAccount { get; set; }
        public int? IsEmployee { get; set; }
        public int? Status { get; set; }
        public int? ContextEnumerationId { get; set; }
        public int? IsDirty { get; set; }
        public int? IsFromScratch { get; set; }
        public decimal? AgreementCreditAmount { get; set; }
        public int? IsSelected { get; set; }
        public decimal? CreditNoteAssignedAmount { get; set; }
        public string? CreditNoteVoucherNumber { get; set; }
        public decimal? PrepaymentAmount { get; set; }
        public DateTime? PrepaymentDate { get; set; }
        public int? PrepaymentNumber { get; set; }
        public string? DiagnosticGroup1 { get; set; }
        public string? DiagnosticGroup2 { get; set; }
        public string? DiagnosticGroup3 { get; set; }
        public int? DiagnosticGroupId1 { get; set; }
        public int? DiagnosticGroupId2 { get; set; }
        public int? DiagnosticGroupId3 { get; set; }
    }
}