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
    public class BillingInvoiceHeaderController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<BillingInvoiceHeaderController> _logger;
        private readonly IConfiguration _configuration;

        public BillingInvoiceHeaderController(LISDbContext context, ILogger<BillingInvoiceHeaderController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BillingInvoiceHeader>>> GetInvoiceHeaders()
        {
            try
            {
                var headers = await _context.Database
                    .SqlQueryRaw<BillingInvoiceHeader>("SELECT * FROM [Billing].[dbo].[InvoiceHeader] WHERE [IsDeleted] = 0 ORDER BY [Date] DESC")
                    .ToListAsync();

                return Ok(headers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice headers");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillingInvoiceHeader>> GetInvoiceHeader(int id)
        {
            try
            {
                var header = await _context.Database
                    .SqlQueryRaw<BillingInvoiceHeader>("SELECT * FROM [Billing].[dbo].[InvoiceHeader] WHERE [ID] = {0} AND [IsDeleted] = 0", id)
                    .FirstOrDefaultAsync();

                if (header == null)
                {
                    return NotFound();
                }

                return Ok(header);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice header {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get invoice headers by Admission ID - returns in the same format as QuickAdmissionController.Save_V1 expects
        /// </summary>
        [HttpGet("ByAdmission/{admissionId}")]
        public async Task<ActionResult<IEnumerable<BillingInvoiceHeader>>> GetInvoiceHeadersByAdmission(int admissionId)
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

                var headers = new List<BillingInvoiceHeader>();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT * FROM [Billing].[dbo].[InvoiceHeader] 
                        WHERE [Admission] = @AdmissionId AND [IsDeleted] = 0 
                        ORDER BY [Date] DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@AdmissionId", admissionId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Helper method to safely get int value (handles both int and string types)
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

                                headers.Add(new BillingInvoiceHeader
                                {
                                    Id = SafeGetInt32(reader, "ID"),
                                    SequenceNumber = SafeGetInt32(reader, "SequenceNumber"),
                                    Type = reader.IsDBNull("Type") ? string.Empty : reader.GetString("Type"),
                                    CounterTypeId = SafeGetInt32(reader, "CounterTypeID"),
                                    Counter = SafeGetInt32(reader, "Counter"),
                                    Date = reader.IsDBNull("Date") ? DateTime.Now : reader.GetDateTime("Date"),
                                    Admission = SafeGetInt32(reader, "Admission"),
                                    Comment = reader.IsDBNull("Comment") ? null : reader.GetString("Comment"),
                                    HospitalAmount = SafeGetDecimal(reader, "HospitalAmount"),
                                    PhysicianAmount = SafeGetDecimal(reader, "PhysicianAmount"),
                                    MedicamentAmount = SafeGetDecimal(reader, "MedicamentAmount"),
                                    Discount = SafeGetDecimal(reader, "Discount"),
                                    DeniedAmount = SafeGetDecimal(reader, "DeniedAmount"),
                                    LumpSum = SafeGetDecimal(reader, "LumpSum"),
                                    AccountId = SafeGetInt32(reader, "AccountID"),
                                    AccountDescription = reader.IsDBNull("AccountDescription") ? string.Empty : reader.GetString("AccountDescription"),
                                    ComplementaryAccountId = SafeGetInt32Nullable(reader, "ComplementaryAccountID"),
                                    ComplementaryAccountDescription = reader.IsDBNull("ComplementaryAccountDescription") ? null : reader.GetString("ComplementaryAccountDescription"),
                                    CurrencyId = SafeGetInt32(reader, "CurrencyID"),
                                    Currency = reader.IsDBNull("Currency") ? string.Empty : reader.GetString("Currency"),
                                    ExchangeRate = SafeGetDecimal(reader, "ExchangeRate"),
                                    CheckInClassId = SafeGetInt32(reader, "CheckInClassID"),
                                    CheckInClass = reader.IsDBNull("CheckInClass") ? string.Empty : reader.GetString("CheckInClass"),
                                    CoverageClassId = SafeGetInt32(reader, "CoverageClassID"),
                                    CoverageClass = reader.IsDBNull("CoverageClass") ? string.Empty : reader.GetString("CoverageClass"),
                                    CoverageRate = SafeGetDecimal(reader, "CoverageRate"),
                                    ReferralPhysicianId = SafeGetInt32(reader, "ReferralPhysicianID"),
                                    ReferralPhysician = reader.IsDBNull("ReferralPhysician") ? string.Empty : reader.GetString("ReferralPhysician"),
                                    AttendingPhysicianId = SafeGetInt32Nullable(reader, "AttendingPhysicianID"),
                                    AttendingPhysician = reader.IsDBNull("AttendingPhysician") ? null : reader.GetString("AttendingPhysician"),
                                    Reference = reader.IsDBNull("Reference") ? null : reader.GetString("Reference"),
                                    MainInvoice = SafeGetInt32Nullable(reader, "MainInvoice"),
                                    OldMainInvoice = SafeGetInt32Nullable(reader, "OldMainInvoice"),
                                    Net = SafeGetDecimal(reader, "Net"),
                                    Gross = SafeGetDecimal(reader, "Gross"),
                                    NetGross = SafeGetDecimal(reader, "NetGross"),
                                    Complementary = SafeGetDecimal(reader, "Complementary"),
                                    ComplementaryOtherCurrency = SafeGetDecimal(reader, "ComplementaryOtherCurrency"),
                                    ComplementaryDifferenceOtherCurrency = SafeGetDecimal(reader, "ComplementaryDifferenceOtherCurrency"),
                                    MRN = reader.IsDBNull("MRN") ? string.Empty : reader.GetString("MRN"),
                                    PatientName = reader.IsDBNull("PatientName") ? string.Empty : reader.GetString("PatientName"),
                                    AdmissionNumber = reader.IsDBNull("AdmissionNumber") ? string.Empty : reader.GetString("AdmissionNumber"),
                                    AdmissionDate = reader.IsDBNull("AdmissionDate") ? DateTime.Now : reader.GetDateTime("AdmissionDate"),
                                    DepartmentId = SafeGetInt32(reader, "DepartmentID"),
                                    Department = reader.IsDBNull("Department") ? string.Empty : reader.GetString("Department"),
                                    DischargeDate = reader.IsDBNull("DischargeDate") ? null : reader.GetDateTime("DischargeDate"),
                                    ContextPriceId = SafeGetInt32(reader, "ContextPriceID"),
                                    ContextPrice = reader.IsDBNull("ContextPrice") ? string.Empty : reader.GetString("ContextPrice"),
                                    ReceiptNumber = reader.IsDBNull("ReceiptNumber") ? null : reader.GetString("ReceiptNumber"),
                                    ReceiptAmount = SafeGetDecimalNullable(reader, "ReceiptAmount"),
                                    ReceiptDate = reader.IsDBNull("ReceiptDate") ? null : reader.GetDateTime("ReceiptDate"),
                                    RoundedAmount = SafeGetDecimalNullable(reader, "RoundedAmount"),
                                    Insurance = SafeGetInt32(reader, "Insurance"),
                                    AdmissionInsuranceCoverage = SafeGetInt32(reader, "AdmissionInsuranceCoverage"),
                                    IsDRG = SafeGetInt32(reader, "IsDRG"),
                                    CollectionScheduleId = SafeGetInt32Nullable(reader, "CollectionScheduleID"),
                                    CollectionScheduleNumber = reader.IsDBNull("CollectionScheduleNumber") ? null : reader.GetString("CollectionScheduleNumber"),
                                    CollectionScheduleDate = reader.IsDBNull("CollectionScheduleDate") ? null : reader.GetDateTime("CollectionScheduleDate"),
                                    ReceivedLBP = SafeGetDecimalNullable(reader, "ReceivedLBP"),
                                    ReceivedUSD = SafeGetDecimalNullable(reader, "ReceivedUSD"),
                                    Difference = SafeGetDecimalNullable(reader, "Difference"),
                                    ReceivingDate = reader.IsDBNull("ReceivingDate") ? null : reader.GetDateTime("ReceivingDate"),
                                    VoucherNumber = reader.IsDBNull("VoucherNumber") ? null : reader.GetString("VoucherNumber"),
                                    SplitedInvoice = SafeGetInt32(reader, "SplitedInvoice"),
                                    PrimaryDischargeDiagnostic = reader.IsDBNull("PrimaryDischargeDiagnostic") ? null : reader.GetString("PrimaryDischargeDiagnostic"),
                                    SecondaryDischargeDiagnostic = reader.IsDBNull("SecondaryDischargeDiagnostic") ? null : reader.GetString("SecondaryDischargeDiagnostic"),
                                    RequireRegenerate = SafeGetInt32(reader, "RequireRegenerate"),
                                    LockedBy = SafeGetInt32Nullable(reader, "LockedBy"),
                                    LockedByName = reader.IsDBNull("LockedByName") ? null : reader.GetString("LockedByName"),
                                    LockedDate = reader.IsDBNull("LockedDate") ? null : reader.GetDateTime("LockedDate"),
                                    AlternateInvoiceId = SafeGetInt32Nullable(reader, "AlternateInvoiceID"),
                                    GlobalDiscount = SafeGetDecimal(reader, "GlobalDiscount"),
                                    DifferenceAdjust = SafeGetDecimalNullable(reader, "DifferenceAdjust"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? DateTime.Now : reader.GetDateTime("ModifiedDate"),
                                    IsDeleted = SafeGetInt32(reader, "IsDeleted"),
                                    CreatedBy = SafeGetInt32(reader, "CreatedBy"),
                                    ModifiedBy = SafeGetInt32Nullable(reader, "ModifiedBy"),
                                    CreatedDate = reader.IsDBNull("CreatedDate") ? DateTime.Now : reader.GetDateTime("CreatedDate"),
                                    Group = SafeGetInt32Nullable(reader, "Group"),
                                    AgreementNumber = reader.IsDBNull("AgreementNumber") ? null : reader.GetString("AgreementNumber"),
                                    ComplementaryDifferenceCalculationState = SafeGetInt32Nullable(reader, "ComplementaryDifferenceCalculationState"),
                                    IsReversed = SafeGetInt32(reader, "IsReversed"),
                                    CreditNoteNumber = SafeGetInt32Nullable(reader, "CreditNoteNumber"),
                                    CreditNoteDate = reader.IsDBNull("CreditNoteDate") ? null : reader.GetDateTime("CreditNoteDate"),
                                    CreditNotePaidAmount = SafeGetDecimalNullable(reader, "CreditNotePaidAmount"),
                                    CreditNoteDiscount = SafeGetDecimalNullable(reader, "CreditNoteDiscount"),
                                    EmployeeAccount = reader.IsDBNull("EmployeeAccount") ? null : reader.GetString("EmployeeAccount"),
                                    IsEmployee = SafeGetInt32Nullable(reader, "IsEmployee"),
                                    Status = SafeGetInt32Nullable(reader, "Status"),
                                    ContextEnumerationId = SafeGetInt32Nullable(reader, "ContextEnumerationID"),
                                    IsDirty = SafeGetInt32Nullable(reader, "IsDirty"),
                                    IsFromScratch = SafeGetInt32Nullable(reader, "IsFromScratch"),
                                    AgreementCreditAmount = SafeGetDecimalNullable(reader, "AgreementCreditAmount"),
                                    IsSelected = SafeGetInt32Nullable(reader, "IsSelected"),
                                    CreditNoteAssignedAmount = SafeGetDecimalNullable(reader, "CreditNoteAssignedAmount"),
                                    CreditNoteVoucherNumber = reader.IsDBNull("CreditNoteVoucherNumber") ? null : reader.GetString("CreditNoteVoucherNumber"),
                                    PrepaymentAmount = SafeGetDecimalNullable(reader, "PrepaymentAmount"),
                                    PrepaymentDate = reader.IsDBNull("PrepaymentDate") ? null : reader.GetDateTime("PrepaymentDate"),
                                    PrepaymentNumber = SafeGetInt32Nullable(reader, "PrepaymentNumber"),
                                    DiagnosticGroup1 = reader.IsDBNull("DiagnosticGroup1") ? null : reader.GetString("DiagnosticGroup1"),
                                    DiagnosticGroup2 = reader.IsDBNull("DiagnosticGroup2") ? null : reader.GetString("DiagnosticGroup2"),
                                    DiagnosticGroup3 = reader.IsDBNull("DiagnosticGroup3") ? null : reader.GetString("DiagnosticGroup3"),
                                    DiagnosticGroupId1 = SafeGetInt32Nullable(reader, "DiagnosticGroupID1"),
                                    DiagnosticGroupId2 = SafeGetInt32Nullable(reader, "DiagnosticGroupID2"),
                                    DiagnosticGroupId3 = SafeGetInt32Nullable(reader, "DiagnosticGroupID3")
                                });
                            }
                        }
                    }
                }

                return Ok(headers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice headers for admission {AdmissionId}", admissionId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<BillingInvoiceHeader>> CreateInvoiceHeader(BillingInvoiceHeader invoiceHeader)
        {
            try
            {
                // Generate new ID and sequence number
                var maxId = await _context.Database
                    .SqlQueryRaw<int>("SELECT ISNULL(MAX([ID]), 0) FROM [Billing].[dbo].[InvoiceHeader] WHERE [IsDeleted] = 0")
                    .FirstOrDefaultAsync();

                var maxSequence = await _context.Database
                    .SqlQueryRaw<int>("SELECT ISNULL(MAX([SequenceNumber]), 0) FROM [Billing].[dbo].[InvoiceHeader] WHERE [IsDeleted] = 0")
                    .FirstOrDefaultAsync();

                invoiceHeader.Id = maxId + 1;
                invoiceHeader.SequenceNumber = maxSequence + 1;
                invoiceHeader.CreatedDate = DateTime.Now;
                invoiceHeader.ModifiedDate = DateTime.Now;
                invoiceHeader.IsDeleted = 0;

                // Insert using raw SQL
                var sql = @"INSERT INTO [Billing].[dbo].[InvoiceHeader] 
                    ([ID], [SequenceNumber], [Type], [CounterTypeID], [Counter], [Date], [Admission], [Comment], 
                     [HospitalAmount], [PhysicianAmount], [MedicamentAmount], [Discount], [DeniedAmount], [LumpSum],
                     [AccountID], [AccountDescription], [ComplementaryAccountID], [ComplementaryAccountDescription],
                     [CurrencyID], [Currency], [ExchangeRate], [CheckInClassID], [CheckInClass], [CoverageClassID], [CoverageClass],
                     [CoverageRate], [ReferralPhysicianID], [ReferralPhysician], [AttendingPhysicianID], [AttendingPhysician],
                     [Reference], [MainInvoice], [OldMainInvoice], [Net], [Gross], [NetGross], [Complementary],
                     [ComplementaryOtherCurrency], [ComplementaryDifferenceOtherCurrency], [MRN], [PatientName],
                     [AdmissionNumber], [AdmissionDate], [DepartmentID], [Department], [DischargeDate], [ContextPriceID], [ContextPrice],
                     [ReceiptNumber], [ReceiptAmount], [ReceiptDate], [RoundedAmount], [Insurance], [AdmissionInsuranceCoverage],
                     [IsDRG], [CollectionScheduleID], [CollectionScheduleNumber], [CollectionScheduleDate], [ReceivedLBP], [ReceivedUSD],
                     [Difference], [ReceivingDate], [VoucherNumber], [SplitedInvoice], [PrimaryDischargeDiagnostic], [SecondaryDischargeDiagnostic],
                     [RequireRegenerate], [LockedBy], [LockedByName], [LockedDate], [AlternateInvoiceID], [GlobalDiscount],
                     [DifferenceAdjust], [ModifiedDate], [IsDeleted], [CreatedBy], [ModifiedBy], [CreatedDate], [Group],
                     [AgreementNumber], [ComplementaryDifferenceCalculationState], [IsReversed], [CreditNoteNumber], [CreditNoteDate],
                     [CreditNotePaidAmount], [CreditNoteDiscount], [EmployeeAccount], [IsEmployee], [Status], [ContextEnumerationID],
                     [IsDirty], [IsFromScratch], [AgreementCreditAmount], [IsSelected], [CreditNoteAssignedAmount], [CreditNoteVoucherNumber],
                     [PrepaymentAmount], [PrepaymentDate], [PrepaymentNumber], [DiagnosticGroup1], [DiagnosticGroup2], [DiagnosticGroup3],
                     [DiagnosticGroupID1], [DiagnosticGroupID2], [DiagnosticGroupID3])
                    VALUES 
                    ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}, {22}, {23}, {24}, {25}, {26}, {27}, {28}, {29}, {30}, {31}, {32}, {33}, {34}, {35}, {36}, {37}, {38}, {39}, {40}, {41}, {42}, {43}, {44}, {45}, {46}, {47}, {48}, {49}, {50}, {51}, {52}, {53}, {54}, {55}, {56}, {57}, {58}, {59}, {60}, {61}, {62}, {63}, {64}, {65}, {66}, {67}, {68}, {69}, {70}, {71}, {72}, {73}, {74}, {75}, {76}, {77}, {78}, {79}, {80}, {81}, {82}, {83}, {84}, {85}, {86}, {87}, {88}, {89}, {90}, {91}, {92}, {93}, {94}, {95}, {96}, {97}, {98}, {99}, {100}, {101}, {102}, {103}, {104})";

                await _context.Database.ExecuteSqlRawAsync(sql,
                    invoiceHeader.Id, invoiceHeader.SequenceNumber, invoiceHeader.Type, invoiceHeader.CounterTypeId, invoiceHeader.Counter,
                    invoiceHeader.Date, invoiceHeader.Admission, invoiceHeader.Comment, invoiceHeader.HospitalAmount, invoiceHeader.PhysicianAmount,
                    invoiceHeader.MedicamentAmount, invoiceHeader.Discount, invoiceHeader.DeniedAmount, invoiceHeader.LumpSum, invoiceHeader.AccountId,
                    invoiceHeader.AccountDescription, invoiceHeader.ComplementaryAccountId, invoiceHeader.ComplementaryAccountDescription,
                    invoiceHeader.CurrencyId, invoiceHeader.Currency, invoiceHeader.ExchangeRate, invoiceHeader.CheckInClassId, invoiceHeader.CheckInClass,
                    invoiceHeader.CoverageClassId, invoiceHeader.CoverageClass, invoiceHeader.CoverageRate, invoiceHeader.ReferralPhysicianId,
                    invoiceHeader.ReferralPhysician, invoiceHeader.AttendingPhysicianId, invoiceHeader.AttendingPhysician, invoiceHeader.Reference,
                    invoiceHeader.MainInvoice, invoiceHeader.OldMainInvoice, invoiceHeader.Net, invoiceHeader.Gross, invoiceHeader.NetGross,
                    invoiceHeader.Complementary, invoiceHeader.ComplementaryOtherCurrency, invoiceHeader.ComplementaryDifferenceOtherCurrency,
                    invoiceHeader.MRN, invoiceHeader.PatientName, invoiceHeader.AdmissionNumber, invoiceHeader.AdmissionDate, invoiceHeader.DepartmentId,
                    invoiceHeader.Department, invoiceHeader.DischargeDate, invoiceHeader.ContextPriceId, invoiceHeader.ContextPrice,
                    invoiceHeader.ReceiptNumber, invoiceHeader.ReceiptAmount, invoiceHeader.ReceiptDate, invoiceHeader.RoundedAmount,
                    invoiceHeader.Insurance, invoiceHeader.AdmissionInsuranceCoverage, invoiceHeader.IsDRG, invoiceHeader.CollectionScheduleId,
                    invoiceHeader.CollectionScheduleNumber, invoiceHeader.CollectionScheduleDate, invoiceHeader.ReceivedLBP, invoiceHeader.ReceivedUSD,
                    invoiceHeader.Difference, invoiceHeader.ReceivingDate, invoiceHeader.VoucherNumber, invoiceHeader.SplitedInvoice,
                    invoiceHeader.PrimaryDischargeDiagnostic, invoiceHeader.SecondaryDischargeDiagnostic, invoiceHeader.RequireRegenerate,
                    invoiceHeader.LockedBy, invoiceHeader.LockedByName, invoiceHeader.LockedDate, invoiceHeader.AlternateInvoiceId,
                    invoiceHeader.GlobalDiscount, invoiceHeader.DifferenceAdjust, invoiceHeader.ModifiedDate, invoiceHeader.IsDeleted,
                    invoiceHeader.CreatedBy, invoiceHeader.ModifiedBy, invoiceHeader.CreatedDate, invoiceHeader.Group, invoiceHeader.AgreementNumber,
                    invoiceHeader.ComplementaryDifferenceCalculationState, invoiceHeader.IsReversed, invoiceHeader.CreditNoteNumber,
                    invoiceHeader.CreditNoteDate, invoiceHeader.CreditNotePaidAmount, invoiceHeader.CreditNoteDiscount, invoiceHeader.EmployeeAccount,
                    invoiceHeader.IsEmployee, invoiceHeader.Status, invoiceHeader.ContextEnumerationId, invoiceHeader.IsDirty, invoiceHeader.IsFromScratch,
                    invoiceHeader.AgreementCreditAmount, invoiceHeader.IsSelected, invoiceHeader.CreditNoteAssignedAmount,
                    invoiceHeader.CreditNoteVoucherNumber, invoiceHeader.PrepaymentAmount, invoiceHeader.PrepaymentDate, invoiceHeader.PrepaymentNumber,
                    invoiceHeader.DiagnosticGroup1, invoiceHeader.DiagnosticGroup2, invoiceHeader.DiagnosticGroup3, invoiceHeader.DiagnosticGroupId1,
                    invoiceHeader.DiagnosticGroupId2, invoiceHeader.DiagnosticGroupId3);

                return CreatedAtAction(nameof(GetInvoiceHeader), new { id = invoiceHeader.Id }, invoiceHeader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice header");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoiceHeader(int id, BillingInvoiceHeader invoiceHeader)
        {
            if (id != invoiceHeader.Id)
            {
                return BadRequest();
            }

            try
            {
                invoiceHeader.ModifiedDate = DateTime.Now;

                var sql = @"UPDATE [Billing].[dbo].[InvoiceHeader] SET 
                    [SequenceNumber] = {1}, [Type] = {2}, [CounterTypeID] = {3}, [Counter] = {4}, [Date] = {5}, [Admission] = {6}, [Comment] = {7},
                    [HospitalAmount] = {8}, [PhysicianAmount] = {9}, [MedicamentAmount] = {10}, [Discount] = {11}, [DeniedAmount] = {12}, [LumpSum] = {13},
                    [AccountID] = {14}, [AccountDescription] = {15}, [ComplementaryAccountID] = {16}, [ComplementaryAccountDescription] = {17},
                    [CurrencyID] = {18}, [Currency] = {19}, [ExchangeRate] = {20}, [CheckInClassID] = {21}, [CheckInClass] = {22}, [CoverageClassID] = {23}, [CoverageClass] = {24},
                    [CoverageRate] = {25}, [ReferralPhysicianID] = {26}, [ReferralPhysician] = {27}, [AttendingPhysicianID] = {28}, [AttendingPhysician] = {29},
                    [Reference] = {30}, [MainInvoice] = {31}, [OldMainInvoice] = {32}, [Net] = {33}, [Gross] = {34}, [NetGross] = {35}, [Complementary] = {36},
                    [ComplementaryOtherCurrency] = {37}, [ComplementaryDifferenceOtherCurrency] = {38}, [MRN] = {39}, [PatientName] = {40},
                    [AdmissionNumber] = {41}, [AdmissionDate] = {42}, [DepartmentID] = {43}, [Department] = {44}, [DischargeDate] = {45}, [ContextPriceID] = {46}, [ContextPrice] = {47},
                    [ReceiptNumber] = {48}, [ReceiptAmount] = {49}, [ReceiptDate] = {50}, [RoundedAmount] = {51}, [Insurance] = {52}, [AdmissionInsuranceCoverage] = {53},
                    [IsDRG] = {54}, [CollectionScheduleID] = {55}, [CollectionScheduleNumber] = {56}, [CollectionScheduleDate] = {57}, [ReceivedLBP] = {58}, [ReceivedUSD] = {59},
                    [Difference] = {60}, [ReceivingDate] = {61}, [VoucherNumber] = {62}, [SplitedInvoice] = {63}, [PrimaryDischargeDiagnostic] = {64}, [SecondaryDischargeDiagnostic] = {65},
                    [RequireRegenerate] = {66}, [LockedBy] = {67}, [LockedByName] = {68}, [LockedDate] = {69}, [AlternateInvoiceID] = {70}, [GlobalDiscount] = {71},
                    [DifferenceAdjust] = {72}, [ModifiedDate] = {73}, [ModifiedBy] = {74}, [Group] = {75},
                    [AgreementNumber] = {76}, [ComplementaryDifferenceCalculationState] = {77}, [IsReversed] = {78}, [CreditNoteNumber] = {79}, [CreditNoteDate] = {80},
                    [CreditNotePaidAmount] = {81}, [CreditNoteDiscount] = {82}, [EmployeeAccount] = {83}, [IsEmployee] = {84}, [Status] = {85}, [ContextEnumerationID] = {86},
                    [IsDirty] = {87}, [IsFromScratch] = {88}, [AgreementCreditAmount] = {89}, [IsSelected] = {90}, [CreditNoteAssignedAmount] = {91}, [CreditNoteVoucherNumber] = {92},
                    [PrepaymentAmount] = {93}, [PrepaymentDate] = {94}, [PrepaymentNumber] = {95}, [DiagnosticGroup1] = {96}, [DiagnosticGroup2] = {97}, [DiagnosticGroup3] = {98},
                    [DiagnosticGroupID1] = {99}, [DiagnosticGroupID2] = {100}, [DiagnosticGroupID3] = {101}
                    WHERE [ID] = {0}";

                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql,
                    id, invoiceHeader.SequenceNumber, invoiceHeader.Type, invoiceHeader.CounterTypeId, invoiceHeader.Counter,
                    invoiceHeader.Date, invoiceHeader.Admission, invoiceHeader.Comment, invoiceHeader.HospitalAmount, invoiceHeader.PhysicianAmount,
                    invoiceHeader.MedicamentAmount, invoiceHeader.Discount, invoiceHeader.DeniedAmount, invoiceHeader.LumpSum, invoiceHeader.AccountId,
                    invoiceHeader.AccountDescription, invoiceHeader.ComplementaryAccountId, invoiceHeader.ComplementaryAccountDescription,
                    invoiceHeader.CurrencyId, invoiceHeader.Currency, invoiceHeader.ExchangeRate, invoiceHeader.CheckInClassId, invoiceHeader.CheckInClass,
                    invoiceHeader.CoverageClassId, invoiceHeader.CoverageClass, invoiceHeader.CoverageRate, invoiceHeader.ReferralPhysicianId,
                    invoiceHeader.ReferralPhysician, invoiceHeader.AttendingPhysicianId, invoiceHeader.AttendingPhysician, invoiceHeader.Reference,
                    invoiceHeader.MainInvoice, invoiceHeader.OldMainInvoice, invoiceHeader.Net, invoiceHeader.Gross, invoiceHeader.NetGross,
                    invoiceHeader.Complementary, invoiceHeader.ComplementaryOtherCurrency, invoiceHeader.ComplementaryDifferenceOtherCurrency,
                    invoiceHeader.MRN, invoiceHeader.PatientName, invoiceHeader.AdmissionNumber, invoiceHeader.AdmissionDate, invoiceHeader.DepartmentId,
                    invoiceHeader.Department, invoiceHeader.DischargeDate, invoiceHeader.ContextPriceId, invoiceHeader.ContextPrice,
                    invoiceHeader.ReceiptNumber, invoiceHeader.ReceiptAmount, invoiceHeader.ReceiptDate, invoiceHeader.RoundedAmount,
                    invoiceHeader.Insurance, invoiceHeader.AdmissionInsuranceCoverage, invoiceHeader.IsDRG, invoiceHeader.CollectionScheduleId,
                    invoiceHeader.CollectionScheduleNumber, invoiceHeader.CollectionScheduleDate, invoiceHeader.ReceivedLBP, invoiceHeader.ReceivedUSD,
                    invoiceHeader.Difference, invoiceHeader.ReceivingDate, invoiceHeader.VoucherNumber, invoiceHeader.SplitedInvoice,
                    invoiceHeader.PrimaryDischargeDiagnostic, invoiceHeader.SecondaryDischargeDiagnostic, invoiceHeader.RequireRegenerate,
                    invoiceHeader.LockedBy, invoiceHeader.LockedByName, invoiceHeader.LockedDate, invoiceHeader.AlternateInvoiceId,
                    invoiceHeader.GlobalDiscount, invoiceHeader.DifferenceAdjust, invoiceHeader.ModifiedDate, invoiceHeader.ModifiedBy,
                    invoiceHeader.Group, invoiceHeader.AgreementNumber, invoiceHeader.ComplementaryDifferenceCalculationState, invoiceHeader.IsReversed,
                    invoiceHeader.CreditNoteNumber, invoiceHeader.CreditNoteDate, invoiceHeader.CreditNotePaidAmount, invoiceHeader.CreditNoteDiscount,
                    invoiceHeader.EmployeeAccount, invoiceHeader.IsEmployee, invoiceHeader.Status, invoiceHeader.ContextEnumerationId,
                    invoiceHeader.IsDirty, invoiceHeader.IsFromScratch, invoiceHeader.AgreementCreditAmount, invoiceHeader.IsSelected,
                    invoiceHeader.CreditNoteAssignedAmount, invoiceHeader.CreditNoteVoucherNumber, invoiceHeader.PrepaymentAmount,
                    invoiceHeader.PrepaymentDate, invoiceHeader.PrepaymentNumber, invoiceHeader.DiagnosticGroup1, invoiceHeader.DiagnosticGroup2,
                    invoiceHeader.DiagnosticGroup3, invoiceHeader.DiagnosticGroupId1, invoiceHeader.DiagnosticGroupId2, invoiceHeader.DiagnosticGroupId3);

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice header {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoiceHeader(int id)
        {
            try
            {
                var sql = "UPDATE [Billing].[dbo].[InvoiceHeader] SET [IsDeleted] = 1, [ModifiedDate] = {1} WHERE [ID] = {0}";
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, id, DateTime.Now);

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice header {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
