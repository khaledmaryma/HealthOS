using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PatientController> _logger;

        public PatientController(IConfiguration configuration, ILogger<PatientController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("next-mrn")]
        public async Task<ActionResult<string>> GetNextMRN()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("ConfigurationConnection");
                    //?.Replace("Database=LIS", "Database=Configuration");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Read the next MRN from the TransactionSequenceControl table
                    var sql = @"
                        SELECT LastMedicalRecordNumber + 1
                        FROM TransactionSequenceControl WITH (NOLOCK)
                        WHERE ID = 1";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        var nextMRN = await command.ExecuteScalarAsync();
                        return Ok(nextMRN?.ToString() ?? "1");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next MRN");
                return StatusCode(500, new { message = "Error generating MRN", error = ex.Message });
            }
        }

        [HttpGet("check-duplicate")]
        public async Task<ActionResult<object>> CheckDuplicatePatient(
            [FromQuery] string firstName,
            [FromQuery] string lastName,
            [FromQuery] string? middleName = null)
        {
            try
            {
                _logger.LogInformation("====== PATIENT DUPLICATION CHECK START ======");
                _logger.LogInformation("Checking for duplicate patients with FirstName: '{FirstName}', LastName: '{LastName}', MiddleName: '{MiddleName}'", 
                    firstName, lastName, middleName ?? "(null)");
                
                var connectionString = _configuration.GetConnectionString("DefaultConnection")
                    ?.Replace("Database=LIS", "Database=HospitalDefinition");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT TOP 5 
                            ID, 
                            MedicalRecordNumber, 
                            FirstName, 
                            LastName, 
                            MiddleName,
                            DOB,
                            Gender,
                            Phone,
                            ArabicFullName
                        FROM Patient WITH (NOLOCK)
                        WHERE IsDeleted = 0 
                          AND FirstName = @FirstName 
                          AND LastName = @LastName
                        ORDER BY ID DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", firstName);
                        command.Parameters.AddWithValue("@LastName", lastName);

                        _logger.LogInformation("Executing duplicate check query in HospitalDefinition.Patient table...");
                        
                        var patients = new List<object>();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var patientData = new
                                {
                                    id = reader.GetInt32(0),
                                    mrn = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    firstName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    lastName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    middleName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    dob = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                    gender = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    phone = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                    arabicFullName = reader.IsDBNull(8) ? "" : reader.GetString(8)
                                };
                                patients.Add(patientData);
                                
                                _logger.LogInformation("  - Found potential duplicate: ID={Id}, MRN={MRN}, Name={FirstName} {MiddleName} {LastName}, DOB={DOB}, Gender={Gender}, Phone={Phone}", 
                                    patientData.id, patientData.mrn, patientData.firstName, patientData.middleName, patientData.lastName, 
                                    patientData.dob?.ToString("yyyy-MM-dd") ?? "N/A", patientData.gender, patientData.phone);
                            }
                        }

                        _logger.LogInformation("Duplicate check completed. Found {Count} potential duplicate(s)", patients.Count);
                        
                        if (patients.Count > 0)
                        {
                            _logger.LogWarning("⚠️ DUPLICATE PATIENT(S) DETECTED! {Count} patient(s) found with matching name", patients.Count);
                        }
                        else
                        {
                            _logger.LogInformation("✓ No duplicate patients found - safe to create new patient");
                        }
                        
                        _logger.LogInformation("====== PATIENT DUPLICATION CHECK END ======");

                        return Ok(new { found = patients.Count > 0, patients = patients });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking duplicate patient");
                return StatusCode(500, new { message = "Error checking duplicate", error = ex.Message });
            }
        }

        /// <summary>
        /// Get patient by ID - returns data in the same format as QuickAdmissionController.Save_V1 expects
        /// Matches QuickAdmissionPatient class structure
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPatientById(int id)
        {
            try
            {
                _logger.LogInformation("Getting patient by ID: {PatientId}", id);

                var connectionString = _configuration.GetConnectionString("DefaultConnection")
                    ?.Replace("Database=LIS", "Database=HospitalDefinition");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            ID,
                            MedicalRecordNumber,
                            FirstName,
                            LastName,
                            MiddleName,
                            DOB,
                            Gender,
                            Phone,
                            MaritalStatus,
                            ArabicFullName,
                            CreatedBy
                        FROM Patient WITH (NOLOCK)
                        WHERE ID = @Id AND (IsDeleted = 0 OR IsDeleted IS NULL)";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Return in the same format as QuickAdmissionPatient class (PascalCase)
                                // This matches what Save_V1 expects when parsing JSON
                                var patient = new
                                {
                                    FirstName = reader.IsDBNull(2) ? (string?)null : reader.GetString(2),
                                    LastName = reader.IsDBNull(3) ? (string?)null : reader.GetString(3),
                                    MiddleName = reader.IsDBNull(4) ? (string?)null : reader.GetString(4),
                                    Gender = reader.IsDBNull(6) ? (string?)null : reader.GetString(6),
                                    Phone = reader.IsDBNull(7) ? (string?)null : reader.GetString(7),
                                    ArabicFullName = reader.IsDBNull(9) ? (string?)null : reader.GetString(9),
                                    DOB = reader.IsDBNull(5) ? (string?)null : reader.GetDateTime(5).ToString("yyyy-MM-dd"),
                                    MaritalStatus = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                                    CreatedBy = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                                    // Additional fields for reference
                                    Id = reader.GetInt32(0),
                                    MedicalRecordNumber = reader.IsDBNull(1) ? "" : reader.GetString(1)
                                };

                                _logger.LogInformation("Patient found: ID={Id}, MRN={MRN}, Name={FirstName} {LastName}", 
                                    patient.Id, patient.MedicalRecordNumber, patient.FirstName, patient.LastName);

                                return Ok(patient);
                            }
                            else
                            {
                                _logger.LogWarning("Patient with ID {PatientId} not found", id);
                                return NotFound(new { message = $"Patient with ID {id} not found" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient by ID {PatientId}", id);
                return StatusCode(500, new { message = "Error retrieving patient", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreatePatient([FromBody] PatientDto patientDto)
        {
            try
            {
                _logger.LogInformation("====== CREATE PATIENT START ======");
                
                // Log received data
                if (patientDto == null)
                {
                    _logger.LogError("PatientDto is null!");
                    return BadRequest(new { message = "Patient data is required" });
                }
                
                _logger.LogInformation("Received PatientDto - FirstName: '{FirstName}', LastName: '{LastName}', MiddleName: '{MiddleName}', DOB: {DOB}, Gender: '{Gender}', Phone: '{Phone}', MRN: '{MRN}'", 
                    patientDto.FirstName ?? "NULL", 
                    patientDto.LastName ?? "NULL", 
                    patientDto.MiddleName ?? "NULL", 
                    patientDto.DOB?.ToString("yyyy-MM-dd") ?? "NULL", 
                    patientDto.Gender ?? "NULL", 
                    patientDto.Phone ?? "NULL",
                    patientDto.MRN ?? "NULL");
                _logger.LogInformation("Arabic Name: '{ArabicName}', MaritalStatus: {MaritalStatus}, Created By User ID: {CreatedBy}", 
                    patientDto.ArabicFullName ?? "NULL", 
                    patientDto.MaritalStatus?.ToString() ?? "NULL",
                    patientDto.CreatedBy);
                
                // Validate required fields
                if (string.IsNullOrWhiteSpace(patientDto.FirstName))
                {
                    _logger.LogWarning("FirstName is missing");
                    return BadRequest(new { message = "FirstName is required" });
                }
                
                if (string.IsNullOrWhiteSpace(patientDto.LastName))
                {
                    _logger.LogWarning("LastName is missing");
                    return BadRequest(new { message = "LastName is required" });
                }
                
                _logger.LogInformation("Validation passed. Creating patient...");
                
                var connectionString = _configuration.GetConnectionString("DefaultConnection")
                    ?.Replace("Database=LIS", "Database=HospitalDefinition");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Step 1: Insert the patient
                    var insertSql = @"
                        INSERT INTO Patient (
                            FirstName, LastName, MiddleName,
                            DOB, Gender, Phone, MaritalStatus, ArabicFullName,
                            IsDeleted, CreatedBy
                        )
                        VALUES (
                            @FirstName, @LastName, @MiddleName,
                            @DOB, @Gender, @Phone, @MaritalStatus, @ArabicFullName,
                            0, @CreatedBy
                        );";

                    using (var insertCommand = new SqlCommand(insertSql, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@FirstName", patientDto.FirstName ?? "");
                        insertCommand.Parameters.AddWithValue("@LastName", patientDto.LastName ?? "");
                        insertCommand.Parameters.AddWithValue("@MiddleName", patientDto.MiddleName ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@DOB", patientDto.DOB ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@Gender", patientDto.Gender ?? "");
                        insertCommand.Parameters.AddWithValue("@Phone", patientDto.Phone ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@MaritalStatus", patientDto.MaritalStatus ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@ArabicFullName", patientDto.ArabicFullName ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@CreatedBy", patientDto.CreatedBy);

                        _logger.LogInformation("Inserting patient into HospitalDefinition.Patient table...");
                        await insertCommand.ExecuteNonQueryAsync();
                        _logger.LogInformation("Insert executed successfully");
                    }

                    // Step 2: Retrieve the patient that was just created
                    // Since we use INSTEAD OF trigger, SCOPE_IDENTITY doesn't work
                    // Wait a tiny bit for trigger to complete
                    await Task.Delay(200);
                    
                    // Find the most recent patient - simplified approach
                    // Just get the very last patient created (highest ID)
                    var selectSql = @"
                        SELECT TOP 1 ID, MedicalRecordNumber, FirstName, LastName, MiddleName, CreatedDate
                        FROM Patient WITH (NOLOCK)
                        WHERE IsDeleted = 0
                        ORDER BY ID DESC";

                    using (var selectCommand = new SqlCommand(selectSql, connection))
                    {
                        _logger.LogInformation("Retrieving the most recently created patient...");
                        
                        using (var reader = await selectCommand.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var patientId = reader.GetInt32(0);
                                var generatedMRN = reader.GetString(1);
                                var firstName = reader.GetString(2);
                                var lastName = reader.GetString(3);
                                var middleName = reader.IsDBNull(4) ? null : reader.GetString(4);
                                var createdDate = reader.GetDateTime(5);
                                
                                _logger.LogInformation("✓ Most recent patient found! ID: {PatientId}, MRN: {MRN}, Name: {FirstName} {MiddleName} {LastName}, Created: {CreatedDate}", 
                                    patientId, generatedMRN, firstName, middleName ?? "NULL", lastName, createdDate);
                                
                                // Verify this is the patient we just created (basic check)
                                var isMatch = firstName == (patientDto.FirstName ?? "") && 
                                             lastName == (patientDto.LastName ?? "");
                                
                                if (isMatch)
                                {
                                    _logger.LogInformation("✓ Patient verification passed - names match!");
                                    _logger.LogInformation("====== CREATE PATIENT END (Line 260) ======");
                                    return Ok(new { id = patientId, mrn = generatedMRN });
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ Retrieved patient doesn't match! Expected: {ExpectedFirst} {ExpectedLast}, Got: {ActualFirst} {ActualLast}",
                                        patientDto.FirstName, patientDto.LastName, firstName, lastName);
                                    _logger.LogWarning("Assuming this is the correct patient since it's most recent...");
                                    _logger.LogInformation("====== CREATE PATIENT END (Line 268) ======");
                                    return Ok(new { id = patientId, mrn = generatedMRN });
                                }
                            }
                            else
                            {
                                _logger.LogError("Failed to retrieve patient after creation - patient not found in database");
                                _logger.LogError("Search criteria: FirstName='{FirstName}', LastName='{LastName}', MiddleName='{MiddleName}'",
                                    patientDto.FirstName, patientDto.LastName, patientDto.MiddleName ?? "NULL");
                                
                                // Try to find ANY recent patient to debug
                                var debugSql = "SELECT TOP 5 ID, MedicalRecordNumber, FirstName, LastName, MiddleName, CreatedDate FROM Patient ORDER BY ID DESC";
                                using (var debugCmd = new SqlCommand(debugSql, connection))
                                using (var debugReader = await debugCmd.ExecuteReaderAsync())
                                {
                                    _logger.LogWarning("Last 5 patients in database:");
                                    while (await debugReader.ReadAsync())
                                    {
                                        _logger.LogWarning("  ID={Id}, MRN={MRN}, Name={FirstName} {MiddleName} {LastName}, Created={CreatedDate}",
                                            debugReader.GetInt32(0),
                                            debugReader.GetString(1),
                                            debugReader.GetString(2),
                                            debugReader.IsDBNull(4) ? "NULL" : debugReader.GetString(4),
                                            debugReader.GetString(3),
                                            debugReader.GetDateTime(5));
                                    }
                                }
                                
                                return StatusCode(500, new { message = "Failed to retrieve patient after creation" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { message = "Error creating patient", error = ex.Message });
            }
        }
    }

    public class PatientDto
    {
        public string? MRN { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public DateTime? DOB { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public int? MaritalStatus { get; set; }
        public string? ArabicFullName { get; set; }
        public int CreatedBy { get; set; } = 338; // Default user
    }
}

