using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models.UserManagement
{
    [Table("UserDefinition")]
    public class UserDefinition
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("Username")]
        public string Username { get; set; } = string.Empty;

        [Column("ProfileID")]
        public int ProfileId { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("FullName")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150)]
        [Column("Email")]
        public string? Email { get; set; }

        [MaxLength(150)]
        [Column("Password")]
        public string? Password { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }

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
    }
}
