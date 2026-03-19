# Multi-Select Medical Class Filter

## Overview
Enhanced the medical class filter to support selecting **multiple medical classes** at once, allowing users to view results from several categories simultaneously.

## Features

### Multi-Select Capability
- Select one or more medical classes
- Combine multiple classes (e.g., Hematology + Chemistry)
- Visual chips/tags show selected classes
- Easy removal by clicking X on each chip

### User Interface

#### Selected Classes Display
```
┌─────────────────────────────────────┐
│ [Hematology ×] [Chemistry ×]        │
│ [Search medical class...]           │
└─────────────────────────────────────┘
```

#### Dropdown with Checkboxes
```
┌─────────────────────────────────────┐
│        [All]  [None]                │
├─────────────────────────────────────┤
│ ☑ Hematology                        │
│ ☑ Chemistry                         │
│ ☐ Microbiology                      │
│ ☐ Immunology                        │
│ ☐ Pathology                         │
└─────────────────────────────────────┘
```

## Implementation

### TypeScript Changes
**File:** `LIS.Web/src/app/patient-results/patient-results.component.ts`

#### Changed From (Single Select)
```typescript
readonly selectedMedicalClassFilter = signal<number | null>(null);
```

#### Changed To (Multi-Select)
```typescript
readonly selectedMedicalClassesFilter = signal<Set<number>>(new Set());
```

### New Methods

#### Toggle Individual Class
```typescript
toggleMedicalClassFilter(medicalClassId: number): void {
  const current = this.selectedMedicalClassesFilter();
  const newSet = new Set(current);
  
  if (newSet.has(medicalClassId)) {
    newSet.delete(medicalClassId);  // Uncheck
  } else {
    newSet.add(medicalClassId);     // Check
  }
  
  this.selectedMedicalClassesFilter.set(newSet);
}
```

#### Check if Selected
```typescript
isMedicalClassSelected(medicalClassId: number): boolean {
  return this.selectedMedicalClassesFilter().has(medicalClassId);
}
```

#### Quick Actions
```typescript
selectAllMedicalClasses(): void {
  const allIds = new Set(this.availableMedicalClasses().map(mc => mc.id));
  this.selectedMedicalClassesFilter.set(allIds);
}

clearMedicalClassFilter(): void {
  this.selectedMedicalClassesFilter.set(new Set());
}
```

#### Get Selected Names
```typescript
getSelectedMedicalClassNames(): string[] {
  const selectedIds = Array.from(this.selectedMedicalClassesFilter());
  return selectedIds
    .map(id => this.availableMedicalClasses().find(c => c.id === id)?.description)
    .filter((desc): desc is string => desc !== undefined);
}
```

### Filtering Logic

#### Updated Filter
```typescript
readonly filteredLabResults = computed(() => {
  let results = this.labResults();
  const filterMedicalClassIds = this.selectedMedicalClassesFilter();
  
  // Filter by multiple medical classes
  if (filterMedicalClassIds.size > 0) {
    results = results.filter(r => filterMedicalClassIds.has(r.medicalClass));
  }
  
  return results;
});
```

**Logic:**
- If 0 classes selected → Show all results
- If 1+ classes selected → Show only results from those classes
- Uses `Set.has()` for O(1) lookup performance

### HTML Template

#### Selected Chips Display
```html
<div class="selected-chips mb-1" *ngIf="selectedMedicalClassesFilter().size > 0">
  <span class="badge bg-warning text-dark me-1 mb-1" 
        *ngFor="let id of Array.from(selectedMedicalClassesFilter())">
    {{ availableMedicalClasses().find(c => c.id === id)?.description }}
    <i class="bi bi-x-circle ms-1" 
       style="cursor: pointer;"
       (click)="toggleMedicalClassFilter(id)"></i>
  </span>
</div>
```

#### Checkbox Items
```html
<div class="autocomplete-item form-check"
     (mousedown)="toggleMedicalClassFilter(medClass.id); $event.preventDefault()">
  <div class="d-flex align-items-center">
    <input 
      class="form-check-input me-2" 
      type="checkbox" 
      [checked]="isMedicalClassSelected(medClass.id)"
      (click)="$event.stopPropagation()">
    <i class="bi bi-folder2 me-2 text-warning"></i>
    <span class="small flex-grow-1">{{ medClass.description }}</span>
  </div>
</div>
```

#### All/None Buttons
```html
<div class="p-2 border-bottom bg-light">
  <div class="btn-group btn-group-sm w-100" role="group">
    <button type="button" 
            class="btn btn-outline-secondary btn-sm" 
            (mousedown)="selectAllMedicalClasses(); $event.preventDefault()">
      <i class="bi bi-check-all"></i> All
    </button>
    <button type="button" 
            class="btn btn-outline-secondary btn-sm" 
            (mousedown)="clearMedicalClassFilter(); $event.preventDefault()">
      <i class="bi bi-x"></i> None
    </button>
  </div>
</div>
```

## Usage Examples

### Example 1: View Hematology Results Only
1. Click medical class filter
2. Check "Hematology"
3. Results show only Hematology tests
4. Chip appears: [Hematology ×]

### Example 2: View Hematology + Chemistry
1. Click medical class filter
2. Check "Hematology"
3. Check "Chemistry"
4. Results show tests from both classes
5. Chips appear: [Hematology ×] [Chemistry ×]

### Example 3: Select All Then Remove One
1. Click medical class filter
2. Click "All" button
3. All classes selected
4. Click "Microbiology" checkbox to uncheck
5. Shows all except Microbiology

### Example 4: Quick Clear
1. Click X on any chip
2. That class is removed
3. OR click "None" button in dropdown
4. All selections cleared

## Visual Design

### Selected Chips
- **Color**: Yellow/warning background
- **Text**: Dark text for contrast
- **Icon**: X circle for removal
- **Hover**: Red X on hover
- **Size**: Small, compact badges

### Checkbox List
- **Checkboxes**: Standard Bootstrap form-check
- **Icons**: Folder icon (yellow) for medical classes
- **Highlight**: Blue background when selected
- **Hover**: Light gray background on hover

### Quick Action Buttons
- **Position**: Top of dropdown
- **Style**: Outlined secondary buttons
- **Icons**: Check-all and X icons
- **Layout**: Full width button group

## Benefits

### For Users
1. **Flexibility**: View multiple categories at once
2. **Comparison**: Compare results across classes
3. **Efficiency**: No need to switch filters repeatedly
4. **Visual**: Clear indication of active filters

### For Workflow
1. **Multi-Specialty Review**: View related specialties together
2. **Comprehensive Analysis**: See broader picture
3. **Custom Views**: Create personalized filter combinations
4. **Quick Toggle**: Easy to add/remove categories

## Technical Details

### Performance
- **Set Operations**: O(1) for add/delete/has
- **Filtering**: O(n) where n = number of results
- **Memory**: Minimal overhead (Set of IDs)
- **Rendering**: Efficient with computed signals

### State Management
- **Signal-based**: Reactive updates
- **Immutable**: New Set created on each change
- **Computed**: Auto-recalculates filtered results

### Compatibility
- **Lab Test Filter**: Works alongside lab test filter
- **Combined Filters**: AND logic (class AND test)
- **Order Preserved**: MedicalClass → DisplayOrder maintained
- **Print Selection**: Independent from print multi-select

## Comparison: Single vs Multi-Select

| Feature | Single Select (Old) | Multi-Select (New) |
|---------|--------------------|--------------------|
| Selection | One class only | Multiple classes |
| UI | Click to select | Checkboxes |
| Visual | Autocomplete fill | Chips/badges |
| Removal | Click to change | X on each chip |
| Quick Actions | None | All / None buttons |
| Flexibility | Limited | High |

## Edge Cases Handled

### No Selection
- Shows all results (default behavior)
- Clear button disabled
- No chips displayed

### All Selected
- Equivalent to no filter
- All checkboxes checked
- Multiple chips displayed

### Search + Multi-Select
- Search filters dropdown list
- Selected items remain checked
- Can select from search results

### Load New Patient
- All selections cleared
- Chips removed
- Fresh state for new patient

## Styling

### CSS Classes
```scss
.selected-chips {
  .badge {
    font-size: 0.75rem;
    padding: 0.25rem 0.5rem;
    font-weight: normal;
    
    i {
      font-size: 0.7rem;
      transition: color 0.15s ease;
      
      &:hover {
        color: #dc3545 !important;
      }
    }
  }
}
```

## Future Enhancements

### Potential Improvements
1. **Save Presets**: Save common filter combinations
2. **Keyboard Shortcuts**: Space to toggle, Arrow keys to navigate
3. **Badge Count**: Show count on filter button
4. **Collapse Chips**: Show "3 classes selected" if too many
5. **Drag & Drop**: Reorder selected chips
6. **Export**: Export current filter selection
7. **Share**: Share URL with filter state

## Testing Checklist

### Functionality
- [ ] Select single medical class
- [ ] Select multiple medical classes
- [ ] Deselect by unchecking
- [ ] Deselect by clicking chip X
- [ ] Use "All" button
- [ ] Use "None" button
- [ ] Search with selections active
- [ ] Load new patient clears selections
- [ ] Combine with lab test filter

### UI/UX
- [ ] Chips display correctly
- [ ] Checkboxes sync with selections
- [ ] Dropdown stays open when selecting
- [ ] Hover effects work
- [ ] Clear button enables/disables
- [ ] Badge shows filtered count

### Edge Cases
- [ ] Select all then deselect all
- [ ] Select then search
- [ ] Rapid clicking
- [ ] Very long class names
- [ ] Many classes (20+)

## Summary

✅ **Multi-Select**: Choose one or more medical classes
✅ **Visual Chips**: Selected classes shown as removable badges
✅ **Checkboxes**: Clear UI for selection state
✅ **Quick Actions**: All/None buttons for convenience
✅ **Combined Filtering**: Works with lab test filter
✅ **Performance**: Efficient Set-based implementation
✅ **User-Friendly**: Intuitive and responsive interface

The multi-select medical class filter provides users with powerful, flexible filtering capabilities while maintaining a clean and intuitive interface.

