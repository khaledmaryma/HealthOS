import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GermsService, Germs, CreateGermRequest, UpdateGermRequest } from '../services/germs.service';

@Component({
  selector: 'app-germs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './germs.component.html',
  styleUrls: ['./germs.component.scss']
})
export class GermsComponent implements OnInit {
  private svc = inject(GermsService);

  readonly items = this.svc.items;
  readonly loading = this.svc.loading;
  readonly error = this.svc.error;
  readonly query = signal('');
  readonly showForm = signal(false);
  readonly editItem = signal<Germs | null>(null);
  readonly selectedItem = signal<Germs | null>(null);

  readonly pageSizeOptions = [10, 20, 50, 75, 100];
  readonly pageSize = signal(20);
  readonly pageIndex = signal(0);

  // Sorting state
  readonly sortColumn = signal<string | null>(null);
  readonly sortDirection = signal<'asc' | 'desc'>('asc');

  readonly filtered = computed(() => {
    const q = this.query().toLowerCase().trim();
    let results = !q ? this.items() : this.items().filter(g =>
      (g.code ?? '').toLowerCase().includes(q) ||
      (g.description ?? '').toLowerCase().includes(q) ||
      (g.identifier ?? '').toLowerCase().includes(q)
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
            aVal = a.description ?? '';
            bVal = b.description ?? '';
            break;
          case 'identifier':
            aVal = a.identifier ?? '';
            bVal = b.identifier ?? '';
            break;
          case 'displayOrder':
            aVal = parseInt(a.displayOrder ?? '0');
            bVal = parseInt(b.displayOrder ?? '0');
            break;
          default:
            return 0;
        }

        // Handle string comparison
        if (typeof aVal === 'string' && typeof bVal === 'string') {
          const comparison = aVal.localeCompare(bVal, undefined, { numeric: true, sensitivity: 'base' });
          return dir === 'asc' ? comparison : -comparison;
        }

        // Handle numeric comparison
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

  ngOnInit() {
    this.svc.load();
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

  selectRow(item: Germs) {
    this.selectedItem.set(item);
  }

  isSelected(item: Germs): boolean {
    return this.selectedItem()?.id === item.id;
  }

  clickAdd() {
    const newItem: Germs = {
      id: 0,
      code: '',
      description: '',
      identifier: '',
      displayOrder: '0',
      createdBy: undefined,
      createdDate: undefined,
      modifiedBy: undefined,
      modifiedDate: undefined,
      isDeleted: false
    };

    this.editItem.set(newItem);
    this.showForm.set(true);
  }

  clickEdit() {
    const selected = this.selectedItem();
    if (!selected) return;

    this.editItem.set({ ...selected });
    this.showForm.set(true);
  }

  cancel() {
    this.showForm.set(false);
    this.editItem.set(null);
  }

  save(event?: Event) {
    event?.preventDefault();

    const model = this.editItem()!;

    if (model.id && model.id > 0) {
      // Update existing germ
      const updateRequest: UpdateGermRequest = {
        code: model.code,
        description: model.description,
        identifier: model.identifier,
        displayOrder: model.displayOrder,
        modifiedBy: 1 // TODO: Get from current user context
      };

      this.svc.update(model.id, updateRequest).subscribe({
        next: () => {
          this.svc.load();
          this.cancel();
          console.log('Germ updated successfully');
        },
        error: (err) => {
          console.error('Error updating germ:', err);
          alert('Failed to save germ. Check console for details.');
        }
      });
    } else {
      // Create new germ
      const createRequest: CreateGermRequest = {
        code: model.code,
        description: model.description,
        identifier: model.identifier,
        displayOrder: model.displayOrder,
        createdBy: 1 // TODO: Get from current user context
      };

      this.svc.create(createRequest).subscribe({
        next: () => {
          this.svc.load();
          this.cancel();
          console.log('Germ created successfully');
        },
        error: (err) => {
          console.error('Error creating germ:', err);
          alert('Failed to create germ. Check console for details.');
        }
      });
    }
  }

  remove() {
    const selected = this.selectedItem();
    if (!selected) return;
    if (!confirm(`Delete germ "${selected.description}"?`)) return;

    this.svc.delete(selected.id).subscribe({
      next: () => {
        this.selectedItem.set(null);
        this.svc.load();
        console.log('Germ deleted successfully');
      },
      error: (err) => {
        console.error('Error deleting germ:', err);
        alert('Failed to delete germ. Check console for details.');
      }
    });
  }

  // Sorting methods
  sortBy(column: string) {
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

  trackByFn(index: number, item: Germs): number {
    return item.id;
  }

  // Helper method for Math.min
  min(a: number, b: number): number {
    return Math.min(a, b);
  }
}
