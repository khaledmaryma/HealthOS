using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models.UserManagement
{
    [Table("PermissionDefinition")]
    public class PermissionDefinition
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("ScreenID")]
        public int? ScreenId { get; set; }

        [MaxLength(100)]
        [Column("Action")]
        public string? Action { get; set; }

        [MaxLength(150)]
        [Column("PermissionKey")]
        public string? PermissionKey { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        [Column("Description")]
        public string? Description { get; set; }

        [Column("ApplicationID")]
        public int? ApplicationId { get; set; }

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
        public ScreenDefinition? Screen { get; set; }
        public ICollection<ProfilePermission> ProfilePermissions { get; set; } = new List<ProfilePermission>();
    }
}
