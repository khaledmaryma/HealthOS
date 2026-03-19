using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models.UserManagement
{
    [Table("AppDefinition")]
    public class AppDefinition
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Column("IsDeleted")]
        public bool IsDeleted { get; set; }

        [Column("CreatedBy")]
        public int CreatedBy { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("ModifiedBy")]
        public int? ModifiedBy { get; set; }

        [Column("ModifiedDate")]
        public DateTime? ModifiedDate { get; set; }

        public ICollection<ScreenDefinition> Screens { get; set; } = new List<ScreenDefinition>();
        public ICollection<PermissionDefinition> Permissions { get; set; } = new List<PermissionDefinition>();
    }
}
