using LIS.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientMedicalFileController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<PatientMedicalFileController> _logger;
        private readonly IConfiguration _configuration;

        public PatientMedicalFileController(
            LISDbContext context, 
            ILogger<PatientMedicalFileController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Get comprehensive medical file for a patient by PatientID from EMR database
        /// </summary>
        [HttpGet("patient/{patientId:int}")]
        public async Task<ActionResult<object>> GetMedicalFileByPatientId(int patientId)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var medicalFile = new
                {
                    // Vital Signs
                    vitalSigns = await GetVitalSigns(conn, patientId),
                    
                    // Medications
                    medications = await GetMedications(conn, patientId),
                    
                    // Medical Orders
                    orders = await GetOrders(conn, patientId),
                    
                    // Progress Notes
                    progressNotes = await GetProgressNotes(conn, patientId),
                    
                    // Clinical Examinations
                    clinicalExaminations = await GetClinicalExaminations(conn, patientId),
                    
                    // Patient History
                    patientHistory = await GetPatientHistory(conn, patientId),
                    
                    // Risk Factors
                    riskFactors = await GetRiskFactors(conn, patientId),
                    
                    // Current Illness
                    currentIllness = await GetCurrentIllness(conn, patientId),
                    
                    // Cardiac History
                    cardiacHistory = await GetCardiacHistory(conn, patientId),
                    
                    // Medication History
                    medicationHistory = await GetMedicationHistory(conn, patientId)
                };

                _logger.LogInformation("Retrieved medical file for PatientID {PatientId}", patientId);
                return Ok(medicalFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medical file for PatientID {PatientId}", patientId);
                return StatusCode(500, new { message = "An error occurred while retrieving medical file", error = ex.Message });
            }
        }

        /// <summary>
        /// Get medical file by AdmissionID
        /// </summary>
        [HttpGet("admission/{admissionId:int}")]
        public async Task<ActionResult<object>> GetMedicalFileByAdmissionId(int admissionId)
        {
            try
            {
                // First get PatientID from Admission
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var patientIdQuery = "SELECT PatientID FROM Admission.dbo.ResidentPatient WHERE Admission = @AdmissionId AND IsDeleted = 0";
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = patientIdQuery;
                var param = cmd.CreateParameter();
                param.ParameterName = "@AdmissionId";
                param.Value = admissionId;
                cmd.Parameters.Add(param);

                var patientIdObj = await cmd.ExecuteScalarAsync();
                if (patientIdObj == null || patientIdObj == DBNull.Value)
                {
                    return NotFound(new { message = $"Admission {admissionId} not found" });
                }

                int patientId = Convert.ToInt32(patientIdObj);
                return await GetMedicalFileByPatientId(patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medical file for AdmissionID {AdmissionId}", admissionId);
                return StatusCode(500, new { message = "An error occurred while retrieving medical file", error = ex.Message });
            }
        }

        private async Task<List<object>> GetVitalSigns(DbConnection conn, int patientId)
        {
            var vitalSigns = new List<object>();
            var query = @"
                SELECT TOP 100
                    vs.ID,
                    vs.PatientID,
                    vs.VitalSignTypeID,
                    vs.VitalSignTypeDesc,
                    vs.Value,
                    vs.DateTaken,
                    vs.Notes
                FROM EMR.dbo.PatientVitalSign vs
                WHERE vs.PatientID = @PatientId AND vs.IsDeleted = 0
                ORDER BY vs.DateTaken DESC";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                vitalSigns.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                    vitalSignType = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                    vitalSignTypeDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                    value = reader.IsDBNull(4) ? null : reader.GetValue(4)?.ToString(),
                    date = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-ddTHH:mm:ss"),
                    comment = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
            return vitalSigns;
        }

        private async Task<List<object>> GetMedications(DbConnection conn, int patientId)
        {
            var medications = new List<object>();
            var query = @"
                SELECT TOP 100
                    ms.Id,
                    ms.PatientID,
                    ms.ProductName,
                    ms.Quantity,
                    ms.Direction,
                    ms.ScheduledDate,
                    ms.DateTaken,
                    ms.Status,
                    ms.Comment
                FROM EMR.dbo.PatientMedicationSchedule ms
                WHERE ms.PatientID = @PatientId AND ms.IsDeleted = 0
                ORDER BY ms.ScheduledDate DESC, ms.CreatedDate DESC";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                medications.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.GetInt32(1),
                    medicationName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    dosage = reader.IsDBNull(3) ? null : reader.GetValue(3)?.ToString(),
                    frequency = reader.IsDBNull(4) ? null : reader.GetString(4),
                    startDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-ddTHH:mm:ss"),
                    endDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6).ToString("yyyy-MM-ddTHH:mm:ss"),
                    status = reader.IsDBNull(7) ? null : reader.GetValue(7)?.ToString(),
                    comment = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }
            return medications;
        }

        private async Task<List<object>> GetOrders(DbConnection conn, int patientId)
        {
            var orders = new List<object>();
            var query = @"
                SELECT TOP 100
                    o.Id,
                    o.PatientID,
                    o.AdmissionID,
                    o.AdmissionNumber,
                    o.RequestType,
                    rt.Description AS RequestTypeDescription,
                    o.RequestDate,
                    o.Status,
                    o.Comments,
                    o.RequestedBy,
                    o.CaseNumber
                FROM EMR.dbo.OrderRequest o
                LEFT JOIN EMR.dbo.RequestType rt ON o.RequestType = rt.ID
                WHERE o.PatientID = @PatientId AND o.IsDeleted = 0
                ORDER BY o.RequestDate DESC";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.GetInt32(1),
                    admissionId = reader.GetInt32(2),
                    admissionNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                    requestType = reader.GetInt32(4),
                    requestTypeDescription = reader.IsDBNull(5) ? null : reader.GetString(5),
                    requestDate = reader.GetDateTime(6).ToString("yyyy-MM-ddTHH:mm:ss"),
                    status = reader.GetInt32(7),
                    comments = reader.IsDBNull(8) ? null : reader.GetString(8),
                    requestedBy = reader.GetInt32(9),
                    caseNumber = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10)
                });
            }
            return orders;
        }

        private async Task<List<object>> GetProgressNotes(DbConnection conn, int patientId)
        {
            var notes = new List<object>();
            var query = @"
                SELECT TOP 100
                    pn.ID,
                    pn.PatientID,
                    pn.Date,
                    pn.Comments,
                    pn.CreatedBy,
                    pn.CreatedDate
                FROM EMR.dbo.ProgressNotes pn
                WHERE pn.PatientID = @PatientId AND pn.IsDeleted = 0
                ORDER BY pn.Date DESC";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notes.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.GetInt32(1),
                    noteDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2).ToString("yyyy-MM-ddTHH:mm:ss"),
                    noteText = reader.IsDBNull(3) ? null : reader.GetString(3),
                    createdBy = reader.GetInt32(4),
                    createdDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-ddTHH:mm:ss")
                });
            }
            return notes;
        }

        private async Task<List<object>> GetClinicalExaminations(DbConnection conn, int patientId)
        {
            var exams = new List<object>();
            var query = @"
                SELECT TOP 100
                    ce.ID,
                    ce.PatientID,
                    ce.CreatedDate,
                    ce.Comments,
                    ce.CreatedBy,
                    ce.CreatedDate
                FROM EMR.dbo.ClinicExam ce
                WHERE ce.PatientID = @PatientId AND ce.IsDeleted = 0
                ORDER BY ce.CreatedDate DESC";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                exams.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.GetInt32(1),
                    examinationDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2).ToString("yyyy-MM-ddTHH:mm:ss"),
                    examinationText = reader.IsDBNull(3) ? null : reader.GetString(3),
                    createdBy = reader.GetInt32(4),
                    createdDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-ddTHH:mm:ss")
                });
            }
            return exams;
        }

        private async Task<List<object>> GetPatientHistory(DbConnection conn, int patientId)
        {
            var history = new List<object>();
            // PatientHistoryTextHelper has multiple note fields, combine them
            var query = @"
                SELECT TOP 50
                    ph.ID,
                    ph.PatientID,
                    COALESCE(ph.IllnessHistoryNotes, '') + 
                    COALESCE(ph.MedicationHistoryNotes, '') + 
                    COALESCE(ph.CardiacHistoryNotes, '') + 
                    COALESCE(ph.RiskFactorNotes, '') + 
                    COALESCE(ph.AllergyNotes, '') + 
                    COALESCE(ph.FamilyHistoryNotes, '') AS HistoryText,
                    ph.CreatedDate,
                    ph.CreatedDate
                FROM EMR.dbo.PatientHistoryTextHelper ph
                WHERE ph.PatientID = @PatientId AND ph.IsDeleted = 0
                ORDER BY ph.CreatedDate DESC";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                history.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.GetInt32(1),
                    historyText = reader.IsDBNull(2) ? null : reader.GetString(2),
                    historyDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3).ToString("yyyy-MM-ddTHH:mm:ss"),
                    createdDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4).ToString("yyyy-MM-ddTHH:mm:ss")
                });
            }
            return history;
        }

        private async Task<List<object>> GetRiskFactors(DbConnection conn, int patientId)
        {
            var riskFactors = new List<object>();
            var query = @"
                SELECT 
                    prf.ID,
                    prf.PatientID,
                    prf.RisckFactorID,
                    rf.Description AS RiskFactorDescription,
                    prf.Notes
                FROM EMR.dbo.PatientRiskFactor prf
                LEFT JOIN EMR.dbo.RiskFactor rf ON prf.RisckFactorID = rf.ID
                WHERE prf.PatientID = @PatientId AND prf.IsDeleted = 0";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                riskFactors.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.GetInt32(1),
                    riskFactorId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                    riskFactorDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                    comment = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
            return riskFactors;
        }

        private async Task<List<object>> GetCurrentIllness(DbConnection conn, int patientId)
        {
            var illnesses = new List<object>();
            var query = @"
                SELECT 
                    pci.ID,
                    pci.PatientID,
                    pci.Comments,
                    pci.CreatedDate,
                    NULL,
                    pci.Comments
                FROM EMR.dbo.PatientCurrentIllness pci
                WHERE pci.PatientID = @PatientId AND pci.IsDeleted = 0
                ORDER BY pci.CreatedDate DESC";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                illnesses.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.GetInt32(1),
                    illnessDescription = reader.IsDBNull(2) ? null : reader.GetString(2),
                    startDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3).ToString("yyyy-MM-ddTHH:mm:ss"),
                    endDate = reader.IsDBNull(4) ? null : (string?)null,
                    comment = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
            return illnesses;
        }

        private async Task<List<object>> GetCardiacHistory(DbConnection conn, int patientId)
        {
            var cardiacHistory = new List<object>();
            var query = @"
                SELECT TOP 50
                    pch.ID,
                    pch.PatientID,
                    pch.CardiacHistoryID,
                    ch.Description AS CardiacHistoryDescription,
                    pch.Comments,
                    pch.CreatedDate
                FROM EMR.dbo.PatientCardiacHX pch
                LEFT JOIN EMR.dbo.CardiacHistory ch ON pch.CardiacHistoryID = ch.ID
                WHERE pch.PatientID = @PatientId AND pch.IsDeleted = 0
                ORDER BY pch.CreatedDate DESC";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cardiacHistory.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.GetInt32(1),
                    cardiacHistoryId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                    cardiacHistoryDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                    comment = reader.IsDBNull(4) ? null : reader.GetString(4),
                    historyDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-ddTHH:mm:ss")
                });
            }
            return cardiacHistory;
        }

        private async Task<List<object>> GetMedicationHistory(DbConnection conn, int patientId)
        {
            var medicationHistory = new List<object>();
            // Note: StartDate and StopDate are int fields (likely date labels), not datetime
            var query = @"
                SELECT TOP 100
                    pmh.ID,
                    pmh.PatientID,
                    NULL AS MedicationName,
                    pmh.Strength,
                    NULL AS Frequency,
                    NULL AS StartDate,
                    NULL AS EndDate,
                    pmh.Notes
                FROM EMR.dbo.PatientMedicationHistory pmh
                WHERE pmh.PatientID = @PatientId AND pmh.IsDeleted = 0
                ORDER BY pmh.CreatedDate DESC";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var param = cmd.CreateParameter();
            param.ParameterName = "@PatientId";
            param.Value = patientId;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                medicationHistory.Add(new
                {
                    id = reader.GetInt32(0),
                    patientId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                    medicationName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    dosage = reader.IsDBNull(3) ? null : reader.GetString(3),
                    frequency = reader.IsDBNull(4) ? null : reader.GetString(4),
                    startDate = reader.IsDBNull(5) ? null : reader.GetString(5),
                    endDate = reader.IsDBNull(6) ? null : reader.GetString(6),
                    comment = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }
            return medicationHistory;
        }

        // ========== POST ENDPOINTS FOR ADDING RECORDS ==========

        /// <summary>
        /// Add a new vital sign record
        /// </summary>
        [HttpPost("vitalsign")]
        public async Task<ActionResult<object>> AddVitalSign([FromBody] AddVitalSignRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var query = @"
                    INSERT INTO EMR.dbo.PatientVitalSign 
                    (PatientID, VitalSignTypeID, Value, DateTaken, Notes, CreatedBy, CreatedDate, IsDeleted, MedicalUnit)
                    VALUES 
                    (@PatientID, @VitalSignTypeID, @Value, @DateTaken, @Notes, @CreatedBy, GETDATE(), 0, @MedicalUnit);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                
                var param1 = cmd.CreateParameter();
                param1.ParameterName = "@PatientID";
                param1.Value = request.PatientID;
                cmd.Parameters.Add(param1);

                var param2 = cmd.CreateParameter();
                param2.ParameterName = "@VitalSignTypeID";
                param2.Value = request.VitalSignTypeID ?? (object)DBNull.Value;
                cmd.Parameters.Add(param2);

                var param3 = cmd.CreateParameter();
                param3.ParameterName = "@Value";
                param3.Value = request.Value != null ? Convert.ToDecimal(request.Value) : (object)DBNull.Value;
                cmd.Parameters.Add(param3);

                var param4 = cmd.CreateParameter();
                param4.ParameterName = "@DateTaken";
                param4.Value = request.DateTaken ?? DateTime.Now;
                cmd.Parameters.Add(param4);

                var param5 = cmd.CreateParameter();
                param5.ParameterName = "@Notes";
                param5.Value = (object?)request.Notes ?? DBNull.Value;
                cmd.Parameters.Add(param5);

                var param6 = cmd.CreateParameter();
                param6.ParameterName = "@CreatedBy";
                param6.Value = request.CreatedBy;
                cmd.Parameters.Add(param6);

                var param7 = cmd.CreateParameter();
                param7.ParameterName = "@MedicalUnit";
                param7.Value = request.MedicalUnit;
                cmd.Parameters.Add(param7);

                var newId = await cmd.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                _logger.LogInformation("Added vital sign with ID {Id} for PatientID {PatientID}", id, request.PatientID);
                return Ok(new { id, message = "Vital sign added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vital sign for PatientID {PatientID}", request.PatientID);
                return StatusCode(500, new { message = "An error occurred while adding vital sign", error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new progress note
        /// </summary>
        [HttpPost("progressnote")]
        public async Task<ActionResult<object>> AddProgressNote([FromBody] AddProgressNoteRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var query = @"
                    INSERT INTO EMR.dbo.ProgressNotes 
                    (PatientID, AdmissionID, PhysicianID, Comments, CommentsHTML, Date, CreatedBy, CreatedDate, IsDeleted, MedicalUnitID)
                    VALUES 
                    (@PatientID, @AdmissionID, @PhysicianID, @Comments, @CommentsHTML, @Date, @CreatedBy, GETDATE(), 0, @MedicalUnitID);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                
                var param1 = cmd.CreateParameter();
                param1.ParameterName = "@PatientID";
                param1.Value = request.PatientID;
                cmd.Parameters.Add(param1);

                var param2 = cmd.CreateParameter();
                param2.ParameterName = "@AdmissionID";
                param2.Value = request.AdmissionID ?? (object)DBNull.Value;
                cmd.Parameters.Add(param2);

                var param3 = cmd.CreateParameter();
                param3.ParameterName = "@PhysicianID";
                param3.Value = request.PhysicianID;
                cmd.Parameters.Add(param3);

                var param4 = cmd.CreateParameter();
                param4.ParameterName = "@Comments";
                param4.Value = request.Comments ?? string.Empty;
                cmd.Parameters.Add(param4);

                var param5 = cmd.CreateParameter();
                param5.ParameterName = "@CommentsHTML";
                param5.Value = request.CommentsHTML ?? request.Comments ?? string.Empty;
                cmd.Parameters.Add(param5);

                var param6 = cmd.CreateParameter();
                param6.ParameterName = "@Date";
                param6.Value = request.Date ?? DateTime.Now;
                cmd.Parameters.Add(param6);

                var param7 = cmd.CreateParameter();
                param7.ParameterName = "@CreatedBy";
                param7.Value = request.CreatedBy;
                cmd.Parameters.Add(param7);

                var param8 = cmd.CreateParameter();
                param8.ParameterName = "@MedicalUnitID";
                param8.Value = request.MedicalUnitID ?? (object)DBNull.Value;
                cmd.Parameters.Add(param8);

                var newId = await cmd.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                _logger.LogInformation("Added progress note with ID {Id} for PatientID {PatientID}", id, request.PatientID);
                return Ok(new { id, message = "Progress note added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding progress note for PatientID {PatientID}", request.PatientID);
                return StatusCode(500, new { message = "An error occurred while adding progress note", error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new clinical examination
        /// </summary>
        [HttpPost("clinicalexamination")]
        public async Task<ActionResult<object>> AddClinicalExamination([FromBody] AddClinicalExaminationRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var query = @"
                    INSERT INTO EMR.dbo.ClinicExam 
                    (PatientID, AdmissionID, Comments, CommentsHTML, CreatedBy, CreatedDate, IsDeleted)
                    VALUES 
                    (@PatientID, @AdmissionID, @Comments, @CommentsHTML, @CreatedBy, GETDATE(), 0);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                
                var param1 = cmd.CreateParameter();
                param1.ParameterName = "@PatientID";
                param1.Value = request.PatientID;
                cmd.Parameters.Add(param1);

                var param2 = cmd.CreateParameter();
                param2.ParameterName = "@AdmissionID";
                param2.Value = request.AdmissionID ?? (object)DBNull.Value;
                cmd.Parameters.Add(param2);

                var param3 = cmd.CreateParameter();
                param3.ParameterName = "@Comments";
                param3.Value = request.Comments ?? string.Empty;
                cmd.Parameters.Add(param3);

                var param4 = cmd.CreateParameter();
                param4.ParameterName = "@CommentsHTML";
                param4.Value = request.CommentsHTML ?? request.Comments ?? string.Empty;
                cmd.Parameters.Add(param4);

                var param5 = cmd.CreateParameter();
                param5.ParameterName = "@CreatedBy";
                param5.Value = request.CreatedBy;
                cmd.Parameters.Add(param5);

                var newId = await cmd.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                _logger.LogInformation("Added clinical examination with ID {Id} for PatientID {PatientID}", id, request.PatientID);
                return Ok(new { id, message = "Clinical examination added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding clinical examination for PatientID {PatientID}", request.PatientID);
                return StatusCode(500, new { message = "An error occurred while adding clinical examination", error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new medication schedule
        /// </summary>
        [HttpPost("medication")]
        public async Task<ActionResult<object>> AddMedication([FromBody] AddMedicationRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var query = @"
                    INSERT INTO EMR.dbo.PatientMedicationSchedule 
                    (PatientID, Admission, ProductName, Quantity, Direction, ScheduledDate, Status, Comment, CreatedBy, CreatedDate, IsDeleted, onTheFly, MedicalUnit)
                    VALUES 
                    (@PatientID, @Admission, @ProductName, @Quantity, @Direction, @ScheduledDate, @Status, @Comment, @CreatedBy, GETDATE(), 0, 0, @MedicalUnit);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                
                cmd.Parameters.Add(CreateParameter(cmd, "@PatientID", request.PatientID));
                cmd.Parameters.Add(CreateParameter(cmd, "@Admission", request.Admission ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@ProductName", request.ProductName ?? string.Empty));
                cmd.Parameters.Add(CreateParameter(cmd, "@Quantity", request.Quantity ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@Direction", request.Direction ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@ScheduledDate", request.ScheduledDate ?? DateTime.Now));
                cmd.Parameters.Add(CreateParameter(cmd, "@Status", request.Status));
                cmd.Parameters.Add(CreateParameter(cmd, "@Comment", request.Comment ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@CreatedBy", request.CreatedBy));
                cmd.Parameters.Add(CreateParameter(cmd, "@MedicalUnit", request.MedicalUnit));

                var newId = await cmd.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                _logger.LogInformation("Added medication with ID {Id} for PatientID {PatientID}", id, request.PatientID);
                return Ok(new { id, message = "Medication added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding medication for PatientID {PatientID}", request.PatientID);
                return StatusCode(500, new { message = "An error occurred while adding medication", error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new risk factor
        /// </summary>
        [HttpPost("riskfactor")]
        public async Task<ActionResult<object>> AddRiskFactor([FromBody] AddRiskFactorRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var query = @"
                    INSERT INTO EMR.dbo.PatientRiskFactor 
                    (PatientID, RisckFactorID, Notes, CreatedBy, CreatedDate, IsDeleted)
                    VALUES 
                    (@PatientID, @RiskFactorID, @Notes, @CreatedBy, GETDATE(), 0);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                
                cmd.Parameters.Add(CreateParameter(cmd, "@PatientID", request.PatientID));
                cmd.Parameters.Add(CreateParameter(cmd, "@RiskFactorID", request.RiskFactorID ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@Notes", request.Notes ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@CreatedBy", request.CreatedBy));

                var newId = await cmd.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                _logger.LogInformation("Added risk factor with ID {Id} for PatientID {PatientID}", id, request.PatientID);
                return Ok(new { id, message = "Risk factor added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding risk factor for PatientID {PatientID}", request.PatientID);
                return StatusCode(500, new { message = "An error occurred while adding risk factor", error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new current illness
        /// </summary>
        [HttpPost("currentillness")]
        public async Task<ActionResult<object>> AddCurrentIllness([FromBody] AddCurrentIllnessRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var query = @"
                    INSERT INTO EMR.dbo.PatientCurrentIllness 
                    (PatientID, AdmissionID, Comments, CommentsHTML, CreatedBy, CreatedDate, IsDeleted)
                    VALUES 
                    (@PatientID, @AdmissionID, @Comments, @CommentsHTML, @CreatedBy, GETDATE(), 0);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                
                cmd.Parameters.Add(CreateParameter(cmd, "@PatientID", request.PatientID));
                cmd.Parameters.Add(CreateParameter(cmd, "@AdmissionID", request.AdmissionID ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@Comments", request.Comments ?? string.Empty));
                cmd.Parameters.Add(CreateParameter(cmd, "@CommentsHTML", request.CommentsHTML ?? request.Comments ?? string.Empty));
                cmd.Parameters.Add(CreateParameter(cmd, "@CreatedBy", request.CreatedBy));

                var newId = await cmd.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                _logger.LogInformation("Added current illness with ID {Id} for PatientID {PatientID}", id, request.PatientID);
                return Ok(new { id, message = "Current illness added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding current illness for PatientID {PatientID}", request.PatientID);
                return StatusCode(500, new { message = "An error occurred while adding current illness", error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new cardiac history
        /// </summary>
        [HttpPost("cardiachistory")]
        public async Task<ActionResult<object>> AddCardiacHistory([FromBody] AddCardiacHistoryRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var query = @"
                    INSERT INTO EMR.dbo.PatientCardiacHX 
                    (PatientID, CardiacHistoryID, Comments, CommentsHTML, CreatedBy, CreatedDate, IsDeleted)
                    VALUES 
                    (@PatientID, @CardiacHistoryID, @Comments, @CommentsHTML, @CreatedBy, GETDATE(), 0);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                
                cmd.Parameters.Add(CreateParameter(cmd, "@PatientID", request.PatientID));
                cmd.Parameters.Add(CreateParameter(cmd, "@CardiacHistoryID", request.CardiacHistoryID ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@Comments", request.Comments ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@CommentsHTML", request.CommentsHTML ?? request.Comments ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@CreatedBy", request.CreatedBy));

                var newId = await cmd.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                _logger.LogInformation("Added cardiac history with ID {Id} for PatientID {PatientID}", id, request.PatientID);
                return Ok(new { id, message = "Cardiac history added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding cardiac history for PatientID {PatientID}", request.PatientID);
                return StatusCode(500, new { message = "An error occurred while adding cardiac history", error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new medication history
        /// </summary>
        [HttpPost("medicationhistory")]
        public async Task<ActionResult<object>> AddMedicationHistory([FromBody] AddMedicationHistoryRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var query = @"
                    INSERT INTO EMR.dbo.PatientMedicationHistory 
                    (PatientID, Strength, Notes, NotesHTML, CreatedBy, CreatedDate, IsDeleted)
                    VALUES 
                    (@PatientID, @Strength, @Notes, @NotesHTML, @CreatedBy, GETDATE(), 0);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                
                cmd.Parameters.Add(CreateParameter(cmd, "@PatientID", request.PatientID));
                cmd.Parameters.Add(CreateParameter(cmd, "@Strength", request.Strength ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@Notes", request.Notes ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@NotesHTML", request.NotesHTML ?? request.Notes ?? (object)DBNull.Value));
                cmd.Parameters.Add(CreateParameter(cmd, "@CreatedBy", request.CreatedBy));

                var newId = await cmd.ExecuteScalarAsync();
                var id = Convert.ToInt32(newId);

                _logger.LogInformation("Added medication history with ID {Id} for PatientID {PatientID}", id, request.PatientID);
                return Ok(new { id, message = "Medication history added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding medication history for PatientID {PatientID}", request.PatientID);
                return StatusCode(500, new { message = "An error occurred while adding medication history", error = ex.Message });
            }
        }

        /// <summary>
        /// Update patient history text helper
        /// </summary>
        [HttpPost("patienthistory")]
        public async Task<ActionResult<object>> AddPatientHistory([FromBody] AddPatientHistoryRequest request)
        {
            try
            {
                await using var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                // Check if record exists
                var checkQuery = "SELECT ID FROM EMR.dbo.PatientHistoryTextHelper WHERE PatientID = @PatientID AND IsDeleted = 0";
                await using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = checkQuery;
                checkCmd.Parameters.Add(CreateParameter(checkCmd, "@PatientID", request.PatientID));
                
                var existingId = await checkCmd.ExecuteScalarAsync();

                if (existingId != null && existingId != DBNull.Value)
                {
                    // Update existing record
                    var updateQuery = @"
                        UPDATE EMR.dbo.PatientHistoryTextHelper 
                        SET IllnessHistoryNotes = COALESCE(@IllnessHistoryNotes, IllnessHistoryNotes),
                            MedicationHistoryNotes = COALESCE(@MedicationHistoryNotes, MedicationHistoryNotes),
                            CardiacHistoryNotes = COALESCE(@CardiacHistoryNotes, CardiacHistoryNotes),
                            RiskFactorNotes = COALESCE(@RiskFactorNotes, RiskFactorNotes),
                            AllergyNotes = COALESCE(@AllergyNotes, AllergyNotes),
                            FamilyHistoryNotes = COALESCE(@FamilyHistoryNotes, FamilyHistoryNotes),
                            ModifiedBy = @ModifiedBy,
                            ModifiedDate = GETDATE()
                        WHERE PatientID = @PatientID AND IsDeleted = 0;
                        SELECT @PatientID;";

                    await using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = updateQuery;
                    updateCmd.Parameters.Add(CreateParameter(updateCmd, "@PatientID", request.PatientID));
                    updateCmd.Parameters.Add(CreateParameter(updateCmd, "@IllnessHistoryNotes", request.IllnessHistoryNotes ?? (object)DBNull.Value));
                    updateCmd.Parameters.Add(CreateParameter(updateCmd, "@MedicationHistoryNotes", request.MedicationHistoryNotes ?? (object)DBNull.Value));
                    updateCmd.Parameters.Add(CreateParameter(updateCmd, "@CardiacHistoryNotes", request.CardiacHistoryNotes ?? (object)DBNull.Value));
                    updateCmd.Parameters.Add(CreateParameter(updateCmd, "@RiskFactorNotes", request.RiskFactorNotes ?? (object)DBNull.Value));
                    updateCmd.Parameters.Add(CreateParameter(updateCmd, "@AllergyNotes", request.AllergyNotes ?? (object)DBNull.Value));
                    updateCmd.Parameters.Add(CreateParameter(updateCmd, "@FamilyHistoryNotes", request.FamilyHistoryNotes ?? (object)DBNull.Value));
                    updateCmd.Parameters.Add(CreateParameter(updateCmd, "@ModifiedBy", request.CreatedBy));

                    await updateCmd.ExecuteScalarAsync();
                    _logger.LogInformation("Updated patient history for PatientID {PatientID}", request.PatientID);
                    return Ok(new { id = Convert.ToInt32(existingId), message = "Patient history updated successfully" });
                }
                else
                {
                    // Insert new record
                    var insertQuery = @"
                        INSERT INTO EMR.dbo.PatientHistoryTextHelper 
                        (PatientID, IllnessHistoryNotes, MedicationHistoryNotes, CardiacHistoryNotes, RiskFactorNotes, AllergyNotes, FamilyHistoryNotes, CreatedBy, CreatedDate, IsDeleted)
                        VALUES 
                        (@PatientID, @IllnessHistoryNotes, @MedicationHistoryNotes, @CardiacHistoryNotes, @RiskFactorNotes, @AllergyNotes, @FamilyHistoryNotes, @CreatedBy, GETDATE(), 0);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    await using var insertCmd = conn.CreateCommand();
                    insertCmd.CommandText = insertQuery;
                    insertCmd.Parameters.Add(CreateParameter(insertCmd, "@PatientID", request.PatientID));
                    insertCmd.Parameters.Add(CreateParameter(insertCmd, "@IllnessHistoryNotes", request.IllnessHistoryNotes ?? (object)DBNull.Value));
                    insertCmd.Parameters.Add(CreateParameter(insertCmd, "@MedicationHistoryNotes", request.MedicationHistoryNotes ?? (object)DBNull.Value));
                    insertCmd.Parameters.Add(CreateParameter(insertCmd, "@CardiacHistoryNotes", request.CardiacHistoryNotes ?? (object)DBNull.Value));
                    insertCmd.Parameters.Add(CreateParameter(insertCmd, "@RiskFactorNotes", request.RiskFactorNotes ?? (object)DBNull.Value));
                    insertCmd.Parameters.Add(CreateParameter(insertCmd, "@AllergyNotes", request.AllergyNotes ?? (object)DBNull.Value));
                    insertCmd.Parameters.Add(CreateParameter(insertCmd, "@FamilyHistoryNotes", request.FamilyHistoryNotes ?? (object)DBNull.Value));
                    insertCmd.Parameters.Add(CreateParameter(insertCmd, "@CreatedBy", request.CreatedBy));

                    var newId = await insertCmd.ExecuteScalarAsync();
                    var id = Convert.ToInt32(newId);
                    _logger.LogInformation("Added patient history with ID {Id} for PatientID {PatientID}", id, request.PatientID);
                    return Ok(new { id, message = "Patient history added successfully" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding patient history for PatientID {PatientID}", request.PatientID);
                return StatusCode(500, new { message = "An error occurred while adding patient history", error = ex.Message });
            }
        }

        // Helper method to create parameters
        private DbParameter CreateParameter(DbCommand cmd, string name, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            return param;
        }
    }

    // Request DTOs
    public class AddVitalSignRequest
    {
        public int PatientID { get; set; }
        public int? VitalSignTypeID { get; set; }
        public decimal? Value { get; set; }
        public DateTime? DateTaken { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
        public int MedicalUnit { get; set; }
    }

    public class AddProgressNoteRequest
    {
        public int PatientID { get; set; }
        public int? AdmissionID { get; set; }
        public int PhysicianID { get; set; }
        public string? Comments { get; set; }
        public string? CommentsHTML { get; set; }
        public DateTime? Date { get; set; }
        public int CreatedBy { get; set; }
        public int? MedicalUnitID { get; set; }
    }

    public class AddClinicalExaminationRequest
    {
        public int PatientID { get; set; }
        public int? AdmissionID { get; set; }
        public string? Comments { get; set; }
        public string? CommentsHTML { get; set; }
        public int CreatedBy { get; set; }
    }

    public class AddMedicationRequest
    {
        public int PatientID { get; set; }
        public int? Admission { get; set; }
        public string? ProductName { get; set; }
        public decimal? Quantity { get; set; }
        public string? Direction { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public int Status { get; set; }
        public string? Comment { get; set; }
        public int CreatedBy { get; set; }
        public int MedicalUnit { get; set; }
    }

    public class AddRiskFactorRequest
    {
        public int PatientID { get; set; }
        public int? RiskFactorID { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }

    public class AddCurrentIllnessRequest
    {
        public int PatientID { get; set; }
        public int? AdmissionID { get; set; }
        public string? Comments { get; set; }
        public string? CommentsHTML { get; set; }
        public int CreatedBy { get; set; }
    }

    public class AddCardiacHistoryRequest
    {
        public int PatientID { get; set; }
        public int? CardiacHistoryID { get; set; }
        public string? Comments { get; set; }
        public string? CommentsHTML { get; set; }
        public int CreatedBy { get; set; }
    }

    public class AddMedicationHistoryRequest
    {
        public int PatientID { get; set; }
        public string? Strength { get; set; }
        public string? Notes { get; set; }
        public string? NotesHTML { get; set; }
        public int CreatedBy { get; set; }
    }

    public class AddPatientHistoryRequest
    {
        public int PatientID { get; set; }
        public string? IllnessHistoryNotes { get; set; }
        public string? MedicationHistoryNotes { get; set; }
        public string? CardiacHistoryNotes { get; set; }
        public string? RiskFactorNotes { get; set; }
        public string? AllergyNotes { get; set; }
        public string? FamilyHistoryNotes { get; set; }
        public int CreatedBy { get; set; }
    }
}

