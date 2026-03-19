using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillingInvoiceDetailController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<BillingInvoiceDetailController> _logger;
        private readonly IConfiguration _configuration;

        public BillingInvoiceDetailController(LISDbContext context, ILogger<BillingInvoiceDetailController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BillingInvoiceDetail>>> GetInvoiceDetails()
        {
            try
            {
                var details = await _context.Database
                    .SqlQueryRaw<BillingInvoiceDetail>("SELECT * FROM [Billing].[dbo].[InvoiceDetail] WHERE [IsDeleted] = 0 ORDER BY [DetailDate] DESC")
                    .ToListAsync();

                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice details");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("ByHeader/{headerId}")]
        public async Task<ActionResult<IEnumerable<BillingInvoiceDetail>>> GetInvoiceDetailsByHeaderId(int headerId)
        {
            try
            {
                // Get BillingConnection or use DefaultConnection and replace database name
                var connectionString = _configuration.GetConnectionString("BillingConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Fallback to DefaultConnection and replace database
                    var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrEmpty(defaultConnection))
                    {
                        connectionString = defaultConnection.Replace("Database=LIS", "Database=Billing");
                    }
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("BillingConnection string is not configured");
                    return StatusCode(500, "Database connection string not configured");
                }

                var details = new List<BillingInvoiceDetail>();

                // Helper methods to safely get values (handles both int/string types and NULLs)
                int SafeGetInt32(SqlDataReader rdr, string columnName, int defaultValue = 0)
                {
                    if (rdr.IsDBNull(columnName)) return defaultValue;
                    try
                    {
                        var value = rdr.GetValue(columnName);
                        return Convert.ToInt32(value);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                int? SafeGetInt32Nullable(SqlDataReader rdr, string columnName)
                {
                    if (rdr.IsDBNull(columnName)) return null;
                    try
                    {
                        var value = rdr.GetValue(columnName);
                        return Convert.ToInt32(value);
                    }
                    catch
                    {
                        return null;
                    }
                }

                decimal SafeGetDecimal(SqlDataReader rdr, string columnName, decimal defaultValue = 0)
                {
                    if (rdr.IsDBNull(columnName)) return defaultValue;
                    try
                    {
                        var value = rdr.GetValue(columnName);
                        return Convert.ToDecimal(value);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                decimal? SafeGetDecimalNullable(SqlDataReader rdr, string columnName)
                {
                    if (rdr.IsDBNull(columnName)) return null;
                    try
                    {
                        var value = rdr.GetValue(columnName);
                        return Convert.ToDecimal(value);
                    }
                    catch
                    {
                        return null;
                    }
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT * FROM [Billing].[dbo].[InvoiceDetail] 
                        WHERE [InvoiceHeader] = @HeaderId AND [IsDeleted] = 0 
                        ORDER BY [OrderDetailSequenceNumber]";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@HeaderId", headerId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {

                                details.Add(new BillingInvoiceDetail
                                {
                                    Id = SafeGetInt32(reader, "ID"),
                                    PrescriptionDate = reader.IsDBNull("PrescriptionDate") ? null : reader.GetDateTime("PrescriptionDate"),
                                    PrescribedBy = SafeGetInt32Nullable(reader, "PrescribedBy"),
                                    MedicationUnit = SafeGetInt32(reader, "MedicationUnit"),
                                    MedicationUnitDescription = reader.IsDBNull("MedicationUnitDescription") ? string.Empty : reader.GetString("MedicationUnitDescription"),
                                    Admission = SafeGetInt32(reader, "Admission"),
                                    Patient = SafeGetInt32(reader, "Patient"),
                                    Denomination = SafeGetInt32(reader, "Denomination"),
                                    DenominationCode = reader.IsDBNull("DenominationCode") ? string.Empty : reader.GetString("DenominationCode"),
                                    DenominationDescription = reader.IsDBNull("DenominationDescription") ? string.Empty : reader.GetString("DenominationDescription"),
                                    DenominationCoeffCode = reader.IsDBNull("DenominationCoeffCode") ? string.Empty : reader.GetString("DenominationCoeffCode"),
                                    DenominationCoeffValue = SafeGetDecimal(reader, "DenominationCoeffValue"),
                                    DenominationCoeffPrice = SafeGetDecimal(reader, "DenominationCoeffPrice"),
                                    Quantity = SafeGetDecimal(reader, "Quantity"),
                                    UnitPrice = SafeGetDecimal(reader, "UnitPrice"),
                                    NetPrice = SafeGetDecimal(reader, "NetPrice"),
                                    NetUnitPrice = SafeGetDecimal(reader, "NetUnitPrice"),
                                    DifferenceAmount = SafeGetDecimal(reader, "DifferenceAmount"),
                                    DeniedAmount = SafeGetDecimal(reader, "DeniedAmount"),
                                    Discount = SafeGetDecimal(reader, "Discount"),
                                    LumpSum = SafeGetDecimal(reader, "LumpSum"),
                                    ComplementaryAmount = SafeGetDecimal(reader, "ComplementaryAmount"),
                                    ComplementaryAmountOtherCurrency = SafeGetDecimal(reader, "ComplementaryAmountOtherCurrency"),
                                    ComplementaryDifferenceOtherCurrency = SafeGetDecimal(reader, "ComplementaryDifferenceOtherCurrency"),
                                    OperatingPhysician = SafeGetInt32(reader, "OperatingPhysician"),
                                    IsMedicalResultOk = SafeGetInt32Nullable(reader, "IsMedicalResultOk"),
                                    MedicalResultDate = reader.IsDBNull("MedicalResultDate") ? null : reader.GetDateTime("MedicalResultDate"),
                                    RequireApproval = SafeGetInt32(reader, "RequireApproval"),
                                    ApprovalReference = reader.IsDBNull("ApprovalReference") ? null : reader.GetString("ApprovalReference"),
                                    ApprovalDate = reader.IsDBNull("ApprovalDate") ? null : reader.GetDateTime("ApprovalDate"),
                                    IsDenied = SafeGetInt32(reader, "IsDenied"),
                                    ApprovedBy = reader.IsDBNull("ApprovedBy") ? null : reader.GetString("ApprovedBy"),
                                    DueDate = reader.IsDBNull("DueDate") ? null : reader.GetDateTime("DueDate"),
                                    ExecutionDate = reader.IsDBNull("ExecutionDate") ? null : reader.GetDateTime("ExecutionDate"),
                                    InvoiceHeader = SafeGetInt32(reader, "InvoiceHeader"),
                                    ReferralPhysician = SafeGetInt32(reader, "ReferralPhysician"),
                                    CostCenter = SafeGetInt32(reader, "CostCenter"),
                                    ProfitCenter = SafeGetInt32(reader, "ProfitCenter"),
                                    PacIndex = SafeGetInt32Nullable(reader, "PacIndex"),
                                    PreInvoiceDetail = SafeGetInt32Nullable(reader, "PreInvoiceDetail"),
                                    DetailDate = reader.IsDBNull("DetailDate") ? DateTime.Now : reader.GetDateTime("DetailDate"),
                                    MainDetailId = SafeGetInt32Nullable(reader, "MainDetailId"),
                                    CopyFlag = SafeGetInt32(reader, "CopyFlag"),
                                    DetailDateHelper = reader.IsDBNull("DetailDateHelper") ? null : reader.GetDateTime("DetailDateHelper"),
                                    IsDoubtfull = SafeGetInt32(reader, "IsDoubtfull"),
                                    Procedure = reader.IsDBNull("Procedure") ? null : reader.GetString("Procedure"),
                                    IsDeleted = SafeGetInt32(reader, "IsDeleted"),
                                    CreatedBy = SafeGetInt32(reader, "CreatedBy"),
                                    ModifiedBy = SafeGetInt32Nullable(reader, "ModifiedBy"),
                                    CreatedDate = reader.IsDBNull("CreatedDate") ? DateTime.Now : reader.GetDateTime("CreatedDate"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                                    PreviousDetailId = SafeGetInt32Nullable(reader, "PreviousDetailId"),
                                    OrderDetailSequenceNumber = SafeGetInt32(reader, "OrderDetailSequenceNumber"),
                                    Source = reader.IsDBNull("Source") ? string.Empty : reader.GetString("Source"),
                                    IsCanceled = SafeGetInt32(reader, "IsCanceled"),
                                    CancelComment = reader.IsDBNull("CancelComment") ? null : reader.GetString("CancelComment"),
                                    OldOrderDetailSequenceNumber = SafeGetInt32Nullable(reader, "OldOrderDetailSequenceNumber"),
                                    IsApproved = SafeGetInt32Nullable(reader, "IsApproved"),
                                    InvoiceNumber = SafeGetInt32Nullable(reader, "InvoiceNumber"),
                                    PatientAmount = SafeGetDecimalNullable(reader, "PatientAmount")
                                });
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} invoice details for header {HeaderId}", details.Count, headerId);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice details for header {HeaderId}: {ErrorMessage}", headerId, ex.Message);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillingInvoiceDetail>> GetInvoiceDetail(int id)
        {
            try
            {
                var detail = await _context.Database
                    .SqlQueryRaw<BillingInvoiceDetail>("SELECT * FROM [Billing].[dbo].[InvoiceDetail] WHERE [ID] = {0} AND [IsDeleted] = 0", id)
                    .FirstOrDefaultAsync();

                if (detail == null)
                {
                    return NotFound();
                }

                return Ok(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice detail {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<BillingInvoiceDetail>> CreateInvoiceDetail(BillingInvoiceDetail invoiceDetail)
        {
            try
            {
                // Generate new ID
                var maxId = await _context.Database
                    .SqlQueryRaw<int>("SELECT ISNULL(MAX([ID]), 0) FROM [Billing].[dbo].[InvoiceDetail] WHERE [IsDeleted] = 0")
                    .FirstOrDefaultAsync();

                var maxSequence = await _context.Database
                    .SqlQueryRaw<int>("SELECT ISNULL(MAX([OrderDetailSequenceNumber]), 0) FROM [Billing].[dbo].[InvoiceDetail] WHERE [InvoiceHeader] = {0} AND [IsDeleted] = 0", invoiceDetail.InvoiceHeader)
                    .FirstOrDefaultAsync();

                invoiceDetail.Id = maxId + 1;
                invoiceDetail.OrderDetailSequenceNumber = maxSequence + 1;
                invoiceDetail.CreatedDate = DateTime.Now;
                invoiceDetail.ModifiedDate = DateTime.Now;
                invoiceDetail.IsDeleted = 0;
                invoiceDetail.CopyFlag = 0;
                invoiceDetail.IsDoubtfull = 0;
                invoiceDetail.IsCanceled = 0;

                // Insert using raw SQL
                var sql = @"INSERT INTO [Billing].[dbo].[InvoiceDetail] 
                    ([ID], [PrescriptionDate], [PrescribedBy], [MedicationUnit], [MedicationUnitDescription], [Admission], [Patient],
                     [Denomination], [DenominationCode], [DenominationDescription], [DenominationCoeffCode], [DenominationCoeffValue], [DenominationCoeffPrice],
                     [Quantity], [UnitPrice], [NetPrice], [NetUnitPrice], [DifferenceAmount], [DeniedAmount], [Discount], [LumpSum],
                     [ComplementaryAmount], [ComplementaryAmountOtherCurrency], [ComplementaryDifferenceOtherCurrency], [OperatingPhysician],
                     [IsMedicalResultOk], [MedicalResultDate], [RequireApproval], [ApprovalReference], [ApprovalDate], [IsDenied], [ApprovedBy],
                     [DueDate], [ExecutionDate], [InvoiceHeader], [ReferralPhysician], [CostCenter], [ProfitCenter], [PacIndex], [PreInvoiceDetail],
                     [DetailDate], [MainDetailID], [CopyFlag], [DetailDateHelper], [IsDoubtfull], [Procedure], [IsDeleted], [CreatedBy], [ModifiedBy],
                     [CreatedDate], [ModifiedDate], [PreviousDetailID], [OrderDetailSequenceNumber], [Source], [IsCanceled], [CancelComment],
                     [OldOrderDetailSequenceNumber], [IsApproved], [InvoiceNumber], [PatientAmount])
                    VALUES 
                    ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}, {22}, {23}, {24}, {25}, {26}, {27}, {28}, {29}, {30}, {31}, {32}, {33}, {34}, {35}, {36}, {37}, {38}, {39}, {40}, {41}, {42}, {43}, {44}, {45}, {46}, {47}, {48}, {49}, {50}, {51}, {52}, {53}, {54}, {55}, {56}, {57}, {58}, {59}, {60}, {61}, {62}, {63})";

                await _context.Database.ExecuteSqlRawAsync(sql,
                    invoiceDetail.Id, invoiceDetail.PrescriptionDate, invoiceDetail.PrescribedBy, invoiceDetail.MedicationUnit,
                    invoiceDetail.MedicationUnitDescription, invoiceDetail.Admission, invoiceDetail.Patient, invoiceDetail.Denomination,
                    invoiceDetail.DenominationCode, invoiceDetail.DenominationDescription, invoiceDetail.DenominationCoeffCode,
                    invoiceDetail.DenominationCoeffValue, invoiceDetail.DenominationCoeffPrice, invoiceDetail.Quantity, invoiceDetail.UnitPrice,
                    invoiceDetail.NetPrice, invoiceDetail.NetUnitPrice, invoiceDetail.DifferenceAmount, invoiceDetail.DeniedAmount,
                    invoiceDetail.Discount, invoiceDetail.LumpSum, invoiceDetail.ComplementaryAmount, invoiceDetail.ComplementaryAmountOtherCurrency,
                    invoiceDetail.ComplementaryDifferenceOtherCurrency, invoiceDetail.OperatingPhysician, invoiceDetail.IsMedicalResultOk,
                    invoiceDetail.MedicalResultDate, invoiceDetail.RequireApproval, invoiceDetail.ApprovalReference, invoiceDetail.ApprovalDate,
                    invoiceDetail.IsDenied, invoiceDetail.ApprovedBy, invoiceDetail.DueDate, invoiceDetail.ExecutionDate, invoiceDetail.InvoiceHeader,
                    invoiceDetail.ReferralPhysician, invoiceDetail.CostCenter, invoiceDetail.ProfitCenter, invoiceDetail.PacIndex,
                    invoiceDetail.PreInvoiceDetail, invoiceDetail.DetailDate, invoiceDetail.MainDetailId, invoiceDetail.CopyFlag,
                    invoiceDetail.DetailDateHelper, invoiceDetail.IsDoubtfull, invoiceDetail.Procedure, invoiceDetail.IsDeleted,
                    invoiceDetail.CreatedBy, invoiceDetail.ModifiedBy, invoiceDetail.CreatedDate, invoiceDetail.ModifiedDate,
                    invoiceDetail.PreviousDetailId, invoiceDetail.OrderDetailSequenceNumber, invoiceDetail.Source, invoiceDetail.IsCanceled,
                    invoiceDetail.CancelComment, invoiceDetail.OldOrderDetailSequenceNumber, invoiceDetail.IsApproved, invoiceDetail.InvoiceNumber,
                    invoiceDetail.PatientAmount);

                return CreatedAtAction(nameof(GetInvoiceDetail), new { id = invoiceDetail.Id }, invoiceDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice detail");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoiceDetail(int id, BillingInvoiceDetail invoiceDetail)
        {
            if (id != invoiceDetail.Id)
            {
                return BadRequest();
            }

            try
            {
                invoiceDetail.ModifiedDate = DateTime.Now;

                var sql = @"UPDATE [Billing].[dbo].[InvoiceDetail] SET 
                    [PrescriptionDate] = {1}, [PrescribedBy] = {2}, [MedicationUnit] = {3}, [MedicationUnitDescription] = {4}, [Admission] = {5}, [Patient] = {6},
                    [Denomination] = {7}, [DenominationCode] = {8}, [DenominationDescription] = {9}, [DenominationCoeffCode] = {10}, [DenominationCoeffValue] = {11}, [DenominationCoeffPrice] = {12},
                    [Quantity] = {13}, [UnitPrice] = {14}, [NetPrice] = {15}, [NetUnitPrice] = {16}, [DifferenceAmount] = {17}, [DeniedAmount] = {18}, [Discount] = {19}, [LumpSum] = {20},
                    [ComplementaryAmount] = {21}, [ComplementaryAmountOtherCurrency] = {22}, [ComplementaryDifferenceOtherCurrency] = {23}, [OperatingPhysician] = {24},
                    [IsMedicalResultOk] = {25}, [MedicalResultDate] = {26}, [RequireApproval] = {27}, [ApprovalReference] = {28}, [ApprovalDate] = {29}, [IsDenied] = {30}, [ApprovedBy] = {31},
                    [DueDate] = {32}, [ExecutionDate] = {33}, [InvoiceHeader] = {34}, [ReferralPhysician] = {35}, [CostCenter] = {36}, [ProfitCenter] = {37}, [PacIndex] = {38}, [PreInvoiceDetail] = {39},
                    [DetailDate] = {40}, [MainDetailID] = {41}, [DetailDateHelper] = {42}, [Procedure] = {43}, [ModifiedBy] = {44}, [ModifiedDate] = {45},
                    [PreviousDetailID] = {46}, [OrderDetailSequenceNumber] = {47}, [Source] = {48}, [CancelComment] = {49},
                    [OldOrderDetailSequenceNumber] = {50}, [IsApproved] = {51}, [InvoiceNumber] = {52}, [PatientAmount] = {53}
                    WHERE [ID] = {0}";

                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql,
                    id, invoiceDetail.PrescriptionDate, invoiceDetail.PrescribedBy, invoiceDetail.MedicationUnit,
                    invoiceDetail.MedicationUnitDescription, invoiceDetail.Admission, invoiceDetail.Patient, invoiceDetail.Denomination,
                    invoiceDetail.DenominationCode, invoiceDetail.DenominationDescription, invoiceDetail.DenominationCoeffCode,
                    invoiceDetail.DenominationCoeffValue, invoiceDetail.DenominationCoeffPrice, invoiceDetail.Quantity, invoiceDetail.UnitPrice,
                    invoiceDetail.NetPrice, invoiceDetail.NetUnitPrice, invoiceDetail.DifferenceAmount, invoiceDetail.DeniedAmount,
                    invoiceDetail.Discount, invoiceDetail.LumpSum, invoiceDetail.ComplementaryAmount, invoiceDetail.ComplementaryAmountOtherCurrency,
                    invoiceDetail.ComplementaryDifferenceOtherCurrency, invoiceDetail.OperatingPhysician, invoiceDetail.IsMedicalResultOk,
                    invoiceDetail.MedicalResultDate, invoiceDetail.RequireApproval, invoiceDetail.ApprovalReference, invoiceDetail.ApprovalDate,
                    invoiceDetail.IsDenied, invoiceDetail.ApprovedBy, invoiceDetail.DueDate, invoiceDetail.ExecutionDate, invoiceDetail.InvoiceHeader,
                    invoiceDetail.ReferralPhysician, invoiceDetail.CostCenter, invoiceDetail.ProfitCenter, invoiceDetail.PacIndex,
                    invoiceDetail.PreInvoiceDetail, invoiceDetail.DetailDate, invoiceDetail.MainDetailId, invoiceDetail.DetailDateHelper,
                    invoiceDetail.Procedure, invoiceDetail.ModifiedBy, invoiceDetail.ModifiedDate, invoiceDetail.PreviousDetailId,
                    invoiceDetail.OrderDetailSequenceNumber, invoiceDetail.Source, invoiceDetail.CancelComment, invoiceDetail.OldOrderDetailSequenceNumber,
                    invoiceDetail.IsApproved, invoiceDetail.InvoiceNumber, invoiceDetail.PatientAmount);

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice detail {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoiceDetail(int id)
        {
            try
            {
                var sql = "UPDATE [Billing].[dbo].[InvoiceDetail] SET [IsDeleted] = 1, [ModifiedDate] = {1} WHERE [ID] = {0}";
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, id, DateTime.Now);

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice detail {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
