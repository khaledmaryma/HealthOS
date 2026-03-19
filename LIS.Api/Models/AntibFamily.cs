using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("AntibFamily")]
    public class AntibFamily
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("Description")]
        [StringLength(255)]
        public string? Description { get; set; }

        [Column("ArabicDescription")]
        [StringLength(255)]
        public string? ArabicDescription { get; set; }

        [Column("DisplayOrder")]
        public int? DisplayOrder { get; set; }

        [Column("IsDeleted")]
        public bool IsDeleted { get; set; }

        [Column("CreatedBy")]
        public int? CreatedBy { get; set; }

        [Column("CreatedDate")]
        public DateTime? CreatedDate { get; set; }

        [Column("ModifiedBy")]
        public int? ModifiedBy { get; set; }

        [Column("ModifiedDate")]
        public DateTime? ModifiedDate { get; set; }
    }
}





























