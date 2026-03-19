using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResidentPatientsController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<ResidentPatientsController> _logger;

        public ResidentPatientsController(LISDbContext context, ILogger<ResidentPatientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get resident patients from the Admission database filtered by check-in date range
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                // Set from date to start of day (00:00:00)
                var from = (fromDate ?? DateTime.Today).Date;
                
                // Set to date to end of day (23:59:59.999)
                var to = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

                _logger.LogInformation("Fetching patients from {FromDate} to {ToDate}", from, to);

                // Use raw SQL to get only essential columns for better performance
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = @"
                    SELECT ID, PatientID, Admission, MRN, AdmissionNumber, PatientName, ArabicFullName, 
                           MedicalRecordNumber, PatientDOB, Age, PatientGender, CheckInDate, 
                           CheckInClassDescription, MainInsuranceDescription, 
                           ReferralPhysicianName, AttendingPhysicianName, 
                           RoomDescription, BedDescription, Contact, IsDischarged, 0 as IsDeleted
                    FROM Admission.dbo.ResidentPatient
                    WHERE IsDeleted = 0 
                      AND MedicationUnitDescription LIKE '%labo%'
                      AND CAST(CheckInDate AS DATE) >= CAST(@FromDate AS DATE)
                      AND CAST(CheckInDate AS DATE) <= CAST(@ToDate AS DATE)
                    ORDER BY CheckInDate DESC";
                
                // Add parameters to prevent SQL injection
                var fromParam = command.CreateParameter();
                fromParam.ParameterName = "@FromDate";
                fromParam.Value = from;
                command.Parameters.Add(fromParam);

                var toParam = command.CreateParameter();
                toParam.ParameterName = "@ToDate";
                toParam.Value = to;
                command.Parameters.Add(toParam);

                command.CommandTimeout = 60; // 60 seconds timeout

                await _context.Database.OpenConnectionAsync();

                var results = new List<object>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        id = reader.GetInt32(0),
                        patientID = reader.GetInt32(1),
                        admission = reader.GetInt32(2),
                        mrn = reader.GetInt32(3),
                        admissionNumber = reader.GetString(4),
                        patientName = reader.GetString(5),
                        arabicFullName = reader.IsDBNull(6) ? null : reader.GetString(6),
                        medicalRecordNumber = reader.GetString(7),
                        patientDOB = reader.GetDateTime(8).ToString("yyyy-MM-dd"),
                        age = reader.IsDBNull(9) ? null : (int?)reader.GetInt32(9),
                        patientGender = reader.GetString(10),
                        checkInDate = reader.GetDateTime(11).ToString("yyyy-MM-ddTHH:mm:ss"),
                        checkInClassDescription = reader.GetString(12),
                        mainInsuranceDescription = reader.GetString(13),
                        referralPhysicianName = reader.GetString(14),
                        attendingPhysicianName = reader.IsDBNull(15) ? null : reader.GetString(15),
                        roomDescription = reader.IsDBNull(16) ? null : reader.GetString(16),
                        bedDescription = reader.IsDBNull(17) ? null : reader.GetString(17),
                        contact = reader.IsDBNull(18) ? null : reader.GetString(18),
                        isDischarged = reader.GetBoolean(19),
                        isDeleted = false
                    });
                }

                _logger.LogInformation("Retrieved {Count} resident patients from Admission database (from {FromDate} to {ToDate})", 
                    results.Count, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"));
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resident patients from Admission database");
                return StatusCode(500, new { message = "An error occurred while retrieving resident patients", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific resident patient by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResidentPatient>> GetById(int id)
        {
            try
            {
                var residentPatient = await _context.ResidentPatients
                    .FromSqlRaw(@"
                        SELECT ID, PatientID, Admission, MRN, AdmissionNumber, PatientName, ArabicFullName, 
                               MedicalRecordNumber, PatientDOB, Age, PatientGender, CheckInDate, CheckInClassID, 
                               CheckInClassDescription, MainInsuranceID, MainInsuranceDescription, MainInsuranceClassID, 
                               MainInsuranceClassDescription, ReferralPhysicianID, ReferralPhysicianName, AttendingPhysicianID, 
                               AttendingPhysicianName, MedicationUnitID, MedicationUnitDescription, RoomID, RoomDescription, 
                               BedID, BedDescription, FloorID, FloorDescription, InsuranceID, InsuranceDescription, 
                               GuarantorID, GuarantorDescription, CurrencyID, CurrencyDescription, ClassID, ClassDescription, 
                               ContextPriceID, ContextPriceDescription, ContextEnumerationID, ContextEnumerationDescription, 
                               AdmissionType, AdmissionTypeDescription, Contact, InsuredName, InsuredNameArabic, InsuredPhone, 
                               AuxiliaryInsuranceID, AuxiliaryInsuranceDescription, AuxiliaryInsuranceClassID, 
                               AuxiliaryInsuranceClassDescription, IsDischarged, DischargeDate, Comment, TotalAdvanceLBP, 
                               TotalAdvanceUSD, Diagnostic, VisaNumber, TotalUncollectedAdvanceLBP, TotalUncollectedAdvanceUSD, 
                               InvoiceGrossAmountLBP, InvoiceGrossAmountUSD, MainInvoiceNumber, IsPharmDisch, PharmDischDate, 
                               Notes, IsDeleted, CreatedBy, ModifiedBy, CreatedDate, ModifiedDate, AdmissionSite, 
                               IsNersingDischarge, NersingDischargeComment, OldBedID, [Group], PatientShortName, 
                               PatientFormattedName, Status, IsRecheckIn, HasInvoices, RequireRegenerate, 
                               DiagnosticGroup1, DiagnosticGroup2, DiagnosticGroup3, OldComment
                        FROM Admission.dbo.ResidentPatient 
                        WHERE ID = {0}
                    ", id)
                    .FirstOrDefaultAsync();

                if (residentPatient == null)
                {
                    _logger.LogWarning("Resident patient with ID {Id} not found in Admission database", id);
                    return NotFound(new { message = $"Resident patient with ID {id} not found" });
                }
                return Ok(residentPatient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resident patient {Id} from Admission database", id);
                return StatusCode(500, "An error occurred while retrieving the resident patient");
            }
        }

        /// <summary>
        /// Search resident patients by MRN or Patient Name
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ResidentPatient>>> Search([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }

                var residentPatients = await _context.ResidentPatients
                    .FromSqlRaw(@"
                        SELECT TOP 100
                               ID, PatientID, Admission, MRN, AdmissionNumber, PatientName, ArabicFullName, 
                               MedicalRecordNumber, PatientDOB, Age, PatientGender, CheckInDate, CheckInClassID, 
                               CheckInClassDescription, MainInsuranceID, MainInsuranceDescription, MainInsuranceClassID, 
                               MainInsuranceClassDescription, ReferralPhysicianID, ReferralPhysicianName, AttendingPhysicianID, 
                               AttendingPhysicianName, MedicationUnitID, MedicationUnitDescription, RoomID, RoomDescription, 
                               BedID, BedDescription, FloorID, FloorDescription, InsuranceID, InsuranceDescription, 
                               GuarantorID, GuarantorDescription, CurrencyID, CurrencyDescription, ClassID, ClassDescription, 
                               ContextPriceID, ContextPriceDescription, ContextEnumerationID, ContextEnumerationDescription, 
                               AdmissionType, AdmissionTypeDescription, Contact, InsuredName, InsuredNameArabic, InsuredPhone, 
                               AuxiliaryInsuranceID, AuxiliaryInsuranceDescription, AuxiliaryInsuranceClassID, 
                               AuxiliaryInsuranceClassDescription, IsDischarged, DischargeDate, Comment, TotalAdvanceLBP, 
                               TotalAdvanceUSD, Diagnostic, VisaNumber, TotalUncollectedAdvanceLBP, TotalUncollectedAdvanceUSD, 
                               InvoiceGrossAmountLBP, InvoiceGrossAmountUSD, MainInvoiceNumber, IsPharmDisch, PharmDischDate, 
                               Notes, IsDeleted, CreatedBy, ModifiedBy, CreatedDate, ModifiedDate, AdmissionSite, 
                               IsNersingDischarge, NersingDischargeComment, OldBedID, [Group], PatientShortName, 
                               PatientFormattedName, Status, IsRecheckIn, HasInvoices, RequireRegenerate, 
                               DiagnosticGroup1, DiagnosticGroup2, DiagnosticGroup3, OldComment
                        FROM Admission.dbo.ResidentPatient
                        WHERE IsDeleted = 0 
                          AND (MedicalRecordNumber LIKE '%' + {0} + '%' 
                            OR PatientName LIKE '%' + {0} + '%'
                            OR AdmissionNumber LIKE '%' + {0} + '%')
                        ORDER BY CheckInDate DESC
                    ", query)
                    .ToListAsync();

                _logger.LogInformation("Search for '{Query}' returned {Count} resident patients", query, residentPatients.Count);
                return Ok(residentPatients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching resident patients with query '{Query}'", query);
                return StatusCode(500, "An error occurred while searching resident patients");
            }
        }
    }
}

