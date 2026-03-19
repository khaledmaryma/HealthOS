using System.ComponentModel.DataAnnotations;

namespace LIS.Api.Models
{
    public class DeliveryItem
    {
        [Key]
        public int Id { get; set; }
        public int DeliveryHeader { get; set; }
        public int? Product { get; set; }
        public int? Pack { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductDescription { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? Discount { get; set; }
        public int? UnitOfMeasure { get; set; }
        public string? UnitOfMeasureName { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public string? Comment { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? IsDeleted { get; set; }
    }
}
