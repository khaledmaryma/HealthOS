using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models.UserManagement
{
    [Table("ScreenDefinition")]
    public class ScreenDefinition
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("AppID")]
        public int AppId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        [Column("Route")]
        public string? Route { get; set; }

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

        public AppDefinition? Application { get; set; }
        public ICollection<PermissionDefinition> Permissions { get; set; } = new List<PermissionDefinition>();
    }
}
