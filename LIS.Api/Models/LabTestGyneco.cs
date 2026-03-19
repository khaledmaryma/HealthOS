using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("LabTestGyneco")]
    public class LabTestGyneco
    {
        [Key]
        public int ID { get; set; }

        public int? LabTest { get; set; }
        
        [MaxLength(100)]
        public string? Description { get; set; }
        
        [MaxLength(10)]
        public string? DisplayOrder { get; set; }
        
        public decimal? FemaleNormalMin { get; set; }
        
        public decimal? FemaleNormalMax { get; set; }
        
        public decimal? ErrorRangeMin { get; set; }
        
        public decimal? ErrorRangeMax { get; set; }
        
        [MaxLength(10)]
        public string? Prefix { get; set; }
        
        [MaxLength(10)]
        public string? Suffix { get; set; }
        
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

