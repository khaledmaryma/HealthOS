using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.Api.Models;

[Table("Product", Schema = "dbo")]
public class Product
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    public int StockType { get; set; }
    public int ProductLine { get; set; }
    public int Category { get; set; }
    public int? Manufacturer { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(250)]
    public string SmallDescription { get; set; } = string.Empty;

    [MaxLength(510)]
    public string? LongDescription { get; set; }

    public int? ROA { get; set; }

    [MaxLength(510)]
    public string? Ingredient { get; set; }

    [MaxLength(50)]
    public string? BarCode { get; set; }

    [MaxLength(50)]
    public string? Identifier { get; set; }

    public int? DefaultPack { get; set; }

    public int Status { get; set; }

    public bool CanNotBeSold { get; set; }
    public bool CanNotBePurchased { get; set; }
    public bool CanNotBeTransferred { get; set; }
    public bool CanNotBeDamaged { get; set; }

    public decimal? SalePriceLocal { get; set; }
    public decimal? SalePriceMain { get; set; }
    public decimal? VatSales { get; set; }
    public bool IncludedSale { get; set; }

    public bool HasExpiry { get; set; }
    public bool? HasSerial { get; set; }

    public decimal? MinimumQty { get; set; }
    public decimal? MaximumQty { get; set; }
    public decimal ROP { get; set; }

    public int ProductStat { get; set; }

    public int DefaultQty { get; set; }

    public decimal? Strenght { get; set; }
    public decimal? Quantity { get; set; }
    public decimal DefaultDiscount { get; set; }

    public bool IsDeleted { get; set; }
    public bool IgnoreDenied { get; set; }
    public bool IsRecycled { get; set; }
    public bool HasRecycle { get; set; }
    public bool AccountSeparated { get; set; }
    public bool CanBeSold { get; set; }
    public bool CanBePurchased { get; set; }
    public bool CanBeTransferred { get; set; }
    public bool CanBeDamaged { get; set; }

    public bool IsServiceProduct { get; set; }
    public bool IsPOSProduct { get; set; }

    public int? DenominationID { get; set; }
}


