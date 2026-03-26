using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace LIS.Api.Controllers;

[ApiController]
[Route("api/inventory/products")]
public class InventoryProductController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IConfiguration _configuration;

    public InventoryProductController(InventoryDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public record ProductListItemDto(
        int Id,
        string Code,
        string SmallDescription,
        int Category,
        int? Manufacturer,
        bool HasExpiry,
        bool CanBeSold,
        bool CanBePurchased,
        decimal? SalePriceLocal);

    public record ProductEditDto(
        int Id,
        string Code,
        string SmallDescription,
        string? LongDescription,
        int StockType,
        int ProductLine,
        int Category,
        int? Manufacturer,
        bool HasExpiry,
        bool CanBeSold,
        bool CanBePurchased,
        decimal? SalePriceLocal);

    [HttpGet]
    public async Task<ActionResult<object>> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 || pageSize > 100 ? 20 : pageSize;

        var query = _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Code,
                p.SmallDescription,
                p.Category,
                p.Manufacturer,
                p.HasExpiry,
                p.CanBeSold,
                p.CanBePurchased,
                p.SalePriceLocal))
            .ToListAsync();

        return Ok(new { totalCount, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductEditDto>> GetProduct(int id)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        var dto = new ProductEditDto(
            product.Id,
            product.Code,
            product.SmallDescription,
            product.LongDescription,
            product.StockType,
            product.ProductLine,
            product.Category,
            product.Manufacturer,
            product.HasExpiry,
            product.CanBeSold,
            product.CanBePurchased,
            product.SalePriceLocal);

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ProductEditDto>> CreateProduct([FromBody] ProductEditDto dto)
    {
        var product = new Product
        {
            Code = dto.Code,
            SmallDescription = dto.SmallDescription,
            LongDescription = dto.LongDescription,
            StockType = dto.StockType,
            ProductLine = dto.ProductLine,
            Category = dto.Category,
            Manufacturer = dto.Manufacturer,
            HasExpiry = dto.HasExpiry,
            CanBeSold = dto.CanBeSold,
            CanBePurchased = dto.CanBePurchased,
            SalePriceLocal = dto.SalePriceLocal,
            // sensible defaults
            IsDeleted = false,
            CanBeTransferred = true,
            CanBeDamaged = true,
            DefaultQty = 1,
            DefaultDiscount = 0,
            ProductStat = 4
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        dto = dto with { Id = product.Id };
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateProduct(int id, [FromBody] ProductEditDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest("Mismatched product id.");
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        product.Code = dto.Code;
        product.SmallDescription = dto.SmallDescription;
        product.LongDescription = dto.LongDescription;
        product.StockType = dto.StockType;
        product.ProductLine = dto.ProductLine;
        product.Category = dto.Category;
        product.Manufacturer = dto.Manufacturer;
        product.HasExpiry = dto.HasExpiry;
        product.CanBeSold = dto.CanBeSold;
        product.CanBePurchased = dto.CanBePurchased;
        product.SalePriceLocal = dto.SalePriceLocal;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("for-medicament")]
    public async Task<ActionResult<IEnumerable<object>>> GetProductsForMedicament()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("InventoryConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                return StatusCode(500, new { message = "InventoryConnection string is not configured" });
            }

            var products = new List<object>();

            var sql = @"
                SELECT        Product.id as prdId, 
                              Product.Code as prdCode, 
                              Product.SmallDescription as prdName , 
                              Product.SalePriceLocal as SalePriceLocal, 
                              Product.SalePriceMain as SalePriceMain, 
                              Category.id AS CatId, 
                              Category.Code AS CatCode, 
                              Category.Description as CatName, 
                              ProductPackage.id AS prdPackId, 
                              ProductPackage.Description as prdPack, 
                              ProductPackage.IsMain as IsMain, 
                              ProductLine.id AS PLId, 
                              ProductLine.Code AS PLCode, 
                              ProductLine.Description AS PLDescription, 
                              StockType.id AS StId, 
                              StockType.Code AS StCode, 
                              StockType.Description AS StName
                FROM            Product INNER JOIN
                                         ProductPackage ON Product.id = ProductPackage.Product INNER JOIN
                                         Category ON Product.Category = Category.id INNER JOIN
                                         ProductLine ON Product.ProductLine = ProductLine.id AND Category.ProductLine = ProductLine.id INNER JOIN
                                         StockType ON Product.StockType = StockType.id AND ProductLine.StockType = StockType.id
                WHERE Product.IsDeleted = 0 and Category.IsDeleted = 0 and ProductPackage.IsDeleted = 0 and ProductLine.IsDeleted = 0 and StockType.IsDeleted = 0
                and ProductPackage.IsMain = 1 and Product.SmallDescription not Like 'XX%'
                order by  Product.SmallDescription";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(new
                            {
                                prdId = reader.IsDBNull("prdId") ? 0 : reader.GetInt32("prdId"),
                                prdCode = reader.IsDBNull("prdCode") ? string.Empty : reader.GetString("prdCode"),
                                prdName = reader.IsDBNull("prdName") ? string.Empty : reader.GetString("prdName"),
                                SalePriceLocal = reader.IsDBNull("SalePriceLocal") ? 0 : reader.GetDecimal("SalePriceLocal"),
                                SalePriceMain = reader.IsDBNull("SalePriceMain") ? 0 : reader.GetDecimal("SalePriceMain"),
                                CatId = reader.IsDBNull("CatId") ? 0 : reader.GetInt32("CatId"),
                                CatCode = reader.IsDBNull("CatCode") ? string.Empty : reader.GetString("CatCode"),
                                CatName = reader.IsDBNull("CatName") ? string.Empty : reader.GetString("CatName"),
                                prdPackId = reader.IsDBNull("prdPackId") ? 0 : reader.GetInt32("prdPackId"),
                                prdPack = reader.IsDBNull("prdPack") ? "" : reader.GetString("prdPack"),
                                IsMain = reader.IsDBNull("IsMain") ? false : reader.GetBoolean("IsMain"),
                                PLId = reader.IsDBNull("PLId") ? 0 : reader.GetInt32("PLId"),
                                PLCode = reader.IsDBNull("PLCode") ? string.Empty : reader.GetString("PLCode"),
                                PLDescription = reader.IsDBNull("PLDescription") ? string.Empty : reader.GetString("PLDescription"),
                                StId = reader.IsDBNull("StId") ? 0 : reader.GetInt32("StId"),
                                StCode = reader.IsDBNull("StCode") ? string.Empty : reader.GetString("StCode"),
                                StName = reader.IsDBNull("StName") ? string.Empty : reader.GetString("StName")
                            });
                        }
                    }
                }
            }

            return Ok(products);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving products for medicament", error = ex.Message });
        }
    }
}


