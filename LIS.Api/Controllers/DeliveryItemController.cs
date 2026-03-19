using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using LIS.Api.Models;
using System.Data;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryItemController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeliveryItemController> _logger;

        public DeliveryItemController(IConfiguration configuration, ILogger<DeliveryItemController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get all delivery items for a specific delivery header
        /// </summary>
        [HttpGet("by-header/{headerId}")]
        public async Task<ActionResult<IEnumerable<DeliveryItem>>> GetDeliveryItemsByHeader(int headerId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("InventoryConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, new { message = "InventoryConnection string is not configured" });
                }

                var items = new List<DeliveryItem>();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            [id] AS [ID],
                            [DeliverHeader] AS [DeliveryHeader],
                            [Product] AS [Product],
                            [Package] AS [Pack],
                            [Code] AS [ProductCode],
                            [AlternateDescription] AS [ProductDescription],
                            [Qty] AS [Quantity],
                            [UnitPrice] AS [UnitPrice],
                            [Net] AS [TotalPrice],
                            [Discount] AS [Discount],
                            CAST(NULL AS INT) AS [UnitOfMeasure],
                            CAST(NULL AS NVARCHAR(50)) AS [UnitOfMeasureName],
                            [ExpiryDate] AS [ExpiryDate],
                            [BatchNumber] AS [BatchNumber],
                            CAST(NULL AS NVARCHAR(255)) AS [Comment],
                            [CreatedBy] AS [CreatedBy],
                            [CreatedDate] AS [CreatedDate],
                            [ModifiedBy] AS [ModifiedBy],
                            [ModifiedDate] AS [ModifiedDate],
                            [IsDeleted] AS [IsDeleted]
                        FROM [dbo].[DeliverItem] 
                        WHERE [DeliverHeader] = @HeaderId 
                        AND ([IsDeleted] = 0 OR [IsDeleted] IS NULL)
                        ORDER BY [id]";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@HeaderId", headerId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(MapReaderToDeliveryItem(reader));
                            }
                        }
                    }
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delivery items for header {HeaderId}", headerId);
                return StatusCode(500, new { message = "An error occurred while retrieving delivery items", error = ex.Message });
            }
        }

        /// <summary>
        /// Get delivery item by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DeliveryItem>> GetDeliveryItem(int id)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("InventoryConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, new { message = "InventoryConnection string is not configured" });
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            [id] AS [ID],
                            [DeliverHeader] AS [DeliveryHeader],
                            [Product] AS [Product],
                            [Package] AS [Pack],
                            [Code] AS [ProductCode],
                            [AlternateDescription] AS [ProductDescription],
                            [Qty] AS [Quantity],
                            [UnitPrice] AS [UnitPrice],
                            [Net] AS [TotalPrice],
                            [Discount] AS [Discount],
                            CAST(NULL AS INT) AS [UnitOfMeasure],
                            CAST(NULL AS NVARCHAR(50)) AS [UnitOfMeasureName],
                            [ExpiryDate] AS [ExpiryDate],
                            [BatchNumber] AS [BatchNumber],
                            CAST(NULL AS NVARCHAR(255)) AS [Comment],
                            [CreatedBy] AS [CreatedBy],
                            [CreatedDate] AS [CreatedDate],
                            [ModifiedBy] AS [ModifiedBy],
                            [ModifiedDate] AS [ModifiedDate],
                            [IsDeleted] AS [IsDeleted]
                        FROM [dbo].[DeliverItem] 
                        WHERE [id] = @Id AND ([IsDeleted] = 0 OR [IsDeleted] IS NULL)";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Ok(MapReaderToDeliveryItem(reader));
                            }
                            else
                            {
                                return NotFound(new { message = $"Delivery item with ID {id} not found" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delivery item {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving delivery item", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new delivery item
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DeliveryItem>> CreateDeliveryItem([FromBody] DeliveryItem deliveryItem)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("InventoryConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, new { message = "InventoryConnection string is not configured" });
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Get next ID
                            var getMaxIdSql = "SELECT ISNULL(MAX([ID]), 0) + 1 FROM [dbo].[DeliveryItem]";
                            int newId;
                            using (var cmd = new SqlCommand(getMaxIdSql, connection, transaction))
                            {
                                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            }

                            var insertSql = @"
                                INSERT INTO [dbo].[DeliveryItem] 
                                ([ID], [DeliveryHeader], [Product], [Quantity], [UnitPrice], [TotalPrice], 
                                 [Discount], [UnitOfMeasure], [ExpiryDate], [BatchNumber], [Comment],
                                 [CreatedBy], [CreatedDate], [IsDeleted])
                                VALUES 
                                (@Id, @DeliveryHeader, @Product, @Quantity, @UnitPrice, @TotalPrice,
                                 @Discount, @UnitOfMeasure, @ExpiryDate, @BatchNumber, @Comment,
                                 @CreatedBy, @CreatedDate, @IsDeleted)";

                            using (var command = new SqlCommand(insertSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Id", newId);
                                command.Parameters.AddWithValue("@DeliveryHeader", deliveryItem.DeliveryHeader);
                                command.Parameters.AddWithValue("@Product", (object?)deliveryItem.Product ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Quantity", (object?)deliveryItem.Quantity ?? DBNull.Value);
                                command.Parameters.AddWithValue("@UnitPrice", (object?)deliveryItem.UnitPrice ?? DBNull.Value);
                                command.Parameters.AddWithValue("@TotalPrice", (object?)deliveryItem.TotalPrice ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Discount", (object?)deliveryItem.Discount ?? DBNull.Value);
                                command.Parameters.AddWithValue("@UnitOfMeasure", (object?)deliveryItem.UnitOfMeasure ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ExpiryDate", (object?)deliveryItem.ExpiryDate ?? DBNull.Value);
                                command.Parameters.AddWithValue("@BatchNumber", (object?)deliveryItem.BatchNumber ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Comment", (object?)deliveryItem.Comment ?? DBNull.Value);
                                command.Parameters.AddWithValue("@CreatedBy", (object?)deliveryItem.CreatedBy ?? DBNull.Value);
                                command.Parameters.AddWithValue("@CreatedDate", (object?)deliveryItem.CreatedDate ?? DateTime.Now);
                                command.Parameters.AddWithValue("@IsDeleted", (object?)deliveryItem.IsDeleted ?? 0);

                                await command.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            deliveryItem.Id = newId;

                            return CreatedAtAction(nameof(GetDeliveryItem), new { id = newId }, deliveryItem);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating delivery item");
                return StatusCode(500, new { message = "An error occurred while creating delivery item", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing delivery item
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDeliveryItem(int id, [FromBody] DeliveryItem deliveryItem)
        {
            try
            {
                if (id != deliveryItem.Id)
                {
                    return BadRequest(new { message = "ID mismatch" });
                }

                var connectionString = _configuration.GetConnectionString("InventoryConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, new { message = "InventoryConnection string is not configured" });
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var updateSql = @"
                        UPDATE [dbo].[DeliveryItem] 
                        SET [DeliveryHeader] = @DeliveryHeader,
                            [Product] = @Product,
                            [Quantity] = @Quantity,
                            [UnitPrice] = @UnitPrice,
                            [TotalPrice] = @TotalPrice,
                            [Discount] = @Discount,
                            [UnitOfMeasure] = @UnitOfMeasure,
                            [ExpiryDate] = @ExpiryDate,
                            [BatchNumber] = @BatchNumber,
                            [Comment] = @Comment,
                            [ModifiedBy] = @ModifiedBy,
                            [ModifiedDate] = @ModifiedDate
                        WHERE [ID] = @Id AND ([IsDeleted] = 0 OR [IsDeleted] IS NULL)";

                    using (var command = new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@DeliveryHeader", deliveryItem.DeliveryHeader);
                        command.Parameters.AddWithValue("@Product", (object?)deliveryItem.Product ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Quantity", (object?)deliveryItem.Quantity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@UnitPrice", (object?)deliveryItem.UnitPrice ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TotalPrice", (object?)deliveryItem.TotalPrice ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Discount", (object?)deliveryItem.Discount ?? DBNull.Value);
                        command.Parameters.AddWithValue("@UnitOfMeasure", (object?)deliveryItem.UnitOfMeasure ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ExpiryDate", (object?)deliveryItem.ExpiryDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@BatchNumber", (object?)deliveryItem.BatchNumber ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Comment", (object?)deliveryItem.Comment ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ModifiedBy", (object?)deliveryItem.ModifiedBy ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ModifiedDate", (object?)deliveryItem.ModifiedDate ?? DateTime.Now);

                        var rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound(new { message = $"Delivery item with ID {id} not found" });
                        }
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating delivery item {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating delivery item", error = ex.Message });
            }
        }

        private DeliveryItem MapReaderToDeliveryItem(SqlDataReader reader)
        {
            return new DeliveryItem
            {
                Id = SafeGetInt32(reader, "ID"),
                DeliveryHeader = SafeGetInt32(reader, "DeliveryHeader"),
                Product = SafeGetInt32Nullable(reader, "Product"),
                Pack = SafeGetInt32Nullable(reader, "Pack"),
                ProductCode = reader.IsDBNull("ProductCode") ? null : reader.GetString("ProductCode"),
                ProductDescription = reader.IsDBNull("ProductDescription") ? null : reader.GetString("ProductDescription"),
                Quantity = SafeGetDecimalNullable(reader, "Quantity"),
                UnitPrice = SafeGetDecimalNullable(reader, "UnitPrice"),
                TotalPrice = SafeGetDecimalNullable(reader, "TotalPrice"),
                Discount = SafeGetDecimalNullable(reader, "Discount"),
                UnitOfMeasure = SafeGetInt32Nullable(reader, "UnitOfMeasure"),
                UnitOfMeasureName = reader.IsDBNull("UnitOfMeasureName") ? null : reader.GetString("UnitOfMeasureName"),
                ExpiryDate = reader.IsDBNull("ExpiryDate") ? null : reader.GetDateTime("ExpiryDate"),
                BatchNumber = reader.IsDBNull("BatchNumber") ? null : reader.GetString("BatchNumber"),
                Comment = reader.IsDBNull("Comment") ? null : reader.GetString("Comment"),
                CreatedBy = SafeGetInt32Nullable(reader, "CreatedBy"),
                CreatedDate = reader.IsDBNull("CreatedDate") ? null : reader.GetDateTime("CreatedDate"),
                ModifiedBy = SafeGetInt32Nullable(reader, "ModifiedBy"),
                ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                IsDeleted = SafeGetInt32Nullable(reader, "IsDeleted")
            };
        }

        private int SafeGetInt32(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return 0;
                var value = reader.GetValue(ordinal);
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private int? SafeGetInt32Nullable(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return null;
                var value = reader.GetValue(ordinal);
                return Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }

        private decimal? SafeGetDecimalNullable(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return null;
                var value = reader.GetValue(ordinal);
                return Convert.ToDecimal(value);
            }
            catch
            {
                return null;
            }
        }
    }
}
