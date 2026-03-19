using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdmissionController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<AdmissionController> _logger;
        private readonly IConfiguration _configuration;

        public AdmissionController(LISDbContext context, ILogger<AdmissionController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Create a new admission
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> CreateAdmission([FromBody] CreateAdmissionRequest request)
        {
            try
            {
                _logger.LogInformation("=== CREATE ADMISSION REQUEST START ===");
                _logger.LogInformation("Request received: {Request}", System.Text.Json.JsonSerializer.Serialize(request));
                
                if (request == null)
                {
                    _logger.LogWarning("Admission request is null");
                    return BadRequest("Admission data is required");
                }

                // Validate required fields
                if (request.Patient <= 0)
                {
                    return BadRequest("Patient ID is required");
                }

                if (request.ReferralPhysician <= 0)
                {
                    return BadRequest("Referral Physician is required");
                }

                if (string.IsNullOrEmpty(request.CheckInDate))
                {
                    return BadRequest("Check-in Date is required");
                }

                // Don't generate admission number - let the database trigger handle it
                // The trigger will generate the number based on admission type and date
                
                // Prepare admission data for database
                var admissionData = new
                {
                    Number = (string?)null, // Let trigger generate this
                    AdmissionSite = request.AdmissionSite,
                    ReferralPhysician = request.ReferralPhysician,
                    AttendingPhysician = request.AttendingPhysician ?? request.ReferralPhysician,
                    MainInsurance = request.MainInsurance,
                    MainInsuranceClass = request.MainInsuranceClass,
                    Insured = request.Insured,
                    AuxiliaryInsurance = request.AuxiliaryInsurance,
                    AuxiliaryInsuranceClass = request.AuxiliaryInsuranceClass,
                    CheckInClass = request.CheckInClass,
                    Department = request.Department,
                    CheckInDate = DateTime.TryParse(request.CheckInDate, out var checkInDate) ? checkInDate : DateTime.Now,
                    CheckOutDate = request.CheckOutDate != null && DateTime.TryParse(request.CheckOutDate, out var checkOutDate) ? checkOutDate : (DateTime?)null,
                    Patient = request.Patient,
                    Type = request.Type,
                    IsWorkAccident = request.IsWorkAccident,
                    IsExtended = request.IsExtended,
                    Group = request.Group,
                    CreatedBy = request.CreatedBy,
                    CreatedDate = DateTime.Now,
                    IsDeleted = false
                };

                // Insert into Admission database using raw SQL
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Admission.dbo.Admission 
                    (Number, AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
                     MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
                     CheckInClass, Department, CheckInDate, CheckOutDate, Patient, Type, IsWorkAccident, 
                     IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted)
                    VALUES 
                    (@Number, @AdmissionSite, @ReferralPhysician, @AttendingPhysician, @MainInsurance,
                     @MainInsuranceClass, @Insured, @AuxiliaryInsurance, @AuxiliaryInsuranceClass,
                     @CheckInClass, @Department, @CheckInDate, @CheckOutDate, @Patient, @Type, @IsWorkAccident,
                     @IsExtended, @Group, @CreatedBy, @CreatedDate, @IsDeleted);
                    SELECT SCOPE_IDENTITY();";

                // Add parameters
                command.Parameters.Add(new SqlParameter("@Number", admissionData.Number ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@AdmissionSite", admissionData.AdmissionSite));
                command.Parameters.Add(new SqlParameter("@ReferralPhysician", admissionData.ReferralPhysician));
                command.Parameters.Add(new SqlParameter("@AttendingPhysician", admissionData.AttendingPhysician));
                command.Parameters.Add(new SqlParameter("@MainInsurance", admissionData.MainInsurance));
                command.Parameters.Add(new SqlParameter("@MainInsuranceClass", admissionData.MainInsuranceClass));
                command.Parameters.Add(new SqlParameter("@Insured", admissionData.Insured));
                command.Parameters.Add(new SqlParameter("@AuxiliaryInsurance", admissionData.AuxiliaryInsurance));
                command.Parameters.Add(new SqlParameter("@AuxiliaryInsuranceClass", admissionData.AuxiliaryInsuranceClass));
                command.Parameters.Add(new SqlParameter("@CheckInClass", admissionData.CheckInClass));
                command.Parameters.Add(new SqlParameter("@Department", admissionData.Department ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@CheckInDate", admissionData.CheckInDate));
                command.Parameters.Add(new SqlParameter("@CheckOutDate", admissionData.CheckOutDate ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Patient", admissionData.Patient));
                command.Parameters.Add(new SqlParameter("@Type", admissionData.Type));
                command.Parameters.Add(new SqlParameter("@IsWorkAccident", admissionData.IsWorkAccident));
                command.Parameters.Add(new SqlParameter("@IsExtended", admissionData.IsExtended));
                command.Parameters.Add(new SqlParameter("@Group", admissionData.Group));
                command.Parameters.Add(new SqlParameter("@CreatedBy", admissionData.CreatedBy));
                command.Parameters.Add(new SqlParameter("@CreatedDate", admissionData.CreatedDate));
                command.Parameters.Add(new SqlParameter("@IsDeleted", admissionData.IsDeleted));

                var admissionId = await command.ExecuteScalarAsync();
                var admissionIdInt = Convert.ToInt32(admissionId);
                
                // Get the generated admission number from the database
                string generatedNumber = "";
                using var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = "SELECT Number FROM Admission.dbo.Admission WHERE ID = @AdmissionId AND (IsDeleted = 0 OR IsDeleted IS NULL)";
                selectCommand.Parameters.Add(new SqlParameter("@AdmissionId", admissionIdInt));
                
                var generatedNumberResult = await selectCommand.ExecuteScalarAsync();
                if (generatedNumberResult != null)
                {
                    generatedNumber = generatedNumberResult.ToString() ?? "";
                }

                _logger.LogInformation("Created admission {AdmissionId} with number {AdmissionNumber} for patient {PatientId}", 
                    admissionIdInt, generatedNumber, request.Patient);

                // Sync ResidentPatient table after admission creation
                try
                {
                    await SyncResidentPatient(connection, admissionIdInt, request.Patient, generatedNumber, admissionData, request.CreatedBy, isUpdate: false);
                }
                catch (Exception syncEx)
                {
                    _logger.LogError(syncEx, "Error syncing ResidentPatient table for admission {AdmissionId}", admissionIdInt);
                    // Don't fail the admission creation if ResidentPatient sync fails
                }

                return Ok(new
                {
                    id = admissionIdInt,
                    number = generatedNumber,
                    patientId = request.Patient,
                    checkInDate = admissionData.CheckInDate.ToString("yyyy-MM-dd"),
                    referralPhysician = request.ReferralPhysician,
                    attendingPhysician = admissionData.AttendingPhysician
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ADMISSION SAVE ERROR ===");
                _logger.LogError("Error creating admission for patient {PatientId}", request?.Patient);
                _logger.LogError("Exception type: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("Exception message: {Message}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerException}", ex.InnerException.Message);
                }
                
                return StatusCode(500, $"An error occurred while creating the admission: {ex.Message}");
            }
        }

        /// <summary>
        /// Update an existing admission
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateAdmission(int id, [FromBody] UpdateAdmissionRequest request)
        {
            try
            {
                _logger.LogInformation("=== UPDATE ADMISSION REQUEST START ===");
                _logger.LogInformation("Admission ID: {AdmissionId}, Request: {Request}", id, System.Text.Json.JsonSerializer.Serialize(request));
                
                if (request == null)
                {
                    _logger.LogWarning("Admission request is null");
                    return BadRequest("Admission data is required");
                }

                // Validate required fields
                if (request.Patient <= 0)
                {
                    return BadRequest("Patient ID is required");
                }

                if (request.ReferralPhysician <= 0)
                {
                    return BadRequest("Referral Physician is required");
                }

                if (string.IsNullOrEmpty(request.CheckInDate))
                {
                    return BadRequest("Check-in Date is required");
                }

                // Prepare admission data for database
                var admissionData = new
                {
                    AdmissionSite = request.AdmissionSite,
                    ReferralPhysician = request.ReferralPhysician,
                    AttendingPhysician = request.AttendingPhysician ?? request.ReferralPhysician,
                    MainInsurance = request.MainInsurance,
                    MainInsuranceClass = request.MainInsuranceClass,
                    Insured = request.Insured,
                    AuxiliaryInsurance = request.AuxiliaryInsurance,
                    AuxiliaryInsuranceClass = request.AuxiliaryInsuranceClass,
                    CheckInClass = request.CheckInClass,
                    Department = request.Department,
                    CheckInDate = DateTime.TryParse(request.CheckInDate, out var checkInDate) ? checkInDate : DateTime.Now,
                    CheckOutDate = request.CheckOutDate != null && DateTime.TryParse(request.CheckOutDate, out var checkOutDate) ? checkOutDate : (DateTime?)null,
                    Patient = request.Patient,
                    Type = request.Type,
                    IsWorkAccident = request.IsWorkAccident,
                    IsExtended = request.IsExtended,
                    Group = request.Group,
                    ModifiedBy = request.ModifiedBy,
                    ModifiedDate = DateTime.Now
                };

                // Update Admission database using raw SQL
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get existing admission number
                string admissionNumber = "";
                using var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = "SELECT Number FROM Admission.dbo.Admission WHERE ID = @AdmissionId AND (IsDeleted = 0 OR IsDeleted IS NULL)";
                selectCommand.Parameters.Add(new SqlParameter("@AdmissionId", id));
                
                var admissionNumberResult = await selectCommand.ExecuteScalarAsync();
                if (admissionNumberResult != null)
                {
                    admissionNumber = admissionNumberResult.ToString() ?? "";
                }

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Admission.dbo.Admission 
                    SET AdmissionSite = @AdmissionSite,
                        ReferralPhysician = @ReferralPhysician,
                        AttendingPhysician = @AttendingPhysician,
                        MainInsurance = @MainInsurance,
                        MainInsuranceClass = @MainInsuranceClass,
                        Insured = @Insured,
                        AuxiliaryInsurance = @AuxiliaryInsurance,
                        AuxiliaryInsuranceClass = @AuxiliaryInsuranceClass,
                        CheckInClass = @CheckInClass,
                        Department = @Department,
                        CheckInDate = @CheckInDate,
                        CheckOutDate = @CheckOutDate,
                        Patient = @Patient,
                        Type = @Type,
                        IsWorkAccident = @IsWorkAccident,
                        IsExtended = @IsExtended,
                        [Group] = @Group,
                        ModifiedBy = @ModifiedBy,
                        ModifiedDate = @ModifiedDate
                    WHERE ID = @AdmissionId AND (IsDeleted = 0 OR IsDeleted IS NULL)";

                // Add parameters
                command.Parameters.Add(new SqlParameter("@AdmissionId", id));
                command.Parameters.Add(new SqlParameter("@AdmissionSite", admissionData.AdmissionSite));
                command.Parameters.Add(new SqlParameter("@ReferralPhysician", admissionData.ReferralPhysician));
                command.Parameters.Add(new SqlParameter("@AttendingPhysician", admissionData.AttendingPhysician));
                command.Parameters.Add(new SqlParameter("@MainInsurance", admissionData.MainInsurance));
                command.Parameters.Add(new SqlParameter("@MainInsuranceClass", admissionData.MainInsuranceClass));
                command.Parameters.Add(new SqlParameter("@Insured", admissionData.Insured));
                command.Parameters.Add(new SqlParameter("@AuxiliaryInsurance", admissionData.AuxiliaryInsurance));
                command.Parameters.Add(new SqlParameter("@AuxiliaryInsuranceClass", admissionData.AuxiliaryInsuranceClass));
                command.Parameters.Add(new SqlParameter("@CheckInClass", admissionData.CheckInClass));
                command.Parameters.Add(new SqlParameter("@Department", admissionData.Department ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@CheckInDate", admissionData.CheckInDate));
                command.Parameters.Add(new SqlParameter("@CheckOutDate", admissionData.CheckOutDate ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Patient", admissionData.Patient));
                command.Parameters.Add(new SqlParameter("@Type", admissionData.Type));
                command.Parameters.Add(new SqlParameter("@IsWorkAccident", admissionData.IsWorkAccident));
                command.Parameters.Add(new SqlParameter("@IsExtended", admissionData.IsExtended));
                command.Parameters.Add(new SqlParameter("@Group", admissionData.Group));
                command.Parameters.Add(new SqlParameter("@ModifiedBy", admissionData.ModifiedBy));
                command.Parameters.Add(new SqlParameter("@ModifiedDate", admissionData.ModifiedDate));

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    return NotFound(new { message = $"Admission with ID {id} not found" });
                }

                _logger.LogInformation("Updated admission {AdmissionId} with number {AdmissionNumber} for patient {PatientId}", 
                    id, admissionNumber, request.Patient);

                // Sync ResidentPatient table after admission update
                try
                {
                    await SyncResidentPatient(connection, id, request.Patient, admissionNumber, admissionData, admissionData.ModifiedBy, isUpdate: true);
                }
                catch (Exception syncEx)
                {
                    _logger.LogError(syncEx, "Error syncing ResidentPatient table for admission {AdmissionId}", id);
                    // Don't fail the admission update if ResidentPatient sync fails
                }

                return Ok(new
                {
                    id = id,
                    number = admissionNumber,
                    patientId = request.Patient,
                    checkInDate = admissionData.CheckInDate.ToString("yyyy-MM-dd"),
                    referralPhysician = request.ReferralPhysician,
                    attendingPhysician = admissionData.AttendingPhysician
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ADMISSION UPDATE ERROR ===");
                _logger.LogError("Error updating admission {AdmissionId} for patient {PatientId}", id, request?.Patient);
                return StatusCode(500, $"An error occurred while updating the admission: {ex.Message}");
            }
        }

        /// <summary>
        /// Sync ResidentPatient table after admission create/update
        /// </summary>
        private async Task SyncResidentPatient(
            SqlConnection connection, 
            int admissionId, 
            int patientId, 
            string admissionNumber,
            dynamic admissionData,
            int userId,
            bool isUpdate)
        {
            try
            {
                _logger.LogInformation("Syncing ResidentPatient table for Admission {AdmissionId}, Patient {PatientId}, IsUpdate: {IsUpdate}", 
                    admissionId, patientId, isUpdate);

                // Get patient information from HospitalDefinition database
                // Patient table is in HospitalDefinition database, so we need a separate connection
                var hospitalDefConnectionString = _configuration.GetConnectionString("HospitalDefinitionConnection");
                if (string.IsNullOrEmpty(hospitalDefConnectionString))
                {
                    _logger.LogError("HospitalDefinitionConnection string is not configured");
                    return;
                }

                var patientQuery = @"
                    SELECT 
                        p.ID,
                        p.MedicalRecordNumber,
                        p.FirstName,
                        p.LastName,
                        p.MiddleName,
                        p.FirstNameArabic,
                        p.LastNameArabic,
                        p.MiddleNameArabic,
                        ISNULL(p.DateOfBirth, p.DOB) as DateOfBirth,
                        p.Gender,
                        p.Phone,
                        ISNULL(p.MRN, 0) as MRN
                    FROM Patient p
                    WHERE p.ID = @PatientId AND (p.IsDeleted = 0 OR p.IsDeleted IS NULL)";

                var patientInfo = new
                {
                    MedicalRecordNumber = "",
                    FirstName = "",
                    LastName = "",
                    MiddleName = "",
                    FirstNameArabic = "",
                    LastNameArabic = "",
                    MiddleNameArabic = "",
                    DateOfBirth = DateTime.MinValue,
                    Gender = "",
                    Phone = "",
                    MRN = 0
                };

                // Use HospitalDefinition connection for patient query
                using (var hospitalDefConnection = new SqlConnection(hospitalDefConnectionString))
                {
                    await hospitalDefConnection.OpenAsync();
                    using (var patientCmd = new SqlCommand(patientQuery, hospitalDefConnection))
                    {
                        patientCmd.Parameters.AddWithValue("@PatientId", patientId);
                        using (var patientReader = await patientCmd.ExecuteReaderAsync())
                        {
                            if (await patientReader.ReadAsync())
                            {
                                patientInfo = new
                                {
                                    MedicalRecordNumber = patientReader.IsDBNull(patientReader.GetOrdinal("MedicalRecordNumber")) ? "" : patientReader.GetString(patientReader.GetOrdinal("MedicalRecordNumber")),
                                    FirstName = patientReader.IsDBNull(patientReader.GetOrdinal("FirstName")) ? "" : patientReader.GetString(patientReader.GetOrdinal("FirstName")),
                                    LastName = patientReader.IsDBNull(patientReader.GetOrdinal("LastName")) ? "" : patientReader.GetString(patientReader.GetOrdinal("LastName")),
                                    MiddleName = patientReader.IsDBNull(patientReader.GetOrdinal("MiddleName")) ? "" : patientReader.GetString(patientReader.GetOrdinal("MiddleName")),
                                    FirstNameArabic = patientReader.IsDBNull(patientReader.GetOrdinal("FirstNameArabic")) ? "" : patientReader.GetString(patientReader.GetOrdinal("FirstNameArabic")),
                                    LastNameArabic = patientReader.IsDBNull(patientReader.GetOrdinal("LastNameArabic")) ? "" : patientReader.GetString(patientReader.GetOrdinal("LastNameArabic")),
                                    MiddleNameArabic = patientReader.IsDBNull(patientReader.GetOrdinal("MiddleNameArabic")) ? "" : patientReader.GetString(patientReader.GetOrdinal("MiddleNameArabic")),
                                    DateOfBirth = patientReader.IsDBNull(patientReader.GetOrdinal("DateOfBirth")) ? DateTime.MinValue : patientReader.GetDateTime(patientReader.GetOrdinal("DateOfBirth")),
                                    Gender = patientReader.IsDBNull(patientReader.GetOrdinal("Gender")) ? "" : patientReader.GetString(patientReader.GetOrdinal("Gender")),
                                    Phone = patientReader.IsDBNull(patientReader.GetOrdinal("Phone")) ? "" : patientReader.GetString(patientReader.GetOrdinal("Phone")),
                                    MRN = patientReader.IsDBNull(patientReader.GetOrdinal("MRN")) ? 0 : patientReader.GetInt32(patientReader.GetOrdinal("MRN"))
                                };
                                _logger.LogInformation("Retrieved patient info: Name={FirstName} {LastName}, MRN={MRN}", patientInfo.FirstName, patientInfo.LastName, patientInfo.MRN);
                            }
                            else
                            {
                                _logger.LogWarning("Patient {PatientId} not found in HospitalDefinition.Patient table", patientId);
                                return; // Can't proceed without patient info
                            }
                        }
                    }
                }

                // Calculate age
                int? age = null;
                if (patientInfo.DateOfBirth != DateTime.MinValue)
                {
                    var today = DateTime.Today;
                    age = today.Year - patientInfo.DateOfBirth.Year;
                    if (patientInfo.DateOfBirth.Date > today.AddYears(-age.Value))
                        age--;
                }

                // Build patient name
                var patientName = $"{patientInfo.FirstName} {patientInfo.MiddleName} {patientInfo.LastName}".Trim();
                var arabicFullName = $"{patientInfo.FirstNameArabic} {patientInfo.MiddleNameArabic} {patientInfo.LastNameArabic}".Trim();

                // Get lookup data (Insurance, Physician, Class, etc.)
                // Use HospitalDefinition connection for lookup data
                var lookupData = await GetLookupData(hospitalDefConnectionString, admissionData);

                // Check if ResidentPatient record already exists
                int? existingResidentPatientId = null;
                using (var checkCmd = new SqlCommand(
                    "SELECT ID FROM Admission.dbo.ResidentPatient WHERE Admission = @AdmissionId AND (IsDeleted = 0 OR IsDeleted IS NULL)", 
                    connection))
                {
                    checkCmd.Parameters.AddWithValue("@AdmissionId", admissionId);
                    var existingId = await checkCmd.ExecuteScalarAsync();
                    if (existingId != null && existingId != DBNull.Value)
                    {
                        existingResidentPatientId = Convert.ToInt32(existingId);
                    }
                }

                if (existingResidentPatientId.HasValue && isUpdate)
                {
                    // Update existing record
                    var updateQuery = @"
                        UPDATE Admission.dbo.ResidentPatient
                        SET PatientID = @PatientID,
                            MRN = @MRN,
                            AdmissionNumber = @AdmissionNumber,
                            PatientName = @PatientName,
                            ArabicFullName = @ArabicFullName,
                            MedicalRecordNumber = @MedicalRecordNumber,
                            PatientDOB = @PatientDOB,
                            Age = @Age,
                            PatientGender = @PatientGender,
                            CheckInDate = @CheckInDate,
                            CheckInClassID = @CheckInClassID,
                            CheckInClassDescription = @CheckInClassDescription,
                            MainInsuranceID = @MainInsuranceID,
                            MainInsuranceDescription = @MainInsuranceDescription,
                            MainInsuranceClassID = @MainInsuranceClassID,
                            MainInsuranceClassDescription = @MainInsuranceClassDescription,
                            ReferralPhysicianID = @ReferralPhysicianID,
                            ReferralPhysicianName = @ReferralPhysicianName,
                            AttendingPhysicianID = @AttendingPhysicianID,
                            AttendingPhysicianName = @AttendingPhysicianName,
                            MedicationUnitID = @MedicationUnitID,
                            MedicationUnitDescription = @MedicationUnitDescription,
                            InsuranceID = @InsuranceID,
                            InsuranceDescription = @InsuranceDescription,
                            GuarantorID = @GuarantorID,
                            GuarantorDescription = @GuarantorDescription,
                            CurrencyID = @CurrencyID,
                            CurrencyDescription = @CurrencyDescription,
                            ClassID = @ClassID,
                            ClassDescription = @ClassDescription,
                            ContextPriceID = @ContextPriceID,
                            ContextPriceDescription = @ContextPriceDescription,
                            ContextEnumerationID = @ContextEnumerationID,
                            ContextEnumerationDescription = @ContextEnumerationDescription,
                            AdmissionType = @AdmissionType,
                            AdmissionTypeDescription = @AdmissionTypeDescription,
                            Contact = @Contact,
                            AuxiliaryInsuranceID = @AuxiliaryInsuranceID,
                            AuxiliaryInsuranceDescription = @AuxiliaryInsuranceDescription,
                            AuxiliaryInsuranceClassID = @AuxiliaryInsuranceClassID,
                            AuxiliaryInsuranceClassDescription = @AuxiliaryInsuranceClassDescription,
                            AdmissionSite = @AdmissionSite,
                            [Group] = @Group,
                            ModifiedBy = @ModifiedBy,
                            ModifiedDate = GETDATE()
                        WHERE ID = @ID";

                    using (var updateCmd = new SqlCommand(updateQuery, connection))
                    {
                        AddResidentPatientParameters(updateCmd, admissionId, patientId, admissionNumber, patientInfo, patientName, arabicFullName, age, admissionData, lookupData, userId);
                        updateCmd.Parameters.AddWithValue("@ID", existingResidentPatientId.Value);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    _logger.LogInformation("Updated ResidentPatient record ID {ResidentPatientId} for Admission {AdmissionId}", 
                        existingResidentPatientId.Value, admissionId);
                }
                else
                {
                    // Insert new record
                    var insertQuery = @"
                        INSERT INTO Admission.dbo.ResidentPatient
                        (PatientID, Admission, MRN, AdmissionNumber, PatientName, ArabicFullName, MedicalRecordNumber,
                         PatientDOB, Age, PatientGender, CheckInDate, CheckInClassID, CheckInClassDescription,
                         MainInsuranceID, MainInsuranceDescription, MainInsuranceClassID, MainInsuranceClassDescription,
                         ReferralPhysicianID, ReferralPhysicianName, AttendingPhysicianID, AttendingPhysicianName,
                         MedicationUnitID, MedicationUnitDescription, InsuranceID, InsuranceDescription,
                         GuarantorID, GuarantorDescription, CurrencyID, CurrencyDescription, ClassID, ClassDescription,
                         ContextPriceID, ContextPriceDescription, ContextEnumerationID, ContextEnumerationDescription,
                         AdmissionType, AdmissionTypeDescription, Contact, AuxiliaryInsuranceID, AuxiliaryInsuranceDescription,
                         AuxiliaryInsuranceClassID, AuxiliaryInsuranceClassDescription, IsDischarged, DischargeDate,
                         AdmissionSite, [Group], IsDeleted, CreatedBy, CreatedDate)
                        VALUES
                        (@PatientID, @Admission, @MRN, @AdmissionNumber, @PatientName, @ArabicFullName, @MedicalRecordNumber,
                         @PatientDOB, @Age, @PatientGender, @CheckInDate, @CheckInClassID, @CheckInClassDescription,
                         @MainInsuranceID, @MainInsuranceDescription, @MainInsuranceClassID, @MainInsuranceClassDescription,
                         @ReferralPhysicianID, @ReferralPhysicianName, @AttendingPhysicianID, @AttendingPhysicianName,
                         @MedicationUnitID, @MedicationUnitDescription, @InsuranceID, @InsuranceDescription,
                         @GuarantorID, @GuarantorDescription, @CurrencyID, @CurrencyDescription, @ClassID, @ClassDescription,
                         @ContextPriceID, @ContextPriceDescription, @ContextEnumerationID, @ContextEnumerationDescription,
                         @AdmissionType, @AdmissionTypeDescription, @Contact, @AuxiliaryInsuranceID, @AuxiliaryInsuranceDescription,
                         @AuxiliaryInsuranceClassID, @AuxiliaryInsuranceClassDescription, @IsDischarged, @DischargeDate,
                         @AdmissionSite, @Group, 0, @CreatedBy, GETDATE())";

                    using (var insertCmd = new SqlCommand(insertQuery, connection))
                    {
                        AddResidentPatientParameters(insertCmd, admissionId, patientId, admissionNumber, patientInfo, patientName, arabicFullName, age, admissionData, lookupData, userId);
                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    _logger.LogInformation("Inserted new ResidentPatient record for Admission {AdmissionId}", admissionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing ResidentPatient table for Admission {AdmissionId}", admissionId);
                throw;
            }
        }

        /// <summary>
        /// Get lookup data for ResidentPatient (Insurance, Physician, Class descriptions, etc.)
        /// </summary>
        private async Task<dynamic> GetLookupData(string hospitalDefConnectionString, dynamic admissionData)
        {
            var lookupData = new
            {
                CheckInClassDescription = "",
                MainInsuranceDescription = "",
                MainInsuranceClassDescription = "",
                ReferralPhysicianName = "",
                AttendingPhysicianName = "",
                MedicationUnitDescription = "",
                InsuranceDescription = "",
                GuarantorDescription = "",
                CurrencyDescription = "",
                ClassDescription = "",
                ContextPriceDescription = "",
                ContextEnumerationDescription = "",
                AdmissionTypeDescription = "",
                AuxiliaryInsuranceDescription = "",
                AuxiliaryInsuranceClassDescription = ""
            };

            try
            {
                using (var connection = new SqlConnection(hospitalDefConnectionString))
                {
                    await connection.OpenAsync();

                    // Get CheckInClass description
                if (admissionData.CheckInClass > 0)
                {
                    using (var cmd = new SqlCommand("SELECT Description FROM HospitalDefinition.dbo.CheckInClass WHERE ID = @Id AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", admissionData.CheckInClass);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            var desc = result.ToString() ?? "";
                            lookupData = new
                            {
                                CheckInClassDescription = desc,
                                MainInsuranceDescription = lookupData.MainInsuranceDescription,
                                MainInsuranceClassDescription = lookupData.MainInsuranceClassDescription,
                                ReferralPhysicianName = lookupData.ReferralPhysicianName,
                                AttendingPhysicianName = lookupData.AttendingPhysicianName,
                                MedicationUnitDescription = lookupData.MedicationUnitDescription,
                                InsuranceDescription = lookupData.InsuranceDescription,
                                GuarantorDescription = lookupData.GuarantorDescription,
                                CurrencyDescription = lookupData.CurrencyDescription,
                                ClassDescription = desc,
                                ContextPriceDescription = lookupData.ContextPriceDescription,
                                ContextEnumerationDescription = lookupData.ContextEnumerationDescription,
                                AdmissionTypeDescription = lookupData.AdmissionTypeDescription,
                                AuxiliaryInsuranceDescription = lookupData.AuxiliaryInsuranceDescription,
                                AuxiliaryInsuranceClassDescription = lookupData.AuxiliaryInsuranceClassDescription
                            };
                        }
                    }
                }

                // Get MainInsurance description
                if (admissionData.MainInsurance > 0)
                {
                    using (var cmd = new SqlCommand("SELECT Description FROM HospitalDefinition.dbo.Insurance WHERE ID = @Id AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", admissionData.MainInsurance);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            var desc = result.ToString() ?? "";
                            lookupData = new
                            {
                                CheckInClassDescription = lookupData.CheckInClassDescription,
                                MainInsuranceDescription = desc,
                                MainInsuranceClassDescription = lookupData.MainInsuranceClassDescription,
                                ReferralPhysicianName = lookupData.ReferralPhysicianName,
                                AttendingPhysicianName = lookupData.AttendingPhysicianName,
                                MedicationUnitDescription = lookupData.MedicationUnitDescription,
                                InsuranceDescription = desc,
                                GuarantorDescription = lookupData.GuarantorDescription,
                                CurrencyDescription = lookupData.CurrencyDescription,
                                ClassDescription = lookupData.ClassDescription,
                                ContextPriceDescription = lookupData.ContextPriceDescription,
                                ContextEnumerationDescription = lookupData.ContextEnumerationDescription,
                                AdmissionTypeDescription = lookupData.AdmissionTypeDescription,
                                AuxiliaryInsuranceDescription = lookupData.AuxiliaryInsuranceDescription,
                                AuxiliaryInsuranceClassDescription = lookupData.AuxiliaryInsuranceClassDescription
                            };
                        }
                    }
                }

                    // Get MainInsuranceClass description
                    if (admissionData.MainInsuranceClass > 0)
                    {
                        using (var cmd = new SqlCommand("SELECT Description FROM InsuranceClass WHERE ID = @Id AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", admissionData.MainInsuranceClass);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            var desc = result.ToString() ?? "";
                            lookupData = new
                            {
                                CheckInClassDescription = lookupData.CheckInClassDescription,
                                MainInsuranceDescription = lookupData.MainInsuranceDescription,
                                MainInsuranceClassDescription = desc,
                                ReferralPhysicianName = lookupData.ReferralPhysicianName,
                                AttendingPhysicianName = lookupData.AttendingPhysicianName,
                                MedicationUnitDescription = lookupData.MedicationUnitDescription,
                                InsuranceDescription = lookupData.InsuranceDescription,
                                GuarantorDescription = lookupData.GuarantorDescription,
                                CurrencyDescription = lookupData.CurrencyDescription,
                                ClassDescription = lookupData.ClassDescription,
                                ContextPriceDescription = lookupData.ContextPriceDescription,
                                ContextEnumerationDescription = lookupData.ContextEnumerationDescription,
                                AdmissionTypeDescription = lookupData.AdmissionTypeDescription,
                                AuxiliaryInsuranceDescription = lookupData.AuxiliaryInsuranceDescription,
                                AuxiliaryInsuranceClassDescription = lookupData.AuxiliaryInsuranceClassDescription
                            };
                        }
                    }
                }

                    // Get ReferralPhysician name
                    if (admissionData.ReferralPhysician > 0)
                    {
                        using (var cmd = new SqlCommand("SELECT Name FROM Physician WHERE ID = @Id AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", admissionData.ReferralPhysician);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            var name = result.ToString() ?? "";
                            lookupData = new
                            {
                                CheckInClassDescription = lookupData.CheckInClassDescription,
                                MainInsuranceDescription = lookupData.MainInsuranceDescription,
                                MainInsuranceClassDescription = lookupData.MainInsuranceClassDescription,
                                ReferralPhysicianName = name,
                                AttendingPhysicianName = lookupData.AttendingPhysicianName,
                                MedicationUnitDescription = lookupData.MedicationUnitDescription,
                                InsuranceDescription = lookupData.InsuranceDescription,
                                GuarantorDescription = lookupData.GuarantorDescription,
                                CurrencyDescription = lookupData.CurrencyDescription,
                                ClassDescription = lookupData.ClassDescription,
                                ContextPriceDescription = lookupData.ContextPriceDescription,
                                ContextEnumerationDescription = lookupData.ContextEnumerationDescription,
                                AdmissionTypeDescription = lookupData.AdmissionTypeDescription,
                                AuxiliaryInsuranceDescription = lookupData.AuxiliaryInsuranceDescription,
                                AuxiliaryInsuranceClassDescription = lookupData.AuxiliaryInsuranceClassDescription
                            };
                        }
                    }
                }

                    // Get AttendingPhysician name
                    if (admissionData.AttendingPhysician > 0)
                    {
                        using (var cmd = new SqlCommand("SELECT Name FROM Physician WHERE ID = @Id AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", admissionData.AttendingPhysician);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            var name = result.ToString() ?? "";
                            lookupData = new
                            {
                                CheckInClassDescription = lookupData.CheckInClassDescription,
                                MainInsuranceDescription = lookupData.MainInsuranceDescription,
                                MainInsuranceClassDescription = lookupData.MainInsuranceClassDescription,
                                ReferralPhysicianName = lookupData.ReferralPhysicianName,
                                AttendingPhysicianName = name,
                                MedicationUnitDescription = lookupData.MedicationUnitDescription,
                                InsuranceDescription = lookupData.InsuranceDescription,
                                GuarantorDescription = lookupData.GuarantorDescription,
                                CurrencyDescription = lookupData.CurrencyDescription,
                                ClassDescription = lookupData.ClassDescription,
                                ContextPriceDescription = lookupData.ContextPriceDescription,
                                ContextEnumerationDescription = lookupData.ContextEnumerationDescription,
                                AdmissionTypeDescription = lookupData.AdmissionTypeDescription,
                                AuxiliaryInsuranceDescription = lookupData.AuxiliaryInsuranceDescription,
                                AuxiliaryInsuranceClassDescription = lookupData.AuxiliaryInsuranceClassDescription
                            };
                        }
                    }
                }

                    // Get AuxiliaryInsurance description
                    if (admissionData.AuxiliaryInsurance > 0)
                    {
                        using (var cmd = new SqlCommand("SELECT Description FROM Insurance WHERE ID = @Id AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", admissionData.AuxiliaryInsurance);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            var desc = result.ToString() ?? "";
                            lookupData = new
                            {
                                CheckInClassDescription = lookupData.CheckInClassDescription,
                                MainInsuranceDescription = lookupData.MainInsuranceDescription,
                                MainInsuranceClassDescription = lookupData.MainInsuranceClassDescription,
                                ReferralPhysicianName = lookupData.ReferralPhysicianName,
                                AttendingPhysicianName = lookupData.AttendingPhysicianName,
                                MedicationUnitDescription = lookupData.MedicationUnitDescription,
                                InsuranceDescription = lookupData.InsuranceDescription,
                                GuarantorDescription = lookupData.GuarantorDescription,
                                CurrencyDescription = lookupData.CurrencyDescription,
                                ClassDescription = lookupData.ClassDescription,
                                ContextPriceDescription = lookupData.ContextPriceDescription,
                                ContextEnumerationDescription = lookupData.ContextEnumerationDescription,
                                AdmissionTypeDescription = lookupData.AdmissionTypeDescription,
                                AuxiliaryInsuranceDescription = desc,
                                AuxiliaryInsuranceClassDescription = lookupData.AuxiliaryInsuranceClassDescription
                            };
                        }
                    }
                }

                    // Get AuxiliaryInsuranceClass description
                    if (admissionData.AuxiliaryInsuranceClass > 0)
                    {
                        using (var cmd = new SqlCommand("SELECT Description FROM InsuranceClass WHERE ID = @Id AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", admissionData.AuxiliaryInsuranceClass);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            var desc = result.ToString() ?? "";
                            lookupData = new
                            {
                                CheckInClassDescription = lookupData.CheckInClassDescription,
                                MainInsuranceDescription = lookupData.MainInsuranceDescription,
                                MainInsuranceClassDescription = lookupData.MainInsuranceClassDescription,
                                ReferralPhysicianName = lookupData.ReferralPhysicianName,
                                AttendingPhysicianName = lookupData.AttendingPhysicianName,
                                MedicationUnitDescription = lookupData.MedicationUnitDescription,
                                InsuranceDescription = lookupData.InsuranceDescription,
                                GuarantorDescription = lookupData.GuarantorDescription,
                                CurrencyDescription = lookupData.CurrencyDescription,
                                ClassDescription = lookupData.ClassDescription,
                                ContextPriceDescription = lookupData.ContextPriceDescription,
                                ContextEnumerationDescription = lookupData.ContextEnumerationDescription,
                                AdmissionTypeDescription = lookupData.AdmissionTypeDescription,
                                AuxiliaryInsuranceDescription = lookupData.AuxiliaryInsuranceDescription,
                                AuxiliaryInsuranceClassDescription = desc
                            };
                        }
                    }
                }

                // Set defaults for other fields
                lookupData = new
                {
                    CheckInClassDescription = lookupData.CheckInClassDescription,
                    MainInsuranceDescription = lookupData.MainInsuranceDescription,
                    MainInsuranceClassDescription = lookupData.MainInsuranceClassDescription,
                    ReferralPhysicianName = lookupData.ReferralPhysicianName,
                    AttendingPhysicianName = lookupData.AttendingPhysicianName,
                    MedicationUnitDescription = "Clinics",
                    InsuranceDescription = lookupData.MainInsuranceDescription,
                    GuarantorDescription = "",
                    CurrencyDescription = "USD",
                    ClassDescription = lookupData.CheckInClassDescription,
                    ContextPriceDescription = "",
                    ContextEnumerationDescription = "",
                    AdmissionTypeDescription = "",
                    AuxiliaryInsuranceDescription = lookupData.AuxiliaryInsuranceDescription,
                    AuxiliaryInsuranceClassDescription = lookupData.AuxiliaryInsuranceClassDescription
                };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting lookup data for ResidentPatient");
            }

            return lookupData;
        }

        /// <summary>
        /// Add parameters for ResidentPatient insert/update
        /// </summary>
        private void AddResidentPatientParameters(
            SqlCommand cmd,
            int admissionId,
            int patientId,
            string admissionNumber,
            dynamic patientInfo,
            string patientName,
            string arabicFullName,
            int? age,
            dynamic admissionData,
            dynamic lookupData,
            int userId)
        {
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@Admission", admissionId);
            cmd.Parameters.AddWithValue("@MRN", patientInfo.MRN);
            cmd.Parameters.AddWithValue("@AdmissionNumber", admissionNumber);
            cmd.Parameters.AddWithValue("@PatientName", patientName);
            cmd.Parameters.AddWithValue("@ArabicFullName", string.IsNullOrEmpty(arabicFullName) ? (object)DBNull.Value : arabicFullName);
            cmd.Parameters.AddWithValue("@MedicalRecordNumber", patientInfo.MedicalRecordNumber);
            cmd.Parameters.AddWithValue("@PatientDOB", patientInfo.DateOfBirth != DateTime.MinValue ? patientInfo.DateOfBirth : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Age", age.HasValue ? (object)age.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@PatientGender", patientInfo.Gender);
            cmd.Parameters.AddWithValue("@CheckInDate", admissionData.CheckInDate);
            cmd.Parameters.AddWithValue("@CheckInClassID", admissionData.CheckInClass);
            cmd.Parameters.AddWithValue("@CheckInClassDescription", lookupData.CheckInClassDescription);
            cmd.Parameters.AddWithValue("@MainInsuranceID", admissionData.MainInsurance);
            cmd.Parameters.AddWithValue("@MainInsuranceDescription", lookupData.MainInsuranceDescription);
            cmd.Parameters.AddWithValue("@MainInsuranceClassID", admissionData.MainInsuranceClass);
            cmd.Parameters.AddWithValue("@MainInsuranceClassDescription", lookupData.MainInsuranceClassDescription);
            cmd.Parameters.AddWithValue("@ReferralPhysicianID", admissionData.ReferralPhysician);
            cmd.Parameters.AddWithValue("@ReferralPhysicianName", lookupData.ReferralPhysicianName);
            cmd.Parameters.AddWithValue("@AttendingPhysicianID", admissionData.AttendingPhysician > 0 ? (object)admissionData.AttendingPhysician : DBNull.Value);
            cmd.Parameters.AddWithValue("@AttendingPhysicianName", string.IsNullOrEmpty(lookupData.AttendingPhysicianName) ? (object)DBNull.Value : lookupData.AttendingPhysicianName);
            cmd.Parameters.AddWithValue("@MedicationUnitID", 113); // Default to Clinics
            cmd.Parameters.AddWithValue("@MedicationUnitDescription", lookupData.MedicationUnitDescription);
            cmd.Parameters.AddWithValue("@InsuranceID", admissionData.MainInsurance);
            cmd.Parameters.AddWithValue("@InsuranceDescription", lookupData.InsuranceDescription);
            cmd.Parameters.AddWithValue("@GuarantorID", 0);
            cmd.Parameters.AddWithValue("@GuarantorDescription", lookupData.GuarantorDescription);
            cmd.Parameters.AddWithValue("@CurrencyID", 1); // Default to USD
            cmd.Parameters.AddWithValue("@CurrencyDescription", lookupData.CurrencyDescription);
            cmd.Parameters.AddWithValue("@ClassID", admissionData.CheckInClass);
            cmd.Parameters.AddWithValue("@ClassDescription", lookupData.ClassDescription);
            cmd.Parameters.AddWithValue("@ContextPriceID", 0);
            cmd.Parameters.AddWithValue("@ContextPriceDescription", lookupData.ContextPriceDescription);
            cmd.Parameters.AddWithValue("@ContextEnumerationID", 0);
            cmd.Parameters.AddWithValue("@ContextEnumerationDescription", lookupData.ContextEnumerationDescription);
            cmd.Parameters.AddWithValue("@AdmissionType", admissionData.Type);
            cmd.Parameters.AddWithValue("@AdmissionTypeDescription", lookupData.AdmissionTypeDescription);
            cmd.Parameters.AddWithValue("@Contact", string.IsNullOrEmpty(patientInfo.Phone) ? (object)DBNull.Value : patientInfo.Phone);
            cmd.Parameters.AddWithValue("@AuxiliaryInsuranceID", admissionData.AuxiliaryInsurance > 0 ? (object)admissionData.AuxiliaryInsurance : DBNull.Value);
            cmd.Parameters.AddWithValue("@AuxiliaryInsuranceDescription", string.IsNullOrEmpty(lookupData.AuxiliaryInsuranceDescription) ? (object)DBNull.Value : lookupData.AuxiliaryInsuranceDescription);
            cmd.Parameters.AddWithValue("@AuxiliaryInsuranceClassID", admissionData.AuxiliaryInsuranceClass > 0 ? (object)admissionData.AuxiliaryInsuranceClass : DBNull.Value);
            cmd.Parameters.AddWithValue("@AuxiliaryInsuranceClassDescription", string.IsNullOrEmpty(lookupData.AuxiliaryInsuranceClassDescription) ? (object)DBNull.Value : lookupData.AuxiliaryInsuranceClassDescription);
            cmd.Parameters.AddWithValue("@IsDischarged", false);
            cmd.Parameters.AddWithValue("@DischargeDate", DBNull.Value);
            cmd.Parameters.AddWithValue("@AdmissionSite", admissionData.AdmissionSite > 0 ? (object)admissionData.AdmissionSite : DBNull.Value);
            cmd.Parameters.AddWithValue("@Group", admissionData.Group > 0 ? (object)admissionData.Group : DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", userId);
        }
    }

    public class CreateAdmissionRequest
    {
        public string? Number { get; set; }
        public int AdmissionSite { get; set; }
        public int ReferralPhysician { get; set; }
        public int? AttendingPhysician { get; set; }
        public int MainInsurance { get; set; }
        public int MainInsuranceClass { get; set; }
        public int Insured { get; set; }
        public int AuxiliaryInsurance { get; set; }
        public int AuxiliaryInsuranceClass { get; set; }
        public int CheckInClass { get; set; }
        public string? Department { get; set; }
        public string? CheckInDate { get; set; }
        public string? CheckOutDate { get; set; }
        public int Patient { get; set; }
        public int Type { get; set; }
        public int IsWorkAccident { get; set; }
        public int IsExtended { get; set; }
        public int Group { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateAdmissionRequest
    {
        public int AdmissionSite { get; set; }
        public int ReferralPhysician { get; set; }
        public int? AttendingPhysician { get; set; }
        public int MainInsurance { get; set; }
        public int MainInsuranceClass { get; set; }
        public int Insured { get; set; }
        public int AuxiliaryInsurance { get; set; }
        public int AuxiliaryInsuranceClass { get; set; }
        public int CheckInClass { get; set; }
        public string? Department { get; set; }
        public string? CheckInDate { get; set; }
        public string? CheckOutDate { get; set; }
        public int Patient { get; set; }
        public int Type { get; set; }
        public int IsWorkAccident { get; set; }
        public int IsExtended { get; set; }
        public int Group { get; set; }
        public int ModifiedBy { get; set; }
    }
}
