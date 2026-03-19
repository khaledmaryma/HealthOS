using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("ResultType")]
    public class ResultType
    {
        [Key]
        public int ID { get; set; }

        [MaxLength(50)]
        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<LabTest>? LabTests { get; set; }
    }
}

