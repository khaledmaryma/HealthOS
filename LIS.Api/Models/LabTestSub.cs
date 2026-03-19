using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("LabTestSub")]
    public class LabTestSub
    {
        [Key]
        public int ID { get; set; }

        public int? LabTest { get; set; }
        
        [MaxLength(100)]
        public string? Description { get; set; }
        
        [MaxLength(100)]
        public string? ParagraphHeader { get; set; }
        
        [MaxLength(10)]
        public string? DisplayOrder { get; set; }
        
        public int? UOM { get; set; }
        
        [MaxLength(25)]
        public string? DefaultNoramlMin { get; set; }
        
        [MaxLength(25)]
        public string? DefaultNormalMax { get; set; }
        
        [MaxLength(25)]
        public string? FemaleNormalMin { get; set; }
        
        [MaxLength(25)]
        public string? FemaleNormalMax { get; set; }
        
        [MaxLength(25)]
        public string? MaleNormalMin { get; set; }
        
        [MaxLength(25)]
        public string? MaleNormalMax { get; set; }
        
        [MaxLength(25)]
        public string? ErrorRangeMin { get; set; }
        
        [MaxLength(25)]
        public string? ErrorRangeMax { get; set; }
        
        [MaxLength(10)]
        public string? Prefix { get; set; }
        
        [MaxLength(10)]
        public string? Suffix { get; set; }
        
        [MaxLength(200)]
        public string? LabTestDescription { get; set; }
        
        public bool IsPercentage { get; set; }
        
        public bool IsComment { get; set; }
        
        [MaxLength(25)]
        public string? AgeNormalMin { get; set; }
        
        [MaxLength(25)]
        public string? AgeNormalMax { get; set; }
        
        public int? AgeType { get; set; }
        
        public int? MachineID { get; set; }
        
        public decimal? LowPanicIndex { get; set; }
        
        public decimal? HighPanicIndex { get; set; }
        
        public bool IsDeleted { get; set; }
        
        // Audit fields
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
