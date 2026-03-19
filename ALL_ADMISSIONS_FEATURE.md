# All Admissions Feature Implementation

## Overview
Added functionality to view all lab results for a patient across all their admissions based on their MRN (Medical Record Number). Old results from previous admissions are displayed as read-only.

## Changes Made

### 1. Backend API Changes

#### New API Endpoint
**File**: `LIS.Api/Controllers/PatientLabResultsController.cs`

Added new endpoint:
```
GET /api/PatientLabResults/byMRN/{mrn}?currentAdmission={admissionNumber}
```

**Features**:
- Retrieves all lab results for a patient across all admissions using their MRN
- Includes `admissionNumber`, `requestDate`, and `isCurrentAdmission` flag in response
- Orders results by admission number (descending), medical class, and display order
- Marks results as current or historical based on the `currentAdmission` parameter

### 2. Frontend Service Changes

#### Updated Service
**File**: `LIS.Web/src/app/services/patient-results.service.ts`

**Added Fields to `PatientLabResult` interface**:
- `admissionNumber?: string` - The admission number for this result
- `requestDate?: string | null` - When the test was requested
- `isCurrentAdmission?: boolean` - Flag indicating if this is from the current admission

**New Method**:
```typescript
getByMRN(mrn: number, currentAdmission?: string): Observable<PatientLabResult[]>
```

### 3. Frontend Component Changes

#### Component Logic
**File**: `LIS.Web/src/app/patient-results/patient-results.component.ts`

**New Signal**:
```typescript
readonly showAllAdmissions = signal(false);
```

**New Methods**:
- `toggleShowAllAdmissions()` - Toggles the checkbox and reloads results
- `isResultReadOnly(result)` - Returns true if result is from a previous admission

**Updated Method**:
- `loadPatientResults()` - Now checks `showAllAdmissions()` flag and calls appropriate API

#### UI Changes
**File**: `LIS.Web/src/app/patient-results/patient-results.component.html`

**Added Checkbox**:
```html
<div class="form-check mt-2">
  <input class="form-check-input" type="checkbox" 
         [checked]="showAllAdmissions()" 
         (change)="toggleShowAllAdmissions()" 
         id="showAllAdmissionsCheck">
  <label class="form-check-label" for="showAllAdmissionsCheck">
    <small>Show All Admissions</small>
  </label>
</div>
```

**Read-Only Styling**:
- Input fields for old results have `[readonly]="isResultReadOnly(result)"`
- Gray background (`bg-light` class) for read-only fields
- Tooltip showing admission number when hovering over read-only fields

**Admission Badges**:
- Green badge for current admission results
- Gray badge for historical admission results
- Tooltip with admission number and request date
- Displayed next to test name in all result sections (Numeric, Text, Antibiogram)

**Disabled Save Button**:
- Save button is disabled when `showAllAdmissions()` is true
- Prevents accidentally trying to save read-only historical data

## User Experience

### When Checkbox is Unchecked (Default)
- Shows only results for the current admission
- All fields are editable
- Save button is enabled
- Normal workflow with collapsible sections

### When Checkbox is Checked
- Shows results from ALL admissions for the patient (by MRN)
- **Each admission is displayed in its own collapsible card/section** with:
  - **Admission Header**: Shows admission number, date, status badge, and result count
  - **Collapsible**: Click header to expand/collapse admission results
  - **Chevron indicator**: Shows ▶ (collapsed) or ▼ (expanded)
  - Color-coded border: 
    - 🟢 **Green border** = Current admission
    - ⚫ **Gray border** = Historical admission
  - Header background tint matches the border color
  - "Read Only" indicator for historical admissions
  - Request date displayed in the header
  - Total result count badge (e.g., "8 results")
- **Results grouped by admission**:
  - Current admission appears first (expanded by default)
  - Historical admissions follow in reverse chronological order
  - Each admission shows its own numeric, text, and antibiogram results
  - Can collapse any admission to save screen space
- Save button is **disabled** (since viewing historical data)
- Simplified read-only display for historical results

## Visual Indicators

### Admission Cards
Each admission is displayed in a collapsible bordered card with:
- **Collapsible Header** (click to expand/collapse):
  - **Chevron icon**: ▶ when collapsed, ▼ when expanded
  - **4px left border**:
    - 🟢 Green = Current admission
    - ⚫ Gray = Historical admission
  - **Header background**: Tinted to match border color (10% opacity)
  - **Status badge**: "Current" (green) or "Historical" (gray)
  - **Result count badge**: Shows total number of results (e.g., "8 results")
  - **Lock icon**: Shown on historical admissions with "Read Only" text
  - **Request date**: Displayed with calendar icon
- **Hover effect**: Cursor changes to pointer on header to indicate clickability

### Result Display Within Each Admission

1. **Numeric Results**: Table view with expand functionality
   - **Expandable rows**: Click ▶ to expand tests with sub-tests
   - Columns: Expand | Test Name | Result | Unit | Reference Range | Status
   - Tests with sub-tests show "See below" and can be expanded
   - Sub-tests appear indented with arrow indicator (→)
   - Status badges: Panic (red), Abnormal (yellow), Normal (green)
   - All expand/collapse functionality preserved

2. **Text Results**: List group display
   - Test name as header
   - Rich text content displayed (read-only)

3. **Antibiogram Results**: Collapsible cards
   - **Each antibiogram is a separate collapsible card**
   - Click header to expand/collapse
   - Shows specimen type in header
   - Expanded view shows:
     - Organism (Germ) and Bacteria information
     - Antibiotic sensitivity table with color-coded columns
     - Checkmarks for Sensitive/Resistant/Intermediate
     - Charge and Diameter values

### Save Button
- Disabled when showing all admissions
- Prevents accidental modification attempts
- Users must uncheck "Show All Admissions" to edit current results

## Technical Details

### Database Field Used
- `PatientLabResultsHeader.MRN` - Medical Record Number linking all admissions for a patient

### API Response Enhancement
Results now include:
```json
{
  "admissionNumber": "03.01254.08.24",
  "requestDate": "2024-08-15T10:30:00",
  "isCurrentAdmission": true,
  ...other fields
}
```

### Security/Data Integrity
- Historical results are marked read-only in UI
- Save functionality is disabled when viewing all admissions
- Only current admission results can be modified

## Testing

### To Test the Feature:
1. Open the application: http://localhost:4200
2. Select a patient who has multiple admissions
3. Check the "Show All Admissions" checkbox
4. Observe:
   - Results from multiple admissions displayed
   - Badge indicators showing which admission each result is from
   - Green badges (current) vs Gray badges (historical)
   - Input fields for historical results are read-only (gray background)
   - Save button is disabled
5. Uncheck the checkbox to return to single-admission view

## Services Status
- ✅ **API**: http://localhost:5050 (Process ID: 42868)
- ✅ **Web**: http://localhost:4200 (Process ID: 20900)

## Files Modified
1. `LIS.Api/Controllers/PatientLabResultsController.cs` - New MRN endpoint
2. `LIS.Web/src/app/services/patient-results.service.ts` - Enhanced interface and new method
3. `LIS.Web/src/app/patient-results/patient-results.component.ts` - Logic for all admissions
4. `LIS.Web/src/app/patient-results/patient-results.component.html` - UI checkbox and read-only states

## Benefits
- ✅ View complete patient history across all admissions
- ✅ Compare current results with historical data
- ✅ Read-only protection for historical data
- ✅ Clear visual distinction between current and historical results
- ✅ Maintains data integrity (no accidental modifications of old data)

