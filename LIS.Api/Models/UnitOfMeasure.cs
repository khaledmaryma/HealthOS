using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("UnitOfMeasure")]
    public class UnitOfMeasure
    {
        [Key]
        public int ID { get; set; }

        [MaxLength(100)]
        public string? Description { get; set; }
    }
}

