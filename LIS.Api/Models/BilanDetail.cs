using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models;

[Table("BilanDetail", Schema = "dbo")]
public class BilanDetail
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("BilanID")]
    public int BilanId { get; set; }

    [Column("LabTestID")]
    public int? LabTestId { get; set; }

    [Column("OperationID")]
    public int? OperationId { get; set; }

    [Column("ImaigngID")]
    public int? ImaigngId { get; set; }

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
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public int? Type { get; set; }

    [Column("Description")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string? Description { get; set; }

    [Column("TypeDescription")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string? TypeDescription { get; set; }

    [Column("DenominationID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public int? DenominationId { get; set; }

    [Column("DenominationCode")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string? DenominationCode { get; set; }

    [Column("DenominationDescription")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string? DenominationDescription { get; set; }
}
