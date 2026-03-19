# Bacteriology Workflow - Germ Selection and Antibiotic Results

## Overview
This document explains how the bacteriology workflow works when a user selects a germ and needs to enter antibiotic sensitivity results.

## Database Structure

### Tables Involved
1. **PatientLabResultsHeader** - Main patient lab result header
2. **PatientLabBacteriologyHeader** - Bacteriology-specific header (one per patient)
3. **Germs** - List of available germs/organisms
4. **GermAntibiotic** - Junction table linking germs to their antibiotics
5. **Antibiotic** - List of all antibiotics
6. **PatientLabBacteriology** - Individual antibiotic test results for each germ

## Workflow Steps

### Step 1: User Selects a Germ
When a user selects a germ from the dropdown or autocomplete:
- Frontend captures the `germId` and `patientHeaderId`

### Step 2: Create Bacteriology Records
Call the API endpoint to create all necessary records:

**Endpoint:**
```http
POST http://localhost:5050/api/PatientLabBacteriology/createForGerm
```

**Request Body:**
```json
{
  "patientHeaderId": 123,              // Required: ID from PatientLabResultsHeader
  "germId": 31,                        // Required: Selected Germ ID
  "labTestId": 10,                     // Optional: The lab test ID
  "labTestDescription": "Culture",     // Optional: Test description
  "specimenType": "Urine",            // Optional: Type of specimen
  "collectionDate": "2025-10-15",     // Optional: When sample was collected
  "receptionDate": "2025-10-15",      // Optional: When sample was received
  "createdBy": 1,                     // Optional: User ID (defaults to 1)
  "colony": "Heavy growth"            // Optional: Colony description
}
```

### Step 3: Backend Processing
The backend (`PatientLabBacteriologyController.CreateForGerm`) performs these actions:

1. **Check if Bacteriology Header exists**
   - Queries `PatientLabBacteriologyHeader` for the given `patientHeaderId`
   - If not found, creates a new header record

2. **Get all antibiotics for the selected germ**
   - Queries `GermAntibiotic` table to find all antibiotics linked to the `germId`
   - Orders by `DisplayOrder` for proper display

3. **Create PatientLabBacteriology records**
   - For each antibiotic found:
     - Creates a new `PatientLabBacteriology` record
     - Links it to the `BacteriologyHeaderId`
     - Sets the `GermId` and `AntibioticId`
     - Initializes empty fields for user input (Sensitivity, Result)
     - Checks for duplicates (won't create if already exists)

4. **Save to database**
   - Saves all created records in one transaction
   - Returns the created records to the frontend

### Step 4: Display Results on Screen
The frontend receives the created records:

**Response Example:**
```json
{
  "message": "Bacteriology records created successfully",
  "headerId": 789,
  "detailsCreated": 15,
  "details": [
    {
      "id": 1001,
      "bacteriologyHeaderId": 789,
      "germId": 31,
      "germDescription": "E. coli",
      "antibioticId": 5,
      "antibioticDescription": "Amoxicillin",
      "sensitivity": null,          // User will fill this
      "result": null,               // User will fill this
      "colony": "Heavy growth",
      "displayOrder": 1
    },
    {
      "id": 1002,
      "bacteriologyHeaderId": 789,
      "germId": 31,
      "germDescription": "E. coli",
      "antibioticId": 12,
      "antibioticDescription": "Ciprofloxacin",
      "sensitivity": null,
      "result": null,
      "colony": "Heavy growth",
      "displayOrder": 2
    }
    // ... more records for each antibiotic
  ]
}
```

### Step 5: User Enters Results
Display the records in a grid/table where users can enter:
- **Sensitivity**: S (Sensitive), R (Resistant), I (Intermediate)
- **Result**: Numeric value if applicable
- **Comments**: Any additional notes

### Step 6: Save User Input
When the user clicks "Save", call the batch update endpoint:

**Endpoint:**
```http
PUT http://localhost:5050/api/PatientLabBacteriology/batchUpdate
```

**Request Body:**
```json
[
  {
    "id": 1001,
    "sensitivity": "S",
    "result": "10",
    "comments": null,
    "modifiedBy": 1
  },
  {
    "id": 1002,
    "sensitivity": "R",
    "result": null,
    "comments": "High resistance",
    "modifiedBy": 1
  }
  // ... more records
]
```

## API Endpoints Summary

### 1. Get Antibiotics for a Germ (Preview)
```http
GET http://localhost:5050/api/Bacteria/antibiotics/{germId}
```
Use this to show the user which antibiotics will be created before actually creating them.

### 2. Create Bacteriology Records
```http
POST http://localhost:5050/api/PatientLabBacteriology/createForGerm
```
Creates header (if needed) and all detail records for the selected germ.

### 3. Get Existing Bacteriology Data
```http
GET http://localhost:5050/api/PatientLabBacteriology/byPatientHeader/{patientHeaderId}
```
Retrieves existing bacteriology header and details for a patient.

### 4. Batch Update Results
```http
PUT http://localhost:5050/api/PatientLabBacteriology/batchUpdate
```
Updates sensitivity and results for multiple antibiotic records at once.

## Frontend Implementation Suggestions

### 1. Germ Selection Component
```typescript
onGermSelected(germId: number) {
  // Show loading indicator
  this.loading = true;
  
  // Call API to create bacteriology records
  this.http.post('/api/PatientLabBacteriology/createForGerm', {
    patientHeaderId: this.currentPatientHeaderId,
    germId: germId,
    labTestId: this.labTestId,
    labTestDescription: 'Culture and Sensitivity',
    specimenType: this.specimenType,
    createdBy: this.currentUserId
  }).subscribe({
    next: (response) => {
      // Display the created records in a grid
      this.bacteriologyRecords = response.details;
      this.showResultsGrid = true;
      this.loading = false;
    },
    error: (error) => {
      console.error('Error creating bacteriology records:', error);
      this.loading = false;
    }
  });
}
```

### 2. Results Grid
Create a table/grid to display:
- Antibiotic name (read-only)
- Sensitivity (dropdown: S/R/I)
- Result (input field)
- Comments (input field)

### 3. Save Results
```typescript
saveResults() {
  const updates = this.bacteriologyRecords.map(record => ({
    id: record.id,
    sensitivity: record.sensitivity,
    result: record.result,
    comments: record.comments,
    modifiedBy: this.currentUserId
  }));
  
  this.http.put('/api/PatientLabBacteriology/batchUpdate', updates)
    .subscribe({
      next: () => {
        this.showSuccessMessage('Results saved successfully');
      },
      error: (error) => {
        this.showErrorMessage('Error saving results');
      }
    });
}
```

## Key Features

✅ **Automatic Header Creation** - Creates `PatientLabBacteriologyHeader` if it doesn't exist
✅ **Bulk Record Creation** - Creates all antibiotic records at once
✅ **Duplicate Prevention** - Won't create duplicate germ+antibiotic combinations
✅ **Ordered Display** - Antibiotics are ordered by DisplayOrder
✅ **Batch Updates** - Efficiently updates multiple records at once
✅ **Error Handling** - Comprehensive error logging and user-friendly messages

## Database Relationships

```
PatientLabResultsHeader (1)
    ↓
PatientLabBacteriologyHeader (1)
    ↓
PatientLabBacteriology (many)
    ├── Germs (many-to-one)
    └── Antibiotic (many-to-one)

GermAntibiotic
    ├── Germs (many-to-one)
    └── Antibiotic (many-to-one)
```

## Example Data Flow

1. User views patient results for MRN 12345
2. User selects "Culture and Sensitivity" test
3. User selects germ "E. coli" (ID: 31)
4. System finds 15 antibiotics in `GermAntibiotic` for E. coli
5. System creates 1 header + 15 detail records
6. Screen displays 15 rows (one per antibiotic)
7. User fills in sensitivity values (S/R/I)
8. User clicks Save
9. System updates all 15 records
10. Results are saved and can be printed

## Notes

- All created records have `IsDeleted = false` by default
- Records are never hard-deleted, only soft-deleted
- The `DisplayOrder` field controls the order of antibiotics shown to the user
- Multiple germs can be added for the same patient (creates separate records for each)
























