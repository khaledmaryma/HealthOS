# Resident Patient List - Recent Updates

## Summary of Changes

### 1. Default Filter Selection
- **Current Date Only** is now selected by default on page load
- Shows only active patients (not discharged) who checked in today
- Automatically filters on component initialization

### 2. Check-In Period Filter with Date Range
- Added new "Check-In Period" option to the filter dropdown
- When selected, displays two date pickers:
  - **Check-In From**: Defaults to 7 days ago
  - **Check-In To**: Defaults to today
- Date pickers automatically set default values when the filter is selected
- Dates can be manually adjusted by the user
- "Apply Date Filter" button for explicit search

### 3. Column Layout Updates
- **MRN Column**: Moved to first position (after #), bold, and colored red
- **Department Column**: Replaced "Room/Bed" with "Department" showing MedicationUnitDescription
- **Rows per page**: Moved to bottom pagination section (from top filters)

### 4. Filter Options
Available filter options:
1. **All Patients** - Shows all patients regardless of discharge status or date
2. **Active Only** - Shows only non-discharged patients (all dates)
3. **Current Date Only** - Shows active patients who checked in today (DEFAULT)
4. **Check-In Period** - Shows active patients who checked in within selected date range
5. **Discharged Only** - Shows only discharged patients (all dates)

## Technical Implementation

### Frontend (Angular)
- **Component State**:
  - `currentDateOnly = signal(true)` - Default to current date
  - `dischargeFilter = signal<boolean | undefined>(false)` - Default to active patients
  - `showDateRange = signal(false)` - Controls date picker visibility
  - `checkInDateFrom = signal<string>('')` - From date value
  - `checkInDateTo = signal<string>('')` - To date value

- **Date Formatting**:
  - `formatDateForInput(date: Date)` - Converts Date object to YYYY-MM-DD format for input fields
  - Automatically sets dates 7 days back from today when "Check-In Period" is selected

- **UI Updates**:
  - Date range section shows/hides based on filter selection
  - Responsive layout with proper column sizing
  - Clean labels and intuitive design

### Backend (API)
- **New Parameters**:
  - `checkInDateFrom: string?` - Filter check-in date >= this date
  - `checkInDateTo: string?` - Filter check-in date <= this date

- **SQL Filtering**:
  ```sql
  -- Current date only
  CAST(CheckInDate AS DATE) = CAST(GETDATE() AS DATE)
  
  -- Date range
  CAST(CheckInDate AS DATE) >= @checkInDateFrom
  CAST(CheckInDate AS DATE) <= @checkInDateTo
  ```

- **Endpoints Updated**:
  - `GET /api/ResidentPatient` - List patients with date filters
  - `GET /api/ResidentPatient/count` - Count patients with date filters

## Default Behavior

When the page loads:
1. Filter is set to "Current Date Only"
2. Shows only active patients (IsDischarged = false)
3. Filters by today's check-in date
4. Page 1 with 50 results per page

When "Check-In Period" is selected:
1. Date pickers appear
2. "From" date is automatically set to 7 days ago
3. "To" date is automatically set to today
4. Results are filtered to show patients who checked in within this range
5. User can adjust dates as needed

## User Experience Flow

### Scenario 1: View today's admissions (DEFAULT)
1. Page loads with "Current Date Only" selected
2. Grid shows patients who checked in today
3. No date pickers visible

### Scenario 2: View last week's admissions
1. Select "Check-In Period" from dropdown
2. Date pickers appear with dates already set (7 days ago to today)
3. Grid updates automatically
4. User can adjust dates if needed

### Scenario 3: View custom date range
1. Select "Check-In Period" from dropdown
2. Adjust "From" date to desired start date
3. Adjust "To" date to desired end date
4. Results update automatically or click "Apply Date Filter"

### Scenario 4: View all active patients
1. Select "Active Only" from dropdown
2. Date pickers disappear
3. Grid shows all active patients regardless of check-in date

## Benefits

1. **Immediate Context**: Users see today's admissions by default
2. **Quick Access**: Common use case (last 7 days) is pre-configured
3. **Flexibility**: Users can easily adjust date range or switch to other filters
4. **Efficiency**: No need to manually select dates for common scenarios
5. **Clear UI**: Date pickers only appear when relevant




















