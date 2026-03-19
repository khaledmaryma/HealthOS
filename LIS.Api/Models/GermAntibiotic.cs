using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models
{
    [Table("GermAntibiotic")]
    public class GermAntibiotic
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("GermID")]
        public int? GermId { get; set; }

        [Column("AntibioticID")]
        public int? AntibioticId { get; set; }

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

        // Navigation properties
        [ForeignKey("GermId")]
        public virtual Germs? Germ { get; set; }

        [ForeignKey("AntibioticId")]
        public virtual Antibiotic? Antibiotic { get; set; }
    }
}






