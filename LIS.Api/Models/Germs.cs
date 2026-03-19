using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("Germs")]
    public class Germs
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("Code")]
        [StringLength(50)]
        public string? Code { get; set; }

        [Column("Description")]
        [StringLength(255)]
        public string? Description { get; set; }

        [Column("Identifier")]
        [StringLength(50)]
        public string? Identifier { get; set; }

        [Column("DisplayOrder")]
        [StringLength(50)]
        public string? DisplayOrder { get; set; }

        [Column("CreatedBy")]
        public int? CreatedBy { get; set; }

        [Column("CreatedDate")]
        public DateTime? CreatedDate { get; set; }

        [Column("ModifiedBy")]
        public int? ModifiedBy { get; set; }

        [Column("ModifiedDate")]
        public DateTime? ModifiedDate { get; set; }

        [Column("IsDeleted")]
        public bool IsDeleted { get; set; }
    }
}


