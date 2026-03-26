import { Component, input, output, effect, signal, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KpiQueryApiService } from '../services/kpi-query-api.service';
import { KpiDataTransformService } from '../services/kpi-data-transform.service';
import {
  DEFAULT_KPI_HOME_PAGE_ID,
  KpiChartType,
  KpiDisplayMode,
  KpiQueryResult,
  UserKpiDefinition,
} from '../models/user-kpi';
import type { KpiAppKey } from '../models/user-kpi';

@Component({
  selector: 'app-kpi-widget-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (open()) {
      <div
        class="position-fixed top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center p-2 p-md-3"
        style="z-index: 1050; background: rgba(0,0,0,.45);"
        (click)="onBackdrop($event)"
      >
        <div class="card shadow-lg" style="max-width: 720px; width: 100%; max-height: 92vh; overflow: auto;" (click)="$event.stopPropagation()">
          <div class="card-header d-flex justify-content-between align-items-center py-2">
            <span class="fw-semibold">{{ existingId() ? 'Edit KPI' : 'New KPI' }}</span>
            <button type="button" class="btn-close" aria-label="Close" (click)="close.emit()"></button>
          </div>
          <div class="card-body">
            <div class="mb-2">
              <label class="form-label small mb-0">Title</label>
              <input class="form-control form-control-sm" [(ngModel)]="titleModel" name="kpiTitle" />
            </div>
            <div class="mb-2">
              <label class="form-label small mb-0">SQL (read-only SELECT / WITH)</label>
              <textarea
                class="form-control font-monospace"
                rows="6"
                [(ngModel)]="sqlModel"
                name="kpiSql"
                spellcheck="false"
              ></textarea>
            </div>
            <div class="row g-2 mb-2">
              <div class="col-md-6">
                <label class="form-label small mb-0">Display</label>
                <select class="form-select form-select-sm" [(ngModel)]="displayModeModel" name="disp">
                  <option [ngValue]="displayChart">Chart</option>
                  <option [ngValue]="displayGrid">Grid</option>
                </select>
              </div>
              <div class="col-md-6">
                <label class="form-label small mb-0">Sort order</label>
                <input class="form-control form-control-sm" type="number" [(ngModel)]="sortOrderModel" name="sort" />
              </div>
            </div>
            @if (displayModeModel === displayGrid) {
              <div class="form-check mb-2">
                <input
                  class="form-check-input"
                  type="checkbox"
                  id="gt"
                  [(ngModel)]="gridTotalsModel"
                  name="gridTotals"
                />
                <label class="form-check-label small" for="gt">Show totals by default (grid)</label>
              </div>
            }
            @if (displayModeModel === displayChart) {
            <div class="border rounded p-2 mb-2 bg-light">
              <div class="small text-muted mb-1">Chart options (optional)</div>
              <div class="row g-2">
                <div class="col-12 col-md-5">
                  <label class="form-label small mb-0">Chart type</label>
                  <select class="form-select form-select-sm" [(ngModel)]="chartTypeModel" name="ct">
                    <option value="bar">Bar</option>
                    <option value="line">Line</option>
                    <option value="pie">Pie</option>
                    <option value="doughnut">Donut</option>
                    <option value="radar">Radar</option>
                    <option value="polarArea">Polar area</option>
                  </select>
                </div>
                <div class="col-6 col-md-3">
                  <label class="form-label small mb-0">Label col #</label>
                  <input
                    class="form-control form-control-sm"
                    type="number"
                    min="0"
                    [(ngModel)]="labelColModel"
                    name="lc"
                    placeholder="auto"
                  />
                </div>
                <div class="col-6 col-md-3">
                  <label class="form-label small mb-0">Value col #</label>
                  <input
                    class="form-control form-control-sm"
                    type="number"
                    min="0"
                    [(ngModel)]="valueColModel"
                    name="vc"
                    placeholder="auto"
                  />
                </div>
              </div>
            </div>
            }
            <div class="d-flex flex-wrap gap-2 mb-2">
              <button type="button" class="btn btn-outline-secondary btn-sm" (click)="runPreview()" [disabled]="previewLoading()">
                {{ previewLoading() ? 'Running…' : 'Run preview' }}
              </button>
            </div>
            @if (previewError()) {
              <div class="alert alert-warning small py-2">{{ previewError() }}</div>
            }
            @if (previewResult()) {
              <div class="small text-muted mb-1">Preview (first rows)</div>
              <div class="table-responsive" style="max-height: 160px; overflow: auto;">
                <table class="table table-sm table-bordered mb-0">
                  <thead>
                    <tr>
                      @for (c of previewResult()!.columns; track c) {
                        <th>{{ c }}</th>
                      }
                    </tr>
                  </thead>
                  <tbody>
                    @for (row of previewResult()!.rows.slice(0, 8); track $index) {
                      <tr>
                        @for (c of previewResult()!.columns; track c) {
                          <td>{{ row[c] }}</td>
                        }
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </div>
          <div class="card-footer d-flex justify-content-end gap-2 py-2">
            <button type="button" class="btn btn-outline-secondary btn-sm" (click)="close.emit()">Cancel</button>
            <button type="button" class="btn btn-primary btn-sm" (click)="save()" [disabled]="saving()">
              {{ saving() ? 'Saving…' : 'Save' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class KpiWidgetEditorComponent {
  readonly open = input(false);
  readonly kpi = input<UserKpiDefinition | null>(null);
  readonly appKey = input.required<KpiAppKey>();
  readonly close = output<void>();
  readonly saved = output<void>();

  constructor(
    private readonly api: KpiQueryApiService,
    private readonly transform: KpiDataTransformService
  ) {
    effect(() => {
      const k = this.kpi();
      const isOpen = this.open();
      if (!isOpen) {
        return;
      }
      untracked(() => {
        if (k) {
          this.existingId.set(k.id);
          this.titleModel = k.title;
          this.sqlModel = k.sqlQuery;
          this.displayModeModel = k.displayMode;
          this.gridTotalsModel = k.gridShowTotals;
          this.sortOrderModel = k.sortOrder;
          this.parseChartOpts(k.chartOptionsJson);
        } else {
          this.existingId.set(null);
          this.titleModel = 'New KPI';
          this.sqlModel = 'SELECT 1 AS sample';
          this.displayModeModel = KpiDisplayMode.Grid;
          this.gridTotalsModel = true;
          this.sortOrderModel = 0;
          this.chartTypeModel = 'bar';
          this.labelColModel = null;
          this.valueColModel = null;
        }
        this.previewResult.set(null);
        this.previewError.set(null);
      });
    });
  }

  protected readonly existingId = signal<number | null>(null);
  protected readonly displayChart = KpiDisplayMode.Chart;
  protected readonly displayGrid = KpiDisplayMode.Grid;

  titleModel = '';
  sqlModel = '';
  displayModeModel: KpiDisplayMode = KpiDisplayMode.Grid;
  gridTotalsModel = true;
  sortOrderModel = 0;
  chartTypeModel: KpiChartType = 'bar';
  labelColModel: number | null = null;
  valueColModel: number | null = null;

  protected readonly previewLoading = signal(false);
  protected readonly previewError = signal<string | null>(null);
  protected readonly previewResult = signal<KpiQueryResult | null>(null);
  protected readonly saving = signal(false);

  onBackdrop(ev: MouseEvent): void {
    if (ev.target === ev.currentTarget) {
      this.close.emit();
    }
  }

  private parseChartOpts(json: string | null): void {
    const o = this.transform.parseChartOptionsJson(json);
    this.chartTypeModel = o.chartType;
    this.labelColModel = o.labelColumnIndex ?? null;
    this.valueColModel = o.valueColumnIndex ?? null;
  }

  private buildChartOptionsJson(): string | null {
    if (this.displayModeModel !== KpiDisplayMode.Chart) {
      return null;
    }
    const o: Record<string, unknown> = { chartType: this.chartTypeModel };
    if (this.labelColModel != null && this.labelColModel >= 0) {
      o['labelColumnIndex'] = this.labelColModel;
    }
    if (this.valueColModel != null && this.valueColModel >= 0) {
      o['valueColumnIndex'] = this.valueColModel;
    }
    return JSON.stringify(o);
  }

  runPreview(): void {
    const userId = Number(localStorage.getItem('loggedInUserId') || '0');
    if (!userId) {
      this.previewError.set('Not logged in.');
      return;
    }
    this.previewLoading.set(true);
    this.previewError.set(null);
    this.previewResult.set(null);
    this.api.execute({ userId, sql: this.sqlModel }).subscribe({
      next: res => {
        this.previewResult.set(res);
        this.previewLoading.set(false);
      },
      error: err => {
        this.previewLoading.set(false);
        const msg = err?.error;
        this.previewError.set(typeof msg === 'string' ? msg : msg?.message ?? err?.message ?? 'Failed.');
      },
    });
  }

  save(): void {
    const userId = Number(localStorage.getItem('loggedInUserId') || '0');
    if (!userId) {
      alert('Not logged in.');
      return;
    }
    const body = {
      userId,
      appKey: this.appKey(),
      homePageId: DEFAULT_KPI_HOME_PAGE_ID,
      title: this.titleModel.trim() || 'Untitled',
      sqlQuery: this.sqlModel,
      displayMode: this.displayModeModel,
      gridShowTotals: this.gridTotalsModel,
      chartOptionsJson: this.buildChartOptionsJson(),
      sortOrder: Number(this.sortOrderModel) || 0,
    };
    this.saving.set(true);
    const id = this.existingId();
    const req = id
      ? this.api.update(id, { ...body, userId })
      : this.api.create(body);
    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.emit();
      },
      error: err => {
        this.saving.set(false);
        alert(err?.error ?? err?.message ?? 'Save failed.');
      },
    });
  }
}
