using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LIS.Api.Models
{
    [Table("LabTest")]
    public class LabTest
    {
        [Key]
        public int ID { get; set; }

        public int? CostCenter { get; set; }
        public int? MedicalClass { get; set; }

        [MaxLength(25)]
        public string? Code { get; set; }

        public int? Denomination { get; set; }
        public string? TestDesciption { get; set; }

        [MaxLength(25)]
        public string? Identifier { get; set; }

        [MaxLength(10)]
        public string? Coef { get; set; }

        public decimal? CoefValue { get; set; }
        
        // Reference to UnitOfMeasure table (EMR database)
        // Note: Not a true foreign key as it's in a different database
        public int? UOM { get; set; }

        [MaxLength(5)]
        public string DisplayOrder { get; set; } = "0";

        public bool IsACollection { get; set; }
        public bool HasReferenceRange { get; set; }
        public bool ReferenceRelatesToAge { get; set; }
        public bool ReferencerelatesToGyneco { get; set; }

        public int? Group { get; set; }
        public int? SystemID { get; set; }
        
        // Foreign key to ResultType table
        public int ResultType { get; set; }

        // Navigation property
        [ForeignKey("ResultType")]
        [JsonIgnore]
        public virtual ResultType? ResultTypeNavigation { get; set; }

        public string? DefaultTextResult { get; set; }

        [MaxLength(25)]
        public string? DefaultNoramlMin { get; set; }

        [MaxLength(25)]
        public string? DefaultNormalMax { get; set; }

        [MaxLength(25)]
        public string? FemaleNormalMin { get; set; }

        [MaxLength(25)]
        public string? FemaleNormalMax { get; set; }

        [MaxLength(25)]
        public string? MaleNormalMin { get; set; }

        [MaxLength(25)]
        public string? MaleNormalMax { get; set; }

        public decimal? ErrorRangeMin { get; set; }
        public decimal? ErrorRangeMax { get; set; }

        [MaxLength(10)]
        public string? Prefix { get; set; }

        [MaxLength(10)]
        public string? Suffix { get; set; }

        public int? ColorID { get; set; }
        public bool IsSelected { get; set; }
        public int Priority { get; set; }
        public string? Comments { get; set; }
        public bool IsRemarkableFactor { get; set; }

        // Audit fields
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }

        public decimal? LowPanicIndex { get; set; }
        public decimal? HighPanicIndex { get; set; }

        [MaxLength(10)]
        public string? MappingCode { get; set; }

        [MaxLength(50)]
        public string? MappingDesc { get; set; }

        public int? DefaultMachine { get; set; }
        public bool ExcludedFromDiscount { get; set; }
        public string? MedicalClassDescription { get; set; }
        public string? DisplayNormalRange { get; set; }
    }
}


