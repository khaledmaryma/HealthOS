using LIS.Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuickAdmissionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QuickAdmissionController> _logger;

        public static string GetDebugSql(SqlCommand cmd)
        {
            var sql = cmd.CommandText;

            foreach (SqlParameter param in cmd.Parameters)
            {
                string value;

                if (param.Value == null || param.Value == DBNull.Value)
                {
                    value = "NULL";
                }
                else if (param.SqlDbType == SqlDbType.VarChar
                      || param.SqlDbType == SqlDbType.NVarChar
                      || param.SqlDbType == SqlDbType.Char
                      || param.SqlDbType == SqlDbType.NChar
                      || param.SqlDbType == SqlDbType.Text)
                {
                    value = $"'{param.Value.ToString().Replace("'", "''")}'";
                }
                else if (param.SqlDbType == SqlDbType.DateTime || param.Value is DateTime)
                {
                    value = $"'{((DateTime)param.Value):yyyy-MM-dd HH:mm:ss}'";
                }
                else
                {
                    value = param.Value.ToString();
                }

                sql = sql.Replace(param.ParameterName, value);
            }

            return sql;
        }

        public QuickAdmissionController(IConfiguration configuration, ILogger<QuickAdmissionController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("SaveComplete")]
        public async Task<ActionResult<object>> SaveComplete([FromBody] QuickAdmissionRequest request)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("AdmissionConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("sp_SaveQuickAdmission", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@existingPatientId", request.ExistingPatientId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@saveData", request.SaveData ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@saveOptions", request.SaveOptions ?? (object)DBNull.Value);

                        // Execute the stored procedure and read results
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var result = new
                                {
                                    MRN = reader["MRN"]?.ToString(),
                                    PatientID = reader["PatientID"] != DBNull.Value ? Convert.ToInt32(reader["PatientID"]) : (int?)null,
                                    AdmissionNumber = reader["AdmissionNumber"]?.ToString(),
                                    AdmissionID = reader["AdmissionID"] != DBNull.Value ? Convert.ToInt32(reader["AdmissionID"]) : (int?)null,
                                    InvoiceHeaderID = reader["InvoiceHeaderID"] != DBNull.Value ? Convert.ToInt32(reader["InvoiceHeaderID"]) : (int?)null,
                                    Status = reader["Status"]?.ToString(),
                                    ErrorMessage = reader["ErrorMessage"]?.ToString()
                                };

                                if (result.Status == "Error")
                                {
                                    _logger.LogError("Stored procedure error: {ErrorMessage}", result.ErrorMessage);
                                    return BadRequest(new { message = "Error saving quick admission", error = result.ErrorMessage });
                                }

                                return Ok(result);
                            }
                        }
                    }
                }

                return BadRequest(new { message = "No response from stored procedure" });
            }
            catch (Exception ex)
            {
                var stackTrace = new System.Diagnostics.StackTrace(ex, true);
                var frame = stackTrace.GetFrame(0);
                var className = frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
                var methodName = frame?.GetMethod()?.Name ?? "Unknown";
                var lineNumber = frame?.GetFileLineNumber() ?? 0;
                var fileName = System.IO.Path.GetFileName(frame?.GetFileName() ?? "Unknown");

                _logger.LogError(ex,
                    "[{ClassName}] Error in SaveComplete - Method: {MethodName}, File: {FileName}, Line: {LineNumber}, Message: {Message}",
                    className, methodName, fileName, lineNumber, ex.Message);
                return StatusCode(500, new { message = "Error saving quick admission", error = $"[{className}] {ex.Message} (Line: {lineNumber})" });
            }
        }

        [HttpPost("Save_V1")]
        public async Task<ActionResult<object>> Save_V1([FromBody] QuickAdmissionRequest request)
        {
            int? patientId = null;
            int? admissionId = null;
            int? invoiceHeaderId = null;
            string mrn = "";
            string admissionNumber = "";
            decimal exchangeRate = 90000;
            try
            {
                _logger.LogInformation("=== Save_V1 START ===");
                _logger.LogInformation("Request received - ExistingPatientId: {ExistingPatientId}, HasSaveData: {HasSaveData}, HasSaveOptions: {HasSaveOptions}",
                    request.ExistingPatientId, !string.IsNullOrEmpty(request.SaveData), !string.IsNullOrEmpty(request.SaveOptions));
                //get exchange rate 

                var configConnStr1 = _configuration.GetConnectionString("ConfigurationConnection");
                if (!string.IsNullOrEmpty(configConnStr1))
                {
                    using var configConn1 = new SqlConnection(configConnStr1);
                    await configConn1.OpenAsync();
                    var getRateQuery = "select top 1 DefaultRate from  [Financial DB].dbo.ConfigurationTable";
                    try
                    {
                        using var cmd = new SqlCommand(getRateQuery, configConn1);
                        exchangeRate = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 1);
                    }
                    catch
                    {

                    }
                }
                // Parse JSON data
                if (string.IsNullOrEmpty(request.SaveData))
                {
                    return BadRequest(new { message = "SaveData is required" });
                }

                // Use JsonDocument to parse flexibly, then convert manually to handle boolean conversion issues
                JsonDocument? jsonDoc = null;
                try
                {
                    jsonDoc = JsonDocument.Parse(request.SaveData);
                }
                catch (Exception ex)
                {
                    var stackTrace = new System.Diagnostics.StackTrace(ex, true);
                    var frame = stackTrace.GetFrame(0);
                    var className = frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
                    var methodName = frame?.GetMethod()?.Name ?? "Unknown";
                    var lineNumber = frame?.GetFileLineNumber() ?? 0;
                    var fileName = System.IO.Path.GetFileName(frame?.GetFileName() ?? "Unknown");

                    _logger.LogError(ex,
                        "[{ClassName}] Failed to parse SaveData JSON - Method: {MethodName}, File: {FileName}, Line: {LineNumber}, Message: {Message}",
                        className, methodName, fileName, lineNumber, ex.Message);
                    return BadRequest(new { message = "Invalid SaveData JSON format", error = $"[{className}] {ex.Message} (Line: {lineNumber})" });
                }

                var saveData = new QuickAdmissionSaveData();

                // Parse patient
                if (jsonDoc.RootElement.TryGetProperty("patient", out var patientElement))
                {
                    saveData.Patient = new QuickAdmissionPatient
                    {
                        FirstName = patientElement.TryGetProperty("FirstName", out var fn) ? fn.GetString() : null,
                        LastName = patientElement.TryGetProperty("LastName", out var ln) ? ln.GetString() : null,
                        MiddleName = patientElement.TryGetProperty("MiddleName", out var mn) ? mn.GetString() : null,
                        Gender = patientElement.TryGetProperty("Gender", out var g) ? g.GetString() : null,
                        Phone = patientElement.TryGetProperty("Phone", out var p) ? p.GetString() : null,
                        ArabicFullName = patientElement.TryGetProperty("ArabicFullName", out var afn) ? afn.GetString() : null,
                        DOB = patientElement.TryGetProperty("DOB", out var dob) ? dob.GetString() : null,
                        MaritalStatus = patientElement.TryGetProperty("MaritalStatus", out var ms) && ms.ValueKind != JsonValueKind.Null ? ms.GetInt32() : null,
                        CreatedBy = patientElement.TryGetProperty("CreatedBy", out var cb) && cb.ValueKind != JsonValueKind.Null ? cb.GetInt32() : null
                    };
                }

                // Parse admission
                if (jsonDoc.RootElement.TryGetProperty("admission", out var admissionElement))
                {
                    saveData.Admission = new QuickAdmissionAdmission
                    {
                        AdmissionSite = admissionElement.TryGetProperty("AdmissionSite", out var asite) && asite.ValueKind != JsonValueKind.Null ? asite.GetInt32() : null,
                        ReferralPhysician = admissionElement.TryGetProperty("ReferralPhysician", out var rp) && rp.ValueKind != JsonValueKind.Null ? rp.GetInt32() : null,
                        AttendingPhysician = admissionElement.TryGetProperty("AttendingPhysician", out var ap) && ap.ValueKind != JsonValueKind.Null ? ap.GetInt32() : null,
                        MainInsurance = admissionElement.TryGetProperty("MainInsurance", out var mi) && mi.ValueKind != JsonValueKind.Null ? mi.GetInt32() : null,
                        MainInsuranceClass = admissionElement.TryGetProperty("MainInsuranceClass", out var mic) && mic.ValueKind != JsonValueKind.Null ? mic.GetInt32() : null,
                        Insured = admissionElement.TryGetProperty("Insured", out var ins) && ins.ValueKind != JsonValueKind.Null ? ins.GetInt32() : null,
                        AuxiliaryInsurance = admissionElement.TryGetProperty("AuxiliaryInsurance", out var ai) && ai.ValueKind != JsonValueKind.Null ? ai.GetInt32() : null,
                        AuxiliaryInsuranceClass = admissionElement.TryGetProperty("AuxiliaryInsuranceClass", out var aic) && aic.ValueKind != JsonValueKind.Null ? aic.GetInt32() : null,
                        CheckInClass = admissionElement.TryGetProperty("CheckInClass", out var cic) && cic.ValueKind != JsonValueKind.Null ? cic.GetInt32() : null,
                        Department = admissionElement.TryGetProperty("Department", out var dept) ? dept.GetString() : null,
                        CheckInDate = admissionElement.TryGetProperty("CheckInDate", out var cid) ? cid.GetString() : null,
                        CheckOutDate = admissionElement.TryGetProperty("CheckOutDate", out var cod) ? cod.GetString() : null,
                        Type = admissionElement.TryGetProperty("Type", out var type) && type.ValueKind != JsonValueKind.Null ? type.GetInt32() : null,
                        IsWorkAccident = admissionElement.TryGetProperty("IsWorkAccident", out var iwa) ? ConvertToBool(iwa) : null,
                        IsExtended = admissionElement.TryGetProperty("IsExtended", out var ie) ? ConvertToBool(ie) : null,
                        CreatedBy = admissionElement.TryGetProperty("CreatedBy", out var acb) && acb.ValueKind != JsonValueKind.Null ? acb.GetInt32() : null
                    };
                }

                // Parse invoice
                if (jsonDoc.RootElement.TryGetProperty("invoice", out var invoiceElement) && invoiceElement.ValueKind == JsonValueKind.Array)
                {
                    saveData.Invoice = new List<QuickAdmissionInvoiceItem>();
                    foreach (var item in invoiceElement.EnumerateArray())
                    {
                        saveData.Invoice.Add(new QuickAdmissionInvoiceItem
                        {
                            MedicationUnit = item.TryGetProperty("MedicationUnit", out var mu) && mu.ValueKind != JsonValueKind.Null ? mu.GetInt32() : null,
                            MedicationUnitDescription = item.TryGetProperty("MedicationUnitDescription", out var mud) ? mud.GetString() : null,
                            Denomination = item.TryGetProperty("Denomination", out var den) && den.ValueKind != JsonValueKind.Null ? den.GetInt32() : null,
                            DenominationCode = item.TryGetProperty("DenominationCode", out var dc) ? dc.GetString() : null,
                            DenominationDescription = item.TryGetProperty("DenominationDescription", out var dd) ? dd.GetString() : null,
                            Quantity = item.TryGetProperty("Quantity", out var qty) && qty.ValueKind != JsonValueKind.Null ? qty.GetDecimal() : null,
                            UnitPrice = item.TryGetProperty("UnitPrice", out var up) && up.ValueKind != JsonValueKind.Null ? up.GetDecimal() : null,
                            NetPrice = item.TryGetProperty("NetPrice", out var np) && np.ValueKind != JsonValueKind.Null ? np.GetDecimal() : null,
                            NetUnitPrice = item.TryGetProperty("NetUnitPrice", out var nup) && nup.ValueKind != JsonValueKind.Null ? nup.GetDecimal() : null,
                            Discount = item.TryGetProperty("Discount", out var disc) && disc.ValueKind != JsonValueKind.Null ? disc.GetDecimal() : null,
                            LumpSum = item.TryGetProperty("LumpSum", out var ls) && ls.ValueKind != JsonValueKind.Null ? ls.GetDecimal() : null,
                            OperatingPhysician = item.TryGetProperty("OperatingPhysician", out var op) && op.ValueKind != JsonValueKind.Null ? op.GetInt32() : null,
                            CostCenter = item.TryGetProperty("CostCenter", out var cc) && cc.ValueKind != JsonValueKind.Null ? cc.GetInt32() : null,
                            ProfitCenter = item.TryGetProperty("ProfitCenter", out var pc) && pc.ValueKind != JsonValueKind.Null ? pc.GetInt32() : null,
                            CreatedBy = item.TryGetProperty("CreatedBy", out var icb) && icb.ValueKind != JsonValueKind.Null ? icb.GetInt32() : null
                        });
                    }
                }

                // Parse invoiceReceipt (for Create Advance - receipt amounts and currency)
                if (jsonDoc.RootElement.TryGetProperty("invoiceReceipt", out var irElement))
                {
                    saveData.InvoiceReceipt = new QuickAdmissionInvoiceReceipt
                    {
                        Currency = irElement.TryGetProperty("currency", out var curr) ? curr.GetString() : null,
                        ReceiptAmount = irElement.TryGetProperty("receiptAmount", out var ra) && ra.ValueKind != JsonValueKind.Null ? ra.GetDecimal() : null,
                        ReceiptLocal = irElement.TryGetProperty("receiptLocal", out var rl) && rl.ValueKind != JsonValueKind.Null ? rl.GetDecimal() : null,
                        InvoiceNet = irElement.TryGetProperty("invoiceNet", out var net) && net.ValueKind != JsonValueKind.Null ? net.GetDecimal() : null,
                        TotalPaidInInvoiceCurrency = irElement.TryGetProperty("totalPaidInInvoiceCurrency", out var tp) && tp.ValueKind != JsonValueKind.Null ? tp.GetDecimal() : null
                    };
                }

                // Parse deliverHeader and deliverItems (for medicament -> Inventory.dbo.DeliverItem)
                if (jsonDoc.RootElement.TryGetProperty("deliverHeader", out var dhElement) && dhElement.ValueKind != JsonValueKind.Null)
                {
                    saveData.DeliverHeader = new QuickAdmissionDeliverHeader
                    {
                        Type = dhElement.TryGetProperty("Type", out var t) && t.ValueKind != JsonValueKind.Null ? t.GetInt32() : null,
                        TypeCounter = dhElement.TryGetProperty("TypeCounter", out var tc) && tc.ValueKind != JsonValueKind.Null ? tc.GetInt32() : null,
                        PatientType = dhElement.TryGetProperty("PatientType", out var pt) && pt.ValueKind != JsonValueKind.Null ? pt.GetInt32() : null,
                        Date = dhElement.TryGetProperty("Date", out var dt) ? dt.GetString() : null,
                        Currency = dhElement.TryGetProperty("Currency", out var curr) && curr.ValueKind != JsonValueKind.Null ? curr.GetInt32() : null,
                        Warehouse = dhElement.TryGetProperty("Warehouse", out var wh) && wh.ValueKind != JsonValueKind.Null ? wh.GetInt32() : null,
                        CreatedBy = dhElement.TryGetProperty("CreatedBy", out var dcb) && dcb.ValueKind != JsonValueKind.Null ? dcb.GetInt32() : null
                    };
                }
                if (jsonDoc.RootElement.TryGetProperty("deliverItems", out var diElement) && diElement.ValueKind == JsonValueKind.Array)
                {
                    saveData.DeliverItems = new List<QuickAdmissionDeliverItem>();
                    foreach (var item in diElement.EnumerateArray())
                    {
                        saveData.DeliverItems.Add(new QuickAdmissionDeliverItem
                        {
                            Product = item.TryGetProperty("Product", out var p) && p.ValueKind != JsonValueKind.Null ? p.GetInt32() : null,
                            Package = item.TryGetProperty("Package", out var pkg) && pkg.ValueKind != JsonValueKind.Null ? pkg.GetInt32() : null,
                            Code = item.TryGetProperty("Code", out var c) ? c.GetString() : null,
                            AlternateDescription = item.TryGetProperty("AlternateDescription", out var ad) ? ad.GetString() : null,
                            Qty = item.TryGetProperty("Qty", out var q) && q.ValueKind != JsonValueKind.Null ? q.GetDecimal() : null,
                            UnitPrice = item.TryGetProperty("UnitPrice", out var up) && up.ValueKind != JsonValueKind.Null ? up.GetDecimal() : null,
                            Net = item.TryGetProperty("Net", out var n) && n.ValueKind != JsonValueKind.Null ? n.GetDecimal() : null,
                            PLDescription = item.TryGetProperty("PLDescription", out var pl) ? pl.GetString() : null
                        });
                    }
                }

                if (saveData.Patient == null && saveData.Admission == null)
                {
                    return BadRequest(new { message = "Invalid SaveData format - must contain patient or admission data" });
                }

                // Parse save options
                var saveOptions = new QuickAdmissionSaveOptions
                {
                    SaveMedicalFile = true,
                    SaveAdmission = true,
                    SaveInvoice = true
                };

                if (!string.IsNullOrEmpty(request.SaveOptions))
                {
                    try
                    {
                        var parsedOptions = JsonSerializer.Deserialize<QuickAdmissionSaveOptions>(request.SaveOptions, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (parsedOptions != null)
                        {
                            saveOptions = parsedOptions;
                        }
                    }
                    catch
                    {
                        // Use defaults if parsing fails
                    }
                }

                // NOTE: Admission transaction is scoped ONLY to Step 2 (Admission + ResidentPatient)
                // to minimize lock hold time on AdmissionCounter/Admission/ResidentPatient.
                // Steps 1, 3, 4 run outside that transaction to avoid blocking other requests.
                var admissionConnectionString = _configuration.GetConnectionString("AdmissionConnection");

                try
                {
                    // =====================================================
                    // 1. SAVE PATIENT (if requested and not existing patient)
                    // =====================================================
                    if (saveOptions.SaveMedicalFile && request.ExistingPatientId == null && saveData.Patient != null)
                    {
                        _logger.LogInformation("=== STEP 1: Creating new patient ===");
                        _logger.LogInformation("Patient data - FirstName: {FirstName}, LastName: {LastName}, Gender: {Gender}",
                            saveData.Patient.FirstName, saveData.Patient.LastName, saveData.Patient.Gender);

                        // Calculate MRN using the provided logic
                        var configConnectionString = _configuration.GetConnectionString("ConfigurationConnection");
                        int newMRN = 0;

                        if (string.IsNullOrEmpty(configConnectionString))
                        {
                            throw new Exception("ConfigurationConnection string is not configured");
                        }

                        _logger.LogInformation("Calculating MRN using ConfigurationConnection");

                        using (var configConnection = new SqlConnection(configConnectionString))
                        {
                            await configConnection.OpenAsync();
                            _logger.LogInformation("Connected to database. Server: {Server}, Database: {Database}",
                                configConnection.DataSource, configConnection.Database);

                            // First, check current database context
                            using var dbCheckCommand = new SqlCommand("SELECT DB_NAME()", configConnection);
                            var currentDb = await dbCheckCommand.ExecuteScalarAsync();
                            _logger.LogInformation("Current database context: {Database}", currentDb);

                            // List all columns in TransactionSequenceControl table
                            var listColumnsSql = @"
                                SELECT COLUMN_NAME, DATA_TYPE 
                                FROM INFORMATION_SCHEMA.COLUMNS 
                                WHERE TABLE_NAME = 'TransactionSequenceControl'
                                ORDER BY ORDINAL_POSITION";

                            using var listColumnsCommand = new SqlCommand(listColumnsSql, configConnection);
                            using var columnsReader = await listColumnsCommand.ExecuteReaderAsync();
                            var columnList = new List<string>();
                            while (await columnsReader.ReadAsync())
                            {
                                var colName = columnsReader["COLUMN_NAME"]?.ToString() ?? "";
                                var colType = columnsReader["DATA_TYPE"]?.ToString() ?? "";
                                columnList.Add($"{colName} ({colType})");
                            }
                            await columnsReader.CloseAsync();

                            _logger.LogInformation("Columns in TransactionSequenceControl: {Columns}", string.Join(", ", columnList));

                            // Check if LastMedicalRecordNumber exists
                            if (!columnList.Any(c => c.Contains("LastMedicalRecordNumber", StringComparison.OrdinalIgnoreCase)))
                            {
                                _logger.LogError("Column 'LastMedicalRecordNumber' NOT FOUND in TransactionSequenceControl table");
                                _logger.LogError("Available columns: {Columns}", string.Join(", ", columnList));
                                throw new Exception($"Column 'LastMedicalRecordNumber' not found. Available columns: {string.Join(", ", columnList)}");
                            }

                            _logger.LogInformation("Column 'LastMedicalRecordNumber' verified - proceeding with MRN calculation");

                            // Use the same pattern as PatientController - simple UPDATE with OUTPUT
                            // Since we're connected to Configuration database, use TransactionSequenceControl (no schema prefix)
                            /*using var mrnCommand = new SqlCommand(@"
                                DECLARE @OutputTable TABLE (NewMRN INT);
                                UPDATE TransactionSequenceControl
                                SET LastMedicalRecordNumber = LastMedicalRecordNumber + 1
                                OUTPUT LastMedicalRecordNumber INTO @OutputTable
                                WHERE ID = 1;
                                SELECT NewMRN FROM @OutputTable;", configConnection);*/
                            using var mrnCommand = new SqlCommand(@"
                                DECLARE @NewMRN as int;
                                UPDATE Configuration.dbo.TransactionSequenceControl
                                SET LastMedicalRecordNumber = LastMedicalRecordNumber + 1,
                                    @NewMRN = LastMedicalRecordNumber + 1
                                WHERE ID = 1;
                                SELECT CAST(@NewMRN AS INT);
                                ", configConnection); var result = await mrnCommand.ExecuteScalarAsync();

                            if (result != null && result != DBNull.Value)
                            {
                                newMRN = Convert.ToInt32(result);
                                _logger.LogInformation("MRN calculated successfully: {MRN}", newMRN);
                            }
                            else
                            {
                                var stackTrace = new System.Diagnostics.StackTrace(true);
                                var frame = stackTrace.GetFrame(0);
                                var className = frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
                                var methodName = frame?.GetMethod()?.Name ?? "Unknown";
                                var lineNumber = frame?.GetFileLineNumber() ?? 0;
                                var fileName = System.IO.Path.GetFileName(frame?.GetFileName() ?? "Unknown");

                                _logger.LogError(
                                    "[{ClassName}] MRN calculation returned null - Method: {MethodName}, File: {FileName}, Line: {LineNumber}",
                                    className, methodName, fileName, lineNumber);
                                throw new Exception($"Failed to calculate MRN from TransactionSequenceControl table - result was null (Line: {lineNumber})");
                            }
                        }

                        mrn = newMRN.ToString();

                        // Insert patient into HospitalDefinition database
                        var hospitalDefConnectionString = _configuration.GetConnectionString("HospitalDefinitionConnection");
                        using var hospitalDefConnection = new SqlConnection(hospitalDefConnectionString);
                        await hospitalDefConnection.OpenAsync();

                        var insertPatientSql = @"
                            INSERT INTO HospitalDefinition.dbo.Patient 
                            (FirstName, LastName, MiddleName, Gender, Phone, ArabicFullName, 
                             DOB, MaritalStatus, MedicalRecordNumber, CreatedBy, CreatedDate, IsDeleted)
                            VALUES 
                            (@FirstName, @LastName, @MiddleName, @Gender, @Phone, @ArabicFullName,
                             @DOB, @MaritalStatus, @MedicalRecordNumber, @CreatedBy, GETDATE(), 0);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        using var patientCommand = new SqlCommand(insertPatientSql, hospitalDefConnection);
                        patientCommand.Parameters.AddWithValue("@FirstName", saveData.Patient.FirstName ?? "");
                        patientCommand.Parameters.AddWithValue("@LastName", saveData.Patient.LastName ?? "");
                        patientCommand.Parameters.AddWithValue("@MiddleName", saveData.Patient.MiddleName ?? (object)DBNull.Value);
                        patientCommand.Parameters.AddWithValue("@Gender", saveData.Patient.Gender ?? "");
                        patientCommand.Parameters.AddWithValue("@Phone", saveData.Patient.Phone ?? (object)DBNull.Value);
                        patientCommand.Parameters.AddWithValue("@ArabicFullName", saveData.Patient.ArabicFullName ?? (object)DBNull.Value);
                        patientCommand.Parameters.AddWithValue("@DOB", saveData.Patient.DOB != null ? DateTime.Parse(saveData.Patient.DOB) : (object)DBNull.Value);
                        patientCommand.Parameters.AddWithValue("@MaritalStatus", saveData.Patient.MaritalStatus ?? (object)DBNull.Value);
                        patientCommand.Parameters.AddWithValue("@MedicalRecordNumber", mrn);
                        patientCommand.Parameters.AddWithValue("@CreatedBy", saveData.Patient.CreatedBy ?? 338);

                        var patientIdResult = await patientCommand.ExecuteScalarAsync();
                        patientId = patientIdResult != null && patientIdResult != DBNull.Value ? (int)patientIdResult : null;
                        _logger.LogInformation("=== STEP 1 COMPLETE: Patient created with ID: {PatientId}, MRN: {MRN} ===", patientId, mrn);
                    }
                    else if (request.ExistingPatientId.HasValue)
                    {
                        patientId = request.ExistingPatientId.Value;

                        // Get existing MRN
                        var hospitalDefConnectionString = _configuration.GetConnectionString("HospitalDefinitionConnection");
                        using var hospitalDefConnection = new SqlConnection(hospitalDefConnectionString);
                        await hospitalDefConnection.OpenAsync();

                        var getMrnSql = @"
                            SELECT MedicalRecordNumber 
                            FROM HospitalDefinition.dbo.Patient 
                            WHERE ID = @PatientId AND (IsDeleted = 0 OR IsDeleted IS NULL)";

                        using var mrnCommand = new SqlCommand(getMrnSql, hospitalDefConnection);
                        mrnCommand.Parameters.AddWithValue("@PatientId", patientId.Value);
                        var mrnResult = await mrnCommand.ExecuteScalarAsync();
                        mrn = mrnResult?.ToString() ?? patientId.Value.ToString();

                        _logger.LogInformation("=== STEP 1: Using existing patient ID: {PatientId}, MRN: {MRN} ===", patientId, mrn);
                    }

                    if (!patientId.HasValue)
                    {
                        throw new Exception("Patient ID is required");
                    }

                    // =====================================================
                    // 2. SAVE ADMISSION (if requested) - scoped transaction to minimize lock hold
                    // =====================================================
                    if (saveOptions.SaveAdmission && saveData.Admission != null)
                    {
                        _logger.LogInformation("=== STEP 2: Creating admission ===");
                        _logger.LogInformation("Admission data - Type: {Type}, CheckInDate: {CheckInDate}, ReferralPhysician: {ReferralPhysician}",
                            saveData.Admission.Type, saveData.Admission.CheckInDate, saveData.Admission.ReferralPhysician);

                        using var admissionConnection = new SqlConnection(admissionConnectionString);
                        await admissionConnection.OpenAsync();
                        using var admissionTransaction = admissionConnection.BeginTransaction();
                        try
                        {
                            // Calculate admission number using the provided logic
                            var checkInDate = saveData.Admission.CheckInDate != null ? DateTime.Now : DateTime.Parse(saveData.Admission.CheckInDate);
                            var admissionType = saveData.Admission.Type ?? 3; // Default to CashOutPatient (3)

                            // Update AdmissionCounter
                            _logger.LogInformation("Updating AdmissionCounter for Year: {Year}, Month: {Month}, Type: {Type}",
                                checkInDate.Year, checkInDate.Month, admissionType);

                            var updateCounterSql = @"
                                UPDATE AdmissionCounter  
                                SET CashOutPatient = CashOutPatient + 1 
                                WHERE [YEAR] = YEAR(@checkInDate) AND [Month] = MONTH(@checkInDate)";

                            using var updateCounterCmd = new SqlCommand(updateCounterSql, admissionConnection, admissionTransaction);
                            updateCounterCmd.Parameters.AddWithValue("@checkInDate", checkInDate);
                            await updateCounterCmd.ExecuteNonQueryAsync();
                            _logger.LogInformation("AdmissionCounter updated successfully");

                            // Get the counter
                            var getCounterSql = @"
                                SELECT CashOutPatient 
                                FROM AdmissionCounter 
                                WHERE [YEAR] = YEAR(@checkInDate) AND [Month] = MONTH(@checkInDate)";

                            int admissionCounter = 0;
                            using var getCounterCmd = new SqlCommand(getCounterSql, admissionConnection, admissionTransaction);
                            getCounterCmd.Parameters.AddWithValue("@checkInDate", checkInDate);
                            var counterResult = await getCounterCmd.ExecuteScalarAsync();
                            if (counterResult != null)
                            {
                                admissionCounter = Convert.ToInt32(counterResult);
                            }

                            // Build admission number: '0' + Type + '.' + Counter (5 digits) + '.' + Month (2 digits) + '.' + Year (2 digits)
                            var typeStr = admissionType.ToString();
                            var counterStr = admissionCounter.ToString().PadLeft(5, '0');
                            var monthStr = checkInDate.Month.ToString().PadLeft(2, '0');
                            var yearStr = checkInDate.Year.ToString().Substring(2, 2);
                            admissionNumber = $"0{typeStr}.{counterStr}.{monthStr}.{yearStr}";

                            _logger.LogInformation("Admission number generated: {AdmissionNumber} (Counter: {Counter})", admissionNumber, admissionCounter);

                            // Insert admission
                            var insertAdmissionSql = @"
                                INSERT INTO dbo.Admission 
                                (Number, AdmissionSite, ReferralPhysician, AttendingPhysician,
                                 MainInsurance, MainInsuranceClass, Insured,
                                 AuxiliaryInsurance, AuxiliaryInsuranceClass, CheckInClass,
                                 Department, CheckInDate, CheckOutDate, Patient, Type,
                                 IsWorkAccident, IsExtended, CreatedBy, CreatedDate, IsDeleted)
                                VALUES 
                                (@Number, @AdmissionSite, @ReferralPhysician, @AttendingPhysician,
                                 @MainInsurance, @MainInsuranceClass, @Insured,
                                 @AuxiliaryInsurance, @AuxiliaryInsuranceClass, @CheckInClass,
                                 @Department, @CheckInDate, @CheckOutDate, @Patient, @Type,
                                 @IsWorkAccident, @IsExtended, @CreatedBy, GETDATE(), 0);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            using var admissionCommand = new SqlCommand(insertAdmissionSql, admissionConnection, admissionTransaction);
                            admissionCommand.Parameters.AddWithValue("@Number", admissionNumber);
                            admissionCommand.Parameters.AddWithValue("@AdmissionSite", saveData.Admission.AdmissionSite ?? (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@ReferralPhysician", saveData.Admission.ReferralPhysician ?? 0);
                            admissionCommand.Parameters.AddWithValue("@AttendingPhysician", saveData.Admission.AttendingPhysician ?? saveData.Admission.ReferralPhysician ?? (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@MainInsurance", saveData.Admission.MainInsurance ?? (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@MainInsuranceClass", saveData.Admission.MainInsuranceClass ?? (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@Insured", saveData.Admission.Insured ?? (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@AuxiliaryInsurance", saveData.Admission.AuxiliaryInsurance ?? (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@AuxiliaryInsuranceClass", saveData.Admission.AuxiliaryInsuranceClass ?? (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@CheckInClass", saveData.Admission.CheckInClass ?? (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@Department", saveData.Admission.Department ?? (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@CheckInDate", checkInDate);
                            admissionCommand.Parameters.AddWithValue("@CheckOutDate", !string.IsNullOrEmpty(saveData.Admission.CheckOutDate) ? DateTime.Parse(saveData.Admission.CheckOutDate) : (object)DBNull.Value);
                            admissionCommand.Parameters.AddWithValue("@Patient", patientId.Value);
                            admissionCommand.Parameters.AddWithValue("@Type", admissionType);
                            admissionCommand.Parameters.AddWithValue("@IsWorkAccident", saveData.Admission.IsWorkAccident ?? false);
                            admissionCommand.Parameters.AddWithValue("@IsExtended", saveData.Admission.IsExtended ?? false);
                            // Group is a calculated field - don't set it
                            admissionCommand.Parameters.AddWithValue("@CreatedBy", saveData.Admission.CreatedBy ?? 338);

                            var admissionIdResult = await admissionCommand.ExecuteScalarAsync();
                            admissionId = admissionIdResult != null && admissionIdResult != DBNull.Value ? (int)admissionIdResult : null;
                            _logger.LogInformation("=== STEP 2 COMPLETE: Admission created with ID: {AdmissionId}, Number: {AdmissionNumber} ===", admissionId, admissionNumber);

                            // Sync ResidentPatient (simplified - you may want to add full logic from AdmissionController)
                            _logger.LogInformation("Syncing ResidentPatient table...");
                            await SyncResidentPatient_V1(admissionConnection, admissionTransaction, admissionId!.Value, patientId!.Value, (saveData.Admission.ReferralPhysician ?? 0), "",
                                admissionNumber, (saveData.Admission.Department ?? ""),
                                saveData.Admission, saveData.Patient ?? new QuickAdmissionPatient(), saveData.Admission.CreatedBy ?? 338);
                            _logger.LogInformation("ResidentPatient sync completed");

                            admissionTransaction.Commit();
                            _logger.LogInformation("=== STEP 2 transaction committed - Admission locks released ===");
                        }
                        catch (Exception step2Ex)
                        {
                            admissionTransaction.Rollback();
                            _logger.LogError(step2Ex, "Step 2 (Admission) failed - transaction rolled back");
                            throw;
                        }
                    }

                    // =====================================================
                    // 3. SAVE INVOICE (if requested)
                    // =====================================================
                    if (saveOptions.SaveInvoice && admissionId.HasValue && saveData.Invoice != null && saveData.Invoice.Count > 0)
                    {
                        _logger.LogInformation("=== STEP 3: Creating invoice ===");
                        _logger.LogInformation("Invoice items count: {ItemCount}", saveData.Invoice.Count);

                        // Calculate totals
                        decimal hospitalAmount = 0;
                        decimal net = 0;
                        decimal gross = 0;

                        foreach (var item in saveData.Invoice.Where(i => i.Denomination > 0))
                        {
                            var itemTotal = (item.UnitPrice ?? 0) * (item.Quantity ?? 1);
                            hospitalAmount += itemTotal;
                            net += item.NetPrice ?? 0;
                            gross += itemTotal;
                        }

                        // Insert invoice header
                        var billingConnectionString = _configuration.GetConnectionString("BillingConnection");
                        using var billingConnection = new SqlConnection(billingConnectionString);
                        await billingConnection.OpenAsync();

                        var insertInvoiceHeaderSql = @"
                            INSERT INTO Billing.dbo.InvoiceHeader 
                            (Type, SequenceNumber,CounterTypeID, Counter, Date, Admission,
                             HospitalAmount, PhysicianAmount, MedicamentAmount,
                             AccountID, AccountDescription, CurrencyID, Currency, ExchangeRate,
                             CheckInClassID, CheckInClass, CoverageClassID, CoverageClass, CoverageRate,
                             ReferralPhysicianID, ReferralPhysician,AttendingPhysicianID, ContextPriceID, ContextPrice,
                             Net, Gross, MRN, PatientName, AdmissionNumber, AdmissionDate,
                             Insurance, CreatedBy, CreatedDate, IsDeleted)
                            VALUES 
                            ('QuickAdmission',1, 1, 'QA', GETDATE(), @Admission,
                             @HospitalAmount, 0, 0,
                             @AccountID, ISNULL((select [Description] from HospitalDefinition.dbo.Insurance where Id = @AccountID), 'Quick Admission Invoice'), 2, 'USD', 1.0,
                             @CheckInClass, 'Quick Admission', 1, 'Full Coverage', 100.0,
                             @ReferralPhysician, (select name from HospitalDefinition.dbo.Physician where Id = @ReferralPhysician),@AttendingPhysician, 1, 'Standard',
                             @Net, @Gross, @MRN, @PatientName, @AdmissionNumber, @AdmissionDate,
                             @Insurance, @CreatedBy, GETDATE(), 0);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        using var invoiceHeaderCommand = new SqlCommand(insertInvoiceHeaderSql, billingConnection);
                        invoiceHeaderCommand.Parameters.AddWithValue("@Admission", admissionId.Value);
                        invoiceHeaderCommand.Parameters.AddWithValue("@HospitalAmount", hospitalAmount);
                        invoiceHeaderCommand.Parameters.AddWithValue("@Net", net);
                        invoiceHeaderCommand.Parameters.AddWithValue("@Gross", gross);
                        invoiceHeaderCommand.Parameters.AddWithValue("@AccountID", saveData.Admission?.AuxiliaryInsurance ?? (object)DBNull.Value);
                        invoiceHeaderCommand.Parameters.AddWithValue("@CheckInClass", saveData.Admission?.CheckInClass ?? (object)DBNull.Value);
                        invoiceHeaderCommand.Parameters.AddWithValue("@ReferralPhysician", saveData.Admission?.ReferralPhysician ?? 0);
                        invoiceHeaderCommand.Parameters.AddWithValue("@AttendingPhysician", saveData.Admission?.AttendingPhysician ?? saveData.Admission?.ReferralPhysician ?? (object)DBNull.Value);
                        invoiceHeaderCommand.Parameters.AddWithValue("@MRN", mrn);
                        invoiceHeaderCommand.Parameters.AddWithValue("@PatientName", $"{saveData.Patient?.FirstName} {saveData.Patient?.LastName}");
                        invoiceHeaderCommand.Parameters.AddWithValue("@AdmissionNumber", admissionNumber);
                        invoiceHeaderCommand.Parameters.AddWithValue("@AdmissionDate", DateTime.Parse(saveData.Admission?.CheckInDate ?? DateTime.Now.ToString()));
                        invoiceHeaderCommand.Parameters.AddWithValue("@Insurance", saveData.Admission?.MainInsurance ?? (object)DBNull.Value);
                        invoiceHeaderCommand.Parameters.AddWithValue("@CreatedBy", saveData.Admission?.CreatedBy ?? 338);

                        var invoiceHeaderIdResult = await invoiceHeaderCommand.ExecuteScalarAsync();
                        invoiceHeaderId = invoiceHeaderIdResult != null && invoiceHeaderIdResult != DBNull.Value ? (int)invoiceHeaderIdResult : null;
                        _logger.LogInformation("Invoice header created with ID: {InvoiceHeaderId}, Totals - Gross: {Gross}, Net: {Net}",
                            invoiceHeaderId, gross, net);

                        // Insert invoice details
                        _logger.LogInformation("Inserting {ItemCount} invoice detail items...", saveData.Invoice.Count(i => i.Denomination > 0));
                        foreach (var item in saveData.Invoice.Where(i => i.Denomination > 0))
                        {
                            /*              
                            DenominationCoeffValue
                            DenominationCoeffPrice
                            DeniedAmount
                            ReferralPhysician
                            CopyFlag
                            IsDoubtfull
                            IsCanceled*/
                            var insertDetailSql = @"
                                INSERT INTO Billing.dbo.InvoiceDetail 
                                (InvoiceHeader, PrescriptionDate, MedicationUnit, MedicationUnitDescription,
                                 Admission, Patient, Denomination, DenominationCode, DenominationDescription,
                                 Quantity, UnitPrice, NetPrice, NetUnitPrice,
                                 DifferenceAmount, Discount, LumpSum,
                                 OperatingPhysician, CostCenter, ProfitCenter,
                                 DetailDate, CreatedBy, CreatedDate, IsDeleted,DenominationCoeffValue,DenominationCoeffPrice,DeniedAmount,
                                 ReferralPhysician, CopyFlag, IsDoubtfull,IsCanceled )
                                VALUES 
                                (@InvoiceHeader, GETDATE(), @MedicationUnit, @MedicationUnitDescription,
                                 @Admission, @Patient, @Denomination, @DenominationCode, @DenominationDescription,
                                 @Quantity, @UnitPrice, @NetPrice, @NetUnitPrice,
                                 0, @Discount, @LumpSum,
                                 @OperatingPhysician, @CostCenter, @ProfitCenter,
                                 GETDATE(), @CreatedBy, GETDATE(), 0,(select den.Coefficientvalue from hospitaldefinition.dbo.denomination as den where den.id  = @Denomination),
                                 @UnitPrice,0,@ReferralPhysician,0,0,0)";

                            using var detailCommand = new SqlCommand(insertDetailSql, billingConnection);
                            detailCommand.Parameters.AddWithValue("@InvoiceHeader", invoiceHeaderId.Value);
                            detailCommand.Parameters.AddWithValue("@MedicationUnit", item.MedicationUnit ?? 113);
                            detailCommand.Parameters.AddWithValue("@MedicationUnitDescription", item.MedicationUnitDescription ?? "Clinics");
                            detailCommand.Parameters.AddWithValue("@Admission", admissionId.Value);
                            detailCommand.Parameters.AddWithValue("@Patient", patientId.Value);
                            detailCommand.Parameters.AddWithValue("@Denomination", item.Denomination ?? 0);
                            detailCommand.Parameters.AddWithValue("@DenominationCode", item.DenominationCode ?? "");
                            detailCommand.Parameters.AddWithValue("@DenominationDescription", item.DenominationDescription ?? "");
                            detailCommand.Parameters.AddWithValue("@Quantity", item.Quantity ?? 1);
                            detailCommand.Parameters.AddWithValue("@UnitPrice", item.UnitPrice ?? 0);
                            detailCommand.Parameters.AddWithValue("@NetPrice", item.NetPrice ?? 0);
                            detailCommand.Parameters.AddWithValue("@NetUnitPrice", item.NetUnitPrice ?? 0);
                            detailCommand.Parameters.AddWithValue("@Discount", item.Discount ?? 0);
                            detailCommand.Parameters.AddWithValue("@LumpSum", item.LumpSum ?? 0);
                            detailCommand.Parameters.AddWithValue("@OperatingPhysician", item.OperatingPhysician ?? 0);
                            detailCommand.Parameters.AddWithValue("@CostCenter", item.CostCenter ?? 12);
                            detailCommand.Parameters.AddWithValue("@ProfitCenter", item.ProfitCenter ?? 3);
                            detailCommand.Parameters.AddWithValue("@CreatedBy", item.CreatedBy ?? saveData.Admission?.CreatedBy ?? 338);
                            detailCommand.Parameters.AddWithValue("@ReferralPhysician", saveData.Admission?.ReferralPhysician ?? 0);

                            await detailCommand.ExecuteNonQueryAsync();
                        }

                        _logger.LogInformation("=== STEP 3 COMPLETE: Invoice created with Header ID: {InvoiceHeaderId}, {DetailCount} details inserted ===",
                            invoiceHeaderId, saveData.Invoice.Count(i => i.Denomination > 0));

                        // Update InvoiceHeader with receipt amounts (ReceivedLBP, ReceivedUSD)
                        var invReceipt = saveData.InvoiceReceipt;
                        if (invReceipt != null && (invReceipt.ReceiptAmount.HasValue && invReceipt.ReceiptAmount > 0 || invReceipt.ReceiptLocal.HasValue && invReceipt.ReceiptLocal > 0))
                        {
                            var updateReceiptSql = @"UPDATE Billing.dbo.InvoiceHeader SET ReceivedUSD = @ReceivedUSD, ReceivedLBP = @ReceivedLBP, ReceiptAmount = @ReceiptAmount WHERE ID = @InvoiceHeaderId";
                            using var updateReceiptCmd = new SqlCommand(updateReceiptSql, billingConnection);
                            updateReceiptCmd.Parameters.AddWithValue("@ReceivedUSD", invReceipt.ReceiptAmount ?? 0);
                            updateReceiptCmd.Parameters.AddWithValue("@ReceivedLBP", invReceipt.ReceiptLocal ?? 0);
                            updateReceiptCmd.Parameters.AddWithValue("@ReceiptAmount", invReceipt.TotalPaidInInvoiceCurrency ?? invReceipt.ReceiptAmount ?? 0);
                            updateReceiptCmd.Parameters.AddWithValue("@InvoiceHeaderId", invoiceHeaderId.Value);
                            await updateReceiptCmd.ExecuteNonQueryAsync();
                            _logger.LogInformation("Updated InvoiceHeader {Id} with ReceivedUSD={Usd}, ReceivedLBP={Lbp}", invoiceHeaderId, invReceipt.ReceiptAmount, invReceipt.ReceiptLocal);
                        }

                        // Create Advance if requested (receipt < invoice net)
                        if (saveOptions.CreateAdvance && invReceipt != null && admissionId.HasValue && invoiceHeaderId.HasValue)
                        {
                            try
                            {
                                var currency = (invReceipt.Currency ?? "USD").ToUpperInvariant();
                                var advanceAmount = invReceipt.TotalPaidInInvoiceCurrency ?? 0;
                                var receivedUsd = invReceipt.ReceiptAmount ?? 0;
                                var receivedLbp = invReceipt.ReceiptLocal ?? 0;

                                int advanceNumber;
                                var configConnStr = _configuration.GetConnectionString("ConfigurationConnection");
                                if (!string.IsNullOrEmpty(configConnStr))
                                {
                                    using var configConn = new SqlConnection(configConnStr);
                                    await configConn.OpenAsync();
                                    var getNextSql = "SELECT ISNULL(LastAdvanceNumber, 0) + 1 FROM Configuration.dbo.TransactionSequenceControl WHERE IsDeleted = 0";
                                    try
                                    {
                                        using var cmd = new SqlCommand(getNextSql, configConn);
                                        advanceNumber = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 1);
                                    }
                                    catch
                                    {
                                        getNextSql = "SELECT ISNULL(LastAdvanceNumber, 0) + 1 FROM Configuration.dbo.TransactionSequenceControl WHERE (IsDeleted = 0 OR IsDeleted IS NULL)";
                                        using var cmd = new SqlCommand(getNextSql, configConn);
                                        advanceNumber = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 1);
                                    }
                                    var updateSeqSql = "UPDATE Configuration.dbo.TransactionSequence SET LastAdvanceNumber = @Val WHERE IsDeleted = 0";
                                    try { using var upCmd = new SqlCommand(updateSeqSql, configConn); upCmd.Parameters.AddWithValue("@Val", advanceNumber); await upCmd.ExecuteNonQueryAsync(); }
                                    catch { try { updateSeqSql = "UPDATE Configuration.dbo.TransactionSequenceControl SET LastAdvanceNumber = @Val WHERE (IsDeleted = 0 OR IsDeleted IS NULL)"; using var upCmd = new SqlCommand(updateSeqSql, configConn); upCmd.Parameters.AddWithValue("@Val", advanceNumber); await upCmd.ExecuteNonQueryAsync(); } catch { } }
                                }
                                else
                                {
                                    var admConnStr = _configuration.GetConnectionString("AdmissionConnection");
                                    using var advConn = new SqlConnection(admConnStr);
                                    await advConn.OpenAsync();
                                    var maxSql = "SELECT ISNULL(MAX(AdvanceNumber), 0) + 1 FROM Admission.dbo.Advance";
                                    try { using var cmd = new SqlCommand(maxSql, advConn); advanceNumber = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 1); }
                                    catch { advanceNumber = 1; }
                                    advConn.Close();
                                }

                                string? invoiceNumber = null;
                                try
                                {
                                    var getInvNumSql = "SELECT ISNULL(SequenceNumber, ID) FROM Billing.dbo.InvoiceHeader WHERE ID = @Id";
                                    using var numCmd = new SqlCommand(getInvNumSql, billingConnection);
                                    numCmd.Parameters.AddWithValue("@Id", invoiceHeaderId.Value);
                                    var r = await numCmd.ExecuteScalarAsync();
                                    invoiceNumber = r?.ToString() ?? invoiceHeaderId.ToString();
                                }
                                catch { invoiceNumber = invoiceHeaderId.ToString()!; }

                                var advConnStr2 = _configuration.GetConnectionString("AdmissionConnection");
                                using var advConn2 = new SqlConnection(advConnStr2);
                                await advConn2.OpenAsync();

                                int currencyId = (currency == "USD" || currency == "$$" || currency == "$.$") ? 2 : 1;
                                decimal dbMain = currencyId == 1 ? advanceAmount / exchangeRate : advanceAmount;
                                decimal dbLocal = currencyId == 2 ? advanceAmount * exchangeRate : advanceAmount;
                                decimal safeRate = exchangeRate > 0 ? exchangeRate : 1m;

                                // Try full INSERT first; fall back to minimal if schema differs
                                var insertAdvanceSql = @"
                                    INSERT INTO Admission.dbo.Advance 
                                    (Date, Admission, AdvanceNumber, AdvanceAmount, DbMain, DbLocal, ReceiptAmount, 
                                     ReceiptMain, ReceiptLocal, IsAssigned, Rate, Currency, 
                                     ReceivedLBP, ReceivedUSD, InvoiceId, InvoiceNumber, IsDeleted, CreatedBy, CreatedDate)
                                    VALUES (GETDATE(), @Admission, @AdvanceNumber, @AdvanceAmount, @DbMain, @DbLocal, 
                                            @ReceiptAmount, @ReceiptMain, @ReceiptLocal, 0, @Rate, @Currency, 
                                            @ReceivedLBP, @ReceivedUSD, @InvoiceId, @InvoiceNumber, 0, 338, GETDATE())";
                                using var advCmd = new SqlCommand(insertAdvanceSql, advConn2);
                                advCmd.Parameters.AddWithValue("@Admission", admissionId.Value);
                                advCmd.Parameters.AddWithValue("@AdvanceNumber", advanceNumber);
                                advCmd.Parameters.AddWithValue("@AdvanceAmount", advanceAmount);
                                advCmd.Parameters.AddWithValue("@DbMain", dbMain);
                                advCmd.Parameters.AddWithValue("@DbLocal", dbLocal);
                                advCmd.Parameters.AddWithValue("@ReceiptAmount", advanceAmount);
                                advCmd.Parameters.AddWithValue("@ReceiptMain", receivedUsd);
                                advCmd.Parameters.AddWithValue("@ReceiptLocal", receivedLbp);
                                advCmd.Parameters.AddWithValue("@Rate", safeRate);
                                advCmd.Parameters.AddWithValue("@Currency", currencyId);
                                advCmd.Parameters.AddWithValue("@ReceivedLBP", receivedLbp);
                                advCmd.Parameters.AddWithValue("@ReceivedUSD", receivedUsd);
                                advCmd.Parameters.AddWithValue("@InvoiceId", invoiceHeaderId.Value);
                                advCmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber ?? invoiceHeaderId.ToString()!);
                                try
                                {
                                    string advancequery = GetDebugSql(advCmd);
                                    await advCmd.ExecuteNonQueryAsync();
                                    _logger.LogInformation("=== Advance created: AdvanceNumber={AdvNum}, Admission={Adm}, Amount={Amt} ===", advanceNumber, admissionId, advanceAmount);
                                }
                                catch (SqlException sqlEx)
                                {
                                    _logger.LogWarning(sqlEx, "Advance full INSERT failed (columns may differ). Trying minimal INSERT. Error: {Message}", sqlEx.Message);
                                    using var advCmd2 = new SqlCommand(@"
                                        INSERT INTO Admission.dbo.Advance (Admission, ReceiptNumber, ReceiptDate, AdvanceAmount, IsDeleted, CreatedBy, CreatedDate)
                                        VALUES (@Admission, @ReceiptNumber, GETDATE(), @AdvanceAmount, 0, 338, GETDATE())", advConn2);
                                    advCmd2.Parameters.AddWithValue("@Admission", admissionId.Value);
                                    advCmd2.Parameters.AddWithValue("@ReceiptNumber", advanceNumber);
                                    advCmd2.Parameters.AddWithValue("@AdvanceAmount", advanceAmount);
                                    await advCmd2.ExecuteNonQueryAsync();
                                    _logger.LogInformation("=== Advance created (minimal): AdvanceNumber={AdvNum}, Admission={Adm} ===", advanceNumber, admissionId);
                                }
                            }
                            catch (Exception advEx)
                            {
                                _logger.LogWarning(advEx, "Failed to create Advance record. Error: {Message}. Inner: {Inner}. SQL State: {State}. Continuing.", 
                                    advEx.Message, advEx.InnerException?.Message, (advEx as SqlException)?.State);
                            }
                        }
                    }

                    // =====================================================
                    // 4. SAVE DELIVERY HEADER + ITEMS (Inventory.dbo) - if medicament items present
                    // =====================================================
                    var deliverItemsList = saveData.DeliverItems;
                    if (admissionId.HasValue && patientId.HasValue && deliverItemsList != null && deliverItemsList.Count > 0)
                    {
                        _logger.LogInformation("=== STEP 4: Creating DeliveryHeader and DeliverItems in Inventory ===");

                        var inventoryConnectionString = _configuration.GetConnectionString("InventoryConnection");
                        if (!string.IsNullOrEmpty(inventoryConnectionString))
                        {
                            using var invConnection = new SqlConnection(inventoryConnectionString);
                            await invConnection.OpenAsync();
                            using var invTransaction = invConnection.BeginTransaction();

                            try
                            {
                                // Insert DeliverHeader (DeliveryHeader table - matches DeliveryHeaderController)
                                var getMaxHeaderSql = "SELECT ISNULL(MAX([ID]), 0) + 1 FROM [dbo].[DeliveryHeader]";
                                int newHeaderId;
                                using (var cmd = new SqlCommand(getMaxHeaderSql, invConnection, invTransaction))
                                {
                                    newHeaderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                }

                                var dh = saveData.DeliverHeader ?? new QuickAdmissionDeliverHeader();
                                var insertHeaderSql = @"
                                    INSERT INTO [dbo].[DeliveryHeader] 
                                    ([ID], [Type], [TypeCounter], [PatientType], [Date], [Patient], [Currency], [Warehouse], [Admission],
                                     [Gross], [Net], [CreatedBy], [CreatedDate], [IsDeleted])
                                    VALUES (@Id, @Type, @TypeCounter, @PatientType, @Date, @Patient, @Currency, @Warehouse, @Admission,
                                     @Gross, @Net, @CreatedBy, GETDATE(), 0)";

                                using (var headerCmd = new SqlCommand(insertHeaderSql, invConnection, invTransaction))
                                {
                                    headerCmd.Parameters.AddWithValue("@Id", newHeaderId);
                                    headerCmd.Parameters.AddWithValue("@Type", dh.Type ?? 1);
                                    headerCmd.Parameters.AddWithValue("@TypeCounter", dh.TypeCounter ?? 0);
                                    headerCmd.Parameters.AddWithValue("@PatientType", dh.PatientType ?? 0);
                                    headerCmd.Parameters.AddWithValue("@Date", !string.IsNullOrEmpty(dh.Date) ? DateTime.Parse(dh.Date) : DateTime.Now);
                                    headerCmd.Parameters.AddWithValue("@Patient", patientId.Value);
                                    headerCmd.Parameters.AddWithValue("@Currency", dh.Currency ?? 2);
                                    headerCmd.Parameters.AddWithValue("@Warehouse", dh.Warehouse ?? 1);
                                    headerCmd.Parameters.AddWithValue("@Admission", admissionId.Value);
                                    var gross = deliverItemsList.Sum(d => (d.Net ?? 0));
                                    headerCmd.Parameters.AddWithValue("@Gross", gross);
                                    headerCmd.Parameters.AddWithValue("@Net", gross);
                                    headerCmd.Parameters.AddWithValue("@CreatedBy", dh.CreatedBy ?? 338);
                                    await headerCmd.ExecuteNonQueryAsync();
                                }

                                // Insert each DeliverItem (DeliverItem table - matches Inventory.dbo.DeliverItem)
                                foreach (var di in deliverItemsList)
                                {
                                    var getMaxItemSql = "SELECT ISNULL(MAX([id]), 0) + 1 FROM [dbo].[DeliverItem]";
                                    int newItemId;
                                    using (var cmd = new SqlCommand(getMaxItemSql, invConnection, invTransaction))
                                    {
                                        newItemId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                    }

                                    var insertItemSql = @"
                                        INSERT INTO [dbo].[DeliverItem] 
                                        ([id], [DeliverHeader], [Product], [Package], [Code], [AlternateDescription], [Qty], [UnitPrice], [Net], [CreatedBy], [CreatedDate], [IsDeleted])
                                        VALUES (@Id, @DeliverHeader, @Product, @Package, @Code, @AlternateDescription, @Qty, @UnitPrice, @Net, @CreatedBy, GETDATE(), 0)";

                                    using (var itemCmd = new SqlCommand(insertItemSql, invConnection, invTransaction))
                                    {
                                        itemCmd.Parameters.AddWithValue("@Id", newItemId);
                                        itemCmd.Parameters.AddWithValue("@DeliverHeader", newHeaderId);
                                        itemCmd.Parameters.AddWithValue("@Product", di.Product ?? 0);
                                        itemCmd.Parameters.AddWithValue("@Package", di.Package ?? 0);
                                        itemCmd.Parameters.AddWithValue("@Code", di.Code ?? "");
                                        itemCmd.Parameters.AddWithValue("@AlternateDescription", di.AlternateDescription ?? "");
                                        itemCmd.Parameters.AddWithValue("@Qty", di.Qty ?? 0);
                                        itemCmd.Parameters.AddWithValue("@UnitPrice", di.UnitPrice ?? 0);
                                        itemCmd.Parameters.AddWithValue("@Net", di.Net ?? 0);
                                        itemCmd.Parameters.AddWithValue("@CreatedBy", 338);
                                        await itemCmd.ExecuteNonQueryAsync();
                                    }
                                }

                                invTransaction.Commit();
                                _logger.LogInformation("=== STEP 4 COMPLETE: DeliveryHeader {HeaderId} and {ItemCount} DeliverItems created ===", newHeaderId, deliverItemsList.Count);
                            }
                            catch (Exception ex)
                            {
                                invTransaction.Rollback();
                                _logger.LogWarning(ex, "Failed to save DeliverHeader/DeliverItems to Inventory - transaction rolled back");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("InventoryConnection not configured - skipping DeliverHeader/DeliverItems save");
                        }
                    }

                    _logger.LogInformation("=== Save_V1 SUCCESS ===");
                    _logger.LogInformation("Final results - MRN: {MRN}, PatientID: {PatientID}, AdmissionID: {AdmissionID}, AdmissionNumber: {AdmissionNumber}, InvoiceHeaderID: {InvoiceHeaderID}",
                        mrn, patientId, admissionId, admissionNumber, invoiceHeaderId);

                    return Ok(new
                    {
                        MRN = mrn,
                        PatientID = patientId,
                        AdmissionNumber = admissionNumber,
                        AdmissionID = admissionId,
                        InvoiceHeaderID = invoiceHeaderId,
                        Status = "Success",
                        ErrorMessage = ""
                    });
                }
                catch (Exception ex)
                {
                    var stackTrace = new System.Diagnostics.StackTrace(ex, true);
                    var frame = stackTrace.GetFrame(0);
                    var className = frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
                    var methodName = frame?.GetMethod()?.Name ?? "Unknown";
                    var lineNumber = frame?.GetFileLineNumber() ?? 0;
                    var fileName = frame?.GetFileName() ?? "Unknown";

                    _logger.LogError(ex,
                        "[{ClassName}] Error in Save_V1 transaction - Method: {MethodName}, File: {FileName}, Line: {LineNumber}, Message: {Message}",
                        className, methodName, fileName, lineNumber, ex.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                var stackTrace = new System.Diagnostics.StackTrace(ex, true);
                var frame = stackTrace.GetFrame(0);
                var className = frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
                var methodName = frame?.GetMethod()?.Name ?? "Unknown";
                var lineNumber = frame?.GetFileLineNumber() ?? 0;
                var fileName = System.IO.Path.GetFileName(frame?.GetFileName() ?? "Unknown");

                _logger.LogError(ex,
                    "[{ClassName}] Error in Save_V1 - Method: {MethodName}, File: {FileName}, Line: {LineNumber}, Message: {Message}, StackTrace: {StackTrace}",
                    className, methodName, fileName, lineNumber, ex.Message, ex.StackTrace);

                return StatusCode(500, new
                {
                    MRN = "",
                    PatientID = (int?)null,
                    AdmissionNumber = "",
                    AdmissionID = (int?)null,
                    InvoiceHeaderID = (int?)null,
                    Status = "Error",
                    ErrorMessage = $"[{className}] {ex.Message} (Line: {lineNumber})"
                });
            }
        }

        /// <summary>
        /// Helper method to convert JsonElement to bool? (handles bool, int, string)
        /// </summary>
        private bool? ConvertToBool(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                return null;

            if (element.ValueKind == JsonValueKind.True)
                return true;

            if (element.ValueKind == JsonValueKind.False)
                return false;

            if (element.ValueKind == JsonValueKind.Number)
            {
                var num = element.GetInt32();
                return num != 0;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var str = element.GetString()?.ToLowerInvariant();
                return str == "true" || str == "1" || str == "yes";
            }

            return null;
        }

        /// <summary>
        /// Simplified ResidentPatient sync for Save_V1
        /// </summary>
        private async Task SyncResidentPatient_V1(
            SqlConnection connection,
            SqlTransaction transaction,
            int admissionId,
            int patientId,
            int refPhysician,
            string refPhysicianName,
            string admissionNumber,
            string department,
            dynamic admissionData,
            dynamic patientData,
            int userId)
        {
            try
            {
                // Get patient info from HospitalDefinition
                var hospitalDefConnectionString = _configuration.GetConnectionString("HospitalDefinitionConnection");
                using var hospitalDefConnection = new SqlConnection(hospitalDefConnectionString);
                await hospitalDefConnection.OpenAsync();

                var patientSql = @"
                    SELECT 
                        MedicalRecordNumber, FirstName, LastName, MiddleName,
                        ArabicFullName, ISNULL(DOB, DOB) as DOB, Gender, Phone
                    FROM Patient
                    WHERE ID = @PatientId AND (IsDeleted = 0 OR IsDeleted IS NULL)";

                using var patientCmd = new SqlCommand(patientSql, hospitalDefConnection);
                patientCmd.Parameters.AddWithValue("@PatientId", patientId);

                string patientMRN = "";
                string patientName = "";
                string patientArabicFullName = "";
                DateTime? patientDOB = null;
                string patientGender = "";
                string patientPhone = "";

                //required fields for resident patient (static values for out cash patients)
                int CheckInClassID = 5;
                string CheckInClassDescription = "Out";
                int MainInsuranceID = 5;
                string MainInsuranceDescription = "Private";
                int MainInsuranceClassID = 4;
                string MainInsuranceClassDescription = "III";
                int ReferralPhysicianID;
                string ReferralPhysicianName;
                int MedicationUnitID = 113; //clinc 113 --  lab 68 -- radio 77
                string MedicationUnitDescription;
                int InsuranceID = 5;
                string InsuranceDescription = "Private";
                int GuarantorID = 9;
                string GuarantorDescription = "Private";
                int CurrencyID = 2;
                string CurrencyDescription = "$$";
                int ClassID = 5;
                string ClassDescription = "Out";
                int ContextPriceID = 4;
                string ContextPriceDescription = "Private";
                int ContextEnumerationID = 1;
                string ContextEnumerationDescription = "CNSS";
                int AdmissionType = 3;
                string AdmissionTypeDescription = "OutPatient(Cash)";
                bool IsDischarged = false;
                bool IsPharmDisch = false;
                bool IsNersingDischarge = false;
                bool IsRecheckIn = false;

                // Use insurance/account values coming from Save_V1 admission payload
                if (admissionData?.MainInsurance != null)
                {
                    MainInsuranceID = (int)admissionData.MainInsurance;
                }
                if (admissionData?.AuxiliaryInsurance != null)
                {
                    InsuranceID = (int)admissionData.AuxiliaryInsurance;
                }

                // Resolve insurance descriptions from HospitalDefinition.dbo.Insurance
                var insuranceLookupSql = "SELECT [Description] FROM HospitalDefinition.dbo.Insurance WHERE [ID] = @InsuranceId AND ([IsDeleted] = 0 OR [IsDeleted] IS NULL)";
                using (var mainInsCmd = new SqlCommand(insuranceLookupSql, hospitalDefConnection))
                {
                    mainInsCmd.Parameters.AddWithValue("@InsuranceId", MainInsuranceID);
                    var mainDescObj = await mainInsCmd.ExecuteScalarAsync();
                    if (mainDescObj != null && mainDescObj != DBNull.Value)
                    {
                        MainInsuranceDescription = mainDescObj.ToString() ?? MainInsuranceDescription;
                    }
                }
                using (var accountInsCmd = new SqlCommand(insuranceLookupSql, hospitalDefConnection))
                {
                    accountInsCmd.Parameters.AddWithValue("@InsuranceId", InsuranceID);
                    var accountDescObj = await accountInsCmd.ExecuteScalarAsync();
                    if (accountDescObj != null && accountDescObj != DBNull.Value)
                    {
                        InsuranceDescription = accountDescObj.ToString() ?? InsuranceDescription;
                    }
                }

                using var reader = await patientCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    patientMRN = reader["MedicalRecordNumber"]?.ToString() ?? "";
                    var firstName = reader["FirstName"]?.ToString() ?? "";
                    var lastName = reader["LastName"]?.ToString() ?? "";
                    var middleName = reader["MiddleName"]?.ToString() ?? "";
                    patientName = $"{firstName} {middleName} {lastName}".Trim();
                    patientArabicFullName = reader["ArabicFullName"]?.ToString() ?? "";
                    patientDOB = reader["DOB"] != DBNull.Value ? (DateTime?)reader["DOB"] : null;
                    patientGender = reader["Gender"]?.ToString() ?? "";
                    patientPhone = reader["Phone"]?.ToString() ?? "";
                }

                // Check if exists
                var checkSql = "SELECT ID FROM dbo.ResidentPatient WHERE Admission = @AdmissionId AND (IsDeleted = 0 OR IsDeleted IS NULL)";
                using var checkCmd = new SqlCommand(checkSql, connection, transaction);
                checkCmd.Parameters.AddWithValue("@AdmissionId", admissionId);
                var existingId = await checkCmd.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Update existing (Age and Group are calculated fields - don't update them)
                    var updateSql = @"
                        UPDATE dbo.ResidentPatient SET
                            PatientID = @PatientID, MRN = @MRN, AdmissionNumber = @AdmissionNumber,
                            PatientName = @PatientName, ArabicFullName = @ArabicFullName, MedicalRecordNumber = @MedicalRecordNumber,
                            PatientDOB = @PatientDOB, PatientGender = @PatientGender,
                            CheckInDate = @CheckInDate,
                            MainInsuranceID = @MainInsuranceID, MainInsuranceDescription = @MainInsuranceDescription,
                            InsuranceID = @InsuranceID, InsuranceDescription = @InsuranceDescription,
                            ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
                        WHERE ID = @ID";

                    using var updateCmd = new SqlCommand(updateSql, connection, transaction);
                    updateCmd.Parameters.AddWithValue("@ID", existingId);
                    updateCmd.Parameters.AddWithValue("@PatientID", patientId);
                    updateCmd.Parameters.AddWithValue("@MRN", string.IsNullOrEmpty(patientMRN) ? 0 : Convert.ToInt32(patientMRN));
                    updateCmd.Parameters.AddWithValue("@AdmissionNumber", admissionNumber);
                    updateCmd.Parameters.AddWithValue("@PatientName", patientName);
                    updateCmd.Parameters.AddWithValue("@ArabicFullName", string.IsNullOrEmpty(patientArabicFullName) ? (object)DBNull.Value : patientArabicFullName);
                    updateCmd.Parameters.AddWithValue("@MedicalRecordNumber", patientMRN);
                    updateCmd.Parameters.AddWithValue("@PatientDOB", patientDOB ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@PatientGender", patientGender);
                    updateCmd.Parameters.AddWithValue("@CheckInDate", DateTime.Parse(admissionData.CheckInDate));
                    updateCmd.Parameters.AddWithValue("@MainInsuranceID", MainInsuranceID);
                    updateCmd.Parameters.AddWithValue("@MainInsuranceDescription", MainInsuranceDescription);
                    updateCmd.Parameters.AddWithValue("@InsuranceID", InsuranceID);
                    updateCmd.Parameters.AddWithValue("@InsuranceDescription", InsuranceDescription);
                    updateCmd.Parameters.AddWithValue("@ModifiedBy", userId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new (Age and Group are calculated fields - don't insert them)
                    var insertSql = @"
                        INSERT INTO dbo.ResidentPatient 
                        (PatientID, Admission, MRN, AdmissionNumber, PatientName, ArabicFullName, MedicalRecordNumber,
                         PatientDOB, PatientGender, CheckInDate, IsDeleted, CreatedBy, CreatedDate,ReferralPhysicianID,ReferralPhysicianName,
                         CheckInClassID,CheckInClassDescription,MainInsuranceID,MainInsuranceDescription,MainInsuranceClassID,MainInsuranceClassDescription,
                         MedicationUnitID,MedicationUnitDescription,InsuranceID,InsuranceDescription,GuarantorID,GuarantorDescription,CurrencyID,CurrencyDescription,ClassID,ClassDescription,
                         ContextPriceID,ContextPriceDescription,ContextEnumerationID,ContextEnumerationDescription,AdmissionType,AdmissionTypeDescription,IsDischarged,IsPharmDisch,
                         IsNersingDischarge, IsRecheckIn)
                        VALUES 
                        (@PatientID, @Admission, @MRN, @AdmissionNumber, @PatientName, @ArabicFullName, @MedicalRecordNumber,
                         @PatientDOB, @PatientGender, @CheckInDate, 0, @CreatedBy, GETDATE(),@ReferralPhysicianID, (select name from HospitalDefinition.dbo.Physician where Id = @ReferralPhysicianID),@CheckInClassID,@CheckInClassDescription,
                         @MainInsuranceID,@MainInsuranceDescription,@MainInsuranceClassID,@MainInsuranceClassDescription,
                         @MedicationUnitID,@MedicationUnitDescription,@InsuranceID,@InsuranceDescription,@GuarantorID,@GuarantorDescription,
                         @CurrencyID,@CurrencyDescription,@ClassID,@ClassDescription,@ContextPriceID,@ContextPriceDescription,
                         @ContextEnumerationID,@ContextEnumerationDescription,@AdmissionType,@AdmissionTypeDescription,@IsDischarged,@IsPharmDisch,@IsNersingDischarge,@IsRecheckIn)";
                    MedicationUnitDescription = department;
                    switch (department)
                    {
                        case "clinic": MedicationUnitID = 113; break;//--  lab 68 -- radio 77
                        case "Lab": MedicationUnitID = 68; break;//--  lab 68 -- radio 77
                        case "radio": MedicationUnitID = 77; break;//--  lab 68 -- radio 77
                        default:
                            break;
                    }
                    using var insertCmd = new SqlCommand(insertSql, connection, transaction);
                    insertCmd.Parameters.AddWithValue("@PatientID", patientId);
                    insertCmd.Parameters.AddWithValue("@Admission", admissionId);
                    insertCmd.Parameters.AddWithValue("@MRN", string.IsNullOrEmpty(patientMRN) ? 0 : Convert.ToInt32(patientMRN));
                    insertCmd.Parameters.AddWithValue("@AdmissionNumber", admissionNumber);
                    insertCmd.Parameters.AddWithValue("@PatientName", patientName);
                    insertCmd.Parameters.AddWithValue("@ArabicFullName", string.IsNullOrEmpty(patientArabicFullName) ? (object)DBNull.Value : patientArabicFullName);
                    insertCmd.Parameters.AddWithValue("@MedicalRecordNumber", patientMRN);
                    insertCmd.Parameters.AddWithValue("@PatientDOB", patientDOB ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PatientGender", patientGender);
                    insertCmd.Parameters.AddWithValue("@CheckInDate", DateTime.Parse(admissionData.CheckInDate));
                    insertCmd.Parameters.AddWithValue("@CreatedBy", userId);
                    insertCmd.Parameters.AddWithValue("@ReferralPhysicianID", refPhysician);
                    insertCmd.Parameters.AddWithValue("@ReferralPhysicianName", refPhysicianName);
                    insertCmd.Parameters.AddWithValue("@CheckInClassID", CheckInClassID);
                    insertCmd.Parameters.AddWithValue("@CheckInClassDescription", CheckInClassDescription);
                    insertCmd.Parameters.AddWithValue("@MainInsuranceID", MainInsuranceID);
                    insertCmd.Parameters.AddWithValue("@MainInsuranceDescription", MainInsuranceDescription);
                    insertCmd.Parameters.AddWithValue("@MainInsuranceClassID", MainInsuranceClassID);
                    insertCmd.Parameters.AddWithValue("@MainInsuranceClassDescription", MainInsuranceClassDescription);
                    insertCmd.Parameters.AddWithValue("@MedicationUnitID", MedicationUnitID);
                    insertCmd.Parameters.AddWithValue("@MedicationUnitDescription", MedicationUnitDescription);
                    insertCmd.Parameters.AddWithValue("@InsuranceID", InsuranceID);
                    insertCmd.Parameters.AddWithValue("@InsuranceDescription", InsuranceDescription);
                    insertCmd.Parameters.AddWithValue("@GuarantorID", GuarantorID);
                    insertCmd.Parameters.AddWithValue("@GuarantorDescription", GuarantorDescription);
                    insertCmd.Parameters.AddWithValue("@CurrencyID", CurrencyID);
                    insertCmd.Parameters.AddWithValue("@CurrencyDescription", CurrencyDescription);
                    insertCmd.Parameters.AddWithValue("@ClassID", ClassID);
                    insertCmd.Parameters.AddWithValue("@ClassDescription", ClassDescription);
                    insertCmd.Parameters.AddWithValue("@ContextPriceID", ContextPriceID);
                    insertCmd.Parameters.AddWithValue("@ContextPriceDescription", ContextPriceDescription);
                    insertCmd.Parameters.AddWithValue("@ContextEnumerationID", ContextEnumerationID);
                    insertCmd.Parameters.AddWithValue("@ContextEnumerationDescription", ContextEnumerationDescription);
                    insertCmd.Parameters.AddWithValue("@AdmissionType", AdmissionType);
                    insertCmd.Parameters.AddWithValue("@AdmissionTypeDescription", AdmissionTypeDescription);
                    insertCmd.Parameters.AddWithValue("@IsDischarged", IsDischarged);
                    insertCmd.Parameters.AddWithValue("@IsPharmDisch", IsPharmDisch);
                    insertCmd.Parameters.AddWithValue("@IsNersingDischarge", IsNersingDischarge);
                    insertCmd.Parameters.AddWithValue("@IsRecheckIn", IsRecheckIn);
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                var stackTrace = new System.Diagnostics.StackTrace(ex, true);
                var frame = stackTrace.GetFrame(0);
                var className = frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
                var methodName = frame?.GetMethod()?.Name ?? "Unknown";
                var lineNumber = frame?.GetFileLineNumber() ?? 0;
                var fileName = System.IO.Path.GetFileName(frame?.GetFileName() ?? "Unknown");

                _logger.LogError(ex,
                    "[{ClassName}] Error syncing ResidentPatient in Save_V1 - Method: {MethodName}, File: {FileName}, Line: {LineNumber}, Message: {Message}",
                    className, methodName, fileName, lineNumber, ex.Message);
                // Don't throw - allow main transaction to continue
            }
        }
    }

    public class QuickAdmissionRequest
    {
        public int? ExistingPatientId { get; set; }
        public string? SaveData { get; set; }
        public string? SaveOptions { get; set; }
    }

    public class QuickAdmissionSaveData
    {
        public QuickAdmissionPatient? Patient { get; set; }
        public QuickAdmissionAdmission? Admission { get; set; }
        public List<QuickAdmissionInvoiceItem>? Invoice { get; set; }
        public QuickAdmissionInvoiceReceipt? InvoiceReceipt { get; set; }
        public QuickAdmissionDeliverHeader? DeliverHeader { get; set; }
        public List<QuickAdmissionDeliverItem>? DeliverItems { get; set; }
    }

    public class QuickAdmissionInvoiceReceipt
    {
        public string? Currency { get; set; }
        public decimal? ReceiptAmount { get; set; }
        public decimal? ReceiptLocal { get; set; }
        public decimal? InvoiceNet { get; set; }
        public decimal? TotalPaidInInvoiceCurrency { get; set; }
    }

    public class QuickAdmissionDeliverHeader
    {
        public int? Type { get; set; }
        public int? TypeCounter { get; set; }
        public int? PatientType { get; set; }
        public string? Date { get; set; }
        public int? Currency { get; set; }
        public int? Warehouse { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class QuickAdmissionDeliverItem
    {
        public int? Product { get; set; }
        public int? Package { get; set; }
        public string? Code { get; set; }
        public string? AlternateDescription { get; set; }
        public decimal? Qty { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Net { get; set; }
        public string? PLDescription { get; set; }
    }

    public class QuickAdmissionPatient
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? ArabicFullName { get; set; }
        public string? DOB { get; set; }
        public int? MaritalStatus { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class QuickAdmissionAdmission
    {
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
        public int? Type { get; set; }
        public bool? IsWorkAccident { get; set; }
        public bool? IsExtended { get; set; }
        public int? Group { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class QuickAdmissionInvoiceItem
    {
        public int? MedicationUnit { get; set; }
        public string? MedicationUnitDescription { get; set; }
        public int? Denomination { get; set; }
        public string? DenominationCode { get; set; }
        public string? DenominationDescription { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? NetPrice { get; set; }
        public decimal? NetUnitPrice { get; set; }
        public decimal? Discount { get; set; }
        public decimal? LumpSum { get; set; }
        public int? OperatingPhysician { get; set; }
        public int? CostCenter { get; set; }
        public int? ProfitCenter { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class QuickAdmissionSaveOptions
    {
        public bool SaveMedicalFile { get; set; } = true;
        public bool SaveAdmission { get; set; } = true;
        public bool SaveInvoice { get; set; } = true;
        public bool CreateAdvance { get; set; } = false;
    }
}