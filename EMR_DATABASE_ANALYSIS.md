# EMR Database Analysis and Purpose

## Overview
The **EMR (Electronic Medical Record)** database is a comprehensive medical records system that stores patient clinical data, medical orders, vital signs, medications, interventions, and various clinical documentation. It serves as the central repository for electronic medical records in the hospital information system.

## Database Connection
- **Server**: BOOK-N38E1PL5F3
- **Database Name**: EMR
- **Connection String**: Configured in `appsettings.json` as `EMRConnection`

## Primary Purpose

The EMR database is used for:

1. **Medical Order Management** - Managing all types of medical orders (laboratory, imaging, operations, medications, etc.)
2. **Patient Clinical Documentation** - Storing patient history, vital signs, examinations, and clinical notes
3. **Unit of Measure Reference** - Providing standardized units of measurement used across the system (especially for lab tests)
4. **Clinical Data Storage** - Storing various clinical assessments, interventions, and medical procedures

## Key Tables and Their Purposes

### 1. Order Management Tables
- **OrderRequest** (51,328 rows) - Main table for all medical orders (laboratory, imaging, operations, medications, etc.)
  - Links to patients via `PatientID` and `AdmissionID`
  - Uses `RequestType` to categorize order types (laboratory, imaging, etc.)
  - Contains `CaseNumber` which links to billing/pre-invoice system
  - Key columns: `Id`, `PatientID`, `AdmissionID`, `CaseNumber`, `RequestType`, `Status`, `RequestDate`
  
- **OrderRequestDetail** (154,254 rows) - Detailed line items for each order request
- **OrderRequestDetailMedicationSettings** (43 rows) - Medication-specific settings
- **OrderRequestDetailMedicationTimeDetails** (58 rows) - Medication scheduling details

### 2. Unit of Measure (Used by LIS System)
- **UnitOfMeasure** (85 rows) - **CRITICAL TABLE** used by the LIS (Laboratory Information System)
  - Provides standardized units like: g/l, UI/l, mg/l, %, ng/l, etc.
  - Referenced in lab test results across the system
  - Used in cross-database queries from LIS database
  - Structure: `ID`, `GroupID`, `Code`, `Description`, `IsDeleted`
  - Accessed via API endpoint: `/api/UnitOfMeasures`

- **UnitOfMeasureGroup** (11 rows) - Groups for organizing units of measure

### 3. Patient Clinical Data
- **PatientVitalSign** (7,817 rows) - Patient vital signs measurements over time
- **PatientCurrentIllness** (67 rows) - Current illness documentation
- **PatientMedicationHistory** (92 rows) - Historical medication records
- **PatientMedicationSchedule** (58 rows) - Scheduled medications
- **PatientRiskFactor** (58 rows) - Patient risk factors
- **PatientHistoryTextHelper** (721 rows) - Text-based patient history
- **ProgressNotes** (230 rows) - Clinical progress notes
- **PatientAppointment** (36 rows) - Patient appointments

### 4. Clinical Examinations and Assessments
- **ClinicExam** (294 rows) - Clinical examination records
- **ChiefComplaints** (122 rows) - Patient chief complaints
- **Bilan** (26 rows) - Medical assessments/bilans
- **BilanDetail** (196 rows) - Detailed bilan information

### 5. Cardiac/Intervention Data
- **CardIntervention** (4 rows) - Cardiac interventions
- **CardInterventionDetail** (30 rows) - Detailed cardiac intervention data
- **Coronaire** (22 rows) - Coronary data
- **CoronaireDetail** (198 rows) - Detailed coronary information
- **CoronaireStat** (66 rows) - Coronary statistics
- **Pontage** (7 rows) - Bypass surgery data
- **Revasc** (3 rows) - Revascularization data

### 6. Operations and Procedures
- **Operation** (64 rows) - Surgical operations
- **OperationType** (10 rows) - Types of operations
- **InterventionSummary** (25 rows) - Summary of interventions
- **InterventionDetail** (23 rows) - Detailed intervention information
- **Anesthesia** (3 rows) - Anesthesia records

### 7. Imaging Data
- **ImagingSummary** (43 rows) - Summary of imaging studies
- **ImagingClasses** (6 rows) - Imaging classification
- **ImagingType** (2 rows) - Types of imaging
- **AdmImage** (3 rows) - Admission images
- **AdmImageDetail** (55 rows) - Detailed image information

### 8. Reference/Lookup Tables
- **RequestType** (9 rows) - Types of medical requests
- **BloodGroup** (8 rows) - Blood group classifications
- **Status** (4 rows) - Status codes
- **StatusOnDischarge** (5 rows) - Discharge statuses
- **Severity** (3 rows) - Severity levels
- **Confidentiality** (7 rows) - Confidentiality levels
- **PrescriptionType** (2 rows) - Prescription types
- **AdministrationMethod** (15 rows) - Medication administration methods

### 9. Scales and Assessments
- **Scale** (3 rows) - Assessment scales
- **ScaleDetails** (10 rows) - Scale details
- **ScaleType** (2 rows) - Types of scales
- **GlasgowScore** (15 rows) - Glasgow Coma Scale data
- **VitalSignType** (5 rows) - Types of vital signs
- **VitalSignDetail** (51 rows) - Vital sign details

### 10. Configuration and System Tables
- **ConfigurationTable** (1 row) - System configuration
- **FlatTableDefinition** (61 rows) - Table definitions for dynamic tables
- **ReportingTable** (7 rows) - Reporting table definitions

## Views (11 total)
- **AdmissionUrgence** - Urgency admission view
- **AvailableDenomination** - Available denominations view
- **CareInstruction** - Care instructions view
- **ModeDeTransfert** - Transfer mode view
- **OrderDetailList** - Order detail listing
- **Procedure** - Procedure view
- **SelectedImaging** - Selected imaging view
- **SelectedLabTest** - Selected lab test view
- **SelectedOperation** - Selected operation view
- **StatusEmotionnel** - Emotional status view
- **UrgenceTransfer** - Urgency transfer view

## Stored Procedures (69 total)

### Key Procedures for LIS Integration:
- **CanGenerateLaboratory** - Checks if laboratory orders can be generated
- **GetLabTests** - Retrieves laboratory test information
- **GetPatientLabTests** - Gets patient-specific lab tests
- **GetPatientLabTestDetails** - Detailed lab test information
- **SaveLaboratory** - Saves laboratory data
- **SaveLaboratoryRequest** - Saves laboratory order requests

### Other Important Procedures:
- **GetUnits** - Retrieves units of measure (used by LIS)
- **GetPatientVitalSign** - Gets patient vital signs
- **GetPatientMedications** - Retrieves patient medications
- **SaveOperationRequest** - Saves operation requests
- **SaveImagingRequest** - Saves imaging requests
- **SaveMedicationRequest** - Saves medication requests

## Integration with Other Systems

### 1. LIS (Laboratory Information System) Integration
The EMR database is **directly integrated** with the LIS system:

- **UnitOfMeasure Table**: The LIS system queries `EMR.dbo.UnitOfMeasure` via cross-database SQL queries
  - Used in: `UnitOfMeasuresController.cs` API endpoint
  - Referenced in: Lab test result displays, lab test creation scripts
  - Example query: `SELECT * FROM EMR.dbo.UnitOfMeasure`

- **OrderRequest Table**: Used to link laboratory orders from EMR to LIS
  - `CaseNumber` field links EMR orders to billing/pre-invoice system
  - `RequestType` field identifies laboratory requests
  - Referenced in stored procedures: `sp_definition.txt` shows joins like:
    ```sql
    left join EMR.dbo.OrderRequest PH on PH.CaseNumber = PHMC.CaseNumber 
    and PH.RequestType = @LaboratoryRequestType
    ```

### 2. Billing System Integration
- OrderRequest `CaseNumber` links to `Billing.dbo.PreInvoiceHeaderMedicalCase`
- Used to track which orders have been billed

### 3. Admission System Integration
- Links to `Admission.dbo.ResidentPatient` via `AdmissionID`
- Patient admission data is referenced in order requests

## Usage in Current Codebase

### API Controllers:
1. **UnitOfMeasuresController.cs**
   - Queries `EMR.dbo.UnitOfMeasure` table
   - Provides REST API endpoints for units of measure
   - Used by frontend for lab test unit selection

### SQL Scripts:
Multiple SQL scripts reference EMR database:
- `add_antibiogram_to_patient.sql` - Joins with `EMR.dbo.UnitOfMeasure`
- `preview_missing_lab_tests.sql` - References UnitOfMeasure
- `add_missing_lab_tests_simple.sql` - Uses UnitOfMeasure
- `copy_lab_tests_to_patient.sql` - Joins with UnitOfMeasure
- `check_unitofmeasure_structure.sql` - Analyzes UnitOfMeasure structure

### Stored Procedures:
- Main stored procedure in `sp_definition.txt` references:
  - `EMR.dbo.OrderRequest` - For laboratory order requests
  - `EMR.dbo.UnitOfMeasure` - For lab test units

## Database Statistics

- **Total Tables**: 153 tables
- **Total Views**: 11 views
- **Total Stored Procedures**: 69 procedures
- **Most Active Tables** (by row count):
  1. OrderRequestDetail: 154,254 rows
  2. OrderRequest: 51,328 rows
  3. PatientVitalSign: 7,817 rows
  4. CoronaireToTechnique: 726 rows
  5. PatientHistoryTextHelper: 721 rows

## Summary

The **EMR database** is the **Electronic Medical Record system** that:

1. **Manages all medical orders** (laboratory, imaging, operations, medications)
2. **Stores patient clinical data** (vital signs, examinations, history, medications)
3. **Provides reference data** (units of measure, blood groups, status codes)
4. **Tracks clinical interventions** (cardiac procedures, operations, imaging studies)
5. **Integrates with LIS** by providing unit of measure data and order request information
6. **Links to billing system** via CaseNumber for financial tracking

It is a **critical component** of the hospital information system, serving as the central repository for all patient clinical documentation and medical orders, while also providing essential reference data (like units of measure) that other systems like LIS depend on.



