namespace LIS.Api.Models
{
    public class InsuranceOption
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ArabicDescription { get; set; }
        public string? Code { get; set; }
    }
}
