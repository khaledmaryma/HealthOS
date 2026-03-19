# ResultType Database Relation Changes

## Summary
Changed ResultType from a hardcoded picklist to a database relation. The ResultType table should already exist in the LIS database as mentioned by the user.

## Backend Changes Made

### 1. Created ResultType Model (`LIS.Api/Models/ResultType.cs`)
- Maps to the existing `ResultType` table in the LIS database
- Properties: ID, Description, IsActive
- Includes navigation property to LabTests

### 2. Updated LabTest Model (`LIS.Api/Models/LabTest.cs`)
- Removed the `ResultTypeEnum` enum
- Added foreign key relationship to ResultType table
- Added navigation property `ResultTypeNavigation`
- The `ResultType` field remains as an integer foreign key

### 3. Updated DbContext (`LIS.Api/Data/LISDbContext.cs`)
- Added `DbSet<ResultType>` for ResultTypes table
- Configured the relationship between LabTest and ResultType
- Set delete behavior to Restrict to prevent cascading deletes

### 4. Created ResultTypesController (`LIS.Api/Controllers/ResultTypesController.cs`)
- New API endpoint: `GET /api/resulttypes` - Returns all active result types
- New API endpoint: `GET /api/resulttypes/{id}` - Returns a specific result type by ID
- Filters to show only active result types (IsActive = true)

### 5. Updated LabTestsController (`LIS.Api/Controllers/LabTestsController.cs`)
- Removed default value assignment for ResultType in the Create method
- ResultType must now be provided by the client

## Frontend Changes Made

### 1. Updated Service (`LIS.Web/src/app/services/lab-tests.service.ts`)
- Added `ResultType` interface matching the backend model
- Added `resultTypes` signal to store result types
- Added `loadResultTypes()` method to fetch result types from API
- Changed base URL to `labTestsUrl` for lab tests
- Added `resultTypesUrl` for result types API

### 2. Updated Component (`LIS.Web/src/app/lab-tests/lab-tests.component.ts`)
- Removed reference to non-existent `RESULT_TYPE_OPTIONS` constant
- Added `resultTypes` from service
- Updated `ngOnInit()` to load result types on initialization
- Modified `getResultTypeLabel()` to fetch labels from database result types

### 3. Updated Template (`LIS.Web/src/app/lab-tests/lab-tests.component.html`)
- Changed Result Type dropdown to use `resultTypes()` from database
- Dropdown now displays: `*ngFor="let rt of resultTypes()" [ngValue]="rt.id"`

## Required Actions

### 1. Stop the Running API
The API is currently running and needs to be stopped before creating the migration:
```bash
# Find and stop the process or press Ctrl+C in the terminal where it's running
```

### 2. Create and Apply Migration
After stopping the API, run:
```bash
cd LIS.Api
dotnet ef migrations add AddResultTypeRelationship
dotnet ef database update
```

**Note:** If the ResultType table already exists in the database, the migration will only add the foreign key relationship and not create the table itself.

### 3. Verify ResultType Table Data
Ensure the ResultType table in the LIS database has the necessary data:
```sql
-- Check if ResultType table exists and has data
SELECT * FROM ResultType;

-- Expected structure:
-- ID (int, primary key)
-- Description (nvarchar(50))
-- IsActive (bit)

-- Example data should include:
-- ID=1, Description='Numeric', IsActive=1
-- ID=2, Description='Text', IsActive=1
```

### 4. Update Existing LabTest Records (if needed)
If there are existing LabTest records, ensure they have valid ResultType foreign keys:
```sql
-- Check for any invalid ResultType references
SELECT lt.ID, lt.Code, lt.ResultType 
FROM LabTest lt
LEFT JOIN ResultType rt ON lt.ResultType = rt.ID
WHERE rt.ID IS NULL;

-- If any records have invalid references, update them:
-- UPDATE LabTest SET ResultType = 2 WHERE ResultType NOT IN (SELECT ID FROM ResultType);
```

### 5. Restart the API
```bash
cd LIS.Api
dotnet run
```

### 6. Start the Frontend
```bash
cd LIS.Web
npm start
```

## Testing Checklist

- [ ] API starts without errors
- [ ] GET /api/resulttypes returns the list of result types from database
- [ ] GET /api/labtests returns lab tests with valid ResultType IDs
- [ ] Frontend loads and displays result types in the dropdown
- [ ] Creating a new lab test with a result type works
- [ ] Editing an existing lab test preserves the result type
- [ ] Result type labels display correctly in the grid

## Database Schema

### ResultType Table (Existing)
```sql
CREATE TABLE [dbo].[ResultType] (
    [ID] INT NOT NULL PRIMARY KEY,
    [Description] NVARCHAR(50) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
);
```

### LabTest Table (Updated)
```sql
-- Foreign key constraint added:
ALTER TABLE [dbo].[LabTest]
ADD CONSTRAINT [FK_LabTest_ResultType_ResultType] 
FOREIGN KEY ([ResultType]) REFERENCES [dbo].[ResultType] ([ID]);
```

## Rollback Plan

If you need to rollback these changes:

1. Backend:
   - Remove the migration: `dotnet ef migrations remove`
   - Revert changes to LabTest.cs, LISDbContext.cs, LabTestsController.cs
   - Delete ResultType.cs and ResultTypesController.cs

2. Frontend:
   - Revert changes to lab-tests.service.ts, lab-tests.component.ts, and lab-tests.component.html
   - Add back the hardcoded RESULT_TYPE_OPTIONS constant

## Notes

- The ResultType field in LabTest remains an integer (foreign key)
- The navigation property is marked with [JsonIgnore] to prevent circular references
- The frontend uses the ID for the foreign key relationship
- Result types are loaded once on component initialization
- Only active result types (IsActive = true) are displayed in the dropdown

