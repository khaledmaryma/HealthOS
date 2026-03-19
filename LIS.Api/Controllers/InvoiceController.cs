using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<InvoiceController> _logger;
        private readonly IConfiguration _configuration;

        public InvoiceController(LISDbContext context, ILogger<InvoiceController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Get all invoice headers
        /// </summary>
        [HttpGet("headers")]
        public async Task<ActionResult<IEnumerable<InvoiceHeader>>> GetInvoiceHeaders()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT 
                        ID, InvoiceNumber, PatientID, MRN, AdmissionNumber, 
                        InvoiceDate, TotalAmount, PaidAmount, BalanceAmount, 
                        Status, IsDeleted, CreatedDate, ModifiedDate, 
                        CreatedBy, ModifiedBy, Notes, DueDate, PaidDate
                    FROM InvoiceHeader 
                    WHERE IsDeleted = 0 
                    ORDER BY InvoiceDate DESC", connection);

                var invoices = new List<InvoiceHeader>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    invoices.Add(new InvoiceHeader
                    {
                        Id = reader.GetInt32("ID"),
                        InvoiceNumber = reader.GetString("InvoiceNumber"),
                        PatientId = reader.IsDBNull("PatientID") ? null : reader.GetInt32("PatientID"),
                        MRN = reader.IsDBNull("MRN") ? null : reader.GetString("MRN"),
                        AdmissionNumber = reader.IsDBNull("AdmissionNumber") ? null : reader.GetString("AdmissionNumber"),
                        InvoiceDate = reader.GetDateTime("InvoiceDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        PaidAmount = reader.IsDBNull("PaidAmount") ? null : reader.GetDecimal("PaidAmount"),
                        BalanceAmount = reader.IsDBNull("BalanceAmount") ? null : reader.GetDecimal("BalanceAmount"),
                        Status = reader.GetString("Status"),
                        IsDeleted = reader.GetBoolean("IsDeleted"),
                        CreatedDate = reader.IsDBNull("CreatedDate") ? null : reader.GetDateTime("CreatedDate"),
                        ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetString("ModifiedBy"),
                        Notes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes"),
                        DueDate = reader.IsDBNull("DueDate") ? null : reader.GetDateTime("DueDate"),
                        PaidDate = reader.IsDBNull("PaidDate") ? null : reader.GetDateTime("PaidDate")
                    });
                }

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice headers");
                return StatusCode(500, "An error occurred while retrieving invoice headers");
            }
        }

        /// <summary>
        /// Get invoice header by ID
        /// </summary>
        [HttpGet("headers/{id}")]
        public async Task<ActionResult<InvoiceHeader>> GetInvoiceHeader(int id)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT 
                        ID, InvoiceNumber, PatientID, MRN, AdmissionNumber, 
                        InvoiceDate, TotalAmount, PaidAmount, BalanceAmount, 
                        Status, IsDeleted, CreatedDate, ModifiedDate, 
                        CreatedBy, ModifiedBy, Notes, DueDate, PaidDate
                    FROM InvoiceHeader 
                    WHERE ID = @Id AND IsDeleted = 0", connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var invoice = new InvoiceHeader
                    {
                        Id = reader.GetInt32("ID"),
                        InvoiceNumber = reader.GetString("InvoiceNumber"),
                        PatientId = reader.IsDBNull("PatientID") ? null : reader.GetInt32("PatientID"),
                        MRN = reader.IsDBNull("MRN") ? null : reader.GetString("MRN"),
                        AdmissionNumber = reader.IsDBNull("AdmissionNumber") ? null : reader.GetString("AdmissionNumber"),
                        InvoiceDate = reader.GetDateTime("InvoiceDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        PaidAmount = reader.IsDBNull("PaidAmount") ? null : reader.GetDecimal("PaidAmount"),
                        BalanceAmount = reader.IsDBNull("BalanceAmount") ? null : reader.GetDecimal("BalanceAmount"),
                        Status = reader.GetString("Status"),
                        IsDeleted = reader.GetBoolean("IsDeleted"),
                        CreatedDate = reader.IsDBNull("CreatedDate") ? null : reader.GetDateTime("CreatedDate"),
                        ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetString("ModifiedBy"),
                        Notes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes"),
                        DueDate = reader.IsDBNull("DueDate") ? null : reader.GetDateTime("DueDate"),
                        PaidDate = reader.IsDBNull("PaidDate") ? null : reader.GetDateTime("PaidDate")
                    };

                    return Ok(invoice);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice header {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the invoice header");
            }
        }

        /// <summary>
        /// Create a new invoice header
        /// </summary>
        [HttpPost("headers")]
        public async Task<ActionResult<InvoiceHeader>> CreateInvoiceHeader([FromBody] CreateInvoiceHeaderRequest request)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumber(connection);

                var command = new SqlCommand(@"
                    INSERT INTO InvoiceHeader 
                    (InvoiceNumber, PatientID, MRN, AdmissionNumber, InvoiceDate, TotalAmount, 
                     Status, IsDeleted, CreatedDate, Notes, DueDate)
                    VALUES 
                    (@InvoiceNumber, @PatientID, @MRN, @AdmissionNumber, @InvoiceDate, 0, 
                     'Draft', 0, GETDATE(), @Notes, @DueDate);
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
                command.Parameters.AddWithValue("@PatientID", (object?)request.PatientId ?? DBNull.Value);
                command.Parameters.AddWithValue("@MRN", (object?)request.MRN ?? DBNull.Value);
                command.Parameters.AddWithValue("@AdmissionNumber", (object?)request.AdmissionNumber ?? DBNull.Value);
                command.Parameters.AddWithValue("@InvoiceDate", request.InvoiceDate);
                command.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@DueDate", (object?)request.DueDate ?? DBNull.Value);

                var newId = await command.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                // Return the created invoice
                return await GetInvoiceHeader(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice header");
                return StatusCode(500, "An error occurred while creating the invoice header");
            }
        }

        /// <summary>
        /// Update an invoice header
        /// </summary>
        [HttpPut("headers/{id}")]
        public async Task<ActionResult<InvoiceHeader>> UpdateInvoiceHeader(int id, [FromBody] UpdateInvoiceHeaderRequest request)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE InvoiceHeader 
                    SET TotalAmount = ISNULL(@TotalAmount, TotalAmount),
                        PaidAmount = ISNULL(@PaidAmount, PaidAmount),
                        BalanceAmount = ISNULL(@BalanceAmount, BalanceAmount),
                        Status = ISNULL(@Status, Status),
                        Notes = ISNULL(@Notes, Notes),
                        DueDate = ISNULL(@DueDate, DueDate),
                        PaidDate = ISNULL(@PaidDate, PaidDate),
                        ModifiedDate = GETDATE()
                    WHERE ID = @Id AND IsDeleted = 0", connection);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@TotalAmount", (object?)request.TotalAmount ?? DBNull.Value);
                command.Parameters.AddWithValue("@PaidAmount", (object?)request.PaidAmount ?? DBNull.Value);
                command.Parameters.AddWithValue("@BalanceAmount", (object?)request.BalanceAmount ?? DBNull.Value);
                command.Parameters.AddWithValue("@Status", (object?)request.Status ?? DBNull.Value);
                command.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@DueDate", (object?)request.DueDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@PaidDate", (object?)request.PaidDate ?? DBNull.Value);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    return await GetInvoiceHeader(id);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice header {Id}", id);
                return StatusCode(500, "An error occurred while updating the invoice header");
            }
        }

        /// <summary>
        /// Delete an invoice header (soft delete)
        /// </summary>
        [HttpDelete("headers/{id}")]
        public async Task<ActionResult> DeleteInvoiceHeader(int id)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE InvoiceHeader 
                    SET IsDeleted = 1, ModifiedDate = GETDATE()
                    WHERE ID = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    return NoContent();
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice header {Id}", id);
                return StatusCode(500, "An error occurred while deleting the invoice header");
            }
        }

        /// <summary>
        /// Get invoice details by header ID
        /// </summary>
        [HttpGet("details/{invoiceHeaderId}")]
        public async Task<ActionResult<IEnumerable<InvoiceDetail>>> GetInvoiceDetails(int invoiceHeaderId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT 
                        ID, InvoiceHeaderID, ItemDescription, Quantity, UnitPrice, TotalPrice,
                        ServiceID, LabTestID, DenominationID, IsDeleted, CreatedDate, ModifiedDate,
                        CreatedBy, ModifiedBy, Sequence, DiscountAmount, TaxAmount, Notes
                    FROM InvoiceDetail 
                    WHERE InvoiceHeaderID = @InvoiceHeaderId AND IsDeleted = 0 
                    ORDER BY Sequence, ID", connection);
                command.Parameters.AddWithValue("@InvoiceHeaderId", invoiceHeaderId);

                var details = new List<InvoiceDetail>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    details.Add(new InvoiceDetail
                    {
                        Id = reader.GetInt32("ID"),
                        InvoiceHeaderId = reader.GetInt32("InvoiceHeaderID"),
                        ItemDescription = reader.GetString("ItemDescription"),
                        Quantity = reader.GetInt32("Quantity"),
                        UnitPrice = reader.GetDecimal("UnitPrice"),
                        TotalPrice = reader.GetDecimal("TotalPrice"),
                        ServiceId = reader.IsDBNull("ServiceID") ? null : reader.GetInt32("ServiceID"),
                        LabTestId = reader.IsDBNull("LabTestID") ? null : reader.GetInt32("LabTestID"),
                        DenominationId = reader.IsDBNull("DenominationID") ? null : reader.GetInt32("DenominationID"),
                        IsDeleted = reader.GetBoolean("IsDeleted"),
                        CreatedDate = reader.IsDBNull("CreatedDate") ? null : reader.GetDateTime("CreatedDate"),
                        ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetString("ModifiedBy"),
                        Sequence = reader.IsDBNull("Sequence") ? null : reader.GetInt32("Sequence"),
                        DiscountAmount = reader.IsDBNull("DiscountAmount") ? null : reader.GetDecimal("DiscountAmount"),
                        TaxAmount = reader.IsDBNull("TaxAmount") ? null : reader.GetDecimal("TaxAmount"),
                        Notes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes")
                    });
                }

                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice details for header {InvoiceHeaderId}", invoiceHeaderId);
                return StatusCode(500, "An error occurred while retrieving invoice details");
            }
        }

        /// <summary>
        /// Create a new invoice detail
        /// </summary>
        [HttpPost("details")]
        public async Task<ActionResult<InvoiceDetail>> CreateInvoiceDetail([FromBody] CreateInvoiceDetailRequest request)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Calculate total price
                var totalPrice = (request.Quantity * request.UnitPrice) - (request.DiscountAmount ?? 0) + (request.TaxAmount ?? 0);

                var command = new SqlCommand(@"
                    INSERT INTO InvoiceDetail 
                    (InvoiceHeaderID, ItemDescription, Quantity, UnitPrice, TotalPrice,
                     ServiceID, LabTestID, DenominationID, IsDeleted, CreatedDate, 
                     Sequence, DiscountAmount, TaxAmount, Notes)
                    VALUES 
                    (@InvoiceHeaderID, @ItemDescription, @Quantity, @UnitPrice, @TotalPrice,
                     @ServiceID, @LabTestID, @DenominationID, 0, GETDATE(),
                     @Sequence, @DiscountAmount, @TaxAmount, @Notes);
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@InvoiceHeaderID", request.InvoiceHeaderId);
                command.Parameters.AddWithValue("@ItemDescription", request.ItemDescription);
                command.Parameters.AddWithValue("@Quantity", request.Quantity);
                command.Parameters.AddWithValue("@UnitPrice", request.UnitPrice);
                command.Parameters.AddWithValue("@TotalPrice", totalPrice);
                command.Parameters.AddWithValue("@ServiceID", (object?)request.ServiceId ?? DBNull.Value);
                command.Parameters.AddWithValue("@LabTestID", (object?)request.LabTestId ?? DBNull.Value);
                command.Parameters.AddWithValue("@DenominationID", (object?)request.DenominationId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Sequence", (object?)request.Sequence ?? DBNull.Value);
                command.Parameters.AddWithValue("@DiscountAmount", (object?)request.DiscountAmount ?? DBNull.Value);
                command.Parameters.AddWithValue("@TaxAmount", (object?)request.TaxAmount ?? DBNull.Value);
                command.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);

                var newId = await command.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                // Return the created detail
                return await GetInvoiceDetail(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice detail");
                return StatusCode(500, "An error occurred while creating the invoice detail");
            }
        }

        /// <summary>
        /// Get invoice detail by ID
        /// </summary>
        [HttpGet("details/single/{id}")]
        public async Task<ActionResult<InvoiceDetail>> GetInvoiceDetail(int id)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT 
                        ID, InvoiceHeaderID, ItemDescription, Quantity, UnitPrice, TotalPrice,
                        ServiceID, LabTestID, DenominationID, IsDeleted, CreatedDate, ModifiedDate,
                        CreatedBy, ModifiedBy, Sequence, DiscountAmount, TaxAmount, Notes
                    FROM InvoiceDetail 
                    WHERE ID = @Id AND IsDeleted = 0", connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var detail = new InvoiceDetail
                    {
                        Id = reader.GetInt32("ID"),
                        InvoiceHeaderId = reader.GetInt32("InvoiceHeaderID"),
                        ItemDescription = reader.GetString("ItemDescription"),
                        Quantity = reader.GetInt32("Quantity"),
                        UnitPrice = reader.GetDecimal("UnitPrice"),
                        TotalPrice = reader.GetDecimal("TotalPrice"),
                        ServiceId = reader.IsDBNull("ServiceID") ? null : reader.GetInt32("ServiceID"),
                        LabTestId = reader.IsDBNull("LabTestID") ? null : reader.GetInt32("LabTestID"),
                        DenominationId = reader.IsDBNull("DenominationID") ? null : reader.GetInt32("DenominationID"),
                        IsDeleted = reader.GetBoolean("IsDeleted"),
                        CreatedDate = reader.IsDBNull("CreatedDate") ? null : reader.GetDateTime("CreatedDate"),
                        ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetString("ModifiedBy"),
                        Sequence = reader.IsDBNull("Sequence") ? null : reader.GetInt32("Sequence"),
                        DiscountAmount = reader.IsDBNull("DiscountAmount") ? null : reader.GetDecimal("DiscountAmount"),
                        TaxAmount = reader.IsDBNull("TaxAmount") ? null : reader.GetDecimal("TaxAmount"),
                        Notes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes")
                    };

                    return Ok(detail);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice detail {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the invoice detail");
            }
        }

        /// <summary>
        /// Update an invoice detail
        /// </summary>
        [HttpPut("details/{id}")]
        public async Task<ActionResult<InvoiceDetail>> UpdateInvoiceDetail(int id, [FromBody] UpdateInvoiceDetailRequest request)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get current values to calculate new total
                var getCurrentCommand = new SqlCommand(@"
                    SELECT Quantity, UnitPrice, DiscountAmount, TaxAmount 
                    FROM InvoiceDetail 
                    WHERE ID = @Id AND IsDeleted = 0", connection);
                getCurrentCommand.Parameters.AddWithValue("@Id", id);

                using var reader = await getCurrentCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return NotFound();
                }

                var currentQuantity = reader.GetInt32("Quantity");
                var currentUnitPrice = reader.GetDecimal("UnitPrice");
                var currentDiscountAmount = reader.IsDBNull("DiscountAmount") ? 0 : reader.GetDecimal("DiscountAmount");
                var currentTaxAmount = reader.IsDBNull("TaxAmount") ? 0 : reader.GetDecimal("TaxAmount");

                reader.Close();

                // Use new values or current values
                var quantity = request.Quantity ?? currentQuantity;
                var unitPrice = request.UnitPrice ?? currentUnitPrice;
                var discountAmount = request.DiscountAmount ?? currentDiscountAmount;
                var taxAmount = request.TaxAmount ?? currentTaxAmount;

                // Calculate new total price
                var totalPrice = (quantity * unitPrice) - discountAmount + taxAmount;

                var command = new SqlCommand(@"
                    UPDATE InvoiceDetail 
                    SET ItemDescription = ISNULL(@ItemDescription, ItemDescription),
                        Quantity = @Quantity,
                        UnitPrice = @UnitPrice,
                        TotalPrice = @TotalPrice,
                        ServiceID = ISNULL(@ServiceID, ServiceID),
                        LabTestID = ISNULL(@LabTestID, LabTestID),
                        DenominationID = ISNULL(@DenominationID, DenominationID),
                        Sequence = ISNULL(@Sequence, Sequence),
                        DiscountAmount = @DiscountAmount,
                        TaxAmount = @TaxAmount,
                        Notes = ISNULL(@Notes, Notes),
                        ModifiedDate = GETDATE()
                    WHERE ID = @Id AND IsDeleted = 0", connection);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@ItemDescription", (object?)request.ItemDescription ?? DBNull.Value);
                command.Parameters.AddWithValue("@Quantity", quantity);
                command.Parameters.AddWithValue("@UnitPrice", unitPrice);
                command.Parameters.AddWithValue("@TotalPrice", totalPrice);
                command.Parameters.AddWithValue("@ServiceID", (object?)request.ServiceId ?? DBNull.Value);
                command.Parameters.AddWithValue("@LabTestID", (object?)request.LabTestId ?? DBNull.Value);
                command.Parameters.AddWithValue("@DenominationID", (object?)request.DenominationId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Sequence", (object?)request.Sequence ?? DBNull.Value);
                command.Parameters.AddWithValue("@DiscountAmount", discountAmount);
                command.Parameters.AddWithValue("@TaxAmount", taxAmount);
                command.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    return await GetInvoiceDetail(id);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice detail {Id}", id);
                return StatusCode(500, "An error occurred while updating the invoice detail");
            }
        }

        /// <summary>
        /// Delete an invoice detail (soft delete)
        /// </summary>
        [HttpDelete("details/{id}")]
        public async Task<ActionResult> DeleteInvoiceDetail(int id)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE InvoiceDetail 
                    SET IsDeleted = 1, ModifiedDate = GETDATE()
                    WHERE ID = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    return NoContent();
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice detail {Id}", id);
                return StatusCode(500, "An error occurred while deleting the invoice detail");
            }
        }

        /// <summary>
        /// Search invoices
        /// </summary>
        [HttpGet("headers/search")]
        public async Task<ActionResult<IEnumerable<InvoiceHeader>>> SearchInvoices([FromQuery] string query)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT 
                        ID, InvoiceNumber, PatientID, MRN, AdmissionNumber, 
                        InvoiceDate, TotalAmount, PaidAmount, BalanceAmount, 
                        Status, IsDeleted, CreatedDate, ModifiedDate, 
                        CreatedBy, ModifiedBy, Notes, DueDate, PaidDate
                    FROM InvoiceHeader 
                    WHERE IsDeleted = 0 
                    AND (InvoiceNumber LIKE @Query 
                         OR MRN LIKE @Query 
                         OR AdmissionNumber LIKE @Query 
                         OR Status LIKE @Query)
                    ORDER BY InvoiceDate DESC", connection);
                command.Parameters.AddWithValue("@Query", $"%{query}%");

                var invoices = new List<InvoiceHeader>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    invoices.Add(new InvoiceHeader
                    {
                        Id = reader.GetInt32("ID"),
                        InvoiceNumber = reader.GetString("InvoiceNumber"),
                        PatientId = reader.IsDBNull("PatientID") ? null : reader.GetInt32("PatientID"),
                        MRN = reader.IsDBNull("MRN") ? null : reader.GetString("MRN"),
                        AdmissionNumber = reader.IsDBNull("AdmissionNumber") ? null : reader.GetString("AdmissionNumber"),
                        InvoiceDate = reader.GetDateTime("InvoiceDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        PaidAmount = reader.IsDBNull("PaidAmount") ? null : reader.GetDecimal("PaidAmount"),
                        BalanceAmount = reader.IsDBNull("BalanceAmount") ? null : reader.GetDecimal("BalanceAmount"),
                        Status = reader.GetString("Status"),
                        IsDeleted = reader.GetBoolean("IsDeleted"),
                        CreatedDate = reader.IsDBNull("CreatedDate") ? null : reader.GetDateTime("CreatedDate"),
                        ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetString("ModifiedBy"),
                        Notes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes"),
                        DueDate = reader.IsDBNull("DueDate") ? null : reader.GetDateTime("DueDate"),
                        PaidDate = reader.IsDBNull("PaidDate") ? null : reader.GetDateTime("PaidDate")
                    });
                }

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching invoices with query: {Query}", query);
                return StatusCode(500, "An error occurred while searching invoices");
            }
        }

        private async Task<string> GenerateInvoiceNumber(SqlConnection connection)
        {
            var command = new SqlCommand(@"
                SELECT ISNULL(MAX(CAST(SUBSTRING(InvoiceNumber, 4, LEN(InvoiceNumber)) AS INT)), 0) + 1 
                FROM InvoiceHeader 
                WHERE InvoiceNumber LIKE 'INV%' AND IsDeleted = 0", connection);

            var nextNumber = await command.ExecuteScalarAsync();
            return $"INV{nextNumber:D6}";
        }
    }
}














