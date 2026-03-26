using Dapper;
using LIS.Api.Controllers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace LIS.Api.Repositories;

/// <summary>
/// Dapper-based repository for QuickAdmission Save_V1 operations.
/// Implemented for future refactoring; not yet used by QuickAdmissionController.
/// </summary>
public class QuickAdmissionDapperRepository
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuickAdmissionDapperRepository> _logger;

    public QuickAdmissionDapperRepository(
        IConfiguration configuration,
        ILogger<QuickAdmissionDapperRepository> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string ConfigurationConnection => _configuration.GetConnectionString("ConfigurationConnection") ?? "";
    private string HospitalDefinitionConnection => _configuration.GetConnectionString("HospitalDefinitionConnection") ?? "";
    private string AdmissionConnection => _configuration.GetConnectionString("AdmissionConnection") ?? "";
    private string BillingConnection => _configuration.GetConnectionString("BillingConnection") ?? "";
    private string InventoryConnection => _configuration.GetConnectionString("InventoryConnection") ?? "";

    /// <summary>Get exchange rate from Financial DB ConfigurationTable.</summary>
    public async Task<decimal> GetExchangeRateAsync(CancellationToken ct = default)
    {
        var sql = "SELECT TOP 1 DefaultRate FROM [Financial DB].dbo.ConfigurationTable";
        await using var conn = new SqlConnection(ConfigurationConnection);
        var rate = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition(sql, cancellationToken: ct));
        return rate ?? 90000m;
    }

    /// <summary>Get next MRN from TransactionSequenceControl and increment.</summary>
    public async Task<int> GetNextMrnAsync(CancellationToken ct = default)
    {
        var sql = @"
            DECLARE @NewMRN INT;
            UPDATE Configuration.dbo.TransactionSequenceControl
            SET LastMedicalRecordNumber = LastMedicalRecordNumber + 1
            WHERE ID = 1;
            SELECT LastMedicalRecordNumber FROM Configuration.dbo.TransactionSequenceControl WHERE ID = 1;";
        await using var conn = new SqlConnection(ConfigurationConnection);
        var mrn = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));
        return mrn;
    }

    /// <summary>Insert patient into HospitalDefinition.dbo.Patient.</summary>
    public async Task<int> InsertPatientAsync(QuickAdmissionPatient patient, string mrn, CancellationToken ct = default)
    {
        var sql = @"
            INSERT INTO HospitalDefinition.dbo.Patient 
            (FirstName, LastName, MiddleName, Gender, Phone, ArabicFullName, DOB, MaritalStatus, MedicalRecordNumber, CreatedBy, CreatedDate, IsDeleted)
            VALUES (@FirstName, @LastName, @MiddleName, @Gender, @Phone, @ArabicFullName, @DOB, @MaritalStatus, @MedicalRecordNumber, @CreatedBy, GETDATE(), 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        await using var conn = new SqlConnection(HospitalDefinitionConnection);
        var param = new DynamicParameters();
        param.Add("@FirstName", patient.FirstName ?? "");
        param.Add("@LastName", patient.LastName ?? "");
        param.Add("@MiddleName", patient.MiddleName);
        param.Add("@Gender", patient.Gender ?? "");
        param.Add("@Phone", patient.Phone);
        param.Add("@ArabicFullName", patient.ArabicFullName);
        param.Add("@DOB", !string.IsNullOrEmpty(patient.DOB) ? DateTime.Parse(patient.DOB) : (DateTime?)null);
        param.Add("@MaritalStatus", patient.MaritalStatus);
        param.Add("@MedicalRecordNumber", mrn);
        param.Add("@CreatedBy", patient.CreatedBy ?? 338);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, cancellationToken: ct));
    }

    /// <summary>Get patient MRN by ID.</summary>
    public async Task<string?> GetPatientMrnAsync(int patientId, CancellationToken ct = default)
    {
        var sql = "SELECT MedicalRecordNumber FROM HospitalDefinition.dbo.Patient WHERE ID = @PatientId AND (IsDeleted = 0 OR IsDeleted IS NULL)";
        await using var conn = new SqlConnection(HospitalDefinitionConnection);
        return await conn.ExecuteScalarAsync<string?>(new CommandDefinition(sql, new { PatientId = patientId }, cancellationToken: ct));
    }

    /// <summary>Update AdmissionCounter and return new CashOutPatient value.</summary>
    public async Task<int> UpdateAdmissionCounterAndGetAsync(DateTime checkInDate, IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var updateSql = @"
            UPDATE AdmissionCounter  
            SET CashOutPatient = CashOutPatient + 1 
            WHERE [YEAR] = YEAR(@CheckInDate) AND [Month] = MONTH(@CheckInDate)";
        await conn.ExecuteAsync(new CommandDefinition(updateSql, new { CheckInDate = checkInDate }, transaction, cancellationToken: ct));

        var getSql = "SELECT CashOutPatient FROM AdmissionCounter WHERE [YEAR] = YEAR(@CheckInDate) AND [Month] = MONTH(@CheckInDate)";
        var counter = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(getSql, new { CheckInDate = checkInDate }, transaction, cancellationToken: ct));
        return counter ?? 0;
    }

    /// <summary>Insert admission and return new ID.</summary>
    public async Task<int> InsertAdmissionAsync(QuickAdmissionAdmission admission, string admissionNumber, int patientId, DateTime checkInDate, IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var admissionType = admission.Type ?? 3;
        var sql = @"
            INSERT INTO dbo.Admission 
            (Number, AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, MainInsuranceClass, Insured,
             AuxiliaryInsurance, AuxiliaryInsuranceClass, CheckInClass, Department, CheckInDate, CheckOutDate, Patient, Type,
             IsWorkAccident, IsExtended, CreatedBy, CreatedDate, IsDeleted)
            VALUES (@Number, @AdmissionSite, @ReferralPhysician, @AttendingPhysician, @MainInsurance, @MainInsuranceClass, @Insured,
                    @AuxiliaryInsurance, @AuxiliaryInsuranceClass, @CheckInClass, @Department, @CheckInDate, @CheckOutDate, @Patient, @Type,
                    @IsWorkAccident, @IsExtended, @CreatedBy, GETDATE(), 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        var param = new DynamicParameters();
        param.Add("@Number", admissionNumber);
        param.Add("@AdmissionSite", admission.AdmissionSite);
        param.Add("@ReferralPhysician", admission.ReferralPhysician ?? 0);
        param.Add("@AttendingPhysician", admission.AttendingPhysician ?? admission.ReferralPhysician);
        param.Add("@MainInsurance", admission.MainInsurance);
        param.Add("@MainInsuranceClass", admission.MainInsuranceClass);
        param.Add("@Insured", admission.Insured);
        param.Add("@AuxiliaryInsurance", admission.AuxiliaryInsurance);
        param.Add("@AuxiliaryInsuranceClass", admission.AuxiliaryInsuranceClass);
        param.Add("@CheckInClass", admission.CheckInClass);
        param.Add("@Department", admission.Department);
        param.Add("@CheckInDate", checkInDate);
        param.Add("@CheckOutDate", !string.IsNullOrEmpty(admission.CheckOutDate) ? DateTime.Parse(admission.CheckOutDate) : (DateTime?)null);
        param.Add("@Patient", patientId);
        param.Add("@Type", admissionType);
        param.Add("@IsWorkAccident", admission.IsWorkAccident ?? false);
        param.Add("@IsExtended", admission.IsExtended ?? false);
        param.Add("@CreatedBy", admission.CreatedBy ?? 338);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, transaction, cancellationToken: ct));
    }

    /// <summary>Insert invoice header and return new ID.</summary>
    public async Task<int> InsertInvoiceHeaderAsync(
        int admissionId, decimal hospitalAmount, decimal net, decimal gross, string mrn, string patientName, string admissionNumber, DateTime admissionDate,
        QuickAdmissionAdmission admission, CancellationToken ct = default)
    {
        var sql = @"
            INSERT INTO Billing.dbo.InvoiceHeader 
            (Type, SequenceNumber, CounterTypeID, Counter, Date, Admission, HospitalAmount, PhysicianAmount, MedicamentAmount,
             AccountID, AccountDescription, CurrencyID, Currency, ExchangeRate, CheckInClassID, CheckInClass, CoverageClassID, CoverageClass, CoverageRate,
             ReferralPhysicianID, ReferralPhysician, AttendingPhysicianID, ContextPriceID, ContextPrice,
             Net, Gross, MRN, PatientName, AdmissionNumber, AdmissionDate, Insurance, CreatedBy, CreatedDate, IsDeleted)
            VALUES 
            ('QuickAdmission', 1, 1, 'QA', GETDATE(), @Admission, @HospitalAmount, 0, 0,
             @AccountID, ISNULL((SELECT [Description] FROM HospitalDefinition.dbo.Insurance WHERE Id = @AccountID), 'Quick Admission Invoice'), 1, 'USD', 1.0,
             @CheckInClass, 'Quick Admission', 1, 'Full Coverage', 100.0,
             @ReferralPhysician, (SELECT name FROM HospitalDefinition.dbo.Physician WHERE Id = @ReferralPhysician), @AttendingPhysician, 1, 'Standard',
             @Net, @Gross, @MRN, @PatientName, @AdmissionNumber, @AdmissionDate,
             @Insurance, @CreatedBy, GETDATE(), 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        var param = new DynamicParameters();
        param.Add("@Admission", admissionId);
        param.Add("@HospitalAmount", hospitalAmount);
        param.Add("@Net", net);
        param.Add("@Gross", gross);
        param.Add("@AccountID", admission.AuxiliaryInsurance);
        param.Add("@CheckInClass", admission.CheckInClass);
        param.Add("@ReferralPhysician", admission.ReferralPhysician ?? 0);
        param.Add("@AttendingPhysician", admission.AttendingPhysician ?? admission.ReferralPhysician);
        param.Add("@MRN", mrn);
        param.Add("@PatientName", patientName);
        param.Add("@AdmissionNumber", admissionNumber);
        param.Add("@AdmissionDate", admissionDate);
        param.Add("@Insurance", admission.MainInsurance);
        param.Add("@CreatedBy", admission.CreatedBy ?? 338);
        await using var conn = new SqlConnection(BillingConnection);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, cancellationToken: ct));
    }

    /// <summary>Insert invoice detail.</summary>
    public async Task InsertInvoiceDetailAsync(
        int invoiceHeaderId, int admissionId, int patientId, QuickAdmissionInvoiceItem item, int referralPhysician, IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = @"
            INSERT INTO Billing.dbo.InvoiceDetail 
            (InvoiceHeader, PrescriptionDate, MedicationUnit, MedicationUnitDescription, Admission, Patient, Denomination, DenominationCode, DenominationDescription,
             Quantity, UnitPrice, NetPrice, NetUnitPrice, DifferenceAmount, Discount, LumpSum, OperatingPhysician, CostCenter, ProfitCenter,
             DetailDate, CreatedBy, CreatedDate, IsDeleted, DenominationCoeffValue, DenominationCoeffPrice, DeniedAmount, ReferralPhysician, CopyFlag, IsDoubtfull, IsCanceled)
            VALUES 
            (@InvoiceHeader, GETDATE(), @MedicationUnit, @MedicationUnitDescription, @Admission, @Patient, @Denomination, @DenominationCode, @DenominationDescription,
             @Quantity, @UnitPrice, @NetPrice, @NetUnitPrice, 0, @Discount, @LumpSum, @OperatingPhysician, @CostCenter, @ProfitCenter,
             GETDATE(), @CreatedBy, GETDATE(), 0, (SELECT den.Coefficientvalue FROM HospitalDefinition.dbo.Denomination den WHERE den.id = @Denomination), @UnitPrice, 0, @ReferralPhysician, 0, 0, 0)";
        var param = new DynamicParameters();
        param.Add("@InvoiceHeader", invoiceHeaderId);
        param.Add("@MedicationUnit", item.MedicationUnit ?? 113);
        param.Add("@MedicationUnitDescription", item.MedicationUnitDescription ?? "Clinics");
        param.Add("@Admission", admissionId);
        param.Add("@Patient", patientId);
        param.Add("@Denomination", item.Denomination ?? 0);
        param.Add("@DenominationCode", item.DenominationCode ?? "");
        param.Add("@DenominationDescription", item.DenominationDescription ?? "");
        param.Add("@Quantity", item.Quantity ?? 1);
        param.Add("@UnitPrice", item.UnitPrice ?? 0);
        param.Add("@NetPrice", item.NetPrice ?? 0);
        param.Add("@NetUnitPrice", item.NetUnitPrice ?? 0);
        param.Add("@Discount", item.Discount ?? 0);
        param.Add("@LumpSum", item.LumpSum ?? 0);
        param.Add("@OperatingPhysician", item.OperatingPhysician ?? 0);
        param.Add("@CostCenter", item.CostCenter ?? 12);
        param.Add("@ProfitCenter", item.ProfitCenter ?? 3);
        param.Add("@CreatedBy", item.CreatedBy ?? 338);
        param.Add("@ReferralPhysician", referralPhysician);
        await conn.ExecuteAsync(new CommandDefinition(sql, param, transaction, cancellationToken: ct));
    }

    /// <summary>Update invoice header receipt amounts.</summary>
    public async Task UpdateInvoiceReceiptAsync(int invoiceHeaderId, decimal receivedUsd, decimal receivedLbp, decimal receiptAmount, CancellationToken ct = default)
    {
        var sql = "UPDATE Billing.dbo.InvoiceHeader SET ReceivedUSD = @ReceivedUSD, ReceivedLBP = @ReceivedLBP, ReceiptAmount = @ReceiptAmount WHERE ID = @InvoiceHeaderId";
        await using var conn = new SqlConnection(BillingConnection);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { InvoiceHeaderId = invoiceHeaderId, ReceivedUSD = receivedUsd, ReceivedLBP = receivedLbp, ReceiptAmount = receiptAmount }, cancellationToken: ct));
    }

    /// <summary>Get next advance number and update sequence.</summary>
    public async Task<int> GetNextAdvanceNumberAsync(CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(ConfigurationConnection);
        int advanceNumber;
        try
        {
            advanceNumber = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT ISNULL(LastAdvanceNumber, 0) + 1 FROM Configuration.dbo.TransactionSequenceControl WHERE IsDeleted = 0", cancellationToken: ct));
        }
        catch
        {
            advanceNumber = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT ISNULL(LastAdvanceNumber, 0) + 1 FROM Configuration.dbo.TransactionSequenceControl WHERE (IsDeleted = 0 OR IsDeleted IS NULL)", cancellationToken: ct));
        }
        try
        {
            await conn.ExecuteAsync(new CommandDefinition("UPDATE Configuration.dbo.TransactionSequenceControl SET LastAdvanceNumber = @Val WHERE (IsDeleted = 0 OR IsDeleted IS NULL)", new { Val = advanceNumber }, cancellationToken: ct));
        }
        catch { /* best effort */ }
        return advanceNumber;
    }

    /// <summary>Get max advance number from Advance table (fallback).</summary>
    public async Task<int> GetMaxAdvanceNumberAsync(CancellationToken ct = default)
    {
        var sql = "SELECT ISNULL(MAX(AdvanceNumber), 0) + 1 FROM Admission.dbo.Advance";
        await using var conn = new SqlConnection(AdmissionConnection);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));
    }

    /// <summary>Get invoice sequence number by ID.</summary>
    public async Task<string?> GetInvoiceNumberAsync(int invoiceHeaderId, CancellationToken ct = default)
    {
        var sql = "SELECT ISNULL(SequenceNumber, ID) FROM Billing.dbo.InvoiceHeader WHERE ID = @Id";
        await using var conn = new SqlConnection(BillingConnection);
        var r = await conn.ExecuteScalarAsync<object?>(new CommandDefinition(sql, new { Id = invoiceHeaderId }, cancellationToken: ct));
        return r?.ToString();
    }

    /// <summary>Insert advance record.</summary>
    public async Task InsertAdvanceAsync(AdvanceInsertDto dto, CancellationToken ct = default)
    {
        var sql = @"
            INSERT INTO Admission.dbo.Advance 
            (Date, Admission, AdvanceNumber, AdvanceAmount, DbMain, DbLocal, ReceiptAmount, ReceiptMain, ReceiptLocal, IsAssigned, Rate, Currency, 
             ReceivedLBP, ReceivedUSD, InvoiceId, InvoiceNumber, IsDeleted, CreatedBy, CreatedDate)
            VALUES (GETDATE(), @Admission, @AdvanceNumber, @AdvanceAmount, @DbMain, @DbLocal, @ReceiptAmount, @ReceiptMain, @ReceiptLocal, 0, @Rate, @Currency, 
                    @ReceivedLBP, @ReceivedUSD, @InvoiceId, @InvoiceNumber, 0, 338, GETDATE())";
        await using var conn = new SqlConnection(AdmissionConnection);
        await conn.ExecuteAsync(new CommandDefinition(sql, dto, cancellationToken: ct));
    }

    /// <summary>Insert advance (minimal columns fallback).</summary>
    public async Task InsertAdvanceMinimalAsync(int admissionId, int advanceNumber, decimal advanceAmount, CancellationToken ct = default)
    {
        var sql = @"
            INSERT INTO Admission.dbo.Advance (Admission, ReceiptNumber, ReceiptDate, AdvanceAmount, IsDeleted, CreatedBy, CreatedDate)
            VALUES (@Admission, @ReceiptNumber, GETDATE(), @AdvanceAmount, 0, 338, GETDATE())";
        await using var conn = new SqlConnection(AdmissionConnection);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Admission = admissionId, ReceiptNumber = advanceNumber, AdvanceAmount = advanceAmount }, cancellationToken: ct));
    }

    /// <summary>Get max DeliveryHeader ID + 1.</summary>
    public async Task<int> GetNextDeliveryHeaderIdAsync(IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = "SELECT ISNULL(MAX([ID]), 0) + 1 FROM [dbo].[DeliveryHeader]";
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, transaction: transaction, cancellationToken: ct));
    }

    /// <summary>Insert delivery header.</summary>
    public async Task InsertDeliveryHeaderAsync(DeliveryHeaderInsertDto dto, IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = @"
            INSERT INTO [dbo].[DeliveryHeader] 
            ([ID], [Type], [TypeCounter], [PatientType], [Date], [Patient], [Currency], [Warehouse], [Admission], [Gross], [Net], [CreatedBy], [CreatedDate], [IsDeleted])
            VALUES (@Id, @Type, @TypeCounter, @PatientType, @Date, @Patient, @Currency, @Warehouse, @Admission, @Gross, @Net, @CreatedBy, GETDATE(), 0)";
        await conn.ExecuteAsync(new CommandDefinition(sql, dto, transaction, cancellationToken: ct));
    }

    /// <summary>Get max DeliverItem ID + 1.</summary>
    public async Task<int> GetNextDeliverItemIdAsync(IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = "SELECT ISNULL(MAX([id]), 0) + 1 FROM [dbo].[DeliverItem]";
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, transaction: transaction, cancellationToken: ct));
    }

    /// <summary>Insert deliver item.</summary>
    public async Task InsertDeliverItemAsync(DeliverItemInsertDto dto, IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = @"
            INSERT INTO [dbo].[DeliverItem] 
            ([id], [DeliverHeader], [Product], [Package], [Code], [AlternateDescription], [Qty], [UnitPrice], [Net], [CreatedBy], [CreatedDate], [IsDeleted])
            VALUES (@Id, @DeliverHeader, @Product, @Package, @Code, @AlternateDescription, @Qty, @UnitPrice, @Net, @CreatedBy, GETDATE(), 0)";
        await conn.ExecuteAsync(new CommandDefinition(sql, dto, transaction, cancellationToken: ct));
    }

    /// <summary>Get patient info from HospitalDefinition for ResidentPatient sync.</summary>
    public async Task<PatientInfoForResidentSyncDto?> GetPatientInfoForResidentSyncAsync(int patientId, CancellationToken ct = default)
    {
        var sql = @"
            SELECT MedicalRecordNumber, FirstName, LastName, MiddleName, ArabicFullName, DOB, Gender, Phone
            FROM HospitalDefinition.dbo.Patient
            WHERE ID = @PatientId AND (IsDeleted = 0 OR IsDeleted IS NULL)";
        await using var conn = new SqlConnection(HospitalDefinitionConnection);
        var row = await conn.QuerySingleOrDefaultAsync<PatientInfoForResidentSyncDto>(new CommandDefinition(sql, new { PatientId = patientId }, cancellationToken: ct));
        return row;
    }

    /// <summary>Get insurance description by ID.</summary>
    public async Task<string?> GetInsuranceDescriptionAsync(int insuranceId, CancellationToken ct = default)
    {
        var sql = "SELECT [Description] FROM HospitalDefinition.dbo.Insurance WHERE [ID] = @InsuranceId AND ([IsDeleted] = 0 OR [IsDeleted] IS NULL)";
        await using var conn = new SqlConnection(HospitalDefinitionConnection);
        return await conn.ExecuteScalarAsync<string?>(new CommandDefinition(sql, new { InsuranceId = insuranceId }, cancellationToken: ct));
    }

    /// <summary>Check if ResidentPatient exists for admission.</summary>
    public async Task<int?> GetExistingResidentPatientIdAsync(int admissionId, IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = "SELECT ID FROM dbo.ResidentPatient WHERE Admission = @AdmissionId AND (IsDeleted = 0 OR IsDeleted IS NULL)";
        return await conn.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { AdmissionId = admissionId }, transaction, cancellationToken: ct));
    }

    /// <summary>Update ResidentPatient.</summary>
    public async Task UpdateResidentPatientAsync(ResidentPatientUpdateDto dto, IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = @"
            UPDATE dbo.ResidentPatient SET
                PatientID = @PatientID, MRN = @MRN, AdmissionNumber = @AdmissionNumber, PatientName = @PatientName, ArabicFullName = @ArabicFullName,
                MedicalRecordNumber = @MedicalRecordNumber, PatientDOB = @PatientDOB, PatientGender = @PatientGender, CheckInDate = @CheckInDate,
                MainInsuranceID = @MainInsuranceID, MainInsuranceDescription = @MainInsuranceDescription, InsuranceID = @InsuranceID, InsuranceDescription = @InsuranceDescription,
                ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
            WHERE ID = @ID";
        await conn.ExecuteAsync(new CommandDefinition(sql, dto, transaction, cancellationToken: ct));
    }

    /// <summary>Insert ResidentPatient.</summary>
    public async Task InsertResidentPatientAsync(ResidentPatientInsertDto dto, IDbConnection conn, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = @"
            INSERT INTO dbo.ResidentPatient 
            (PatientID, Admission, MRN, AdmissionNumber, PatientName, ArabicFullName, MedicalRecordNumber, PatientDOB, PatientGender, CheckInDate, IsDeleted, CreatedBy, CreatedDate,
             ReferralPhysicianID, ReferralPhysicianName, CheckInClassID, CheckInClassDescription, MainInsuranceID, MainInsuranceDescription, MainInsuranceClassID, MainInsuranceClassDescription,
             MedicationUnitID, MedicationUnitDescription, InsuranceID, InsuranceDescription, GuarantorID, GuarantorDescription, CurrencyID, CurrencyDescription, ClassID, ClassDescription,
             ContextPriceID, ContextPriceDescription, ContextEnumerationID, ContextEnumerationDescription, AdmissionType, AdmissionTypeDescription, IsDischarged, IsPharmDisch, IsNersingDischarge, IsRecheckIn)
            VALUES 
            (@PatientID, @Admission, @MRN, @AdmissionNumber, @PatientName, @ArabicFullName, @MedicalRecordNumber, @PatientDOB, @PatientGender, @CheckInDate, 0, @CreatedBy, GETDATE(),
             @ReferralPhysicianID, (SELECT name FROM HospitalDefinition.dbo.Physician WHERE Id = @ReferralPhysicianID), @CheckInClassID, @CheckInClassDescription,
             @MainInsuranceID, @MainInsuranceDescription, @MainInsuranceClassID, @MainInsuranceClassDescription,
             @MedicationUnitID, @MedicationUnitDescription, @InsuranceID, @InsuranceDescription, @GuarantorID, @GuarantorDescription,
             @CurrencyID, @CurrencyDescription, @ClassID, @ClassDescription, @ContextPriceID, @ContextPriceDescription,
             @ContextEnumerationID, @ContextEnumerationDescription, @AdmissionType, @AdmissionTypeDescription, @IsDischarged, @IsPharmDisch, @IsNersingDischarge, @IsRecheckIn)";
        await conn.ExecuteAsync(new CommandDefinition(sql, dto, transaction, cancellationToken: ct));
    }
}

/// <summary>DTO for Advance INSERT.</summary>
public record AdvanceInsertDto(
    int Admission, int AdvanceNumber, decimal AdvanceAmount, decimal DbMain, decimal DbLocal,
    decimal ReceiptAmount, decimal ReceiptMain, decimal ReceiptLocal, decimal Rate, int Currency,
    decimal ReceivedLBP, decimal ReceivedUSD, int InvoiceId, string InvoiceNumber);

/// <summary>DTO for DeliveryHeader INSERT.</summary>
public record DeliveryHeaderInsertDto(
    int Id, int Type, int TypeCounter, int PatientType, DateTime Date, int Patient, int Currency, int Warehouse, int Admission,
    decimal Gross, decimal Net, int CreatedBy);

/// <summary>DTO for DeliverItem INSERT.</summary>
public record DeliverItemInsertDto(
    int Id, int DeliverHeader, int Product, int Package, string Code, string AlternateDescription,
    decimal Qty, decimal UnitPrice, decimal Net, int CreatedBy);

/// <summary>DTO for ResidentPatient UPDATE.</summary>
public record ResidentPatientUpdateDto(
    int ID, int PatientID, int MRN, string AdmissionNumber, string PatientName, string? ArabicFullName, string MedicalRecordNumber,
    DateTime? PatientDOB, string PatientGender, DateTime CheckInDate, int MainInsuranceID, string MainInsuranceDescription,
    int InsuranceID, string InsuranceDescription, int ModifiedBy);

/// <summary>DTO for patient info used in ResidentPatient sync.</summary>
public record PatientInfoForResidentSyncDto(
    string? MedicalRecordNumber, string? FirstName, string? LastName, string? MiddleName,
    string? ArabicFullName, DateTime? DOB, string? Gender, string? Phone);

/// <summary>DTO for ResidentPatient INSERT.</summary>
public record ResidentPatientInsertDto(
    int PatientID, int Admission, int MRN, string AdmissionNumber, string PatientName, string? ArabicFullName, string MedicalRecordNumber,
    DateTime? PatientDOB, string PatientGender, DateTime CheckInDate, int CreatedBy, int ReferralPhysicianID,
    int CheckInClassID, string CheckInClassDescription, int MainInsuranceID, string MainInsuranceDescription,
    int MainInsuranceClassID, string MainInsuranceClassDescription, int MedicationUnitID, string MedicationUnitDescription,
    int InsuranceID, string InsuranceDescription, int GuarantorID, string GuarantorDescription,
    int CurrencyID, string CurrencyDescription, int ClassID, string ClassDescription,
    int ContextPriceID, string ContextPriceDescription, int ContextEnumerationID, string ContextEnumerationDescription,
    int AdmissionType, string AdmissionTypeDescription, int IsDischarged, int IsPharmDisch, int IsNersingDischarge, int IsRecheckIn);
