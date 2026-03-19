using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("Denomination")]
    public class Denomination
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("Description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        [Column("Code")]
        [MaxLength(50)]
        public string? Code { get; set; }

        [Column("DisplayOrder")]
        public int? DisplayOrder { get; set; }

        [Column("CostCenter")]
        public int? CostCenter { get; set; }

        [Column("CreatedBy")]
        public int CreatedBy { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("ModifiedBy")]
        public int? ModifiedBy { get; set; }

        [Column("ModifiedDate")]
        public DateTime? ModifiedDate { get; set; }

        [Column("IsDeleted")]
        public bool IsDeleted { get; set; }
    }
}

