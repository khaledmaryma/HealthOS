using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models.UserManagement
{
    [Table("UserKpiDefinition")]
    public class UserKpiDefinition
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("AppKey")]
        public string AppKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("HomePageId")]
        public string HomePageId { get; set; } = "main";

        [Required]
        [MaxLength(200)]
        [Column("Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("SqlQuery")]
        public string SqlQuery { get; set; } = string.Empty;

        /// <summary>0 = chart, 1 = grid.</summary>
        [Column("DisplayMode")]
        public int DisplayMode { get; set; }

        [Column("GridShowTotals")]
        public bool GridShowTotals { get; set; } = true;

        [Column("ChartOptionsJson")]
        public string? ChartOptionsJson { get; set; }

        [Column("SortOrder")]
        public int SortOrder { get; set; }

        [Column("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        [Column("ModifiedUtc")]
        public DateTime? ModifiedUtc { get; set; }

        [Column("IsDeleted")]
        public bool IsDeleted { get; set; }
    }
}
