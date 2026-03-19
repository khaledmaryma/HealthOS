# Quick Admission Stored Procedure Implementation

## Overview
This implementation changes the quick admission save process from multiple individual API calls to a single stored procedure that handles all operations atomically.

## Files Created/Modified

### 1. `sp_SaveQuickAdmission.sql`
- **Location**: `c:\d\LHH_Backup\sp_SaveQuickAdmission.sql`
- **Purpose**: SQL Server stored procedure that handles complete quick admission save
- **Parameters**:
  - `@existingPatientId INT = NULL` - ID of existing patient (NULL for new patients)
  - `@saveData NVARCHAR(MAX)` - JSON string containing patient, admission, and invoice data
  - `@saveOptions NVARCHAR(MAX)` - JSON string containing save options

### 2. `QuickAdmissionController.cs`
- **Location**: `c:\d\LHH_Backup\LIS.Api\Controllers\QuickAdmissionController.cs`
- **Purpose**: API controller that calls the stored procedure
- **Endpoint**: `POST /api/QuickAdmission/SaveComplete`

### 3. Modified Frontend (`quick-admission.component.ts`)
- **Location**: `c:\d\LHH_Backup\LIS.Web\src\app\quick-admission\quick-admission.component.ts`
- **Changes**:
  - Replaced individual API calls with single stored procedure call
  - Added `saveViaStoredProcedure()` method
  - Modified save methods to collect data into JSON format

## JSON Data Structure

### SaveData Parameter Structure:
```json
{
  "patient": {
    "FirstName": "John",
    "LastName": "Doe",
    "MiddleName": null,
    "Gender": "M",
    "Phone": "123456789",
    "ArabicFullName": "جون دو",
    "DOB": "1990-01-01T00:00:00.000Z",
    "MaritalStatus": null,
    "CreatedBy": 338
  },
  "admission": {
    "AdmissionSite": 1,
    "ReferralPhysician": 123,
    "AttendingPhysician": 123,
    "MainInsurance": 1,
    "MainInsuranceClass": 1,
    "Insured": 1,
    "AuxiliaryInsurance": null,
    "AuxiliaryInsuranceClass": null,
    "CheckInClass": 1,
    "Department": 456,
    "CheckInDate": "2024-01-01",
    "CheckOutDate": null,
    "Type": 1,
    "IsWorkAccident": false,
    "IsExtended": false,
    "Group": null,
    "CreatedBy": 338
  },
  "invoice": [
    {
      "denomination": 789,
      "denominationCode": "LAB001",
      "denominationDescription": "Blood Test",
      "quantity": 1,
      "unitPrice": 25.00,
      "netPrice": 25.00,
      "netUnitPrice": 25.00,
      "discount": 0,
      "lumpSum": 0,
      "operatingPhysician": null,
      "costCenter": 12,
      "profitCenter": 3
    }
  ]
}
```

### SaveOptions Parameter Structure:
```json
{
  "saveMedicalFile": true,
  "saveAdmission": true,
  "saveInvoice": true
}
```

## Database Operations

The stored procedure performs the following operations in a single transaction:

1. **Patient Creation** (if `saveMedicalFile = 1` and no existing patient):
   - Inserts into `HospitalDefinition.dbo.Patient`
   - Generates MRN

2. **Admission Creation** (if `saveAdmission = 1`):
   - Inserts into `Admission.dbo.Admission`
   - Generates admission number

3. **Invoice Creation** (if `saveInvoice = 1`):
   - Inserts into `Billing.dbo.InvoiceHeader`
   - Inserts invoice details into `Billing.dbo.InvoiceDetail`

## Return Values

The stored procedure returns a result set with:
```json
{
  "MRN": "20240001",
  "PatientID": 123,
  "AdmissionNumber": "ADM20240001",
  "AdmissionID": 456,
  "InvoiceHeaderID": 789,
  "Status": "Success",
  "ErrorMessage": ""
}
```

## Implementation Steps

### 1. Deploy the Stored Procedure
```sql
-- Execute the stored procedure creation script
USE [YourDatabase];
GO
-- Run the contents of sp_SaveQuickAdmission.sql
```

### 2. Update API Connection String
Ensure the API has access to all three databases:
- `HospitalDefinition`
- `Admission`
- `Billing`

### 3. Test the Implementation
1. Start the API server
2. Test the quick admission form
3. Verify data is saved correctly across all tables
4. Check transaction rollback on errors

## Benefits

1. **Atomic Operations**: All saves happen in a single transaction
2. **Better Performance**: Single database round-trip
3. **Data Consistency**: No partial saves
4. **Easier Error Handling**: Complete rollback on any failure
5. **Maintainable**: Business logic centralized in the database

## Error Handling

The stored procedure includes comprehensive error handling:
- Transaction rollback on any error
- Detailed error messages returned to the client
- Proper logging of error details

## Security Considerations

- Ensure proper database permissions for the API user
- Validate JSON input to prevent SQL injection
- Consider implementing request rate limiting
- Audit all database operations