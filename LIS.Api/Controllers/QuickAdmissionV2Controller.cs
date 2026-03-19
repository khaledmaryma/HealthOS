using Microsoft.AspNetCore.Mvc;
using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    /// <summary>
    /// V2 API for QuickAdmission - provides composite endpoints for the new QuickAdmissionV2 Angular component.
    /// This controller handles saving and loading of patient, admission, and invoice data.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QuickAdmissionV2Controller : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<QuickAdmissionV2Controller> _logger;
        private readonly IConfiguration _configuration;

        public QuickAdmissionV2Controller(LISDbContext context, ILogger<QuickAdmissionV2Controller> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        #region DTOs

        public class QuickAdmissionSaveRequest
        {
            public bool SaveMedicalFile { get; set; }
            public bool SaveAdmission { get; set; }
            public bool SaveInvoice { get; set; }
            public PatientObject? Patient { get; set; }
            public AdmissionObject? Admission { get; set; }
            public List<object>? InvoiceDetails { get; set; }
        }

        public class PatientObject
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? MiddleName { get; set; }
            public string? Gender { get; set; }
            public DateTime? DOB { get; set; }
            public string? Phone { get; set; }
            public string? ArabicFullName { get; set; }
            public int? MaritalStatus { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class AdmissionObject
        {
            public string? Number { get; set; }
            public int? AdmissionSite { get; set; }
            public int? ReferralPhysician { get; set; }
            public int? AttendingPhysician { get; set; }
            public int? MainInsurance { get; set; }
            public int? MainInsuranceClass { get; set; }
            public int? Insured { get; set; }
            public int? AuxiliaryInsurance { get; set; }
            public int? AuxiliaryInsuranceClass { get; set; }
            public int? CheckInClass { get; set; }
            public string? Department { get; set; }
            public string? CheckInDate { get; set; }
            public string? CheckOutDate { get; set; }
            public int? Patient { get; set; }
            public int? Type { get; set; }
            public int? IsWorkAccident { get; set; }
            public int? IsExtended { get; set; }
            public int? Group { get; set; }
            public int? CreatedBy { get; set; }
        }

        public class QuickAdmissionResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public SaveResult? Data { get; set; }
        }

        public class SaveResult
        {
            public int? PatientId { get; set; }
            public int? AdmissionId { get; set; }
            public int? InvoiceId { get; set; }
        }

        #endregion

        /// <summary>
        /// Save complete quick admission data based on what needs to be saved
        /// </summary>
        [HttpPost("save-complete")]
        public async Task<ActionResult<QuickAdmissionResponse>> SaveComplete([FromBody] QuickAdmissionSaveRequest request)
        {
            if (request == null)
                return BadRequest(new QuickAdmissionResponse { Success = false, Message = "Request is required" });

            try
            {
                _logger.LogInformation("QuickAdmissionV2: SaveComplete request received");

                int? patientId = null;
                int? admissionId = null;
                int? invoiceId = null;

                // Save Patient Medical File
                if (request.SaveMedicalFile && request.Patient != null)
                {
                    patientId = await SaveOrUpdatePatient(request.Patient);
                    _logger.LogInformation("QuickAdmissionV2: Patient saved with ID {PatientId}", patientId);
                }

                // NOTE: Admission and Invoice saving are deferred to future implementation
                // They require additional database tables and models that will be added later

                if (request.SaveAdmission && request.Admission != null && patientId.HasValue)
                {
                    _logger.LogInformation("QuickAdmissionV2: Admission save is deferred (future implementation)");
                    // TODO: implement when Admission table infrastructure is available
                }

                if (request.SaveInvoice && request.InvoiceDetails?.Count > 0 && patientId.HasValue)
                {
                    _logger.LogInformation("QuickAdmissionV2: Invoice save is deferred (future implementation)");
                    // TODO: implement when Invoice table infrastructure is available
                }

                return Ok(new QuickAdmissionResponse
                {
                    Success = true,
                    Message = "Quick admission data saved successfully",
                    Data = new SaveResult
                    {
                        PatientId = patientId,
                        AdmissionId = admissionId,
                        InvoiceId = invoiceId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveComplete");
                return StatusCode(500, new QuickAdmissionResponse
                {
                    Success = false,
                    Message = $"Error saving quick admission: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Load admission data by id (for edit mode)
        /// </summary>
        [HttpGet("load-admission/{admissionId}")]
        public async Task<ActionResult<object>> LoadAdmission(int admissionId)
        {
            try
            {
                _logger.LogInformation("QuickAdmissionV2: LoadAdmission {AdmissionId}", admissionId);

                // For now, return a placeholder since Admission table infrastructure is not yet available
                return Ok(new
                {
                    admissionId = admissionId,
                    message = "Load admission endpoint - implementation pending (awaiting database infrastructure)",
                    invoiceDetails = new List<object>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoadAdmission");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        #region Private Helper Methods

        private async Task<int?> SaveOrUpdatePatient(PatientObject patientData)
        {
            try
            {
                var patient = new Patient
                {
                    FirstName = patientData.FirstName ?? "",
                    LastName = patientData.LastName ?? "",
                };

                // Set DOB if provided
                if (patientData.DOB.HasValue)
                {
                    patient.DateOfBirth = DateOnly.FromDateTime(patientData.DOB.Value);
                }

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("QuickAdmissionV2: Patient created with ID {PatientId}", patient.Id);
                return patient.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving patient");
                throw;
            }
        }

        #endregion
    }
}
