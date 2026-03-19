# Denomination Feature Implementation

## Overview
Added denomination autocomplete field to the Lab Test definition form, allowing users to select a denomination when creating or editing lab tests.

## Changes Made

### 1. Backend API

#### New Model
**File**: `LIS.Api/Models/Denomination.cs`

Created Denomination model with fields:
- `Id` - Primary key
- `Description` - Denomination name
- `Code` - Optional code
- `DisplayOrder` - Display order
- Standard audit fields (CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, IsDeleted)

#### New Controller
**File**: `LIS.Api/Controllers/DenominationController.cs`

Added endpoints:
- `GET /api/denomination` - Get all denominations (ordered by DisplayOrder)
- `GET /api/denomination/search?query={query}` - Search denominations by description
- `GET /api/denomination/{id}` - Get specific denomination by ID

#### Updated DbContext
**File**: `LIS.Api/Data/LISDbContext.cs`

Added:
```csharp
public DbSet<LIS.Api.Models.Denomination> Denominations { get; set; } = null!;
```

### 2. Frontend Service

#### Updated Interface
**File**: `LIS.Web/src/app/services/lab-tests.service.ts`

**Added Denomination Interface**:
```typescript
export interface Denomination {
  id: number;
  description: string;
  code?: string | null;
  displayOrder?: number | null;
}
```

**Updated LabTest Interface**:
Added field: `denomination?: number | null;`

**New Service Methods**:
- `loadDenominations()` - Loads all denominations into signal
- `searchDenominations(query: string)` - Searches denominations

**New Signal**:
- `denominations = signal<Denomination[]>([]);`

### 3. Frontend Component

#### Component Logic
**File**: `LIS.Web/src/app/lab-tests/lab-tests.component.ts`

**Added**:
- `readonly denominations = this.svc.denominations;` - Exposes denominations to template
- Updated `ngOnInit()` to call `this.svc.loadDenominations();`

#### UI Changes
**File**: `LIS.Web/src/app/lab-tests/lab-tests.component.html`

**Added Denomination Field** (after Code, before Description):
```html
<div>
  <label class="form-label">Denomination</label>
  <select [(ngModel)]="editItem()!.denomination" name="denomination" class="form-select">
    <option [ngValue]="null">-- Select Denomination --</option>
    <option *ngFor="let denom of denominations()" [ngValue]="denom.id">
      {{ denom.description }}
    </option>
  </select>
</div>
```

## Features

### Load Operation
- Denominations are loaded when the Lab Tests component initializes
- All non-deleted denominations are fetched from the database
- Sorted by DisplayOrder, then Description
- Cached in a signal for reactive updates

### Save Operation
- Denomination value is automatically included when saving lab tests
- Saved as an integer ID reference to the Denomination table
- Can be null (optional field)

### User Interface
- **Dropdown select** field (not autocomplete, for simplicity)
- Shows all available denominations
- Option to select "none" (null value)
- Located in the form between Code and Description fields

## Database Integration

### LabTest Table (LIS Database)
The `Denomination` column already exists in the LabTest table:
- Column: `Denomination`
- Type: `int` (nullable)
- References Denomination table ID

### Denomination Table (HospitalDefinition Database)
**Location**: `HospitalDefinition.dbo.Denomination`

**Filters Applied**:
- `IsDeleted = 0` (only active denominations)
- `CostCenter = 1` (only cost center 1 denominations)

**Structure**:
- `ID` - Primary key
- `Description` - Denomination name/description
- `Code` - Optional code
- `DisplayOrder` - Sort order
- `CostCenter` - Cost center filter
- Audit fields (CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, IsDeleted)

**Cross-Database Query**:
The API uses `FromSqlRaw` to query across databases, similar to:
- UnitOfMeasure (from EMR database)
- ResidentPatient (from Admission database)
- HospitalConfiguration (from Configuration database)

## API Endpoints

### Get All Denominations
```http
GET http://localhost:5050/api/denomination
```

### Search Denominations
```http
GET http://localhost:5050/api/denomination/search?query=blood
```

### Get Denomination by ID
```http
GET http://localhost:5050/api/denomination/123
```

## Testing

### To Test the Feature:
1. Open http://localhost:4200
2. Navigate to Lab Tests page
3. Click "Add New" or edit existing test
4. See the "Denomination" dropdown field
5. Select a denomination from the list
6. Save the lab test
7. Verify denomination is saved and loads correctly when editing

## Files Created/Modified

### Created:
1. `LIS.Api/Models/Denomination.cs` - Model
2. `LIS.Api/Controllers/DenominationController.cs` - API controller
3. `check_denomination_table.sql` - SQL script to verify table structure

### Modified:
1. `LIS.Api/Data/LISDbContext.cs` - Added Denominations DbSet
2. `LIS.Web/src/app/services/lab-tests.service.ts` - Added Denomination interface and methods
3. `LIS.Web/src/app/lab-tests/lab-tests.component.ts` - Added denomination loading
4. `LIS.Web/src/app/lab-tests/lab-tests.component.html` - Added denomination dropdown

## Services Status
- ✅ **API**: http://localhost:5050 (PID: 35764) - Updated with Denomination support
- ✅ **Web**: http://localhost:4200 (PID: 20900) - Auto-reloaded with new field

## Benefits
- ✅ Proper categorization of lab tests by denomination
- ✅ Easy selection from dropdown
- ✅ Data integrity through foreign key reference
- ✅ Automatic loading and caching
- ✅ Included in all CRUD operations

## Future Enhancements (Optional)
- Convert dropdown to autocomplete search for large denomination lists
- Add denomination description in lab test list view
- Filter lab tests by denomination
- Add denomination management CRUD interface

