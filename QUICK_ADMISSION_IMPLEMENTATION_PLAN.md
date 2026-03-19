# Quick Admission Feature - Implementation Plan

## Overview
Create a comprehensive page for quick patient admission that handles:
1. Patient creation/editing (HospitalDefinition.dbo.Patient)
2. Admission creation/editing (Admission.dbo.Admission)
3. Invoice generation (Billing.dbo.InvoiceHeader & InvoiceDetail)

## Database Tables Summary

### 1. HospitalDefinition.dbo.Patient (36 columns)
**Essential Fields:**
- ID (PK, Identity)
- MedicalRecordNumber (auto-generated)
- FirstName, LastName, MiddleName
- ArabicFullName
- DOB (Date of Birth)
- Gender (M/F)
- Phone
- Address
- IsDeleted, CreatedBy, CreatedDate

**Optional Fields:**
- MaritalStatus, Profession, Nationality
- IDNumber, BloodGroupID
- Contact information

### 2. Admission.dbo.Admission (53 columns)
**Essential Fields:**
- ID (PK, Identity)
- Number (admission number - auto-generated)
- Patient (FK to Patient)
- AdmissionSite
- ReferralPhysician
- AttendingPhysician
- CheckInDate
- Type (admission type)
- MainInsurance, MainInsuranceClass
- CheckInClass
- IsDeleted, CreatedBy, CreatedDate

**Optional Fields:**
- Companion, Period
- Insurance details
- Work accident info
- Height, Weight
- Diagnostic

### 3. Billing.dbo.InvoiceHeader (105 columns)
**Essential Fields:**
- ID (PK, Identity)
- Counter (invoice number - auto-generated)
- Type
- Date
- Admission (FK)
- Patient info (MRN, Name, AdmissionNumber)
- Insurance, Currency
- Net, Gross amounts
- IsDeleted, CreatedBy, CreatedDate

**Complex Fields:**
- Multiple amount calculations
- Coverage rates
- Complementary amounts
- Receipt information

### 4. Billing.dbo.InvoiceDetail (50+ columns)
**Essential Fields:**
- ID (PK, Identity)
- InvoiceHeader (FK)
- Admission, Patient
- Denomination (service/item)
- Quantity, UnitPrice, NetPrice
- CostCenter, ProfitCenter
- IsDeleted, CreatedBy, CreatedDate

## Implementation Phases

### Phase 1: Basic Patient Creation ✅
**Frontend:**
- Create QuickAdmissionComponent
- Patient information form (name, DOB, gender, phone)
- Basic validation

**Backend:**
- PatientController with Create/Update endpoints
- Handle MRN generation
- Validation logic

### Phase 2: Admission Management
**Frontend:**
- Admission form section
- Insurance selection
- Physician selection
- Admission type and class

**Backend:**
- AdmissionController with Create/Update endpoints
- Handle admission number generation
- Link patient to admission

### Phase 3: Invoice Generation
**Frontend:**
- Service/item selection (denominations)
- Quantity and pricing
- Add multiple line items
- Total calculation

**Backend:**
- InvoiceController with Create/Update endpoints
- Handle invoice number generation
- Create invoice header and details
- Price calculations

### Phase 4: Integration & Edit Mode
- Load existing patient/admission/invoice
- Update mode
- Validation across all forms
- Transaction management

## Simplified MVP Approach

Due to the massive complexity (190+ fields across 4 tables), we'll implement a simplified version:

### MVP Features:
1. **Patient Creation:**
   - FirstName, LastName, DOB, Gender, Phone
   - Auto-generate MRN
   - Basic validation

2. **Admission Creation:**
   - Link to patient
   - Basic admission info (site, date, type)
   - Select physician and insurance
   - Auto-generate admission number

3. **Basic Invoice:**
   - Create invoice header linked to admission
   - Add 1-3 basic service items
   - Calculate totals
   - Auto-generate invoice number

### MVP Excluded (Future Enhancement):
- Complex insurance calculations
- Work accident handling
- DRG processing
- Credit notes
- Receipt management
- Complementary amounts
- Advanced diagnostic groups
- Military information
- Approval workflows

## Technical Architecture

### Frontend (Angular):
```
/quick-admission
  ├── quick-admission.component.ts
  ├── quick-admission.component.html
  ├── quick-admission.component.scss
  └── models/
      ├── patient.model.ts
      ├── admission.model.ts
      └── invoice.model.ts
```

### Backend (ASP.NET Core):
```
Controllers/
  ├── QuickAdmissionController.cs  (orchestrates all operations)
  ├── PatientController.cs
  ├── AdmissionController.cs
  └── InvoiceController.cs

Models/
  ├── Patient.cs
  ├── Admission.cs
  ├── InvoiceHeader.cs
  └── InvoiceDetail.cs
```

### API Endpoints:
- `POST /api/QuickAdmission` - Create complete admission (patient + admission + invoice)
- `PUT /api/QuickAdmission/{id}` - Update existing admission
- `GET /api/QuickAdmission/{admissionId}` - Get admission details for editing

## Auto-Number Generation Logic

### Medical Record Number (MRN):
- Query: `SELECT MAX(CAST(MedicalRecordNumber AS INT)) FROM Patient WHERE IsDeleted = 0`
- Format: Increment by 1
- Example: If max is 20586, next is 20587

### Admission Number:
- Format: `{site}.{sequence}.{month}.{year}`
- Example: `03.01292.08.24` (Site 03, Sequence 01292, August 2024)
- Query: Get max sequence for current month/year

### Invoice Number:
- Query: `SELECT MAX(CAST(Counter AS INT)) FROM InvoiceHeader`
- Format: Increment by 1

## UI Design

### Layout:
1. **Header**: Title + Save/Cancel buttons
2. **Patient Section**: Collapsible card
   - Name fields
   - DOB, Gender
   - Contact info
3. **Admission Section**: Collapsible card
   - Admission date
   - Physician selection
   - Insurance selection
   - Type/Class dropdowns
4. **Invoice Section**: Collapsible card
   - Service items grid
   - Add/Remove buttons
   - Total calculation

### Validation Rules:
- Required fields: FirstName, LastName, DOB, Gender, CheckInDate
- DOB must be in the past
- Phone number format validation
- At least one invoice item required

## Data Flow

### Create Mode (New Admission):
1. User enters patient info
2. System generates MRN
3. User enters admission details
4. System generates admission number
5. User adds invoice items
6. System calculates totals
7. Save button: Transactional save of all 3 entities
8. Success: Navigate back to resident list

### Edit Mode:
1. Load admission ID from route
2. Fetch patient, admission, invoice data
3. Populate forms
4. User makes changes
5. Save button: Update all modified entities
6. Success: Navigate back to resident list

## Next Steps

1. Create simplified models for MVP
2. Implement backend controllers with essential fields
3. Create frontend component with basic forms
4. Add validation
5. Implement save logic
6. Test create flow
7. Implement edit/load logic
8. Test update flow
9. Add error handling
10. UI polish

## Future Enhancements
- Advanced insurance calculations
- Multiple insurance support
- Approval workflows
- Receipt generation
- Credit note handling
- Advanced diagnostic codes
- Work accident module
- Reporting integration




















