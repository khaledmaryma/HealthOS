using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using LIS.Api.Models;
using System.Data;
using System.Security.Claims;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResidentPatientController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResidentPatientController> _logger;
        private sealed record LoggedInUserInfo(
            int? UserId,
            string? Username,
            string? FullName,
            string? Email,
            int? DepartmentId,
            string? DepartmentName
        );

        public ResidentPatientController(IConfiguration configuration, ILogger<ResidentPatientController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private LoggedInUserInfo GetLoggedInUserInfo()
        {
            string? ReadClaim(params string[] claimTypes)
            {
                foreach (var claimType in claimTypes)
                {
                    var value = User?.FindFirst(claimType)?.Value;
                    if (!string.IsNullOrWhiteSpace(value))
                        return value.Trim();
                }
                return null;
            }

            string? ReadHeader(params string[] headerKeys)
            {
                foreach (var key in headerKeys)
                {
                    var value = Request.Headers[key].ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value.Trim();
                }
                return null;
            }

            static int? ParseNullableInt(string? value)
                => int.TryParse(value, out var parsed) ? parsed : null;

            var userId = ReadClaim(ClaimTypes.NameIdentifier, "userId", "id")
                         ?? ReadHeader("X-User-Id", "x-user-id");
            var username = ReadClaim(ClaimTypes.Name, "username", "preferred_username")
                           ?? ReadHeader("X-Username", "x-username");
            var fullName = ReadClaim("fullName", ClaimTypes.GivenName)
                           ?? ReadHeader("X-FullName", "x-fullname");
            var email = ReadClaim(ClaimTypes.Email, "email")
                        ?? ReadHeader("X-Email", "x-email");
            var departmentId = ReadClaim("DepartmentId", "departmentId")
                               ?? ReadHeader("X-User-Department-Id", "x-user-department-id");
            var departmentName = ReadClaim("DepartmentName", "departmentName", "department")
                                 ?? ReadHeader("X-User-Department", "x-user-department");

            return new LoggedInUserInfo(
                ParseNullableInt(userId),
                username,
                fullName,
                email,
                ParseNullableInt(departmentId),
                departmentName
            );
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResidentPatient>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] bool? isDischarged = null,
            [FromQuery] bool? currentDateOnly = null,
            [FromQuery] string? checkInDateFrom = null,
            [FromQuery] string? checkInDateTo = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                var patients = new List<ResidentPatient>();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var whereConditions = new List<string> { "rp.IsDeleted = 0" };
                    var parameters = new List<SqlParameter>();

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        whereConditions.Add("(rp.PatientName LIKE @search OR rp.AdmissionNumber LIKE @search OR rp.MedicalRecordNumber LIKE @search)");
                        parameters.Add(new SqlParameter("@search", $"%{search}%"));
                    }

                    if (isDischarged.HasValue)
                    {
                        whereConditions.Add("rp.IsDischarged = @isDischarged");
                        parameters.Add(new SqlParameter("@isDischarged", isDischarged.Value));
                    }

                    if (currentDateOnly.HasValue && currentDateOnly.Value)
                    {
                        whereConditions.Add("CAST(rp.CheckInDate AS DATE) = CAST(GETDATE() AS DATE)");
                    }

                    if (!string.IsNullOrWhiteSpace(checkInDateFrom))
                    {
                        whereConditions.Add("CAST(rp.CheckInDate AS DATE) >= @checkInDateFrom");
                        parameters.Add(new SqlParameter("@checkInDateFrom", checkInDateFrom));
                    }

                    if (!string.IsNullOrWhiteSpace(checkInDateTo))
                    {
                        whereConditions.Add("CAST(rp.CheckInDate AS DATE) <= @checkInDateTo");
                        parameters.Add(new SqlParameter("@checkInDateTo", checkInDateTo));
                    }

                    var whereClause = string.Join(" AND ", whereConditions);
                    var offset = (page - 1) * pageSize;

                    var query = $@"
                        SELECT 
                            rp.ID, rp.PatientID, rp.Admission, rp.MRN, rp.AdmissionNumber, rp.PatientName, rp.ArabicFullName,
                            invh.Net, invh.ReceiptNumber, invh.ReceiptDate, invh.Currency,
                            adv.ReceiptNumber AS AdvReceiptNumber, adv.ReceiptDate AS AdvReceiptDate, adv.AdvanceAmount,
                            rp.MedicalRecordNumber, rp.PatientDOB, rp.Age, rp.PatientGender, rp.CheckInDate,
                            rp.CheckInClassID, rp.CheckInClassDescription, rp.MainInsuranceID, rp.MainInsuranceDescription,
                            rp.MainInsuranceClassID, rp.MainInsuranceClassDescription, rp.ReferralPhysicianID, rp.ReferralPhysicianName,
                            rp.AttendingPhysicianID, rp.AttendingPhysicianName, rp.MedicationUnitID, rp.MedicationUnitDescription,
                            rp.RoomID, rp.RoomDescription, rp.BedID, rp.BedDescription, rp.FloorID, rp.FloorDescription,
                            rp.InsuranceID, rp.InsuranceDescription, rp.GuarantorID, rp.GuarantorDescription,
                            rp.CurrencyID, rp.CurrencyDescription, rp.ClassID, rp.ClassDescription,
                            rp.ContextPriceID, rp.ContextPriceDescription, rp.ContextEnumerationID, rp.ContextEnumerationDescription,
                            rp.AdmissionType, rp.AdmissionTypeDescription, rp.Contact, rp.InsuredName, rp.InsuredNameArabic,
                            rp.InsuredPhone, rp.AuxiliaryInsuranceID, rp.AuxiliaryInsuranceDescription,
                            rp.AuxiliaryInsuranceClassID, rp.AuxiliaryInsuranceClassDescription, rp.IsDischarged,
                            rp.DischargeDate, rp.Comment, rp.TotalAdvanceLBP, rp.TotalAdvanceUSD, rp.Diagnostic,
                            rp.VisaNumber, rp.TotalUncollectedAdvanceLBP, rp.TotalUncollectedAdvanceUSD,
                            rp.InvoiceGrossAmountLBP, rp.InvoiceGrossAmountUSD, rp.MainInvoiceNumber,
                            rp.IsPharmDisch, rp.PharmDischDate, rp.IsDeleted, rp.CreatedBy, rp.ModifiedBy,
                            rp.CreatedDate, rp.ModifiedDate, rp.AdmissionSite, rp.IsNersingDischarge,
                            rp.NersingDischargeComment, rp.OldBedID, rp.[Group], rp.PatientShortName,
                            rp.PatientFormattedName, rp.Status, rp.IsRecheckIn, rp.HasInvoices, rp.RequireRegenerate,
                            rp.DiagnosticGroup1, rp.DiagnosticGroup2, rp.DiagnosticGroup3
                        FROM ResidentPatient rp WITH (NOLOCK)
                        LEFT OUTER JOIN [Billing].[dbo].[InvoiceHeader] invh WITH (NOLOCK) ON rp.Admission = invh.Admission AND invh.IsDeleted = 0
                        LEFT OUTER JOIN Advance adv WITH (NOLOCK) ON adv.Admission = rp.Admission AND (adv.IsDeleted = 0 OR adv.IsDeleted IS NULL)
                        WHERE {whereClause}
                        ORDER BY rp.ID DESC
                        OFFSET @offset ROWS
                        FETCH NEXT @pageSize ROWS ONLY";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        command.Parameters.AddWithValue("@offset", offset);
                        command.Parameters.AddWithValue("@pageSize", pageSize);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                patients.Add(MapReaderToPatient(reader));
                            }
                        }
                    }
                }

                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resident patients");
                return StatusCode(500, new { message = "An error occurred while retrieving resident patients", error = ex.Message });
            }
        }

        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCount(
            [FromQuery] string? search = null,
            [FromQuery] bool? isDischarged = null,
            [FromQuery] bool? currentDateOnly = null,
            [FromQuery] string? checkInDateFrom = null,
            [FromQuery] string? checkInDateTo = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var whereConditions = new List<string> { "IsDeleted = 0" };
                    var parameters = new List<SqlParameter>();

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        whereConditions.Add("(PatientName LIKE @search OR AdmissionNumber LIKE @search OR MedicalRecordNumber LIKE @search)");
                        parameters.Add(new SqlParameter("@search", $"%{search}%"));
                    }

                    if (isDischarged.HasValue)
                    {
                        whereConditions.Add("IsDischarged = @isDischarged");
                        parameters.Add(new SqlParameter("@isDischarged", isDischarged.Value));
                    }

                    if (currentDateOnly.HasValue && currentDateOnly.Value)
                    {
                        whereConditions.Add("CAST(CheckInDate AS DATE) = CAST(GETDATE() AS DATE)");
                    }

                    if (!string.IsNullOrWhiteSpace(checkInDateFrom))
                    {
                        whereConditions.Add("CAST(CheckInDate AS DATE) >= @checkInDateFrom");
                        parameters.Add(new SqlParameter("@checkInDateFrom", checkInDateFrom));
                    }

                    if (!string.IsNullOrWhiteSpace(checkInDateTo))
                    {
                        whereConditions.Add("CAST(CheckInDate AS DATE) <= @checkInDateTo");
                        parameters.Add(new SqlParameter("@checkInDateTo", checkInDateTo));
                    }

                    var whereClause = string.Join(" AND ", whereConditions);

                    var query = $@"
                        SELECT COUNT(*)
                        FROM ResidentPatient WITH (NOLOCK)
                        WHERE {whereClause}";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        var count = (int)await command.ExecuteScalarAsync();
                        return Ok(count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient count");
                return StatusCode(500, new { message = "An error occurred while getting patient count", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResidentPatient>> GetById(int id)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        SELECT 
                            rp.ID, rp.PatientID, rp.Admission, rp.MRN, rp.AdmissionNumber, rp.PatientName, rp.ArabicFullName,
                            invh.Net, invh.ReceiptNumber, invh.ReceiptDate, invh.Currency,
                            adv.ReceiptNumber AS AdvReceiptNumber, adv.ReceiptDate AS AdvReceiptDate, adv.AdvanceAmount,
                            rp.MedicalRecordNumber, rp.PatientDOB, rp.Age, rp.PatientGender, rp.CheckInDate,
                            rp.CheckInClassID, rp.CheckInClassDescription, rp.MainInsuranceID, rp.MainInsuranceDescription,
                            rp.MainInsuranceClassID, rp.MainInsuranceClassDescription, rp.ReferralPhysicianID, rp.ReferralPhysicianName,
                            rp.AttendingPhysicianID, rp.AttendingPhysicianName, rp.MedicationUnitID, rp.MedicationUnitDescription,
                            rp.RoomID, rp.RoomDescription, rp.BedID, rp.BedDescription, rp.FloorID, rp.FloorDescription,
                            rp.InsuranceID, rp.InsuranceDescription, rp.GuarantorID, rp.GuarantorDescription,
                            rp.CurrencyID, rp.CurrencyDescription, rp.ClassID, rp.ClassDescription,
                            rp.ContextPriceID, rp.ContextPriceDescription, rp.ContextEnumerationID, rp.ContextEnumerationDescription,
                            rp.AdmissionType, rp.AdmissionTypeDescription, rp.Contact, rp.InsuredName, rp.InsuredNameArabic,
                            rp.InsuredPhone, rp.AuxiliaryInsuranceID, rp.AuxiliaryInsuranceDescription,
                            rp.AuxiliaryInsuranceClassID, rp.AuxiliaryInsuranceClassDescription, rp.IsDischarged,
                            rp.DischargeDate, rp.Comment, rp.TotalAdvanceLBP, rp.TotalAdvanceUSD, rp.Diagnostic,
                            rp.VisaNumber, rp.TotalUncollectedAdvanceLBP, rp.TotalUncollectedAdvanceUSD,
                            rp.InvoiceGrossAmountLBP, rp.InvoiceGrossAmountUSD, rp.MainInvoiceNumber,
                            rp.IsPharmDisch, rp.PharmDischDate, rp.IsDeleted, rp.CreatedBy, rp.ModifiedBy,
                            rp.CreatedDate, rp.ModifiedDate, rp.AdmissionSite, rp.IsNersingDischarge,
                            rp.NersingDischargeComment, rp.OldBedID, rp.[Group], rp.PatientShortName,
                            rp.PatientFormattedName, rp.Status, rp.IsRecheckIn, rp.HasInvoices, rp.RequireRegenerate,
                            rp.DiagnosticGroup1, rp.DiagnosticGroup2, rp.DiagnosticGroup3
                        FROM ResidentPatient rp WITH (NOLOCK)
                        LEFT OUTER JOIN [Billing].[dbo].[InvoiceHeader] invh WITH (NOLOCK) ON rp.Admission = invh.Admission AND invh.IsDeleted = 0
                        LEFT OUTER JOIN Advance adv WITH (NOLOCK) ON adv.Admission = rp.Admission AND (adv.IsDeleted = 0 OR adv.IsDeleted IS NULL)
                        WHERE rp.Admission = @id AND rp.IsDeleted = 0";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Ok(MapReaderToPatient(reader));
                            }
                            else
                            {
                                return NotFound(new { message = $"Patient with ID {id} not found" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving patient with ID {id}");
                return StatusCode(500, new { message = "An error occurred while retrieving patient", error = ex.Message });
            }
        }

        /// <summary>
        /// Unpaid invoices: no receipt, auxiliary insurance Private, not deleted.
        /// When department (query or logged-in) is set, filter by MedicationUnitDescription; when null/empty, all departments.
        /// </summary>
        [HttpGet("unpaid-private-invoices")]
        public async Task<ActionResult<IEnumerable<UnpaidPrivateInvoiceDto>>> GetUnpaidPrivateInvoices([FromQuery] string? departmentName)
        {
            try
            {
                var loggedIn = GetLoggedInUserInfo();
                var dept = !string.IsNullOrWhiteSpace(departmentName)
                    ? departmentName.Trim()
                    : (loggedIn.DepartmentName ?? "").Trim();
                var filterByDepartment = !string.IsNullOrEmpty(dept);

                var admissionConn = _configuration.GetConnectionString("AdmissionConnection");
                if (string.IsNullOrEmpty(admissionConn))
                    return StatusCode(500, new { message = "Database connection strings not configured" });

                var deptClause = filterByDepartment
                    ? "  AND LTRIM(RTRIM(ISNULL(rp.MedicationUnitDescription, ''))) = LTRIM(RTRIM(@DepartmentName))\n"
                    : "";

                var sql = $@"
                            SELECT
                                LTRIM(RTRIM(ISNULL(rp.MedicationUnitDescription, ''))) AS Department,
                                rp.CheckInDate AS CheckInDate,
                                rp.MedicalRecordNumber AS Mrn,
                                rp.AdmissionNumber,
                                rp.PatientName,
                                LTRIM(RTRIM(ISNULL(pt.Phone, ''))) AS PatientPhone,
                                ih.ID AS InvoiceHeaderId,
                                ISNULL(ih.Net, 0) AS InvoiceNet,
                                ISNULL(advSum.PaidAdvance, 0) AS PaidAdvance,
                                CAST(ISNULL(ih.Net, 0) - ISNULL(advSum.PaidAdvance, 0) AS DECIMAL(18, 2)) AS RestToPay,
                                ih.Currency AS Currency,
                                ISNULL(ih.ReceivedLBP, 0) AS ReceivedLBP,
                                ISNULL(ih.ReceivedUSD, 0) AS ReceivedUSD
                            FROM [Billing].[dbo].[InvoiceHeader] ih WITH (NOLOCK)
                            INNER JOIN [Admission].[dbo].[ResidentPatient] rp WITH (NOLOCK) ON rp.Admission = ih.Admission
                            LEFT JOIN [HospitalDefinition].[dbo].[Patient] pt WITH (NOLOCK) ON pt.ID = rp.PatientID
                            OUTER APPLY (
                                SELECT ISNULL(SUM(a.AdvanceAmount), 0) AS PaidAdvance
                                FROM [Admission].[dbo].[Advance] a WITH (NOLOCK)
                                WHERE a.Admission = ih.Admission AND (a.IsDeleted = 0 OR a.IsDeleted IS NULL AND a.ReceiptNumber is not null)
                            ) advSum
                            WHERE ih.IsDeleted = 0
                              AND (ih.ReceiptNumber IS NULL OR LTRIM(RTRIM(ih.ReceiptNumber)) = '')
                              AND  ih.AccountID = 5
                              AND rp.IsDeleted = 0 AND Year(rp.CheckInDate) >= 2025
                            {deptClause}ORDER BY rp.PatientName, ih.ID";

                var rows = new List<UnpaidPrivateInvoiceDto>();
                await using (var conn = new SqlConnection(admissionConn))
                {
                    await conn.OpenAsync();
                    await using var cmd = new SqlCommand(sql, conn);
                    if (filterByDepartment)
                        cmd.Parameters.AddWithValue("@DepartmentName", dept);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        rows.Add(MapUnpaidPrivateInvoiceReader(reader));
                }

                return Ok(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unpaid private invoices");
                return StatusCode(500, new { message = "An error occurred while loading unpaid invoices", error = ex.Message });
            }
        }

        /// <summary>Updates ReceivedLBP / ReceivedUSD on an invoice that still matches unpaid-private rules (no receipt, Private auxiliary, not deleted).</summary>
        [HttpPatch("unpaid-private-invoices/{invoiceHeaderId:int}/received")]
        public async Task<ActionResult<UnpaidPrivateInvoiceDto>> PatchUnpaidPrivateInvoiceReceived(
            int invoiceHeaderId,
            [FromBody] UpdateUnpaidPrivateInvoiceReceivedRequest body)
        {
            if (body == null)
                return BadRequest(new { message = "Request body is required." });

            var billingConn = _configuration.GetConnectionString("BillingConnection");
            if (string.IsNullOrEmpty(billingConn))
                return StatusCode(500, new { message = "Billing database connection not configured" });

            const string updateSql = @"
UPDATE ih
SET ReceivedLBP = @ReceivedLBP,
    ReceivedUSD = @ReceivedUSD
FROM [Billing].[dbo].[InvoiceHeader] ih
INNER JOIN [Admission].[dbo].[ResidentPatient] rp WITH (NOLOCK) ON rp.Admission = ih.Admission AND rp.IsDeleted = 0
WHERE ih.ID = @InvoiceHeaderId
  AND ih.IsDeleted = 0
  AND (ih.ReceiptNumber IS NULL OR LTRIM(RTRIM(ih.ReceiptNumber)) = '')
  AND LOWER(LTRIM(ISNULL(rp.AuxiliaryInsuranceDescription, ''))) = N'private'";

            try
            {
                await using (var conn = new SqlConnection(billingConn))
                {
                    await conn.OpenAsync();
                    await using (var cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceHeaderId", invoiceHeaderId);
                        cmd.Parameters.AddWithValue("@ReceivedLBP", body.ReceivedLbp);
                        cmd.Parameters.AddWithValue("@ReceivedUSD", body.ReceivedUsd);
                        var affected = await cmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                            return NotFound(new { message = "Invoice not found or not eligible for this update (receipt issued, wrong insurance, or deleted)." });
                    }

                    var row = await GetUnpaidPrivateInvoiceByIdAsync(conn, invoiceHeaderId);
                    if (row == null)
                    {
                        _logger.LogWarning("Patch unpaid invoice {Id}: update succeeded but re-select returned no row", invoiceHeaderId);
                        return StatusCode(500, new { message = "Saved but failed to reload invoice row." });
                    }
                    return Ok(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unpaid private invoice {Id} received amounts", invoiceHeaderId);
                return StatusCode(500, new { message = "An error occurred while saving receipt amounts", error = ex.Message });
            }
        }

        private static UnpaidPrivateInvoiceDto MapUnpaidPrivateInvoiceReader(SqlDataReader reader)
        {
            var checkInOrd = reader.GetOrdinal("CheckInDate");
            return new UnpaidPrivateInvoiceDto
            {
                Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? "" : reader.GetString(reader.GetOrdinal("Department")),
                CheckInDate = reader.IsDBNull(checkInOrd) ? null : reader.GetDateTime(checkInOrd),
                Mrn = reader.IsDBNull(reader.GetOrdinal("Mrn")) ? "" : reader.GetString(reader.GetOrdinal("Mrn")),
                AdmissionNumber = reader.IsDBNull(reader.GetOrdinal("AdmissionNumber")) ? "" : reader.GetString(reader.GetOrdinal("AdmissionNumber")),
                PatientName = reader.IsDBNull(reader.GetOrdinal("PatientName")) ? "" : reader.GetString(reader.GetOrdinal("PatientName")),
                PatientPhone = reader.IsDBNull(reader.GetOrdinal("PatientPhone")) ? "" : reader.GetString(reader.GetOrdinal("PatientPhone")),
                InvoiceHeaderId = reader.GetInt32(reader.GetOrdinal("InvoiceHeaderId")),
                InvoiceNet = reader.GetDecimal(reader.GetOrdinal("InvoiceNet")),
                PaidAdvance = reader.GetDecimal(reader.GetOrdinal("PaidAdvance")),
                RestToPay = reader.GetDecimal(reader.GetOrdinal("RestToPay")),
                Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? null : reader.GetString(reader.GetOrdinal("Currency")),
                ReceivedLbp = reader.GetDecimal(reader.GetOrdinal("ReceivedLBP")),
                ReceivedUsd = reader.GetDecimal(reader.GetOrdinal("ReceivedUSD"))
            };
        }

        private static async Task<UnpaidPrivateInvoiceDto?> GetUnpaidPrivateInvoiceByIdAsync(SqlConnection conn, int invoiceHeaderId)
        {
            const string sql = @"
SELECT
    LTRIM(RTRIM(ISNULL(rp.MedicationUnitDescription, ''))) AS Department,
    rp.CheckInDate AS CheckInDate,
    rp.MedicalRecordNumber AS Mrn,
    rp.AdmissionNumber,
    rp.PatientName,
    LTRIM(RTRIM(ISNULL(pt.Phone, ''))) AS PatientPhone,
    ih.ID AS InvoiceHeaderId,
    ISNULL(ih.Net, 0) AS InvoiceNet,
    ISNULL(advSum.PaidAdvance, 0) AS PaidAdvance,
    CAST(ISNULL(ih.Net, 0) - ISNULL(advSum.PaidAdvance, 0) AS DECIMAL(18, 2)) AS RestToPay,
    ih.Currency AS Currency,
    ISNULL(ih.ReceivedLBP, 0) AS ReceivedLBP,
    ISNULL(ih.ReceivedUSD, 0) AS ReceivedUSD
FROM [Billing].[dbo].[InvoiceHeader] ih WITH (NOLOCK)
INNER JOIN [Admission].[dbo].[ResidentPatient] rp WITH (NOLOCK) ON rp.Admission = ih.Admission
LEFT JOIN [HospitalDefinition].[dbo].[Patient] pt WITH (NOLOCK) ON pt.ID = rp.PatientID
OUTER APPLY (
    SELECT ISNULL(SUM(a.AdvanceAmount), 0) AS PaidAdvance
    FROM [Admission].[dbo].[Advance] a WITH (NOLOCK)
    WHERE a.Admission = ih.Admission AND (a.IsDeleted = 0 OR a.IsDeleted IS NULL)
) advSum
WHERE ih.ID = @InvoiceHeaderId
  AND ih.IsDeleted = 0
  AND (ih.ReceiptNumber IS NULL OR LTRIM(RTRIM(ih.ReceiptNumber)) = '')
  AND LOWER(LTRIM(ISNULL(rp.AuxiliaryInsuranceDescription, ''))) = N'private'
  AND rp.IsDeleted = 0";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@InvoiceHeaderId", invoiceHeaderId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;
            return MapUnpaidPrivateInvoiceReader(reader);
        }

        public sealed class UpdateUnpaidPrivateInvoiceReceivedRequest
        {
            public decimal ReceivedLbp { get; set; }
            public decimal ReceivedUsd { get; set; }
        }

        public sealed class UnpaidPrivateInvoiceDto
        {
            public string Department { get; set; } = string.Empty;
            public DateTime? CheckInDate { get; set; }
            public string Mrn { get; set; } = string.Empty;
            public string AdmissionNumber { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string PatientPhone { get; set; } = string.Empty;
            public int InvoiceHeaderId { get; set; }
            public decimal InvoiceNet { get; set; }
            public decimal PaidAdvance { get; set; }
            public decimal RestToPay { get; set; }
            public string? Currency { get; set; }
            public decimal ReceivedLbp { get; set; }
            public decimal ReceivedUsd { get; set; }
        }

        private static decimal? SafeGetDecimal(SqlDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
            }
            catch (IndexOutOfRangeException) { return null; }
        }

        private static string? SafeGetString(SqlDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException) { return null; }
        }

        private static DateTime? SafeGetDateTime(SqlDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch (IndexOutOfRangeException) { return null; }
        }

        private ResidentPatient MapReaderToPatient(SqlDataReader reader)
        {
            return new ResidentPatient
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                Admission = reader.GetInt32(reader.GetOrdinal("Admission")),
                MRN = reader.GetInt32(reader.GetOrdinal("MRN")),
                AdmissionNumber = reader.GetString(reader.GetOrdinal("AdmissionNumber")),
                PatientName = reader.GetString(reader.GetOrdinal("PatientName")),
                ArabicFullName = reader.IsDBNull(reader.GetOrdinal("ArabicFullName")) ? null : reader.GetString(reader.GetOrdinal("ArabicFullName")),
                MedicalRecordNumber = reader.GetString(reader.GetOrdinal("MedicalRecordNumber")),
                PatientDOB = reader.GetDateTime(reader.GetOrdinal("PatientDOB")),
                Age = reader.IsDBNull(reader.GetOrdinal("Age")) ? null : reader.GetInt32(reader.GetOrdinal("Age")),
                PatientGender = reader.GetString(reader.GetOrdinal("PatientGender")),
                CheckInDate = reader.GetDateTime(reader.GetOrdinal("CheckInDate")),
                CheckInClassID = reader.GetInt16(reader.GetOrdinal("CheckInClassID")),
                CheckInClassDescription = reader.GetString(reader.GetOrdinal("CheckInClassDescription")),
                MainInsuranceID = reader.GetInt32(reader.GetOrdinal("MainInsuranceID")),
                MainInsuranceDescription = reader.GetString(reader.GetOrdinal("MainInsuranceDescription")),
                MainInsuranceClassID = reader.GetInt32(reader.GetOrdinal("MainInsuranceClassID")),
                MainInsuranceClassDescription = reader.GetString(reader.GetOrdinal("MainInsuranceClassDescription")),
                ReferralPhysicianID = reader.GetInt32(reader.GetOrdinal("ReferralPhysicianID")),
                ReferralPhysicianName = reader.GetString(reader.GetOrdinal("ReferralPhysicianName")),
                AttendingPhysicianID = reader.IsDBNull(reader.GetOrdinal("AttendingPhysicianID")) ? null : reader.GetInt32(reader.GetOrdinal("AttendingPhysicianID")),
                AttendingPhysicianName = reader.IsDBNull(reader.GetOrdinal("AttendingPhysicianName")) ? null : reader.GetString(reader.GetOrdinal("AttendingPhysicianName")),
                MedicationUnitID = reader.GetInt32(reader.GetOrdinal("MedicationUnitID")),
                MedicationUnitDescription = reader.GetString(reader.GetOrdinal("MedicationUnitDescription")),
                RoomID = reader.IsDBNull(reader.GetOrdinal("RoomID")) ? null : reader.GetInt32(reader.GetOrdinal("RoomID")),
                RoomDescription = reader.IsDBNull(reader.GetOrdinal("RoomDescription")) ? null : reader.GetString(reader.GetOrdinal("RoomDescription")),
                BedID = reader.IsDBNull(reader.GetOrdinal("BedID")) ? null : reader.GetInt32(reader.GetOrdinal("BedID")),
                BedDescription = reader.IsDBNull(reader.GetOrdinal("BedDescription")) ? null : reader.GetString(reader.GetOrdinal("BedDescription")),
                FloorID = reader.IsDBNull(reader.GetOrdinal("FloorID")) ? null : reader.GetInt32(reader.GetOrdinal("FloorID")),
                FloorDescription = reader.IsDBNull(reader.GetOrdinal("FloorDescription")) ? null : reader.GetString(reader.GetOrdinal("FloorDescription")),
                InsuranceID = reader.GetInt32(reader.GetOrdinal("InsuranceID")),
                InsuranceDescription = reader.GetString(reader.GetOrdinal("InsuranceDescription")),
                GuarantorID = reader.GetInt32(reader.GetOrdinal("GuarantorID")),
                GuarantorDescription = reader.GetString(reader.GetOrdinal("GuarantorDescription")),
                CurrencyID = reader.GetInt32(reader.GetOrdinal("CurrencyID")),
                CurrencyDescription = reader.GetString(reader.GetOrdinal("CurrencyDescription")),
                ClassID = reader.GetInt16(reader.GetOrdinal("ClassID")),
                ClassDescription = reader.GetString(reader.GetOrdinal("ClassDescription")),
                ContextPriceID = reader.GetInt32(reader.GetOrdinal("ContextPriceID")),
                ContextPriceDescription = reader.GetString(reader.GetOrdinal("ContextPriceDescription")),
                ContextEnumerationID = reader.GetInt32(reader.GetOrdinal("ContextEnumerationID")),
                ContextEnumerationDescription = reader.GetString(reader.GetOrdinal("ContextEnumerationDescription")),
                AdmissionType = reader.GetInt16(reader.GetOrdinal("AdmissionType")),
                AdmissionTypeDescription = reader.GetString(reader.GetOrdinal("AdmissionTypeDescription")),
                Contact = reader.IsDBNull(reader.GetOrdinal("Contact")) ? null : reader.GetString(reader.GetOrdinal("Contact")),
                InsuredName = reader.IsDBNull(reader.GetOrdinal("InsuredName")) ? null : reader.GetString(reader.GetOrdinal("InsuredName")),
                InsuredNameArabic = reader.IsDBNull(reader.GetOrdinal("InsuredNameArabic")) ? null : reader.GetString(reader.GetOrdinal("InsuredNameArabic")),
                InsuredPhone = reader.IsDBNull(reader.GetOrdinal("InsuredPhone")) ? null : reader.GetString(reader.GetOrdinal("InsuredPhone")),
                AuxiliaryInsuranceID = reader.IsDBNull(reader.GetOrdinal("AuxiliaryInsuranceID")) ? null : reader.GetInt32(reader.GetOrdinal("AuxiliaryInsuranceID")),
                AuxiliaryInsuranceDescription = reader.IsDBNull(reader.GetOrdinal("AuxiliaryInsuranceDescription")) ? null : reader.GetString(reader.GetOrdinal("AuxiliaryInsuranceDescription")),
                AuxiliaryInsuranceClassID = reader.IsDBNull(reader.GetOrdinal("AuxiliaryInsuranceClassID")) ? null : reader.GetInt32(reader.GetOrdinal("AuxiliaryInsuranceClassID")),
                AuxiliaryInsuranceClassDescription = reader.IsDBNull(reader.GetOrdinal("AuxiliaryInsuranceClassDescription")) ? null : reader.GetString(reader.GetOrdinal("AuxiliaryInsuranceClassDescription")),
                IsDischarged = reader.GetBoolean(reader.GetOrdinal("IsDischarged")),
                DischargeDate = reader.IsDBNull(reader.GetOrdinal("DischargeDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DischargeDate")),
                Comment = reader.IsDBNull(reader.GetOrdinal("Comment")) ? null : reader.GetString(reader.GetOrdinal("Comment")),
                TotalAdvanceLBP = reader.IsDBNull(reader.GetOrdinal("TotalAdvanceLBP")) ? null : reader.GetDecimal(reader.GetOrdinal("TotalAdvanceLBP")),
                TotalAdvanceUSD = reader.IsDBNull(reader.GetOrdinal("TotalAdvanceUSD")) ? null : reader.GetDecimal(reader.GetOrdinal("TotalAdvanceUSD")),
                Diagnostic = reader.IsDBNull(reader.GetOrdinal("Diagnostic")) ? null : reader.GetString(reader.GetOrdinal("Diagnostic")),
                VisaNumber = reader.IsDBNull(reader.GetOrdinal("VisaNumber")) ? null : reader.GetString(reader.GetOrdinal("VisaNumber")),
                TotalUncollectedAdvanceLBP = reader.IsDBNull(reader.GetOrdinal("TotalUncollectedAdvanceLBP")) ? null : reader.GetDecimal(reader.GetOrdinal("TotalUncollectedAdvanceLBP")),
                TotalUncollectedAdvanceUSD = reader.IsDBNull(reader.GetOrdinal("TotalUncollectedAdvanceUSD")) ? null : reader.GetDecimal(reader.GetOrdinal("TotalUncollectedAdvanceUSD")),
                InvoiceGrossAmountLBP = reader.IsDBNull(reader.GetOrdinal("InvoiceGrossAmountLBP")) ? null : reader.GetDecimal(reader.GetOrdinal("InvoiceGrossAmountLBP")),
                InvoiceGrossAmountUSD = reader.IsDBNull(reader.GetOrdinal("InvoiceGrossAmountUSD")) ? null : reader.GetDecimal(reader.GetOrdinal("InvoiceGrossAmountUSD")),
                MainInvoiceNumber = reader.IsDBNull(reader.GetOrdinal("MainInvoiceNumber")) ? null : reader.GetString(reader.GetOrdinal("MainInvoiceNumber")),
                IsPharmDisch = reader.GetBoolean(reader.GetOrdinal("IsPharmDisch")),
                PharmDischDate = reader.IsDBNull(reader.GetOrdinal("PharmDischDate")) ? null : reader.GetDateTime(reader.GetOrdinal("PharmDischDate")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ModifiedBy")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                AdmissionSite = reader.IsDBNull(reader.GetOrdinal("AdmissionSite")) ? null : reader.GetInt32(reader.GetOrdinal("AdmissionSite")),
                IsNersingDischarge = reader.GetBoolean(reader.GetOrdinal("IsNersingDischarge")),
                NersingDischargeComment = reader.IsDBNull(reader.GetOrdinal("NersingDischargeComment")) ? null : reader.GetString(reader.GetOrdinal("NersingDischargeComment")),
                OldBedID = reader.IsDBNull(reader.GetOrdinal("OldBedID")) ? null : reader.GetInt32(reader.GetOrdinal("OldBedID")),
                Group = reader.IsDBNull(reader.GetOrdinal("Group")) ? null : reader.GetInt32(reader.GetOrdinal("Group")),
                PatientShortName = reader.IsDBNull(reader.GetOrdinal("PatientShortName")) ? null : reader.GetString(reader.GetOrdinal("PatientShortName")),
                PatientFormattedName = reader.IsDBNull(reader.GetOrdinal("PatientFormattedName")) ? null : reader.GetString(reader.GetOrdinal("PatientFormattedName")),
                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetByte(reader.GetOrdinal("Status")),
                IsRecheckIn = reader.GetBoolean(reader.GetOrdinal("IsRecheckIn")),
                HasInvoices = reader.IsDBNull(reader.GetOrdinal("HasInvoices")) ? null : reader.GetBoolean(reader.GetOrdinal("HasInvoices")),
                RequireRegenerate = reader.IsDBNull(reader.GetOrdinal("RequireRegenerate")) ? null : reader.GetBoolean(reader.GetOrdinal("RequireRegenerate")),
                DiagnosticGroup1 = reader.IsDBNull(reader.GetOrdinal("DiagnosticGroup1")) ? null : reader.GetString(reader.GetOrdinal("DiagnosticGroup1")),
                DiagnosticGroup2 = reader.IsDBNull(reader.GetOrdinal("DiagnosticGroup2")) ? null : reader.GetString(reader.GetOrdinal("DiagnosticGroup2")),
                DiagnosticGroup3 = reader.IsDBNull(reader.GetOrdinal("DiagnosticGroup3")) ? null : reader.GetString(reader.GetOrdinal("DiagnosticGroup3")),
                InvTotal = SafeGetDecimal(reader, "Net"),
                ReceiptNumber = SafeGetString(reader, "ReceiptNumber"),
                ReceiptDate = SafeGetDateTime(reader, "ReceiptDate"),
                Currency = SafeGetString(reader, "Currency"),
                AdvReceiptNumber = SafeGetString(reader, "AdvReceiptNumber"),
                AdvReceiptDate = SafeGetDateTime(reader, "AdvReceiptDate"),
                AdvanceAmount = SafeGetDecimal(reader, "AdvanceAmount")
            };
        }

        /// <summary>
        /// Get report of all patients grouped by department showing MRN, PatientName, AdmissionNumber, InvoiceTotal, ReceiptLBP, ReceiptUSD.
        /// Includes invoice-level rows with ReceiptNumber, hasAdvance for Transfer to Cashier.
        /// </summary>
        /// <param name="checkInDateFrom">Filter by check-in date from (yyyy-MM-dd). Defaults to today.</param>
        /// <param name="checkInDateTo">Filter by check-in date to (yyyy-MM-dd). Defaults to today.</param>
        [HttpGet("report/by-department")]
        public async Task<ActionResult> GetReportByDepartment([FromQuery] string? checkInDateFrom, [FromQuery] string? checkInDateTo, [FromQuery] string? departmentName)
        {
            try
            {
                var admissionConnectionString = _configuration.GetConnectionString("AdmissionConnection");
                var billingConnectionString = _configuration.GetConnectionString("BillingConnection");
                var loggedInUser = GetLoggedInUserInfo();
                string? logedInDep = string.IsNullOrWhiteSpace(departmentName)
                    ? null
                    : departmentName.Trim();
                var logedInDepID = loggedInUser.DepartmentId?.ToString() ?? "";
                if (string.IsNullOrEmpty(billingConnectionString))
                {
                    var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrEmpty(defaultConnection))
                        billingConnectionString = defaultConnection.Replace("Database=LIS", "Database=Billing");
                }
                if (string.IsNullOrEmpty(admissionConnectionString) || string.IsNullOrEmpty(billingConnectionString))
                    return StatusCode(500, new { message = "Database connection strings not configured" });

                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var dateFrom = string.IsNullOrWhiteSpace(checkInDateFrom) ? today : checkInDateFrom.Trim();
                var dateTo = string.IsNullOrWhiteSpace(checkInDateTo) ? today : checkInDateTo.Trim();
                var reportData = new List<DepartmentReportItem>();
                var query = @"
                    SELECT rp.MedicationUnitDescription AS Department, rp.MedicalRecordNumber AS MRN, rp.PatientName, rp.AdmissionNumber,
                        rp.Admission,
                        ih.ID AS InvoiceHeaderId, ih.ReceiptNumber, ih.Net AS InvoiceTotal, ISNULL(ih.ReceivedLBP, 0) AS ReceiptLBP, ISNULL(ih.ReceivedUSD, 0) AS ReceiptUSD,
                        ih.SequenceNumber, ih.CurrencyId,
                        CAST(NULL AS INT) AS AdvanceId, CAST(NULL AS NVARCHAR(50)) AS AdvanceReceiptNumber, CAST(0 AS DECIMAL(18,2)) AS AdvanceAmount,
                        CASE WHEN EXISTS(SELECT 1 FROM [Admission].[dbo].[Advance] a WITH (NOLOCK) WHERE a.Admission = rp.Admission AND (a.ReceiptNumber IS NULL OR a.ReceiptNumber = '') AND (a.IsDeleted = 0 OR a.IsDeleted IS NULL)) THEN 1 ELSE 0 END AS HasUnreceiptedAdvance
                    FROM [Admission].[dbo].[ResidentPatient] rp WITH (NOLOCK)
                    INNER JOIN [Billing].[dbo].[InvoiceHeader] ih WITH (NOLOCK) ON rp.Admission = ih.Admission AND ih.IsDeleted = 0
                    WHERE rp.IsDeleted = 0 AND CAST(rp.CheckInDate AS DATE) >= @DateFrom AND CAST(rp.CheckInDate AS DATE) <= @DateTo
                   
                    ORDER BY rp.MedicationUnitDescription, rp.PatientName, rp.AdmissionNumber, ih.ID";

                using (var connection = new SqlConnection(admissionConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DateFrom", dateFrom);
                        command.Parameters.AddWithValue("@DateTo", dateTo);
                        //command.Parameters.AddWithValue("@Dep", logedInDep);
                        using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var dept = reader.IsDBNull(reader.GetOrdinal("Department")) ? "Unknown" : reader.GetString(reader.GetOrdinal("Department"));
                            var mrn = reader.IsDBNull(reader.GetOrdinal("MRN")) ? "" : reader.GetString(reader.GetOrdinal("MRN"));
                            var pname = reader.IsDBNull(reader.GetOrdinal("PatientName")) ? "" : reader.GetString(reader.GetOrdinal("PatientName"));
                            var admNum = reader.IsDBNull(reader.GetOrdinal("AdmissionNumber")) ? "" : reader.GetString(reader.GetOrdinal("AdmissionNumber"));
                            var adm = reader.IsDBNull(reader.GetOrdinal("Admission")) ? 0 : reader.GetInt32(reader.GetOrdinal("Admission"));
                            var invId = reader.IsDBNull(reader.GetOrdinal("InvoiceHeaderId")) ? 0 : reader.GetInt32(reader.GetOrdinal("InvoiceHeaderId"));
                            var receiptNum = reader.IsDBNull(reader.GetOrdinal("ReceiptNumber")) ? null : reader.GetString(reader.GetOrdinal("ReceiptNumber"));
                            var invTotal = reader.IsDBNull(reader.GetOrdinal("InvoiceTotal")) ? 0m : reader.GetDecimal(reader.GetOrdinal("InvoiceTotal"));
                            var recLBP = reader.IsDBNull(reader.GetOrdinal("ReceiptLBP")) ? 0m : reader.GetDecimal(reader.GetOrdinal("ReceiptLBP"));
                            var recUSD = reader.IsDBNull(reader.GetOrdinal("ReceiptUSD")) ? 0m : reader.GetDecimal(reader.GetOrdinal("ReceiptUSD"));
                            var seqNum = reader.IsDBNull(reader.GetOrdinal("SequenceNumber")) ? 0 : reader.GetInt32(reader.GetOrdinal("SequenceNumber"));
                            var currencyId = reader.IsDBNull(reader.GetOrdinal("CurrencyId")) ? 2 : reader.GetInt32(reader.GetOrdinal("CurrencyId"));
                            var hasAdv = reader.IsDBNull(reader.GetOrdinal("HasUnreceiptedAdvance")) ? false : reader.GetInt32(reader.GetOrdinal("HasUnreceiptedAdvance")) == 1;

                            reportData.Add(new DepartmentReportItem
                            {
                                Department = dept,
                                MRN = mrn,
                                PatientName = pname,
                                AdmissionNumber = admNum,
                                Admission = adm,
                                InvoiceHeaderId = invId,
                                AdvanceId = null,
                                ReceiptNumber = string.IsNullOrEmpty(receiptNum) ? null : receiptNum,
                                InvoiceTotal = invTotal,
                                ReceiptLBP = recLBP,
                                ReceiptUSD = recUSD,
                                SequenceNumber = seqNum,
                                CurrencyId = currencyId,
                                AdvanceAmount = 0m,
                                AdvanceReceiptNumber = null,
                                HasAdvance = hasAdv,
                                IsAdvanceRow = false
                            });
                        }
                    }
                    }

                    var advQuery = @"SELECT a.ID AS AdvanceId, a.Admission, a.AdvanceAmount, a.ReceiptNumber AS AdvanceReceiptNumber,
                        ISNULL(a.ReceivedLBP, 0) AS ReceivedLBP, ISNULL(a.ReceivedUSD, 0) AS ReceivedUSD,
                        rp.MedicationUnitDescription AS Department, rp.MedicalRecordNumber AS MRN, rp.PatientName, rp.AdmissionNumber,
                        (SELECT TOP 1 ID FROM [Billing].[dbo].[InvoiceHeader] WITH (NOLOCK) WHERE Admission = a.Admission AND IsDeleted = 0) AS InvoiceHeaderId,
                        (SELECT TOP 1 SequenceNumber FROM [Billing].[dbo].[InvoiceHeader] WITH (NOLOCK) WHERE Admission = a.Admission AND IsDeleted = 0) AS SequenceNumber
                        FROM [Admission].[dbo].[Advance] a WITH (NOLOCK)
                        INNER JOIN [Admission].[dbo].[ResidentPatient] rp WITH (NOLOCK) ON rp.Admission = a.Admission
                        WHERE (a.ReceiptNumber IS NULL OR a.ReceiptNumber = '') AND (a.IsDeleted = 0 OR a.IsDeleted IS NULL) AND rp.IsDeleted = 0
                        AND CAST(rp.CheckInDate AS DATE) >= @AdvDateFrom AND CAST(rp.CheckInDate AS DATE) <= @AdvDateTo";
                    using (var advCmd = new SqlCommand(advQuery, connection))
                    {
                        advCmd.Parameters.AddWithValue("@AdvDateFrom", dateFrom);
                        advCmd.Parameters.AddWithValue("@AdvDateTo", dateTo);
                    using (var advRdr = await advCmd.ExecuteReaderAsync())
                    {
                        while (await advRdr.ReadAsync())
                        {
                            var advReceiptNum = advRdr.IsDBNull(advRdr.GetOrdinal("AdvanceReceiptNumber")) ? null : advRdr.GetString(advRdr.GetOrdinal("AdvanceReceiptNumber"));
                            var recLBP = advRdr.IsDBNull(advRdr.GetOrdinal("ReceivedLBP")) ? 0m : advRdr.GetDecimal(advRdr.GetOrdinal("ReceivedLBP"));
                            var recUSD = advRdr.IsDBNull(advRdr.GetOrdinal("ReceivedUSD")) ? 0m : advRdr.GetDecimal(advRdr.GetOrdinal("ReceivedUSD"));
                            reportData.Add(new DepartmentReportItem
                            {
                                Department = advRdr.IsDBNull(advRdr.GetOrdinal("Department")) ? "Unknown" : advRdr.GetString(advRdr.GetOrdinal("Department")),
                                MRN = advRdr.IsDBNull(advRdr.GetOrdinal("MRN")) ? "" : advRdr.GetString(advRdr.GetOrdinal("MRN")),
                                PatientName = advRdr.IsDBNull(advRdr.GetOrdinal("PatientName")) ? "" : advRdr.GetString(advRdr.GetOrdinal("PatientName")),
                                AdmissionNumber = advRdr.IsDBNull(advRdr.GetOrdinal("AdmissionNumber")) ? "" : advRdr.GetString(advRdr.GetOrdinal("AdmissionNumber")),
                                Admission = advRdr.GetInt32(advRdr.GetOrdinal("Admission")),
                                InvoiceHeaderId = advRdr.IsDBNull(advRdr.GetOrdinal("InvoiceHeaderId")) ? 0 : advRdr.GetInt32(advRdr.GetOrdinal("InvoiceHeaderId")),
                                AdvanceId = advRdr.GetInt32(advRdr.GetOrdinal("AdvanceId")),
                                ReceiptNumber = string.IsNullOrEmpty(advReceiptNum) ? null : advReceiptNum,
                                InvoiceTotal = advRdr.GetDecimal(advRdr.GetOrdinal("AdvanceAmount")),
                                ReceiptLBP = recLBP,
                                ReceiptUSD = recUSD,
                                SequenceNumber = advRdr.IsDBNull(advRdr.GetOrdinal("SequenceNumber")) ? 0 : advRdr.GetInt32(advRdr.GetOrdinal("SequenceNumber")),
                                CurrencyId = 2,
                                AdvanceAmount = advRdr.GetDecimal(advRdr.GetOrdinal("AdvanceAmount")),
                                AdvanceReceiptNumber = advReceiptNum,
                                HasAdvance = false,
                                IsAdvanceRow = true
                            });
                        }
                    }
                    }

                    // When admission has an advance row, hide invoice rows for that admission and exclude from totals
                    var admissionsWithAdvance = reportData.Where(r => r.IsAdvanceRow).Select(r => r.Admission).ToHashSet();
                    reportData = reportData
                        .Where(r => r.IsAdvanceRow || !admissionsWithAdvance.Contains(r.Admission))
                        .ToList();

                var groupedReport = reportData
                    .GroupBy(x => x.Department)
                    .Select(g => new
                    {
                        Department = g.Key,
                        Patients = g.Select(p => new
                        {
                            p.MRN,
                            p.PatientName,
                            p.AdmissionNumber,
                            p.InvoiceHeaderId,
                            p.AdvanceId,
                            p.ReceiptNumber,
                            p.InvoiceTotal,
                            p.ReceiptLBP,
                            p.ReceiptUSD,
                            p.HasAdvance,
                            p.IsAdvanceRow,
                            canSelect = string.IsNullOrEmpty(p.ReceiptNumber)
                        }).ToList(),
                        DepartmentTotal = new
                        {
                            InvoiceTotal = g.Sum(p => p.InvoiceTotal),
                            ReceiptLBP = g.Sum(p => p.ReceiptLBP),
                            ReceiptUSD = g.Sum(p => p.ReceiptUSD),
                            PatientCount = g.Select(p => new { p.MRN, p.AdmissionNumber }).Distinct().Count()
                        }
                    })
                    .OrderBy(x => x.Department)
                    .ToList();

                return Ok(groupedReport);
            }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating department report");
                return StatusCode(500, new { message = "An error occurred while generating the report", error = ex.Message });
            }
        }

        /// <summary>Transfer selected department report items to current cashier.</summary>
        [HttpPost("report/transfer-to-cashier")]
        public async Task<ActionResult> TransferToCashier([FromBody] TransferToCashierRequest request)
        {
            if (request?.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "No items selected for transfer" });
            decimal exchangeRate = 90000;
            var admissionConnStr = _configuration.GetConnectionString("AdmissionConnection");
            var billingConnStr = _configuration.GetConnectionString("BillingConnection")
                ?? _configuration.GetConnectionString("DefaultConnection")?.Replace("Database=LIS", "Database=Billing");
            var finConnStr = _configuration.GetConnectionString("FinancialConnection");
            if (string.IsNullOrEmpty(finConnStr))
            {
                var billingConn = billingConnStr ?? "";
                var finDb = _configuration["FinancialDatabaseName"] ?? "Financial DB";
                var b = new SqlConnectionStringBuilder(billingConn);
                b.InitialCatalog = finDb;
                finConnStr = b.ToString();
            }
            if (string.IsNullOrEmpty(admissionConnStr) || string.IsNullOrEmpty(billingConnStr) || string.IsNullOrEmpty(finConnStr))
                return StatusCode(500, new { message = "Database connections not configured" });

            var fin = $"[{_configuration["FinancialDatabaseName"] ?? "Financial DB"}]";

            try
            {
                using var finConn = new SqlConnection(finConnStr);
                await finConn.OpenAsync();

                var getHeaderSql = $@"SELECT TOP 1 ch.ID, ch.CashierID, ch.OpenDate FROM {fin}.dbo.CashierHeader ch WHERE ISNULL(ch.IsDeleted, 0) = 0 AND ch.CloseDate IS NULL ORDER BY ch.ID DESC";
                int cashierHeaderId;
                DateTime openDate;
                using (var cmd = finConn.CreateCommand())
                {
                    cmd.CommandText = getHeaderSql;
                    using var rdr = await cmd.ExecuteReaderAsync();
                    if (!await rdr.ReadAsync())
                        return BadRequest(new { message = "No open cashier found. Please open a cashier first." });
                    cashierHeaderId = rdr.GetInt32(0);
                    openDate = rdr.IsDBNull(2) ? DateTime.Today : rdr.GetDateTime(2);
                }

                var getMaxDailySql = $@"SELECT ISNULL(MAX(DailyCounter), 0) FROM {fin}.dbo.CashierDetail WHERE CashierHeaderID = @Hid AND ISNULL(IsDeleted, 0) = 0";
                int dailyCounter;
                using (var cmd = finConn.CreateCommand())
                {
                    cmd.CommandText = getMaxDailySql;
                    cmd.Parameters.Add(new SqlParameter("@Hid", cashierHeaderId));
                    var obj = await cmd.ExecuteScalarAsync();
                    dailyCounter = (obj == null || obj == DBNull.Value) ? 1 : Convert.ToInt32(obj) + 1;
                }

                using var billingConn = new SqlConnection(billingConnStr);
                await billingConn.OpenAsync();
                using var admConn = new SqlConnection(admissionConnStr);
                await admConn.OpenAsync();

                foreach (var item in request.Items)
                {
                    string? dept = null;
                    string? pname = null;
                    string? admNum = null;
                    decimal amt = 0;
                    decimal collLBP = 0, collUSD = 0;
                    int accountCurrency = 2;
                    string? receiptNum = null;
                    int? invId = null;
                    int? advId = null;
                    int? mouvNB = null;

                    // Prioritize AdvanceId: if present, process as advance (don't require InvoiceHeaderId)
                    if (item.AdvanceId.HasValue)
                    {
                        var advSql = @"SELECT a.ID, a.AdvanceNumber, a.Admission, a.AdvanceAmount, a.ReceiptNumber, rp.MedicationUnitDescription, rp.PatientName, rp.AdmissionNumber
                            FROM [Admission].[dbo].[Advance] a WITH (NOLOCK)
                            INNER JOIN [Admission].[dbo].[ResidentPatient] rp WITH (NOLOCK) ON rp.Admission = a.Admission
                            WHERE a.ID = @AdvId AND (a.IsDeleted = 0 OR a.IsDeleted IS NULL)";
                        using (var cmd = admConn.CreateCommand())
                        {
                            cmd.CommandText = advSql;
                            cmd.Parameters.Add(new SqlParameter("@AdvId", item.AdvanceId.Value));
                            using var rdr = await cmd.ExecuteReaderAsync();
                            if (!await rdr.ReadAsync()) continue;
                            if (!rdr.IsDBNull(rdr.GetOrdinal("ReceiptNumber")) && !string.IsNullOrEmpty(rdr.GetString(rdr.GetOrdinal("ReceiptNumber")))) continue;
                            amt = rdr.GetDecimal(rdr.GetOrdinal("AdvanceAmount"));
                            dept = rdr.IsDBNull(rdr.GetOrdinal("MedicationUnitDescription")) ? "Unknown" : rdr.GetString(rdr.GetOrdinal("MedicationUnitDescription"));
                            pname = rdr.IsDBNull(rdr.GetOrdinal("PatientName")) ? null : rdr.GetString(rdr.GetOrdinal("PatientName"));
                            admNum = rdr.IsDBNull(rdr.GetOrdinal("AdmissionNumber")) ? null : rdr.GetString(rdr.GetOrdinal("AdmissionNumber"));
                            advId = rdr.IsDBNull(rdr.GetOrdinal("ID")) ? null : rdr.GetInt32(rdr.GetOrdinal("ID"));
                            mouvNB = rdr.IsDBNull(rdr.GetOrdinal("AdvanceNumber")) ? null : rdr.GetInt32(rdr.GetOrdinal("AdvanceNumber"));
                        }
                        collUSD = amt;
                        collLBP = 0;
                        accountCurrency = 2;
                    }
                    else if (item.InvoiceHeaderId.HasValue && item.InvoiceHeaderId.Value != 0)
                    {
                        var invSql = @"SELECT Ih.ID, ih.SequenceNumber,ih.Net, ih.ReceivedLBP, ih.ReceivedUSD, ih.CurrencyId, ih.ReceiptNumber, ih.SequenceNumber,
                            rp.MedicationUnitDescription, rp.PatientName, rp.AdmissionNumber
                            FROM [Billing].[dbo].[InvoiceHeader] ih WITH (NOLOCK)
                            INNER JOIN [Admission].[dbo].[ResidentPatient] rp WITH (NOLOCK) ON rp.Admission = ih.Admission
                            WHERE ih.ID = @InvId AND ih.IsDeleted = 0";
                        using (var cmd = admConn.CreateCommand())
                        {
                            cmd.CommandText = invSql;
                            cmd.Parameters.Add(new SqlParameter("@InvId", item.InvoiceHeaderId.Value));
                            using var rdr = await cmd.ExecuteReaderAsync();
                            if (!await rdr.ReadAsync()) continue;
                            if (!rdr.IsDBNull(rdr.GetOrdinal("ReceiptNumber")) && !string.IsNullOrEmpty(rdr.GetString(rdr.GetOrdinal("ReceiptNumber")))) continue;
                            amt = rdr.GetDecimal(rdr.GetOrdinal("Net"));
                            collLBP = rdr.IsDBNull(rdr.GetOrdinal("ReceivedLBP")) ? 0 : rdr.GetDecimal(rdr.GetOrdinal("ReceivedLBP"));
                            collUSD = rdr.IsDBNull(rdr.GetOrdinal("ReceivedUSD")) ? 0 : rdr.GetDecimal(rdr.GetOrdinal("ReceivedUSD"));
                            accountCurrency = rdr.IsDBNull(rdr.GetOrdinal("CurrencyId")) ? 2 : rdr.GetInt32(rdr.GetOrdinal("CurrencyId"));
                            dept = rdr.IsDBNull(rdr.GetOrdinal("MedicationUnitDescription")) ? "Unknown" : rdr.GetString(rdr.GetOrdinal("MedicationUnitDescription"));
                            pname = rdr.IsDBNull(rdr.GetOrdinal("PatientName")) ? null : rdr.GetString(rdr.GetOrdinal("PatientName"));
                            admNum = rdr.IsDBNull(rdr.GetOrdinal("AdmissionNumber")) ? null : rdr.GetString(rdr.GetOrdinal("AdmissionNumber"));
                            invId = rdr.IsDBNull(rdr.GetOrdinal("ID")) ? null : rdr.GetInt32(rdr.GetOrdinal("ID"));
                            mouvNB = rdr.IsDBNull(rdr.GetOrdinal("SequenceNumber")) ? null : rdr.GetInt32(rdr.GetOrdinal("SequenceNumber"));
                        }
                        if (collLBP == 0 && collUSD == 0) { collLBP = accountCurrency == 1 ? amt : 0; collUSD = accountCurrency != 1 ? amt : 0; }
                    }
                    else continue;

                    if (string.IsNullOrEmpty(dept)) dept = "Unknown";
                    var distTypeId = item.AdvanceId.HasValue ? 2 : 10; // DistributionTypesID: 2=advance, 10=invoice
                    decimal receivedAmount;
                    if (item.AdvanceId.HasValue)
                        receivedAmount = amt; // For advance: use advance amount directly
                    else
                        receivedAmount = accountCurrency == 1 ? collLBP + collUSD * exchangeRate : collUSD + collLBP / exchangeRate;

                    var voucherTypeId = item.AdvanceId.HasValue ? 15 : 12; // VoucherTypeID = ID from VoucherType (2=advance, 10=invoice)
                    var counterCol = advId.HasValue ? "Counter" : "DCounter"; // advance: Counter, invoice: DCounter

                    // Get next CashierDetailCounter from VoucherType: advance uses Counter, invoice uses DCounter
                    int detailCounter;
                    using (var cmd = finConn.CreateCommand())
                    {
                        cmd.CommandText = $@"SELECT ISNULL({counterCol}, 0) + 1 FROM {fin}.dbo.VoucherType WITH (UPDLOCK, ROWLOCK) WHERE ID = @rvid";
                        cmd.Parameters.Add(new SqlParameter("@rvid", voucherTypeId));
                        var nextCounterObj = await cmd.ExecuteScalarAsync();
                        if (nextCounterObj == null || nextCounterObj == DBNull.Value)
                            continue; // No VoucherType row for this ID, skip
                        detailCounter = Convert.ToInt32(nextCounterObj);
                        receiptNum = detailCounter.ToString();
                    }
                    using (var cmd = finConn.CreateCommand())
                    {
                        cmd.CommandText = $@"UPDATE {fin}.dbo.VoucherType SET {counterCol} = @newVal WHERE ID = @rvid";
                        cmd.Parameters.Add(new SqlParameter("@rvid", voucherTypeId));
                        cmd.Parameters.Add(new SqlParameter("@newVal", detailCounter));
                        await cmd.ExecuteNonQueryAsync();
                    }
                    var insertCd = $@"INSERT INTO {fin}.dbo.CashierDetail (CashierHeaderID, OpenDate, VousherTypeID, DailyCounter, CashierDetailCounter, Department, DistributionTypesID, comment, MouvementNb, VoucherNumber, 
                                                                           AmoutToBePayed, AccountCurrency, CollectionLBP, CollectionUSD, DifferenceUSD, DifferenceLBP, AdmissionNB, IsDeleted, CreatedBy, 
                                                                           CreatedDate,ReceivedAmount,IsCheque,ReturnAdvance,DifferenceLL,DifferenceUSB,AgreementToBePayedAmount,AgremmentAmount,IsImportedFromUncollected,
                                                                           InvoiceID,AdvanceID)
                        VALUES (@ChId, @OpenDate, @VoucherType, @DC, @CDC, @Dept, @DistType, @Comment, @Mouv, @Voucher, @Amt, @Curr, @CollLBP, @CollUSD, @diffUSD, @diffLL, @AdmNb, 0, 338, GETDATE(),
                                @ReceivedAmount,0,@retAdv,@diffLL, @diffUSD,@agree,@agree,0,@InvoiceID,@AdvanceID)";
                    using (var cmd = finConn.CreateCommand())
                    {
                        cmd.CommandText = insertCd;
                        cmd.Parameters.AddWithValue("@ChId", cashierHeaderId);
                        cmd.Parameters.AddWithValue("@OpenDate", openDate.Date);
                        cmd.Parameters.AddWithValue("@VoucherType", voucherTypeId);
                        cmd.Parameters.AddWithValue("@DC", dailyCounter);
                        cmd.Parameters.AddWithValue("@CDC", detailCounter);
                        cmd.Parameters.AddWithValue("@Dept", dept);
                        cmd.Parameters.AddWithValue("@DistType", distTypeId);
                        cmd.Parameters.AddWithValue("@Comment", $"{pname ?? ""} / {admNum ?? ""} / {(mouvNB.HasValue ? mouvNB.Value.ToString() : "-")}");
                        cmd.Parameters.AddWithValue("@Mouv", mouvNB);
                        cmd.Parameters.AddWithValue("@Voucher", item.InvoiceHeaderId ?? 0);
                        cmd.Parameters.AddWithValue("@Amt", amt);
                        cmd.Parameters.AddWithValue("@Curr", accountCurrency);
                        cmd.Parameters.AddWithValue("@CollLBP", collLBP);
                        cmd.Parameters.AddWithValue("@CollUSD", collUSD);
                        cmd.Parameters.AddWithValue("@AdmNb", admNum ?? "");
                        cmd.Parameters.AddWithValue("@ReceivedAmount", receivedAmount);
                        cmd.Parameters.AddWithValue("@diffLL", accountCurrency == 1 ? amt - receivedAmount : 0);
                        cmd.Parameters.AddWithValue("@diffUSD", accountCurrency == 2 ? amt - receivedAmount : 0);
                        cmd.Parameters.AddWithValue("@retAdv", 0);
                        cmd.Parameters.AddWithValue("@agree", 0);
                        cmd.Parameters.AddWithValue("@InvoiceID", invId ?? 0);
                        cmd.Parameters.AddWithValue("@AdvanceID", advId ?? 0);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    dailyCounter++;

                    if (item.AdvanceId.HasValue)
                    {
                        using var updAdv = admConn.CreateCommand();
                        updAdv.CommandText = "UPDATE [Admission].[dbo].[Advance] SET ReceiptNumber = @Rcpt, ReceiptDate = GETDATE() WHERE ID = @Id";
                        updAdv.Parameters.AddWithValue("@Rcpt", receiptNum);
                        updAdv.Parameters.AddWithValue("@Id", item.AdvanceId.Value);
                        await updAdv.ExecuteNonQueryAsync();
                    }
                    else if (item.InvoiceHeaderId.HasValue && item.InvoiceHeaderId.Value != 0)
                    {
                        using var updInv = billingConn.CreateCommand();
                        updInv.CommandText = "UPDATE [Billing].[dbo].[InvoiceHeader] SET ReceiptNumber = @Rcpt, ReceiptDate = GETDATE() WHERE ID = @Id";
                        updInv.Parameters.AddWithValue("@Rcpt", receiptNum);
                        updInv.Parameters.AddWithValue("@Id", item.InvoiceHeaderId.Value);
                        await updInv.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { message = "Transfer completed successfully", count = request.Items.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer to cashier failed");
                return StatusCode(500, new { message = "Transfer failed", error = ex.Message });
            }
        }

        private class DepartmentReportItem
        {
            public string Department { get; set; } = string.Empty;
            public string MRN { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string AdmissionNumber { get; set; } = string.Empty;
            public int Admission { get; set; }
            public int InvoiceHeaderId { get; set; }
            public int? AdvanceId { get; set; }
            public string? ReceiptNumber { get; set; }
            public decimal InvoiceTotal { get; set; }
            public decimal ReceiptLBP { get; set; }
            public decimal ReceiptUSD { get; set; }
            public int SequenceNumber { get; set; }
            public int CurrencyId { get; set; }
            public decimal AdvanceAmount { get; set; }
            public string? AdvanceReceiptNumber { get; set; }
            public bool HasAdvance { get; set; }
            public bool IsAdvanceRow { get; set; }
        }

        public class TransferToCashierRequest
        {
            public List<TransferToCashierItem> Items { get; set; } = new();
        }

        public class TransferToCashierItem
        {
            public int? InvoiceHeaderId { get; set; }
            public int? AdvanceId { get; set; }
            public bool? IsAdvanceRow { get; set; }
        }
    }
}

