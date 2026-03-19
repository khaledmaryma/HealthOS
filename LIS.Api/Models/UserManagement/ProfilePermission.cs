using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models.UserManagement
{
    [Table("ProfilePermission")]
    public class ProfilePermission
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("ProfileID")]
        public int ProfileId { get; set; }

        [Column("PermissionID")]
        public int PermissionId { get; set; }

        [Column("CanAdd")]
        public bool CanAdd { get; set; }

        [Column("CanModify")]
        public bool CanModify { get; set; }

        [Column("CanDelete")]
        public bool CanDelete { get; set; }

        [Column("CanSee")]
        public bool CanSee { get; set; }

        [Column("HasAccessToMenu")]
        public bool HasAccessToMenu { get; set; }

        [Column("HasAccessToApp")]
        public bool HasAccessToApp { get; set; }

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

        public PermissionDefinition? Permission { get; set; }
        public ProfileDefinition? Profile { get; set; }
    }
}
