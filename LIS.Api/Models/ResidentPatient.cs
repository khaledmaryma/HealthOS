using System;

namespace LIS.Api.Models
{
    public class ResidentPatient
    {
        public int ID { get; set; }
        public int PatientID { get; set; }
        public int Admission { get; set; }
        public int MRN { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? ArabicFullName { get; set; }
        public string MedicalRecordNumber { get; set; } = string.Empty;
        public DateTime PatientDOB { get; set; }
        public int? Age { get; set; }
        public string PatientGender { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public short CheckInClassID { get; set; }
        public string CheckInClassDescription { get; set; } = string.Empty;
        public int MainInsuranceID { get; set; }
        public string MainInsuranceDescription { get; set; } = string.Empty;
        public int MainInsuranceClassID { get; set; }
        public string MainInsuranceClassDescription { get; set; } = string.Empty;
        public int ReferralPhysicianID { get; set; }
        public string ReferralPhysicianName { get; set; } = string.Empty;
        public int? AttendingPhysicianID { get; set; }
        public string? AttendingPhysicianName { get; set; }
        public int MedicationUnitID { get; set; }
        public string MedicationUnitDescription { get; set; } = string.Empty;
        public int? RoomID { get; set; }
        public string? RoomDescription { get; set; }
        public int? BedID { get; set; }
        public string? BedDescription { get; set; }
        public int? FloorID { get; set; }
        public string? FloorDescription { get; set; }
        public int InsuranceID { get; set; }
        public string InsuranceDescription { get; set; } = string.Empty;
        public int GuarantorID { get; set; }
        public string GuarantorDescription { get; set; } = string.Empty;
        public int CurrencyID { get; set; }
        public string CurrencyDescription { get; set; } = string.Empty;
        public short ClassID { get; set; }
        public string ClassDescription { get; set; } = string.Empty;
        public int ContextPriceID { get; set; }
        public string ContextPriceDescription { get; set; } = string.Empty;
        public int ContextEnumerationID { get; set; }
        public string ContextEnumerationDescription { get; set; } = string.Empty;
        public short AdmissionType { get; set; }
        public string AdmissionTypeDescription { get; set; } = string.Empty;
        public string? Contact { get; set; }
        public string? InsuredName { get; set; }
        public string? InsuredNameArabic { get; set; }
        public string? InsuredPhone { get; set; }
        public int? AuxiliaryInsuranceID { get; set; }
        public string? AuxiliaryInsuranceDescription { get; set; }
        public int? AuxiliaryInsuranceClassID { get; set; }
        public string? AuxiliaryInsuranceClassDescription { get; set; }
        public bool IsDischarged { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string? Comment { get; set; }
        public decimal? TotalAdvanceLBP { get; set; }
        public decimal? TotalAdvanceUSD { get; set; }
        public string? Diagnostic { get; set; }
        public string? VisaNumber { get; set; }
        public decimal? TotalUncollectedAdvanceLBP { get; set; }
        public decimal? TotalUncollectedAdvanceUSD { get; set; }
        public decimal? InvoiceGrossAmountLBP { get; set; }
        public decimal? InvoiceGrossAmountUSD { get; set; }
        public string? MainInvoiceNumber { get; set; }
        public bool IsPharmDisch { get; set; }
        public DateTime? PharmDischDate { get; set; }
        public bool IsDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? AdmissionSite { get; set; }
        public bool IsNersingDischarge { get; set; }
        public string? NersingDischargeComment { get; set; }
        public int? OldBedID { get; set; }
        public int? Group { get; set; }
        public string? PatientShortName { get; set; }
        public string? PatientFormattedName { get; set; }
        public byte? Status { get; set; }
        public bool IsRecheckIn { get; set; }
        public bool? HasInvoices { get; set; }
        public bool? RequireRegenerate { get; set; }
        public string? DiagnosticGroup1 { get; set; }
        public string? DiagnosticGroup2 { get; set; }
        public string? DiagnosticGroup3 { get; set; }
        public decimal? InvTotal { get; set; }
        public string? ReceiptNumber { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public string? Currency { get; set; }
        public string? AdvReceiptNumber { get; set; }
        public DateTime? AdvReceiptDate { get; set; }
        public decimal? AdvanceAmount { get; set; }
    }
}
