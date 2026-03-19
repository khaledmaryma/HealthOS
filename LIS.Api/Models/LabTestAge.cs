using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("LabTestAge")]
    public class LabTestAge
    {
        [Key]
        public int ID { get; set; }

        public int? LabTest { get; set; }
        
        [MaxLength(100)]
        public string? Description { get; set; }
        
        [MaxLength(10)]
        public string? DisplayOrder { get; set; }
        
        [MaxLength(25)]
        public string? DefaultMin { get; set; }
        
        [MaxLength(25)]
        public string? DefaultMax { get; set; }
        
        [MaxLength(25)]
        public string? ErrorRangeMin { get; set; }
        
        [MaxLength(25)]
        public string? ErrorRangeMax { get; set; }
        
        [MaxLength(10)]
        public string? Prefix { get; set; }
        
        [MaxLength(10)]
        public string? Suffix { get; set; }
        
        public int? Lower { get; set; }
        
        public int? Higher { get; set; }
        
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

