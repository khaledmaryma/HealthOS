namespace LIS.Api.Models
{
    public class DenominationSearchResult
    {
        public string? Insurance { get; set; }
        public int InsId { get; set; }
        public string? CostCenterName { get; set; }
        public int CostCenterId { get; set; }
        public int DenId { get; set; }
        public string? ActCode { get; set; } // Denomination.Code
        public string? ActName { get; set; } // SmallDescription
        public string? LabTest { get; set; }
        public decimal? CoefficientValue { get; set; }
        public decimal OutLL { get; set; }
        public decimal OutUsd { get; set; }
        public decimal PriceLL { get; set; }
        public decimal PriceUsd { get; set; }
        public bool? HasOperatingPhysician { get; set; }
    }
}

