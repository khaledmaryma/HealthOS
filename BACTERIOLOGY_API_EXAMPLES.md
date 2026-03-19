# Bacteriology API - Quick Examples

## Complete Workflow: From Germ Selection to Results Entry

### Scenario
Patient MRN 12345 has a urine culture test. The lab technician identifies E. coli (Germ ID: 31) and needs to enter antibiotic sensitivity results.

---

## Step 1: Select a Germ

When the user selects "E. coli" from the dropdown, immediately call:

### API Call
```http
POST http://localhost:5050/api/PatientLabBacteriology/createForGerm
Content-Type: application/json

{
  "patientHeaderId": 123,
  "germId": 31,
  "labTestId": 10,
  "labTestDescription": "Urine Culture and Sensitivity",
  "specimenType": "Urine",
  "collectionDate": "2025-10-15T08:00:00",
  "receptionDate": "2025-10-15T09:00:00",
  "createdBy": 1,
  "colony": "Heavy growth"
}
```

### What Happens Automatically

1. ✅ **Checks** if `PatientLabBacteriologyHeader` exists for patient 123
2. ✅ **Creates** new header if it doesn't exist
3. ✅ **Queries** `GermAntibiotic` table for all antibiotics linked to E. coli (Germ ID: 31)
4. ✅ **Creates** a `PatientLabBacteriology` record for **EACH** antibiotic found
5. ✅ **Returns** all created records ready for display

### Response Example
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
      "sensitivity": null,
      "result": null,
      "colony": "Heavy growth",
      "displayOrder": 1,
      "isDeleted": false,
      "createdDate": "2025-10-15T10:00:00"
    },
    {
      "id": 1002,
      "bacteriologyHeaderId": 789,
      "germId": 31,
      "germDescription": "E. coli",
      "antibioticId": 8,
      "antibioticDescription": "Ampicillin",
      "sensitivity": null,
      "result": null,
      "colony": "Heavy growth",
      "displayOrder": 2,
      "isDeleted": false,
      "createdDate": "2025-10-15T10:00:00"
    },
    {
      "id": 1003,
      "bacteriologyHeaderId": 789,
      "germId": 31,
      "germDescription": "E. coli",
      "antibioticId": 12,
      "antibioticDescription": "Ciprofloxacin",
      "sensitivity": null,
      "result": null,
      "colony": "Heavy growth",
      "displayOrder": 3,
      "isDeleted": false,
      "createdDate": "2025-10-15T10:00:00"
    }
    // ... 12 more antibiotics
  ]
}
```

---

## Step 2: Display Records on Screen

Display all 15 records in a table/grid where the user can enter:

| Antibiotic | Sensitivity | Result | Comments |
|------------|-------------|--------|----------|
| Amoxicillin | [Dropdown: S/R/I] | [Input] | [Input] |
| Ampicillin | [Dropdown: S/R/I] | [Input] | [Input] |
| Ciprofloxacin | [Dropdown: S/R/I] | [Input] | [Input] |
| ... | ... | ... | ... |

---

## Step 3: User Enters Results

User fills in the form:
- Amoxicillin: **S** (Sensitive)
- Ampicillin: **R** (Resistant)
- Ciprofloxacin: **S** (Sensitive)
- etc.

---

## Step 4: Save Results

When user clicks "Save", call:

### API Call
```http
PUT http://localhost:5050/api/PatientLabBacteriology/batchUpdate
Content-Type: application/json

[
  {
    "id": 1001,
    "sensitivity": "S",
    "result": "10",
    "colony": "Heavy growth",
    "comments": null,
    "modifiedBy": 1
  },
  {
    "id": 1002,
    "sensitivity": "R",
    "result": null,
    "colony": "Heavy growth",
    "comments": "High resistance noted",
    "modifiedBy": 1
  },
  {
    "id": 1003,
    "sensitivity": "S",
    "result": "12",
    "colony": "Heavy growth",
    "comments": null,
    "modifiedBy": 1
  }
  // ... remaining 12 records
]
```

### Response
```json
{
  "message": "Bacteriology details updated successfully",
  "count": 15
}
```

---

## Additional Endpoints

### Preview Antibiotics Before Creating
To show the user how many antibiotics will be created:

```http
GET http://localhost:5050/api/Bacteria/antibiotics/31
```

**Response:**
```json
[
  {
    "id": 5,
    "description": "Amoxicillin",
    "displayOrder": 1
  },
  {
    "id": 8,
    "description": "Ampicillin",
    "displayOrder": 2
  }
  // ... more antibiotics
]
```

### Retrieve Existing Records
To load existing bacteriology data for a patient:

```http
GET http://localhost:5050/api/PatientLabBacteriology/byPatientHeader/123
```

**Response:**
```json
{
  "header": {
    "id": 789,
    "patientHeaderId": 123,
    "labTestDescription": "Urine Culture and Sensitivity",
    "specimenType": "Urine",
    "collectionDate": "2025-10-15T08:00:00",
    // ... more header fields
  },
  "details": [
    // ... all PatientLabBacteriology records
  ]
}
```

---

## Key Points

### ✅ One API Call Creates Everything
When a germ is selected, ONE API call (`createForGerm`) automatically:
- Creates the header (if needed)
- Fetches all antibiotics from `GermAntibiotic`
- Creates all `PatientLabBacteriology` records
- Returns everything ready to display

### ✅ No Manual Iteration Needed
You don't need to:
- Loop through antibiotics in the frontend
- Make multiple API calls
- Manually create each record

The backend does it ALL automatically!

### ✅ Batch Updates
Save all results with one API call - efficient and fast.

### ✅ Duplicate Prevention
If you select the same germ again, it won't create duplicate records.

---

## Frontend Pseudocode

```typescript
// When germ is selected
onGermSelected(germId: number) {
  this.bacteriologyService
    .createForGerm(this.patientHeaderId, germId, this.labTestInfo)
    .subscribe(response => {
      // Automatically get all antibiotic records
      this.antibioticRecords = response.details;
      
      // Show the grid for user input
      this.showResultsGrid = true;
    });
}

// When user clicks Save
saveResults() {
  // Collect all modified records
  const updates = this.antibioticRecords.map(record => ({
    id: record.id,
    sensitivity: record.sensitivity,
    result: record.result,
    comments: record.comments,
    modifiedBy: this.currentUserId
  }));
  
  // Save all at once
  this.bacteriologyService
    .batchUpdate(updates)
    .subscribe(() => {
      this.showSuccess('Results saved successfully');
    });
}
```

---

## Summary

| Action | Endpoint | Method | Purpose |
|--------|----------|--------|---------|
| Select Germ | `/api/PatientLabBacteriology/createForGerm` | POST | Creates header + all antibiotic records |
| Preview Antibiotics | `/api/Bacteria/antibiotics/{germId}` | GET | See what antibiotics will be created |
| Load Existing Data | `/api/PatientLabBacteriology/byPatientHeader/{id}` | GET | Retrieve existing results |
| Save Results | `/api/PatientLabBacteriology/batchUpdate` | PUT | Update all antibiotic results |

**The workflow is fully automatic - just select a germ and get all antibiotic records created instantly!** 🎉
























