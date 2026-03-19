using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using LIS.Api.Models;
using System.Data;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResidentPatientController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResidentPatientController> _logger;

        public ResidentPatientController(IConfiguration configuration, ILogger<ResidentPatientController> logger)
        {
            _configuration = configuration;
            _logger = logger;
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
                    var offset = (page - 1) * pageSize;

                    var query = $@"
                        SELECT 
                            ID, PatientID, Admission, MRN, AdmissionNumber, PatientName, ArabicFullName,
                            MedicalRecordNumber, PatientDOB, Age, PatientGender, CheckInDate,
                            CheckInClassID, CheckInClassDescription, MainInsuranceID, MainInsuranceDescription,
                            MainInsuranceClassID, MainInsuranceClassDescription, ReferralPhysicianID, ReferralPhysicianName,
                            AttendingPhysicianID, AttendingPhysicianName, MedicationUnitID, MedicationUnitDescription,
                            RoomID, RoomDescription, BedID, BedDescription, FloorID, FloorDescription,
                            InsuranceID, InsuranceDescription, GuarantorID, GuarantorDescription,
                            CurrencyID, CurrencyDescription, ClassID, ClassDescription,
                            ContextPriceID, ContextPriceDescription, ContextEnumerationID, ContextEnumerationDescription,
                            AdmissionType, AdmissionTypeDescription, Contact, InsuredName, InsuredNameArabic,
                            InsuredPhone, AuxiliaryInsuranceID, AuxiliaryInsuranceDescription,
                            AuxiliaryInsuranceClassID, AuxiliaryInsuranceClassDescription, IsDischarged,
                            DischargeDate, Comment, TotalAdvanceLBP, TotalAdvanceUSD, Diagnostic,
                            VisaNumber, TotalUncollectedAdvanceLBP, TotalUncollectedAdvanceUSD,
                            InvoiceGrossAmountLBP, InvoiceGrossAmountUSD, MainInvoiceNumber,
                            IsPharmDisch, PharmDischDate, IsDeleted, CreatedBy, ModifiedBy,
                            CreatedDate, ModifiedDate, AdmissionSite, IsNersingDischarge,
                            NersingDischargeComment, OldBedID, [Group], PatientShortName,
                            PatientFormattedName, Status, IsRecheckIn, HasInvoices, RequireRegenerate,
                            DiagnosticGroup1, DiagnosticGroup2, DiagnosticGroup3
                        FROM ResidentPatient WITH (NOLOCK)
                        WHERE {whereClause}
                        ORDER BY ID DESC
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
                            ID, PatientID, Admission, MRN, AdmissionNumber, PatientName, ArabicFullName,
                            MedicalRecordNumber, PatientDOB, Age, PatientGender, CheckInDate,
                            CheckInClassID, CheckInClassDescription, MainInsuranceID, MainInsuranceDescription,
                            MainInsuranceClassID, MainInsuranceClassDescription, ReferralPhysicianID, ReferralPhysicianName,
                            AttendingPhysicianID, AttendingPhysicianName, MedicationUnitID, MedicationUnitDescription,
                            RoomID, RoomDescription, BedID, BedDescription, FloorID, FloorDescription,
                            InsuranceID, InsuranceDescription, GuarantorID, GuarantorDescription,
                            CurrencyID, CurrencyDescription, ClassID, ClassDescription,
                            ContextPriceID, ContextPriceDescription, ContextEnumerationID, ContextEnumerationDescription,
                            AdmissionType, AdmissionTypeDescription, Contact, InsuredName, InsuredNameArabic,
                            InsuredPhone, AuxiliaryInsuranceID, AuxiliaryInsuranceDescription,
                            AuxiliaryInsuranceClassID, AuxiliaryInsuranceClassDescription, IsDischarged,
                            DischargeDate, Comment, TotalAdvanceLBP, TotalAdvanceUSD, Diagnostic,
                            VisaNumber, TotalUncollectedAdvanceLBP, TotalUncollectedAdvanceUSD,
                            InvoiceGrossAmountLBP, InvoiceGrossAmountUSD, MainInvoiceNumber,
                            IsPharmDisch, PharmDischDate, IsDeleted, CreatedBy, ModifiedBy,
                            CreatedDate, ModifiedDate, AdmissionSite, IsNersingDischarge,
                            NersingDischargeComment, OldBedID, [Group], PatientShortName,
                            PatientFormattedName, Status, IsRecheckIn, HasInvoices, RequireRegenerate,
                            DiagnosticGroup1, DiagnosticGroup2, DiagnosticGroup3
                        FROM ResidentPatient WITH (NOLOCK)
                        WHERE Admission = @id AND IsDeleted = 0";

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
                DiagnosticGroup3 = reader.IsDBNull(reader.GetOrdinal("DiagnosticGroup3")) ? null : reader.GetString(reader.GetOrdinal("DiagnosticGroup3"))
            };
        }

        /// <summary>
        /// Get report of all patients grouped by department showing MRN, PatientName, AdmissionNumber, InvoiceTotal, ReceiptLBP, ReceiptUSD
        /// </summary>
        [HttpGet("report/by-department")]
        public async Task<ActionResult> GetReportByDepartment()
        {
            try
            {
                var admissionConnectionString = _configuration.GetConnectionString("AdmissionConnection");
                var billingConnectionString = _configuration.GetConnectionString("BillingConnection");
                
                if (string.IsNullOrEmpty(billingConnectionString))
                {
                    var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrEmpty(defaultConnection))
                    {
                        billingConnectionString = defaultConnection.Replace("Database=LIS", "Database=Billing");
                    }
                }

                if (string.IsNullOrEmpty(admissionConnectionString) || string.IsNullOrEmpty(billingConnectionString))
                {
                    return StatusCode(500, new { message = "Database connection strings not configured" });
                }

                var reportData = new List<DepartmentReportItem>();

                // Query to join ResidentPatient with InvoiceHeader
                // Group by Department (MedicationUnitDescription)
                // Filter by current date only
                var query = @"
                    SELECT 
                        rp.MedicationUnitDescription AS Department,
                        rp.MedicalRecordNumber AS MRN,
                        rp.PatientName,
                        rp.AdmissionNumber,
                        ISNULL(SUM(ih.Net), 0) AS InvoiceTotal,
                        ISNULL(SUM(ih.ReceivedLBP), 0) AS ReceiptLBP,
                        ISNULL(SUM(ih.ReceivedUSD), 0) AS ReceiptUSD
                    FROM [Admission].[dbo].[ResidentPatient] rp WITH (NOLOCK)
                    LEFT JOIN [Billing].[dbo].[InvoiceHeader] ih WITH (NOLOCK) 
                        ON rp.Admission = ih.Admission AND ih.IsDeleted = 0
                    WHERE rp.IsDeleted = 0
                        --AND CAST(rp.CheckInDate AS DATE) = CAST(GETDATE() AS DATE)
                        AND YEAR(rp.CheckInDate) >= 2026
                    GROUP BY 
                        rp.MedicationUnitDescription,
                        rp.MedicalRecordNumber,
                        rp.PatientName,
                        rp.AdmissionNumber
                    ORDER BY 
                        rp.MedicationUnitDescription,
                        rp.PatientName,
                        rp.AdmissionNumber";

                // Use AdmissionConnection and enable cross-database queries
                using (var connection = new SqlConnection(admissionConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var deptOrdinal = reader.GetOrdinal("Department");
                                var mrnOrdinal = reader.GetOrdinal("MRN");
                                var nameOrdinal = reader.GetOrdinal("PatientName");
                                var admNumOrdinal = reader.GetOrdinal("AdmissionNumber");
                                var invTotalOrdinal = reader.GetOrdinal("InvoiceTotal");
                                var recLBPOrdinal = reader.GetOrdinal("ReceiptLBP");
                                var recUSDOrdinal = reader.GetOrdinal("ReceiptUSD");

                                reportData.Add(new DepartmentReportItem
                                {
                                    Department = reader.IsDBNull(deptOrdinal) ? "Unknown" : reader.GetString(deptOrdinal),
                                    MRN = reader.IsDBNull(mrnOrdinal) ? "" : reader.GetString(mrnOrdinal),
                                    PatientName = reader.IsDBNull(nameOrdinal) ? "" : reader.GetString(nameOrdinal),
                                    AdmissionNumber = reader.IsDBNull(admNumOrdinal) ? "" : reader.GetString(admNumOrdinal),
                                    InvoiceTotal = reader.IsDBNull(invTotalOrdinal) ? 0 : reader.GetDecimal(invTotalOrdinal),
                                    ReceiptLBP = reader.IsDBNull(recLBPOrdinal) ? 0 : reader.GetDecimal(recLBPOrdinal),
                                    ReceiptUSD = reader.IsDBNull(recUSDOrdinal) ? 0 : reader.GetDecimal(recUSDOrdinal)
                                });
                            }
                        }
                    }
                }

                // Group by department for response
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
                            p.InvoiceTotal,
                            p.ReceiptLBP,
                            p.ReceiptUSD
                        }).ToList(),
                        DepartmentTotal = new
                        {
                            InvoiceTotal = g.Sum(p => p.InvoiceTotal),
                            ReceiptLBP = g.Sum(p => p.ReceiptLBP),
                            ReceiptUSD = g.Sum(p => p.ReceiptUSD),
                            PatientCount = g.Count()
                        }
                    })
                    .OrderBy(x => x.Department)
                    .ToList();

                return Ok(groupedReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating department report");
                return StatusCode(500, new { message = "An error occurred while generating the report", error = ex.Message });
            }
        }

        private class DepartmentReportItem
        {
            public string Department { get; set; } = string.Empty;
            public string MRN { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string AdmissionNumber { get; set; } = string.Empty;
            public decimal InvoiceTotal { get; set; }
            public decimal ReceiptLBP { get; set; }
            public decimal ReceiptUSD { get; set; }
        }
    }
}

