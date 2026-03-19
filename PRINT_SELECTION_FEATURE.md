# Print Selection Feature - Multi-Select Medical Classes and Lab Tests

## Overview
Added the ability to select one or more medical classes and/or lab tests for printing. If nothing is selected, all results are printed.

## Features

### Multi-Select Capabilities
1. **Medical Classes**: Select specific categories to print (e.g., only Hematology)
2. **Lab Tests**: Select specific tests to print (e.g., only CBC and Glucose)
3. **Combined Selection**: Select both medical classes AND lab tests
4. **Print All (Default)**: If nothing is selected, all results are printed

### User Interface

#### Print Button Dropdown
Located in the patient header section, the print button now opens a dropdown menu with:

```
┌─────────────────────────────────────┐
│ Select Items to Print               │
├─────────────────────────────────────┤
│ Medical Classes:        [All] [None]│
│ ☐ Hematology                        │
│ ☐ Chemistry                         │
│ ☐ Microbiology                      │
├─────────────────────────────────────┤
│ Lab Tests:              [All] [None]│
│ ☐ CBC                               │
│ ☐ Glucose                           │
│ ☐ Creatinine                        │
├─────────────────────────────────────┤
│ [Print Selected] or [Print All]     │
│ No selection = Print all results    │
└─────────────────────────────────────┘
```

#### Quick Actions
- **All Button**: Select all items in that category
- **None Button**: Deselect all items in that category
- **Checkboxes**: Individual selection
- **Scrollable Lists**: For many medical classes or lab tests

## Implementation

### Frontend (Patient Results Component)

**File:** `LIS.Web/src/app/patient-results/patient-results.component.ts`

#### New Signals
```typescript
readonly selectedMedicalClassesForPrint = signal<Set<number>>(new Set());
readonly selectedLabTestsForPrint = signal<Set<number>>(new Set());
```

#### Key Methods
```typescript
// Toggle individual items
toggleMedicalClassForPrint(medicalClassId: number)
toggleLabTestForPrint(labTestId: number)

// Check if selected
isMedicalClassSelectedForPrint(medicalClassId: number): boolean
isLabTestSelectedForPrint(labTestId: number): boolean

// Select/Clear all
selectAllMedicalClassesForPrint()
clearAllMedicalClassesForPrint()
selectAllLabTestsForPrint()
clearAllLabTestsForPrint()

// Generate print URL with filters
getPrintUrl(): string
```

#### URL Generation
```typescript
getPrintUrl(): string {
  const params = new URLSearchParams();
  
  // Add selected medical classes
  if (selectedMedClasses.length > 0) {
    params.set('medicalClasses', selectedMedClasses.join(','));
  }
  
  // Add selected lab tests
  if (selectedTests.length > 0) {
    params.set('labTests', selectedTests.join(','));
  }
  
  return `/print-lab-results/${admissionNumber}?${queryString}`;
}
```

### Frontend (Print Lab Results Component)

**File:** `LIS.Web/src/app/print-lab-results/print-lab-results.component.ts`

#### New Signals
```typescript
readonly selectedMedicalClasses = signal<number[]>([]);
readonly selectedLabTests = signal<number[]>([]);
```

#### Query Parameter Parsing
```typescript
ngOnInit(): void {
  const queryParams = this.route.snapshot.queryParamMap;
  
  // Parse medical classes
  const medClassesParam = queryParams.get('medicalClasses');
  if (medClassesParam) {
    const medClasses = medClassesParam.split(',')
      .map(id => parseInt(id, 10))
      .filter(id => !isNaN(id));
    this.selectedMedicalClasses.set(medClasses);
  }
  
  // Parse lab tests
  const labTestsParam = queryParams.get('labTests');
  if (labTestsParam) {
    const labTests = labTestsParam.split(',')
      .map(id => parseInt(id, 10))
      .filter(id => !isNaN(id));
    this.selectedLabTests.set(labTests);
  }
}
```

#### Filtering Logic
```typescript
readonly filteredLabResults = computed(() => {
  let results = this.labResults();
  const medClasses = this.selectedMedicalClasses();
  const labTests = this.selectedLabTests();
  
  // Filter by medical classes if any selected
  if (medClasses.length > 0) {
    results = results.filter(r => medClasses.includes(r.medicalClass));
  }
  
  // Filter by lab tests if any selected
  if (labTests.length > 0) {
    results = results.filter(r => r.labTestID && labTests.includes(r.labTestID));
  }
  
  return results;
});
```

## URL Format

### No Selection (Print All)
```
/print-lab-results/03.01254.08.24
```

### Medical Classes Only
```
/print-lab-results/03.01254.08.24?medicalClasses=1,2,3
```

### Lab Tests Only
```
/print-lab-results/03.01254.08.24?labTests=595,596,597
```

### Combined Selection
```
/print-lab-results/03.01254.08.24?medicalClasses=1,2&labTests=595,596
```

## Usage Examples

### Example 1: Print Only Hematology Tests
1. Click Print button
2. Check "Hematology" in Medical Classes
3. Leave Lab Tests empty
4. Click "Print Selected"
5. Result: Only tests from Hematology class are printed

### Example 2: Print Only CBC and Glucose
1. Click Print button
2. Leave Medical Classes empty
3. Check "CBC" and "Glucose" in Lab Tests
4. Click "Print Selected"
5. Result: Only CBC and Glucose tests are printed

### Example 3: Print Chemistry Tests - Only Glucose
1. Click Print button
2. Check "Chemistry" in Medical Classes
3. Check "Glucose" in Lab Tests
4. Click "Print Selected"
5. Result: Only Glucose test from Chemistry class is printed

### Example 4: Print Everything
1. Click Print button
2. Leave all unchecked (or click "None" on both)
3. Click "Print Selected" (shows "All" badge)
4. Result: All results are printed

## UI Features

### Visual Indicators
- **Badge**: Shows "All" when nothing is selected
- **Tooltip**: "No selection = Print all results"
- **Hover Effects**: Checkboxes and labels highlight on hover
- **Scrollable**: Lists scroll if many items

### Responsive Behavior
- Dropdown menu: 350px wide minimum
- Lists: Max height 150px with scroll
- Adapts to screen size

### User Feedback
- Checked items stay visually selected
- All/None buttons for quick selection
- Clear indication of what will be printed

## Filter Logic

### Medical Classes Filter
```typescript
if (selectedMedicalClasses.length > 0) {
  results = results.filter(r => selectedMedicalClasses.includes(r.medicalClass));
}
```

### Lab Tests Filter
```typescript
if (selectedLabTests.length > 0) {
  results = results.filter(r => r.labTestID && selectedLabTests.includes(r.labTestID));
}
```

### Combined Filters (AND Logic)
When both are selected, results must match BOTH criteria:
```
Selected: Medical Class = Chemistry (ID: 2)
          Lab Test = Glucose (ID: 595)

Result: Only Glucose tests that are also in Chemistry class
```

### No Filters (Print All)
When nothing is selected:
```
selectedMedicalClasses = []
selectedLabTests = []

Result: All lab results are printed
```

## Benefits

### For Users
1. **Selective Printing**: Print only what's needed
2. **Save Paper**: Avoid printing unnecessary results
3. **Faster Review**: Focus on specific categories
4. **Flexible**: Combine filters for precision

### For Workflow
1. **Department-Specific**: Print only relevant departments
2. **Test-Specific**: Print individual test results
3. **Custom Reports**: Create targeted reports
4. **Efficient**: Reduce waste and save time

## Technical Details

### State Management
- Uses Angular signals for reactive updates
- Set-based storage for selections
- Computed signals for filtered results

### URL Encoding
- Comma-separated IDs in query parameters
- Parsed as integers on print page
- Invalid IDs filtered out

### Performance
- Client-side filtering (instant)
- No additional API calls
- Efficient with Set operations

### Compatibility
- Works with existing print functionality
- Maintains order: MedicalClass → DisplayOrder
- Respects result type separation (Numeric, Text, Antibiogram)

## Testing Checklist

### Functionality
- [ ] Select individual medical classes
- [ ] Select individual lab tests
- [ ] Use "All" button for medical classes
- [ ] Use "All" button for lab tests
- [ ] Use "None" button to clear selections
- [ ] Combine medical class + lab test filters
- [ ] Print with no selections (all results)
- [ ] Verify URL parameters are correct

### UI/UX
- [ ] Dropdown opens properly
- [ ] Checkboxes toggle correctly
- [ ] Lists scroll when many items
- [ ] "All" badge shows when nothing selected
- [ ] Tooltip displays correctly
- [ ] Print button opens in new tab

### Edge Cases
- [ ] Patient with 1 medical class
- [ ] Patient with 1 lab test
- [ ] Patient with 100+ lab tests
- [ ] Select all then deselect all
- [ ] Rapid clicking of checkboxes
- [ ] Browser back button behavior

## Future Enhancements

### Potential Improvements
1. **Save Preferences**: Remember last selection per user
2. **Preset Filters**: Quick buttons for common selections
3. **Search in Lists**: Filter checkboxes by name
4. **Visual Preview**: Show count of results before printing
5. **Export Options**: PDF, Excel, etc.
6. **Print Templates**: Different layouts for different purposes
7. **Batch Printing**: Print multiple patients with same filters

## Troubleshooting

### Checkboxes Not Working
- Verify `$event.stopPropagation()` is preventing dropdown close
- Check that methods are being called
- Verify signals are updating

### Print Shows Wrong Results
- Check URL query parameters
- Verify filter parsing in print component
- Ensure computed signals are recalculating

### Dropdown Closes Immediately
- Ensure `$event.stopPropagation()` on checkbox changes
- Check Bootstrap dropdown configuration
- Verify button data attributes

## Summary

✅ **Multi-Select**: Choose medical classes and/or lab tests
✅ **Print All**: Default behavior when nothing selected
✅ **Flexible Filtering**: Combine filters for precision
✅ **User-Friendly**: Clear UI with All/None buttons
✅ **URL-Based**: Share or bookmark specific print views
✅ **No Backend Changes**: Pure frontend implementation
✅ **Performance**: Instant client-side filtering

This feature provides complete control over what gets printed, making the system more efficient and user-friendly for medical staff who need to print only specific categories or tests.

