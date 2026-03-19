import { Component, computed, inject, signal, ViewChild, ElementRef, AfterViewInit, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LabTestsService, LabTest, ResultType, UnitOfMeasure, LabTestAge, LabTestGyneco, LabTestSub } from '../services/lab-tests.service';

@Component({
  selector: 'app-lab-tests',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lab-tests.component.html',
  styleUrls: ['./lab-tests.component.scss']
})
export class LabTestsComponent implements OnInit {
  private svc = inject(LabTestsService);

  @ViewChild('richTextEditor') richTextEditor?: ElementRef<HTMLElement>;

  // Expose Math to template
  Math = Math;

  readonly items = this.svc.items;
  readonly resultTypes = this.svc.resultTypes;
  readonly unitOfMeasures = this.svc.unitOfMeasures;
  readonly denominations = this.svc.denominations;
  readonly query = signal('');
  readonly showForm = signal(false);
  readonly editItem = signal<LabTest | null>(null);
  readonly selectedItem = signal<LabTest | null>(null);

  // Child tables
  readonly labTestAges = signal<LabTestAge[]>([]);
  readonly labTestGynecos = signal<LabTestGyneco[]>([]);
  readonly labTestSubs = signal<LabTestSub[]>([]);

  // Child table pagination
  readonly agePageSize = signal(10);
  readonly agePageIndex = signal(0);
  readonly gynecoPageSize = signal(10);
  readonly gynecoPageIndex = signal(0);
  readonly subPageSize = signal(10);
  readonly subPageIndex = signal(0);

  // Child table sorting
  readonly ageSortColumn = signal<string | null>(null);
  readonly ageSortDirection = signal<'asc' | 'desc'>('asc');
  readonly gynecoSortColumn = signal<string | null>(null);
  readonly gynecoSortDirection = signal<'asc' | 'desc'>('asc');
  readonly subSortColumn = signal<string | null>(null);
  readonly subSortDirection = signal<'asc' | 'desc'>('asc');

  readonly pageSizeOptions = [10, 20, 50, 75, 100];
  readonly pageSize = signal(20);
  readonly pageIndex = signal(0); // zero-based

  // Column resizing state
  private resizingColumn: {
    index: number;
    startX: number;
    startWidth: number;
    headerElement: HTMLElement;
  } | null = null;

  private readonly DEFAULT_COLUMN_WIDTHS = [120, 300, 100, 120, 120, 120, 100, 120];
  private readonly MIN_COLUMN_WIDTH = 60;
  private readonly STORAGE_KEY = 'lab-tests-column-widths';

  readonly columnWidths = signal<number[]>(this.loadColumnWidths());
  readonly isResizing = signal(false);

  // Sorting state
  readonly sortColumn = signal<string | null>(null);
  readonly sortDirection = signal<'asc' | 'desc'>('asc');

  // Table picker state
  readonly showTablePicker = signal(false);
  readonly tablePickerRows = signal(3);
  readonly tablePickerCols = signal(3);

  // Track active formatting
  readonly activeFormats = signal<Set<string>>(new Set());

  // Save selection/range
  private savedSelection: Range | null = null;

  readonly filtered = computed(() => {
    const q = this.query().toLowerCase().trim();
    let results = !q ? this.items() : this.items().filter(t =>
      (t.code ?? '').toLowerCase().includes(q) ||
      (t.testDesciption ?? '').toLowerCase().includes(q)
    );

    // Apply sorting
    const col = this.sortColumn();
    const dir = this.sortDirection();

    if (col) {
      results = [...results].sort((a, b) => {
        let aVal: any;
        let bVal: any;

        switch (col) {
          case 'code':
            aVal = a.code ?? '';
            bVal = b.code ?? '';
            break;
          case 'description':
            aVal = a.testDesciption ?? '';
            bVal = b.testDesciption ?? '';
            break;
          case 'display':
            aVal = parseInt(a.displayOrder ?? '0');
            bVal = parseInt(b.displayOrder ?? '0');
            break;
          case 'resultType':
            aVal = a.resultType ?? 0;
            bVal = b.resultType ?? 0;
            break;
          case 'collection':
            aVal = a.isACollection ? 1 : 0;
            bVal = b.isACollection ? 1 : 0;
            break;
          case 'refRange':
            aVal = a.hasReferenceRange ? 1 : 0;
            bVal = b.hasReferenceRange ? 1 : 0;
            break;
          case 'ageRef':
            aVal = a.referenceRelatesToAge ? 1 : 0;
            bVal = b.referenceRelatesToAge ? 1 : 0;
            break;
          case 'gynecoRef':
            aVal = a.referencerelatesToGyneco ? 1 : 0;
            bVal = b.referencerelatesToGyneco ? 1 : 0;
            break;
          default:
            return 0;
        }

        // Handle string comparison
        if (typeof aVal === 'string' && typeof bVal === 'string') {
          const comparison = aVal.localeCompare(bVal, undefined, { numeric: true, sensitivity: 'base' });
          return dir === 'asc' ? comparison : -comparison;
        }

        // Handle numeric/boolean comparison
        if (aVal < bVal) return dir === 'asc' ? -1 : 1;
        if (aVal > bVal) return dir === 'asc' ? 1 : -1;
        return 0;
      });
    }

    return results;
  });

  readonly totalPages = computed(() => {
    const total = this.filtered().length;
    const size = this.pageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  readonly paged = computed(() => {
    const size = this.pageSize();
    const idx = this.pageIndex();
    const start = idx * size;
    return this.filtered().slice(start, start + size);
  });

  // LabTestAge computed properties
  readonly sortedAges = computed(() => {
    let results = [...this.labTestAges()];
    const col = this.ageSortColumn();
    const dir = this.ageSortDirection();

    if (col) {
      results.sort((a, b) => {
        let aVal: any;
        let bVal: any;

        switch (col) {
          case 'description':
            aVal = a.description ?? '';
            bVal = b.description ?? '';
            break;
          case 'lower':
            aVal = a.lower ?? 0;
            bVal = b.lower ?? 0;
            break;
          case 'higher':
            aVal = a.higher ?? 0;
            bVal = b.higher ?? 0;
            break;
          case 'defaultMin':
            aVal = a.defaultMin ?? '';
            bVal = b.defaultMin ?? '';
            break;
          case 'defaultMax':
            aVal = a.defaultMax ?? '';
            bVal = b.defaultMax ?? '';
            break;
          case 'displayOrder':
            aVal = a.displayOrder ?? '';
            bVal = b.displayOrder ?? '';
            break;
          default:
            return 0;
        }

        if (typeof aVal === 'string' && typeof bVal === 'string') {
          const comparison = aVal.localeCompare(bVal, undefined, { numeric: true, sensitivity: 'base' });
          return dir === 'asc' ? comparison : -comparison;
        }

        if (aVal < bVal) return dir === 'asc' ? -1 : 1;
        if (aVal > bVal) return dir === 'asc' ? 1 : -1;
        return 0;
      });
    }

    return results;
  });

  readonly ageTotalPages = computed(() => {
    const total = this.sortedAges().length;
    const size = this.agePageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  readonly pagedAges = computed(() => {
    const size = this.agePageSize();
    const idx = this.agePageIndex();
    const start = idx * size;
    return this.sortedAges().slice(start, start + size);
  });

  // LabTestGyneco computed properties
  readonly sortedGynecos = computed(() => {
    let results = [...this.labTestGynecos()];
    const col = this.gynecoSortColumn();
    const dir = this.gynecoSortDirection();

    if (col) {
      results.sort((a, b) => {
        let aVal: any;
        let bVal: any;

        switch (col) {
          case 'description':
            aVal = a.description ?? '';
            bVal = b.description ?? '';
            break;
          case 'femaleNormalMin':
            aVal = a.femaleNormalMin ?? 0;
            bVal = b.femaleNormalMin ?? 0;
            break;
          case 'femaleNormalMax':
            aVal = a.femaleNormalMax ?? 0;
            bVal = b.femaleNormalMax ?? 0;
            break;
          case 'displayOrder':
            aVal = a.displayOrder ?? '';
            bVal = b.displayOrder ?? '';
            break;
          default:
            return 0;
        }

        if (typeof aVal === 'string' && typeof bVal === 'string') {
          const comparison = aVal.localeCompare(bVal, undefined, { numeric: true, sensitivity: 'base' });
          return dir === 'asc' ? comparison : -comparison;
        }

        if (aVal < bVal) return dir === 'asc' ? -1 : 1;
        if (aVal > bVal) return dir === 'asc' ? 1 : -1;
        return 0;
      });
    }

    return results;
  });

  readonly gynecoTotalPages = computed(() => {
    const total = this.sortedGynecos().length;
    const size = this.gynecoPageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  readonly pagedGynecos = computed(() => {
    const size = this.gynecoPageSize();
    const idx = this.gynecoPageIndex();
    const start = idx * size;
    return this.sortedGynecos().slice(start, start + size);
  });

  // LabTestSub computed properties
  readonly sortedSubs = computed(() => {
    let results = [...this.labTestSubs()];
    const col = this.subSortColumn();
    const dir = this.subSortDirection();

    if (col) {
      results.sort((a, b) => {
        let aVal: any;
        let bVal: any;

        switch (col) {
          case 'description':
            aVal = a.description ?? '';
            bVal = b.description ?? '';
            break;
          case 'displayOrder':
            aVal = a.displayOrder ?? '';
            bVal = b.displayOrder ?? '';
            break;
          case 'defaultNoramlMin':
            aVal = a.defaultNoramlMin ?? '';
            bVal = b.defaultNoramlMin ?? '';
            break;
          case 'defaultNormalMax':
            aVal = a.defaultNormalMax ?? '';
            bVal = b.defaultNormalMax ?? '';
            break;
          default:
            return 0;
        }

        if (typeof aVal === 'string' && typeof bVal === 'string') {
          const comparison = aVal.localeCompare(bVal, undefined, { numeric: true, sensitivity: 'base' });
          return dir === 'asc' ? comparison : -comparison;
        }

        if (aVal < bVal) return dir === 'asc' ? -1 : 1;
        if (aVal > bVal) return dir === 'asc' ? 1 : -1;
        return 0;
      });
    }

    return results;
  });

  readonly subTotalPages = computed(() => {
    const total = this.sortedSubs().length;
    const size = this.subPageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  readonly pagedSubs = computed(() => {
    const size = this.subPageSize();
    const idx = this.subPageIndex();
    const start = idx * size;
    return this.sortedSubs().slice(start, start + size);
  });

  ngOnInit() {
    this.svc.load();
    this.svc.loadResultTypes();
    this.svc.loadUnitOfMeasures();
    this.svc.loadDenominations();
  }

  setPageSize(size: number) {
    this.pageSize.set(size);
    this.pageIndex.set(0);
  }

  nextPage() {
    if (this.pageIndex() + 1 < this.totalPages()) this.pageIndex.set(this.pageIndex() + 1);
  }

  prevPage() {
    if (this.pageIndex() > 0) this.pageIndex.set(this.pageIndex() - 1);
  }

  firstPage() {
    this.pageIndex.set(0);
  }

  lastPage() {
    this.pageIndex.set(this.totalPages() - 1);
  }

  onSearchChange(v: string) {
    this.query.set(v);
    this.pageIndex.set(0);
  }

  // LabTestAge pagination & sorting methods
  sortAgeColumn(column: string) {
    if (this.ageSortColumn() === column) {
      this.ageSortDirection.set(this.ageSortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.ageSortColumn.set(column);
      this.ageSortDirection.set('asc');
    }
  }

  setAgePageSize(size: number) {
    this.agePageSize.set(size);
    this.agePageIndex.set(0);
  }

  ageFirstPage() {
    this.agePageIndex.set(0);
  }

  agePrevPage() {
    const current = this.agePageIndex();
    if (current > 0) this.agePageIndex.set(current - 1);
  }

  ageNextPage() {
    const current = this.agePageIndex();
    if (current < this.ageTotalPages() - 1) this.agePageIndex.set(current + 1);
  }

  ageLastPage() {
    this.agePageIndex.set(this.ageTotalPages() - 1);
  }

  // LabTestGyneco pagination & sorting methods
  sortGynecoColumn(column: string) {
    if (this.gynecoSortColumn() === column) {
      this.gynecoSortDirection.set(this.gynecoSortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.gynecoSortColumn.set(column);
      this.gynecoSortDirection.set('asc');
    }
  }

  setGynecoPageSize(size: number) {
    this.gynecoPageSize.set(size);
    this.gynecoPageIndex.set(0);
  }

  gynecoFirstPage() {
    this.gynecoPageIndex.set(0);
  }

  gynecoPrevPage() {
    const current = this.gynecoPageIndex();
    if (current > 0) this.gynecoPageIndex.set(current - 1);
  }

  gynecoNextPage() {
    const current = this.gynecoPageIndex();
    if (current < this.gynecoTotalPages() - 1) this.gynecoPageIndex.set(current + 1);
  }

  gynecoLastPage() {
    this.gynecoPageIndex.set(this.gynecoTotalPages() - 1);
  }

  // LabTestSub pagination & sorting methods
  sortSubColumn(column: string) {
    if (this.subSortColumn() === column) {
      this.subSortDirection.set(this.subSortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.subSortColumn.set(column);
      this.subSortDirection.set('asc');
    }
  }

  setSubPageSize(size: number) {
    this.subPageSize.set(size);
    this.subPageIndex.set(0);
  }

  subFirstPage() {
    this.subPageIndex.set(0);
  }

  subPrevPage() {
    const current = this.subPageIndex();
    if (current > 0) this.subPageIndex.set(current - 1);
  }

  subNextPage() {
    const current = this.subPageIndex();
    if (current < this.subTotalPages() - 1) this.subPageIndex.set(current + 1);
  }

  subLastPage() {
    this.subPageIndex.set(this.subTotalPages() - 1);
  }

  selectRow(item: LabTest) {
    console.log('=== ROW SELECTED ===');
    console.log('Selected item:', item);
    console.log('Has defaultTextResult field?', 'defaultTextResult' in item);
    console.log('defaultTextResult value:', item.defaultTextResult);
    console.log('====================');
    this.selectedItem.set(item);
  }

  isSelected(item: LabTest): boolean {
    return this.selectedItem()?.id === item.id;
  }

  clickAdd() {
    console.log('=== ADD POPUP OPENED ===');

    const newItem = {
      id: 0,
      code: '',
      testDesciption: '',
      defaultTextResult: '',
      displayOrder: '0',
      resultType: null,
      uom: null,
      isACollection: false,
      hasReferenceRange: false,
      referenceRelatesToAge: false,
      referencerelatesToGyneco: false,
      isDeleted: false
    };

    console.log('New item initialized:', JSON.stringify(newItem, null, 2));
    console.log('========================');

    this.editItem.set(newItem);
    this.showForm.set(true);

    // Clear child tables for new items
    this.labTestAges.set([]);
    this.labTestGynecos.set([]);
    this.labTestSubs.set([]);

    // Clear the rich text editor
    setTimeout(() => {
      const editor = document.querySelector('.rich-text-editor') as HTMLElement;
      if (editor) {
        editor.innerHTML = '';
        console.log('Editor cleared for new item');
      }
    }, 100);
  }

  clickEdit() {
    const selected = this.selectedItem();
    if (!selected) return;

    console.log('=== EDIT POPUP OPENED ===');
    console.log('Selected test data:', JSON.stringify(selected, null, 2));
    console.log('ID:', selected.id);
    console.log('Code:', selected.code);
    console.log('Description:', selected.testDesciption);
    console.log('Result Type:', selected.resultType);
    console.log('DefaultTextResult:', selected.defaultTextResult);
    console.log('DefaultTextResult length:', selected.defaultTextResult?.length ?? 0);
    console.log('Is Text Type (1)?', selected.resultType === 1);
    console.log('========================');

    this.editItem.set({ ...selected });
    this.showForm.set(true);

    // Load child tables if editing existing item
    if (selected.id && selected.id > 0) {
      this.loadChildTables(selected.id);
    } else {
      // Clear child tables for new items
      this.labTestAges.set([]);
      this.labTestGynecos.set([]);
      this.labTestSubs.set([]);
    }

    // Load content into editor after modal opens
    setTimeout(() => {
      const editor = document.querySelector('.rich-text-editor') as HTMLElement;
      console.log('Editor element found:', !!editor);

      if (editor && selected.defaultTextResult) {
        editor.innerHTML = selected.defaultTextResult;
        console.log('Editor content loaded:', editor.innerHTML);
      } else {
        console.log('Editor not loaded - defaultTextResult:', selected.defaultTextResult);
      }
    }, 100);
  }

  loadChildTables(labTestId: number) {
    // Load LabTestAge
    this.svc.getLabTestAgeByLabTestId(labTestId).subscribe({
      next: (data) => this.labTestAges.set(data),
      error: (err) => console.error('Error loading LabTestAge:', err)
    });

    // Load LabTestGyneco
    this.svc.getLabTestGynecoByLabTestId(labTestId).subscribe({
      next: (data) => this.labTestGynecos.set(data),
      error: (err) => console.error('Error loading LabTestGyneco:', err)
    });

    // Load LabTestSub
    this.svc.getLabTestSubByLabTestId(labTestId).subscribe({
      next: (data) => this.labTestSubs.set(data),
      error: (err) => console.error('Error loading LabTestSub:', err)
    });
  }

  cancel() {
    this.showForm.set(false);
    this.editItem.set(null);

    // Clear the rich text editor
    const editor = document.querySelector('.rich-text-editor') as HTMLElement;
    if (editor) {
      editor.innerHTML = '';
    }
  }

  save(event?: Event) {
    event?.preventDefault();

    const model = this.editItem()!;

    // Force capture rich text editor content before saving - try multiple methods
    let editor: HTMLElement | null = null;

    // Try ViewChild first
    if (this.richTextEditor?.nativeElement) {
      editor = this.richTextEditor.nativeElement;
      console.log('Editor found via ViewChild');
    } else {
      // Fallback to querySelector
      editor = document.querySelector('.rich-text-editor') as HTMLElement;
      console.log('Editor found via querySelector');
    }

    console.log('Editor element found:', !!editor);
    console.log('Result type:', model.resultType);
    console.log('Model.defaultTextResult before capture:', model.defaultTextResult);

    if (editor && model.resultType === 1) {
      const content = editor.innerHTML;
      console.log('Raw editor innerHTML:', content);
      console.log('Content length:', content.length);

      // Only set to null if truly empty
      if (!content || content === '<br>' || content.trim() === '') {
        model.defaultTextResult = null;
      } else {
        model.defaultTextResult = content;
      }
      console.log('Final captured content:', model.defaultTextResult);
    } else if (model.resultType !== 1) {
      // Clear defaultTextResult if not text type
      model.defaultTextResult = null;
      console.log('Cleared defaultTextResult - not text type');
    }

    console.log('Complete model to save:', JSON.stringify(model, null, 2));

    if (model.id && model.id > 0) {
      this.svc.update(model.id, model).subscribe({
        next: () => {
          this.svc.load();
          this.cancel();
          console.log('Lab test updated successfully');
        },
        error: (err) => {
          console.error('Error updating lab test:', err);
          alert('Failed to save lab test. Check console for details.');
        }
      });
    } else {
      const { id, ...payload } = model;
      this.svc.create(payload).subscribe({
        next: () => {
          this.svc.load();
          this.cancel();
          console.log('Lab test created successfully');
        },
        error: (err) => {
          console.error('Error creating lab test:', err);
          alert('Failed to create lab test. Check console for details.');
        }
      });
    }
  }

  remove() {
    const selected = this.selectedItem();
    if (!selected) return;
    if (!confirm(`Delete lab test "${selected.testDesciption}"?`)) return;
    this.svc.delete(selected.id).subscribe(() => {
      this.selectedItem.set(null);
      this.svc.load();
    });
  }

  chip(val: boolean): string {
    return val ? 'Yes' : 'No';
  }

  getResultTypeLabel(value?: number | null): string {
    if (!value) return '-';
    const resultType = this.resultTypes().find(rt => rt.id === value);
    return resultType ? (resultType.description || '-') : '-';
  }

  getUnitOfMeasureLabel(value?: number | null): string {
    if (!value) return '-';
    const uom = this.unitOfMeasures().find(u => u.id === value);
    return uom ? (uom.description || '-') : '-';
  }

  isTextResultType(): boolean {
    const item = this.editItem();
    return item?.resultType === 1; // Text type (ID=1 in database)
  }

  // Mutually exclusive checkbox handlers
  onCollectionChange(checked: boolean) {
    const item = this.editItem();
    if (item && checked) {
      item.referenceRelatesToAge = false;
      item.referencerelatesToGyneco = false;
    }
  }

  onAgeReferenceChange(checked: boolean) {
    const item = this.editItem();
    if (item && checked) {
      item.isACollection = false;
      item.referencerelatesToGyneco = false;
    }
  }

  onGynecoReferenceChange(checked: boolean) {
    const item = this.editItem();
    if (item && checked) {
      item.isACollection = false;
      item.referenceRelatesToAge = false;
    }
  }

  hasAnyCheckboxSelected(): boolean {
    const item = this.editItem();
    return !!(item && (item.isACollection || item.referenceRelatesToAge || item.referencerelatesToGyneco));
  }

  // Rich text editor methods
  saveSelection() {
    const selection = window.getSelection();
    if (selection && selection.rangeCount > 0) {
      this.savedSelection = selection.getRangeAt(0);
    }
  }

  restoreSelection() {
    const editor = document.querySelector('.rich-text-editor') as HTMLElement;
    if (!editor) return;

    editor.focus();

    if (this.savedSelection) {
      const selection = window.getSelection();
      if (selection) {
        selection.removeAllRanges();
        selection.addRange(this.savedSelection);
      }
    }
  }

  execCommand(command: string, value?: string) {
    const editor = document.querySelector('.rich-text-editor') as HTMLElement;
    if (!editor) return;

    // Restore the saved selection
    this.restoreSelection();

    // Execute the command
    const result = document.execCommand(command, false, value);

    if (!result) {
      console.warn(`Command ${command} failed to execute`);
    }

    // Save selection again for next command
    this.saveSelection();

    // Update the model immediately
    const item = this.editItem();
    if (item) {
      item.defaultTextResult = editor.innerHTML;
    }

    // Update formatting state
    this.updateFormattingState();
  }

  onToolbarMouseDown(event: MouseEvent) {
    // Prevent button from taking focus away from editor
    event.preventDefault();
  }

  formatBlock(event: Event) {
    const select = event.target as HTMLSelectElement;
    const value = select.value;
    if (value) {
      this.execCommand('formatBlock', value);
    }
    select.selectedIndex = 0; // Reset to default
  }

  changeTextColor(event: Event) {
    const input = event.target as HTMLInputElement;
    const color = input.value;

    // Apply to selection or show message
    const selection = window.getSelection();
    const editor = document.querySelector('.rich-text-editor') as HTMLElement;

    if (selection && selection.toString().length > 0) {
      this.execCommand('foreColor', color);
    } else {
      if (editor) {
        editor.focus();
        // Store color for next typing
        editor.style.color = color;
      }
    }
  }

  changeBackgroundColor(event: Event) {
    const input = event.target as HTMLInputElement;
    const color = input.value;

    const selection = window.getSelection();
    const editor = document.querySelector('.rich-text-editor') as HTMLElement;

    if (selection && selection.toString().length > 0) {
      // Use hiliteColor or backColor depending on browser
      let success = document.execCommand('hiliteColor', false, color);
      if (!success) {
        success = document.execCommand('backColor', false, color);
      }

      if (editor) {
        const item = this.editItem();
        if (item) {
          item.defaultTextResult = editor.innerHTML;
        }
      }
    } else {
      if (editor) {
        editor.focus();
      }
    }
  }

  insertLink() {
    // Check if there's a selection
    const selection = window.getSelection();
    if (!selection || selection.toString().length === 0) {
      alert('Please select text first to create a link');
      return;
    }

    const url = prompt('Enter the URL:', 'https://');
    if (url && url.trim()) {
      this.execCommand('createLink', url);
    }
  }

  toggleTablePicker(event?: Event) {
    event?.stopPropagation();
    const newState = !this.showTablePicker();
    this.showTablePicker.set(newState);

    if (newState) {
      this.tablePickerRows.set(3);
      this.tablePickerCols.set(3);

      // Add click outside listener
      setTimeout(() => {
        document.addEventListener('click', this.closeTablePicker);
      }, 0);
    } else {
      document.removeEventListener('click', this.closeTablePicker);
    }
  }

  private closeTablePicker = (event: Event) => {
    const target = event.target as HTMLElement;
    if (!target.closest('.table-picker-dropdown') && !target.closest('.btn-outline-primary')) {
      this.showTablePicker.set(false);
      document.removeEventListener('click', this.closeTablePicker);
    }
  }

  onTableGridHover(row: number, col: number) {
    this.tablePickerRows.set(row + 1);
    this.tablePickerCols.set(col + 1);
  }

  insertTableFromPicker(event?: Event) {
    event?.stopPropagation();

    const numRows = this.tablePickerRows();
    const numCols = this.tablePickerCols();

    // Find the rich text editor element
    const editor = document.querySelector('.rich-text-editor') as HTMLElement;
    if (!editor) return;

    // Focus the editor first
    editor.focus();

    let tableHTML = '<table class="editor-table" border="1" cellpadding="5" cellspacing="0" style="border-collapse: collapse; width: 100%; margin: 10px 0;">';

    // Create header row
    tableHTML += '<thead><tr>';
    for (let j = 0; j < numCols; j++) {
      tableHTML += '<th style="border: 1px solid #dee2e6; padding: 8px; background-color: #f8f9fa; font-weight: 600; text-align: left;">Header ' + (j + 1) + '</th>';
    }
    tableHTML += '</tr></thead>';

    // Create body rows
    tableHTML += '<tbody>';
    for (let i = 0; i < numRows; i++) {
      tableHTML += '<tr>';
      for (let j = 0; j < numCols; j++) {
        tableHTML += '<td style="border: 1px solid #dee2e6; padding: 8px; text-align: left;">Cell</td>';
      }
      tableHTML += '</tr>';
    }
    tableHTML += '</tbody></table><p><br></p>';

    // Insert table at cursor position or at end
    try {
      document.execCommand('insertHTML', false, tableHTML);
    } catch (e) {
      // Fallback: append to editor
      editor.innerHTML += tableHTML;
      // Trigger change event
      const event = new Event('input', { bubbles: true });
      editor.dispatchEvent(event);
    }

    this.showTablePicker.set(false);

    // Update the model
    const item = this.editItem();
    if (item) {
      item.defaultTextResult = editor.innerHTML;
    }
  }

  deleteTable() {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    let node = selection.anchorNode;
    while (node && node.nodeName !== 'TABLE') {
      node = node.parentNode;
    }

    if (node && node.nodeName === 'TABLE') {
      if (confirm('Delete this table?')) {
        node.parentNode?.removeChild(node);
      }
    } else {
      alert('Please place cursor inside a table first');
    }
  }

  insertTableRow() {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    let node = selection.anchorNode;
    let tr: HTMLTableRowElement | null = null;

    // Find the TR element
    while (node) {
      if (node.nodeName === 'TR') {
        tr = node as HTMLTableRowElement;
        break;
      }
      node = node.parentNode;
    }

    if (tr) {
      const newRow = tr.cloneNode(true) as HTMLTableRowElement;

      // Clear cell contents but preserve all formatting
      const cells = newRow.querySelectorAll('td, th');
      cells.forEach(cell => {
        // Preserve all attributes and styles, just clear text content
        cell.textContent = 'Cell';
      });

      // Insert the new row after the current row
      tr.parentNode?.insertBefore(newRow, tr.nextSibling);
    } else {
      alert('Please place cursor inside a table row first');
    }
  }

  deleteTableRow() {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    let node = selection.anchorNode;
    let tr: HTMLTableRowElement | null = null;

    while (node) {
      if (node.nodeName === 'TR') {
        tr = node as HTMLTableRowElement;
        break;
      }
      node = node.parentNode;
    }

    if (tr) {
      if (confirm('Delete this row?')) {
        tr.parentNode?.removeChild(tr);
      }
    } else {
      alert('Please place cursor inside a table row first');
    }
  }

  insertTableColumn() {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    let node = selection.anchorNode;
    let cell: HTMLTableCellElement | null = null;

    while (node) {
      if (node.nodeName === 'TD' || node.nodeName === 'TH') {
        cell = node as HTMLTableCellElement;
        break;
      }
      node = node.parentNode;
    }

    if (cell) {
      const table = cell.closest('table');
      if (!table) return;

      const cellIndex = cell.cellIndex;
      const rows = table.querySelectorAll('tr');

      rows.forEach(row => {
        const existingCell = row.cells[cellIndex] as HTMLElement;
        const newCell = document.createElement(existingCell.nodeName.toLowerCase()) as HTMLTableCellElement;

        // Copy all inline styles from the existing cell
        newCell.style.cssText = existingCell.style.cssText;

        // Copy computed styles if inline styles are not present
        if (!existingCell.style.border) {
          const computed = window.getComputedStyle(existingCell);
          newCell.style.border = computed.border;
          newCell.style.padding = computed.padding;
          newCell.style.backgroundColor = computed.backgroundColor;
          newCell.style.color = computed.color;
          newCell.style.fontWeight = computed.fontWeight;
        }

        newCell.textContent = 'Cell';

        if (cellIndex < row.cells.length - 1) {
          row.insertBefore(newCell, row.cells[cellIndex + 1]);
        } else {
          row.appendChild(newCell);
        }
      });
    } else {
      alert('Please place cursor inside a table cell first');
    }
  }

  deleteTableColumn() {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    let node = selection.anchorNode;
    let cell: HTMLTableCellElement | null = null;

    while (node) {
      if (node.nodeName === 'TD' || node.nodeName === 'TH') {
        cell = node as HTMLTableCellElement;
        break;
      }
      node = node.parentNode;
    }

    if (cell) {
      const table = cell.closest('table');
      if (!table) return;

      const cellIndex = cell.cellIndex;
      const rows = table.querySelectorAll('tr');

      // Check if table has more than one column
      if (rows[0]?.cells.length <= 1) {
        alert('Cannot delete the last column');
        return;
      }

      if (confirm('Delete this column?')) {
        rows.forEach(row => {
          if (row.cells[cellIndex]) {
            row.deleteCell(cellIndex);
          }
        });
      }
    } else {
      alert('Please place cursor inside a table cell first');
    }
  }

  toggleTableBorders() {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    let node = selection.anchorNode;
    while (node && node.nodeName !== 'TABLE') {
      node = node.parentNode;
    }

    if (node && node.nodeName === 'TABLE') {
      const table = node as HTMLTableElement;
      const cells = table.querySelectorAll('th, td');
      const currentBorder = (cells[0] as HTMLElement).style.border;

      // Toggle between border styles
      if (currentBorder && currentBorder !== 'none' && currentBorder !== '0px') {
        // Remove borders
        cells.forEach(cell => {
          (cell as HTMLElement).style.border = 'none';
        });
        table.style.border = 'none';
      } else {
        // Add borders
        cells.forEach(cell => {
          (cell as HTMLElement).style.border = '1px solid #dee2e6';
        });
        table.style.border = '1px solid #dee2e6';
      }
    } else {
      alert('Please place cursor inside a table first');
    }
  }

  applyTableBorderStyle(style: string) {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    let node = selection.anchorNode;
    while (node && node.nodeName !== 'TABLE') {
      node = node.parentNode;
    }

    if (node && node.nodeName === 'TABLE') {
      const table = node as HTMLTableElement;
      const cells = table.querySelectorAll('th, td');

      switch(style) {
        case 'all':
          cells.forEach(cell => {
            (cell as HTMLElement).style.border = '1px solid #dee2e6';
          });
          table.style.border = '1px solid #dee2e6';
          break;
        case 'outer':
          cells.forEach(cell => {
            (cell as HTMLElement).style.border = 'none';
          });
          table.style.border = '2px solid #495057';
          break;
        case 'horizontal':
          cells.forEach(cell => {
            (cell as HTMLElement).style.border = 'none';
            (cell as HTMLElement).style.borderTop = '1px solid #dee2e6';
            (cell as HTMLElement).style.borderBottom = '1px solid #dee2e6';
          });
          table.style.border = 'none';
          break;
        case 'none':
          cells.forEach(cell => {
            (cell as HTMLElement).style.border = 'none';
          });
          table.style.border = 'none';
          break;
      }
    } else {
      alert('Please place cursor inside a table first');
    }
  }

  changeTableBorderColor(event: Event) {
    const input = event.target as HTMLInputElement;
    const color = input.value;

    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    let node = selection.anchorNode;
    while (node && node.nodeName !== 'TABLE') {
      node = node.parentNode;
    }

    if (node && node.nodeName === 'TABLE') {
      const table = node as HTMLTableElement;
      const cells = table.querySelectorAll('th, td');

      cells.forEach(cell => {
        const cellElement = cell as HTMLElement;
        if (cellElement.style.border && cellElement.style.border !== 'none') {
          cellElement.style.borderColor = color;
        }
      });

      if (table.style.border && table.style.border !== 'none') {
        table.style.borderColor = color;
      }
    }
  }

  onRichTextFocus(event: Event) {
    const target = event.target as HTMLElement;
    const item = this.editItem();

    // Load content when editor gains focus
    if (item && item.defaultTextResult) {
      target.innerHTML = item.defaultTextResult;
    }

    // Update formatting indicators
    this.updateFormattingState();
  }

  onRichTextChange(event: Event) {
    const target = event.target as HTMLElement;
    const item = this.editItem();
    console.log('Rich text change event fired');
    console.log('Target innerHTML:', target.innerHTML);
    if (item) {
      const content = target.innerHTML;
      // Clean empty content
      item.defaultTextResult = content === '<br>' || content.trim() === '' ? '' : content;
      console.log('Updated item.defaultTextResult:', item.defaultTextResult);
    }

    // Update formatting indicators
    this.updateFormattingState();
  }

  updateFormattingState() {
    const formats = new Set<string>();

    try {
      if (document.queryCommandState('bold')) formats.add('bold');
      if (document.queryCommandState('italic')) formats.add('italic');
      if (document.queryCommandState('underline')) formats.add('underline');
      if (document.queryCommandState('strikeThrough')) formats.add('strikeThrough');
      if (document.queryCommandState('insertUnorderedList')) formats.add('ul');
      if (document.queryCommandState('insertOrderedList')) formats.add('ol');
      if (document.queryCommandState('justifyLeft')) formats.add('justifyLeft');
      if (document.queryCommandState('justifyCenter')) formats.add('justifyCenter');
      if (document.queryCommandState('justifyRight')) formats.add('justifyRight');
    } catch (e) {
      // queryCommandState may fail in some browsers
    }

    this.activeFormats.set(formats);
  }

  isFormatActive(format: string): boolean {
    return this.activeFormats().has(format);
  }

  onRichTextBlur(event: Event) {
    const target = event.target as HTMLElement;
    const item = this.editItem();
    if (item) {
      const content = target.innerHTML;
      // Clean empty content
      item.defaultTextResult = content === '<br>' || content.trim() === '' ? '' : content;
      console.log('Rich text blur - content saved:', item.defaultTextResult);
    }
  }

  // Column width persistence
  private loadColumnWidths(): number[] {
    try {
      const saved = localStorage.getItem(this.STORAGE_KEY);
      if (saved) {
        const parsed = JSON.parse(saved);
        if (Array.isArray(parsed) && parsed.length === this.DEFAULT_COLUMN_WIDTHS.length) {
          return parsed;
        }
      }
    } catch (error) {
      console.warn('Failed to load column widths from localStorage:', error);
    }
    return [...this.DEFAULT_COLUMN_WIDTHS];
  }

  private saveColumnWidths(widths: number[]) {
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(widths));
    } catch (error) {
      console.warn('Failed to save column widths to localStorage:', error);
    }
  }

  resetColumnWidths() {
    this.columnWidths.set([...this.DEFAULT_COLUMN_WIDTHS]);
    this.saveColumnWidths(this.DEFAULT_COLUMN_WIDTHS);
  }

  // Column resizing methods
  onResizeStart(event: MouseEvent, columnIndex: number) {
    event.preventDefault();
    event.stopPropagation();

    const th = (event.target as HTMLElement).parentElement as HTMLElement;
    if (!th) return;

    this.resizingColumn = {
      index: columnIndex,
      startX: event.pageX,
      startWidth: th.offsetWidth,
      headerElement: th
    };

    this.isResizing.set(true);
    th.classList.add('resizing');

    document.addEventListener('mousemove', this.onResize);
    document.addEventListener('mouseup', this.onResizeEnd);

    // Prevent text selection and add visual feedback
    document.body.classList.add('col-resizing');
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  private onResize = (event: MouseEvent) => {
    if (!this.resizingColumn) return;

    event.preventDefault();

    const diff = event.pageX - this.resizingColumn.startX;
    const newWidth = Math.max(
      this.MIN_COLUMN_WIDTH,
      this.resizingColumn.startWidth + diff
    );

    const widths = [...this.columnWidths()];
    widths[this.resizingColumn.index] = newWidth;
    this.columnWidths.set(widths);
  }

  private onResizeEnd = () => {
    if (this.resizingColumn) {
      this.resizingColumn.headerElement.classList.remove('resizing');
      this.saveColumnWidths(this.columnWidths());
    }

    this.resizingColumn = null;
    this.isResizing.set(false);

    document.removeEventListener('mousemove', this.onResize);
    document.removeEventListener('mouseup', this.onResizeEnd);

    document.body.classList.remove('col-resizing');
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }

  onColumnDoubleClick(columnIndex: number) {
    // Auto-fit column width based on content
    const widths = [...this.columnWidths()];
    widths[columnIndex] = this.DEFAULT_COLUMN_WIDTHS[columnIndex];
    this.columnWidths.set(widths);
    this.saveColumnWidths(widths);
  }

  // Sorting methods
  sortBy(column: string, event?: MouseEvent) {
    // Prevent sorting when clicking on resize handle
    if (event && (event.target as HTMLElement).classList.contains('resize-handle')) {
      return;
    }

    const currentColumn = this.sortColumn();
    const currentDirection = this.sortDirection();

    if (currentColumn === column) {
      // Toggle direction or clear sort
      if (currentDirection === 'asc') {
        this.sortDirection.set('desc');
      } else {
        // Clear sort on third click
        this.sortColumn.set(null);
        this.sortDirection.set('asc');
      }
    } else {
      // New column, sort ascending
      this.sortColumn.set(column);
      this.sortDirection.set('asc');
    }

    // Reset to first page when sorting changes
    this.pageIndex.set(0);
  }

  getSortIcon(column: string): string {
    if (this.sortColumn() !== column) {
      return 'bi-arrow-down-up'; // Neutral/sortable indicator
    }
    return this.sortDirection() === 'asc' ? 'bi-sort-up' : 'bi-sort-down';
  }

  isSorted(column: string): boolean {
    return this.sortColumn() === column;
  }

  ngOnDestroy() {
    // Clean up event listeners
    document.removeEventListener('mousemove', this.onResize);
    document.removeEventListener('mouseup', this.onResizeEnd);
    document.removeEventListener('click', this.closeTablePicker);
    document.body.classList.remove('col-resizing');
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }
}


