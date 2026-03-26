import { Component, inject, input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KpiQueryApiService } from '../services/kpi-query-api.service';
import { DEFAULT_KPI_HOME_PAGE_ID, UserKpiDefinition } from '../models/user-kpi';
import type { KpiAppKey } from '../models/user-kpi';
import { DynamicKpiWidgetComponent } from './dynamic-kpi-widget.component';
import { KpiWidgetEditorComponent } from './kpi-widget-editor.component';

@Component({
  selector: 'app-kpi-home-section',
  standalone: true,
  imports: [CommonModule, DynamicKpiWidgetComponent, KpiWidgetEditorComponent],
  template: `
    <div class="card shadow-sm h-100">
      <div class="card-header py-2 d-flex flex-wrap justify-content-between align-items-center gap-2">
        <span class="text-muted small mb-0">KPIs & dashboards</span>
        <button type="button" class="btn btn-sm btn-primary" (click)="openNew()" [disabled]="!userId()">
          + Add KPI
        </button>
      </div>
      <div class="card-body">
        @if (!userId()) {
          <div class="alert alert-secondary small mb-0">Sign in to load and save KPIs.</div>
        } @else if (loadError()) {
          <div class="alert alert-warning small mb-0">{{ loadError() }}</div>
        } @else if (loading()) {
          <div class="text-muted small">Loading KPIs…</div>
        } @else if (!kpis().length) {
          <div class="text-muted small mb-0">
            No KPI widgets yet. Add a SQL query and choose chart or grid — definitions are saved per user and app.
          </div>
        } @else {
          <div class="row g-3">
            @for (k of kpis(); track k.id) {
              <div class="col-12 col-xl-6">
                <app-dynamic-kpi-widget [kpi]="k" (edit)="openEdit($event)" (deleted)="onDeleted($event)" />
              </div>
            }
          </div>
        }
      </div>
    </div>

    <app-kpi-widget-editor
      [open]="editorOpen()"
      [kpi]="editorKpi()"
      [appKey]="appKey()"
      (close)="editorOpen.set(false)"
      (saved)="onSaved()"
    />
  `,
})
export class KpiHomeSectionComponent implements OnInit {
  private readonly api = inject(KpiQueryApiService);

  /** Route `data.appKey` */
  readonly appKey = input.required<KpiAppKey>();

  protected readonly kpis = signal<UserKpiDefinition[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly editorOpen = signal(false);
  protected readonly editorKpi = signal<UserKpiDefinition | null>(null);

  protected readonly userId = () => Number(localStorage.getItem('loggedInUserId') || '0');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const uid = this.userId();
    if (!uid) {
      return;
    }
    this.loading.set(true);
    this.loadError.set(null);
    this.api.list(uid, this.appKey(), DEFAULT_KPI_HOME_PAGE_ID).subscribe({
      next: rows => {
        this.kpis.set(rows);
        this.loading.set(false);
      },
      error: err => {
        this.loading.set(false);
        const msg = err?.error;
        const text =
          typeof msg === 'string'
            ? msg
            : (msg && JSON.stringify(msg)) || err?.message || 'Failed to load KPIs.';
        this.loadError.set(text);
      },
    });
  }

  openNew(): void {
    this.editorKpi.set(null);
    this.editorOpen.set(true);
  }

  openEdit(k: UserKpiDefinition): void {
    this.editorKpi.set(k);
    this.editorOpen.set(true);
  }

  onSaved(): void {
    this.editorOpen.set(false);
    this.load();
  }

  onDeleted(id: number): void {
    this.kpis.update(list => list.filter(k => k.id !== id));
  }
}
