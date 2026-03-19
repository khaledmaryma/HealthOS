using System.ComponentModel.DataAnnotations;

namespace LIS.Api.Models
{
    public class HospitalDenomination
    {
        [Key]
        public int Id { get; set; }
        public string? SmallDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? Code { get; set; }
        public string? Abreviation { get; set; }
        public int? HasOperatingPhysician { get; set; }
        public int? HasAnesthesiaPhysician { get; set; }
        public int? HasOperatingRoom { get; set; }
        public int? IsHonoraryExcluded { get; set; }
        public int? IsResidenceRelated { get; set; }
        public int? HasMedicalResult { get; set; }
        public int? App { get; set; }
        public string? OperatingRoom { get; set; }
        public string? CoefficientCode { get; set; }
        public decimal? CoefficientValue { get; set; }
        public decimal? CashPriceUsd { get; set; }
        public decimal? CashPriceLlbp { get; set; }
        public int? Status { get; set; }
        public string? DisplayOrder { get; set; }
        public string? CostCenter { get; set; }
        public int? ExpectedResidenceDays { get; set; }
        public int? IsSubItem { get; set; }
        public int? IsDeleted { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? StartDate { get; set; }
        public int? StartDateLabel { get; set; }
        public int? EndDate { get; set; }
        public int? EndDateLabel { get; set; }
        public int? IsSelectedOrNot { get; set; }
        public int? SeverityId { get; set; }
        public int? StatusId { get; set; }
        public string? Comments { get; set; }
        public string? InCrAppCode { get; set; }
        public string? InCaAppCode { get; set; }
        public string? OutCrAppCode { get; set; }
        public string? OutCaAppCode { get; set; }
        public int? DenominationDefaultTime { get; set; }
        public decimal? Rate { get; set; }
        public int? HasVideo { get; set; }
        public int? IsOpenHeart { get; set; }
        public int? IsReferralShare { get; set; }
        public decimal? ReferralAmount { get; set; }
        public int? DenominationGroupId { get; set; }
        public int? IsClassRelated { get; set; }
        public string? CreditDiscount { get; set; }
        public string? CashDiscount { get; set; }
        public int? IsPrintable { get; set; }
    }
}
