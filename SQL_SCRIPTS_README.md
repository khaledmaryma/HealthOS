# SQL Scripts for Managing Patient Lab Tests

This folder contains SQL scripts to help manage lab tests for patients, specifically to find and add lab tests that exist for other patients but are missing for a specific patient.

## Files Overview

### 1. `preview_missing_lab_tests.sql` ⭐ **Start Here**
**Purpose**: Shows which lab tests are missing for a patient WITHOUT making any changes.

**Use this to**:
- Preview what lab tests would be added
- See statistics and breakdown by medical class
- Verify before making any changes

**How to use**:
1. Open the file
2. Change `@TargetPatientHeaderID` to your patient's ID
3. Run the script
4. Review the results

**Example**:
```sql
DECLARE @TargetPatientHeaderID INT = 108712;  -- Change this
```

### 2. `add_missing_lab_tests_simple.sql` ⚠️ **Makes Changes**
**Purpose**: Automatically adds ALL missing lab tests to a patient.

**Use this to**:
- Quickly add all missing lab tests in one go
- Copy lab test definitions from the master LabTest table
- Initialize a patient's lab test panel

**⚠️ WARNING**: This script makes actual changes to the database!

**How to use**:
1. First run `preview_missing_lab_tests.sql` to see what will be added
2. Open `add_missing_lab_tests_simple.sql`
3. Change these variables:
   ```sql
   DECLARE @TargetPatientHeaderID INT = 108712;  -- Your patient ID
   DECLARE @CreatedByUserID INT = 1;              -- Your user ID
   ```
4. Review and uncomment the script if needed
5. Run the script
6. Check the summary output

**What it does**:
- Loops through all missing lab tests
- Creates a new `PatientLabResult` record for each
- Copies default values from the `LabTest` master table
- Shows a summary of what was added

### 3. `copy_lab_tests_to_patient.sql` 🔧 **Advanced/Customizable**
**Purpose**: A template for custom processing with multiple options.

**Use this to**:
- Customize exactly how lab tests are added
- Choose from different options (see below)
- Process lab tests with custom logic

**Options included**:

#### Option 1: Display Only (Default)
Just shows the lab test IDs and descriptions without making changes.

#### Option 2: Insert from LabTest Master Table
Uncomment the block to insert lab tests from the LabTest definition table.
```sql
-- Look for this comment in the script:
-- OPTION 2: Insert a new lab result record for this patient
```

#### Option 3: Copy from Template Patient
Uncomment to copy lab tests from another patient who has a good setup.
```sql
-- OPTION 3: Copy from a template patient
DECLARE @TemplatePatientHeaderID INT = 12345; -- Change to template patient
```

**How to use**:
1. Open the file
2. Change `@PatientHeaderID` to your patient
3. Choose which option to use
4. Uncomment the code for that option
5. Run the script

## Decision Guide

### When to use each script:

```
┌─────────────────────────────────────┐
│ What do you want to do?            │
└─────────────────────────────────────┘
                │
                ├─ Just want to see what's missing?
                │  → Use: preview_missing_lab_tests.sql
                │
                ├─ Add ALL missing tests quickly?
                │  → Use: add_missing_lab_tests_simple.sql
                │
                └─ Need custom processing?
                   → Use: copy_lab_tests_to_patient.sql
```

## Common Scenarios

### Scenario 1: New Patient Setup
**Goal**: Add all standard lab tests to a new patient.

**Steps**:
1. Run `preview_missing_lab_tests.sql` (Patient ID = NEW_PATIENT)
2. Review the list
3. Run `add_missing_lab_tests_simple.sql` (Patient ID = NEW_PATIENT)
4. Verify in the application

### Scenario 2: Matching Another Patient
**Goal**: Give Patient A the same lab tests as Patient B.

**Steps**:
1. Run `preview_missing_lab_tests.sql` (Patient ID = PATIENT_A)
2. Review what Patient A is missing
3. Run `copy_lab_tests_to_patient.sql` with Option 3
4. Set `@TemplatePatientHeaderID` to PATIENT_B's ID
5. Uncomment Option 3 code
6. Run the script

### Scenario 3: Selective Addition
**Goal**: Only add certain medical classes or specific tests.

**Steps**:
1. Run `preview_missing_lab_tests.sql`
2. Note the test IDs you want to add
3. Modify `copy_lab_tests_to_patient.sql`
4. Add a WHERE clause to filter:
   ```sql
   WHERE lt.ID = @LabTestID
     AND lt.IsDeleted = 0
     AND lt.MedicalClass = 'DESIRED_CLASS'  -- Add this line
   ```
5. Run the script

## Safety Tips

1. **Always Preview First**: Run `preview_missing_lab_tests.sql` before making changes
2. **Backup**: Consider backing up the `PatientLabResult` table before bulk inserts
3. **Test on One Patient**: Test on a single patient first before processing multiple
4. **Transaction**: The scripts use transactions, so they can be rolled back if needed
5. **Check Results**: After running, query the patient's results to verify

## Troubleshooting

### "No lab tests found"
- The patient already has all available lab tests
- Or there are no lab tests in the master LabTest table
- Run the preview script to confirm

### "Error adding LabTestID"
- Check if the LabTest exists in the master table
- Verify foreign key constraints
- Check for required fields that might be NULL

### "Duplicate key error"
- The lab test already exists for this patient
- Check if `IsDeleted = 1` for existing records
- You may need to update instead of insert

## Example Output

### Preview Script Output:
```
Preview of lab tests missing for Patient Header ID: 108712
==================================================================================

LabTestID | LabTestDescription | MedicalClass  | ResultType
----------|-------------------|---------------|------------
125       | Hemoglobin        | HEMATOLOGY    | Numeric
126       | WBC Count         | HEMATOLOGY    | Numeric
...

Summary:
==================================================================================
TotalMissingLabTests: 45
NumericTests: 40
TextTests: 5
```

### Add Script Output:
```
Adding missing lab tests to Patient Header ID: 108712
================================================================
Added LabTestID 125
Added LabTestID 126
...
================================================================
Summary:
  Successfully added: 45 lab tests
  Errors: 0
================================================================
```

## Script Versions

- Version: 1.0
- Last Updated: 2025-01-09
- Compatible with: SQL Server 2016+

## Notes

- All scripts use cursors for maximum control and logging
- Scripts include error handling with TRY/CATCH blocks
- Progress is printed as the script runs
- Scripts use transactions for data integrity


