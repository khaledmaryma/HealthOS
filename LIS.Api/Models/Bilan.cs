using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models;

[Table("Bilan", Schema = "dbo")]
public class Bilan
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("Description")]
    [MaxLength(50)]
    public string? Description { get; set; }

    [Column("ModifiedBy")]
    public int? ModifiedBy { get; set; }

    [Column("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [Column("CreatedBy")]
    public int CreatedBy { get; set; }

    [Column("CreatedDate")]
    public DateTime CreatedDate { get; set; }

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("IsSelected")]
    public bool IsSelected { get; set; }

    [Column("Type")]
    public int? Type { get; set; }
}
