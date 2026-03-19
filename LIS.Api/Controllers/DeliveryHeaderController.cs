using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using LIS.Api.Models;
using System.Data;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryHeaderController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeliveryHeaderController> _logger;

        public DeliveryHeaderController(IConfiguration configuration, ILogger<DeliveryHeaderController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get all delivery headers
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeliveryHeader>>> GetDeliveryHeaders()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("InventoryConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, new { message = "InventoryConnection string is not configured" });
                }

                var headers = new List<DeliveryHeader>();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT * FROM [dbo].[DeliveryHeader] 
                        WHERE [IsDeleted] = 0 OR [IsDeleted] IS NULL
                        ORDER BY [Date] DESC, [ID] DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                headers.Add(MapReaderToDeliveryHeader(reader));
                            }
                        }
                    }
                }

                return Ok(headers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delivery headers");
                return StatusCode(500, new { message = "An error occurred while retrieving delivery headers", error = ex.Message });
            }
        }

        /// <summary>
        /// Get delivery header by admission ID
        /// </summary>
        [HttpGet("ByAdmission/{admissionId}")]
        public async Task<ActionResult<DeliveryHeader>> GetDeliveryHeaderByAdmission(int admissionId)
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
                        SELECT TOP 1 dh.* 
                        FROM [dbo].[DeliveryHeader] dh
                        WHERE 
                            dh.[Admission] = @AdmissionId AND (dh.[IsDeleted] = 0 OR dh.[IsDeleted] IS NULL)
                        ORDER BY dh.[Date] DESC, dh.[ID] DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@AdmissionId", admissionId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Ok(MapReaderToDeliveryHeader(reader));
                            }
                            else
                            {
                                return NotFound(new { message = $"No delivery header found for admission {admissionId}" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delivery header by admission ID");
                return StatusCode(500, new { message = "An error occurred while retrieving delivery header", error = ex.Message });
            }
        }

        /// <summary>
        /// Get delivery header by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DeliveryHeader>> GetDeliveryHeader(int id)
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
                        SELECT * FROM [dbo].[DeliveryHeader] 
                        WHERE [ID] = @Id AND ([IsDeleted] = 0 OR [IsDeleted] IS NULL)";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Ok(MapReaderToDeliveryHeader(reader));
                            }
                            else
                            {
                                return NotFound(new { message = $"Delivery header with ID {id} not found" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delivery header {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving delivery header", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new delivery header
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DeliveryHeader>> CreateDeliveryHeader([FromBody] DeliveryHeader deliveryHeader)
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
                            var getMaxIdSql = "SELECT ISNULL(MAX([ID]), 0) + 1 FROM [dbo].[DeliveryHeader]";
                            int newId;
                            using (var cmd = new SqlCommand(getMaxIdSql, connection, transaction))
                            {
                                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            }

                            var insertSql = @"
                                INSERT INTO [dbo].[DeliveryHeader] 
                                ([ID], [Type], [TypeCounter], [PatientType], [Date], [PrescriptionDate],
                                 [ReferenceNb], [Patient], [Insurance], [Doctor], [Currency], [Warehouse],
                                 [Comment], [Gross], [Discount], [Vat], [Round], [Net], [Admission],
                                 [DrugInvoiceNumber], [CreatedBy], [CreatedDate], [IsDeleted], [GUID], [MedicalUnitID])
                                VALUES 
                                (@Id, @Type, @TypeCounter, @PatientType, @Date, @PrescriptionDate,
                                 @ReferenceNb, @Patient, @Insurance, @Doctor, @Currency, @Warehouse,
                                 @Comment, @Gross, @Discount, @Vat, @Round, @Net, @Admission,
                                 @DrugInvoiceNumber, @CreatedBy, @CreatedDate, @IsDeleted, @Guid, @MedicalUnitId)";

                            using (var command = new SqlCommand(insertSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Id", newId);
                                command.Parameters.AddWithValue("@Type", deliveryHeader.Type);
                                command.Parameters.AddWithValue("@TypeCounter", deliveryHeader.TypeCounter);
                                command.Parameters.AddWithValue("@PatientType", deliveryHeader.PatientType);
                                command.Parameters.AddWithValue("@Date", deliveryHeader.Date == default ? DateTime.Now : deliveryHeader.Date);
                                command.Parameters.AddWithValue("@PrescriptionDate", (object?)deliveryHeader.PrescriptionDate ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ReferenceNb", (object?)deliveryHeader.ReferenceNb ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Patient", deliveryHeader.Patient);
                                command.Parameters.AddWithValue("@Insurance", (object?)deliveryHeader.Insurance ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Doctor", (object?)deliveryHeader.Doctor ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Currency", deliveryHeader.Currency);
                                command.Parameters.AddWithValue("@Warehouse", deliveryHeader.Warehouse);
                                command.Parameters.AddWithValue("@Comment", (object?)deliveryHeader.Comment ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Gross", (object?)deliveryHeader.Gross ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Discount", (object?)deliveryHeader.Discount ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Vat", (object?)deliveryHeader.Vat ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Round", (object?)deliveryHeader.Round ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Net", (object?)deliveryHeader.Net ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Admission", deliveryHeader.Admission);
                                command.Parameters.AddWithValue("@DrugInvoiceNumber", (object?)deliveryHeader.DrugInvoiceNumber ?? DBNull.Value);
                                command.Parameters.AddWithValue("@CreatedBy", (object?)deliveryHeader.CreatedBy ?? DBNull.Value);
                                command.Parameters.AddWithValue("@CreatedDate", deliveryHeader.CreatedDate == default ? DateTime.Now : deliveryHeader.CreatedDate);
                                command.Parameters.AddWithValue("@IsDeleted", deliveryHeader.IsDeleted);
                                command.Parameters.AddWithValue("@Guid", (object?)deliveryHeader.GUID ?? DBNull.Value);
                                command.Parameters.AddWithValue("@MedicalUnitId", (object?)deliveryHeader.MedicalUnitId ?? DBNull.Value);

                                await command.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            deliveryHeader.Id = newId;

                            return CreatedAtAction(nameof(GetDeliveryHeader), new { id = newId }, deliveryHeader);
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
                _logger.LogError(ex, "Error creating delivery header");
                return StatusCode(500, new { message = "An error occurred while creating delivery header", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing delivery header
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDeliveryHeader(int id, [FromBody] DeliveryHeader deliveryHeader)
        {
            try
            {
                if (id != deliveryHeader.Id)
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
                        UPDATE [dbo].[DeliveryHeader] 
                        SET [Type] = @Type,
                            [TypeCounter] = @TypeCounter,
                            [PatientType] = @PatientType,
                            [Date] = @Date,
                            [PrescriptionDate] = @PrescriptionDate,
                            [ReferenceNb] = @ReferenceNb,
                            [Patient] = @Patient,
                            [Insurance] = @Insurance,
                            [Doctor] = @Doctor,
                            [Currency] = @Currency,
                            [Warehouse] = @Warehouse,
                            [Comment] = @Comment,
                            [Gross] = @Gross,
                            [Discount] = @Discount,
                            [Vat] = @Vat,
                            [Round] = @Round,
                            [Net] = @Net,
                            [Admission] = @Admission,
                            [DrugInvoiceNumber] = @DrugInvoiceNumber,
                            [ModifiedBy] = @ModifiedBy,
                            [ModifiedDate] = @ModifiedDate,
                            [IsDeleted] = @IsDeleted,
                            [GUID] = @Guid,
                            [MedicalUnitID] = @MedicalUnitId
                        WHERE [ID] = @Id AND ([IsDeleted] = 0 OR [IsDeleted] IS NULL)";

                    using (var command = new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Type", deliveryHeader.Type);
                        command.Parameters.AddWithValue("@TypeCounter", deliveryHeader.TypeCounter);
                        command.Parameters.AddWithValue("@PatientType", deliveryHeader.PatientType);
                        command.Parameters.AddWithValue("@Date", deliveryHeader.Date == default ? DateTime.Now : deliveryHeader.Date);
                        command.Parameters.AddWithValue("@PrescriptionDate", (object?)deliveryHeader.PrescriptionDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ReferenceNb", (object?)deliveryHeader.ReferenceNb ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Patient", deliveryHeader.Patient);
                        command.Parameters.AddWithValue("@Insurance", (object?)deliveryHeader.Insurance ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Doctor", (object?)deliveryHeader.Doctor ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Currency", deliveryHeader.Currency);
                        command.Parameters.AddWithValue("@Warehouse", deliveryHeader.Warehouse);
                        command.Parameters.AddWithValue("@Comment", (object?)deliveryHeader.Comment ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Gross", (object?)deliveryHeader.Gross ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Discount", (object?)deliveryHeader.Discount ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Vat", (object?)deliveryHeader.Vat ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Round", (object?)deliveryHeader.Round ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Net", (object?)deliveryHeader.Net ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Admission", deliveryHeader.Admission);
                        command.Parameters.AddWithValue("@DrugInvoiceNumber", (object?)deliveryHeader.DrugInvoiceNumber ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ModifiedBy", (object?)deliveryHeader.ModifiedBy ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ModifiedDate", (object?)deliveryHeader.ModifiedDate ?? DateTime.Now);
                        command.Parameters.AddWithValue("@IsDeleted", deliveryHeader.IsDeleted);
                        command.Parameters.AddWithValue("@Guid", (object?)deliveryHeader.GUID ?? DBNull.Value);
                        command.Parameters.AddWithValue("@MedicalUnitId", (object?)deliveryHeader.MedicalUnitId ?? DBNull.Value);

                        var rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound(new { message = $"Delivery header with ID {id} not found" });
                        }
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating delivery header {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating delivery header", error = ex.Message });
            }
        }

        private DeliveryHeader MapReaderToDeliveryHeader(SqlDataReader reader)
        {
            return new DeliveryHeader
            {
                Id = SafeGetInt32(reader, "ID"),
                Type = SafeGetInt32(reader, "Type"),
                TypeCounter = SafeGetInt32(reader, "TypeCounter"),
                PatientType = SafeGetInt32(reader, "PatientType"),
                Date = reader.IsDBNull("Date") ? DateTime.Now : reader.GetDateTime("Date"),
                PrescriptionDate = reader.IsDBNull("PrescriptionDate") ? null : reader.GetDateTime("PrescriptionDate"),
                ReferenceNb = reader.IsDBNull("ReferenceNb") ? null : reader.GetString("ReferenceNb"),
                Patient = SafeGetInt32(reader, "Patient"),
                Insurance = SafeGetInt32Nullable(reader, "Insurance"),
                Doctor = SafeGetInt32Nullable(reader, "Doctor"),
                Currency = SafeGetInt32(reader, "Currency"),
                Warehouse = SafeGetInt32(reader, "Warehouse"),
                Comment = reader.IsDBNull("Comment") ? null : reader.GetString("Comment"),
                Gross = SafeGetDecimalNullable(reader, "Gross"),
                Discount = SafeGetDecimalNullable(reader, "Discount"),
                Vat = SafeGetDecimalNullable(reader, "Vat"),
                Round = SafeGetDecimalNullable(reader, "Round"),
                Net = SafeGetDecimalNullable(reader, "Net"),
                Admission = SafeGetInt32(reader, "Admission"),
                DrugInvoiceNumber = SafeGetInt32Nullable(reader, "DrugInvoiceNumber"),
                CreatedBy = SafeGetInt32Nullable(reader, "CreatedBy"),
                CreatedDate = reader.IsDBNull("CreatedDate") ? DateTime.Now : reader.GetDateTime("CreatedDate"),
                ModifiedBy = SafeGetInt32Nullable(reader, "ModifiedBy"),
                ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                IsDeleted = !reader.IsDBNull("IsDeleted") && reader.GetBoolean("IsDeleted"),
                GUID = reader.IsDBNull("GUID") ? null : reader.GetGuid("GUID"),
                MedicalUnitId = SafeGetInt32Nullable(reader, "MedicalUnitID")
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
