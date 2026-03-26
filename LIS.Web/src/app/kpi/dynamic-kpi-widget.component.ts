import { Component, input, output, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KpiQueryApiService } from '../services/kpi-query-api.service';
import { KpiDisplayMode, KpiQueryResult, UserKpiDefinition } from '../models/user-kpi';
import { KpiResultViewComponent } from './kpi-result-view.component';

@Component({
  selector: 'app-dynamic-kpi-widget',
  standalone: true,
  imports: [CommonModule, KpiResultViewComponent],
  template: `
    <div class="card h-100 shadow-sm border">
      <div class="card-header py-2 d-flex justify-content-between align-items-center gap-2">
        <span class="fw-semibold text-truncate" [title]="kpi().title">{{ kpi().title }}</span>
        <div class="btn-group btn-group-sm flex-shrink-0">
          <button
            type="button"
            class="btn btn-outline-secondary"
            (click)="refresh()"
            [disabled]="loading()"
            title="Refresh"
          >
            ↻
          </button>
          <button type="button" class="btn btn-outline-secondary" (click)="edit.emit(kpi())" title="Edit">
            ✎
          </button>
          <button type="button" class="btn btn-outline-danger" (click)="remove()" title="Remove">×</button>
        </div>
      </div>
      <div class="card-body pt-2">
        @if (loading()) {
          <div class="text-muted small py-3 text-center">Loading…</div>
        } @else if (error()) {
          <div class="alert alert-danger py-2 small mb-0">{{ error() }}</div>
        } @else if (result()) {
          <app-kpi-result-view
            [result]="result()!"
            [viewMode]="viewModeKind"
            [chartOptionsJson]="kpi().chartOptionsJson"
            [gridShowTotalsDefault]="kpi().gridShowTotals"
          />
        } @else {
          <div class="text-muted small">No rows returned.</div>
        }
      </div>
    </div>
  `,
})
export class DynamicKpiWidgetComponent implements OnInit {
  private readonly api = inject(KpiQueryApiService);

  readonly kpi = input.required<UserKpiDefinition>();
  readonly edit = output<UserKpiDefinition>();
  readonly deleted = output<number>();

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly result = signal<KpiQueryResult | null>(null);

  protected get viewModeKind(): 'chart' | 'grid' {
    return this.kpi().displayMode === KpiDisplayMode.Chart ? 'chart' : 'grid';
  }

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    const k = this.kpi();
    const userId = Number(localStorage.getItem('loggedInUserId') || '0');
    if (!userId) {
      this.error.set('Not logged in.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.api.execute({ userId, kpiId: k.id }).subscribe({
      next: res => {
        this.result.set(res);
        this.loading.set(false);
      },
      error: err => {
        this.loading.set(false);
        const msg = err?.error;
        this.error.set(typeof msg === 'string' ? msg : msg?.message ?? err?.message ?? 'Query failed.');
      },
    });
  }

  remove(): void {
    if (!confirm(`Remove KPI "${this.kpi().title}"?`)) {
      return;
    }
    const userId = Number(localStorage.getItem('loggedInUserId') || '0');
    this.api.delete(this.kpi().id, userId).subscribe({
      next: () => this.deleted.emit(this.kpi().id),
      error: err => alert(err?.message ?? 'Delete failed.'),
    });
  }
}
