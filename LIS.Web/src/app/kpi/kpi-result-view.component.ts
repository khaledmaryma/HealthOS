import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import ChartDataLabels from 'chartjs-plugin-datalabels';
import { KpiDataTransformService } from '../services/kpi-data-transform.service';
import { KpiChartType, KpiQueryResult } from '../models/user-kpi';

Chart.register(...registerables, ChartDataLabels);

/**
 * Presentation layer: renders tabular {@link KpiQueryResult} as chart or grid.
 * Data shaping is delegated to {@link KpiDataTransformService}.
 */
@Component({
  selector: 'app-kpi-result-view',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (!result) {
      <div class="text-muted small">No data.</div>
    } @else if (viewMode === 'chart') {
      <div class="kpi-chart-wrap">
        <canvas #chartCanvas></canvas>
      </div>
    } @else {
      <div class="d-flex justify-content-end mb-2">
        <div class="btn-group btn-group-sm" role="group">
          <button
            type="button"
            class="btn"
            [class.btn-primary]="totalsVisible()"
            [class.btn-outline-secondary]="!totalsVisible()"
            (click)="totalsVisible.set(true)"
          >
            Totals on
          </button>
          <button
            type="button"
            class="btn"
            [class.btn-primary]="!totalsVisible()"
            [class.btn-outline-secondary]="totalsVisible()"
            (click)="totalsVisible.set(false)"
          >
            Totals off
          </button>
        </div>
      </div>
      <div class="table-responsive kpi-grid-scroll">
        <table class="table table-sm table-striped table-bordered align-middle mb-0">
          <thead class="table-light">
            <tr>
              @for (c of result.columns; track c) {
                <th scope="col">{{ c }}</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (row of result.rows; track $index) {
              <tr>
                @for (c of result.columns; track c) {
                  <td>{{ formatCell(row[c]) }}</td>
                }
              </tr>
            }
          </tbody>
          @if (totalsVisible() && totalsRow()) {
            <tfoot class="table-secondary fw-semibold">
              <tr>
                @for (c of result.columns; track c) {
                  <td>{{ formatTotal(totalsRow()![c]) }}</td>
                }
              </tr>
            </tfoot>
          }
        </table>
      </div>
    }
  `,
  styles: [
    `
      .kpi-chart-wrap {
        position: relative;
        min-height: 220px;
        max-height: 360px;
      }
      .kpi-grid-scroll {
        max-height: 320px;
        overflow: auto;
      }
    `,
  ],
})
export class KpiResultViewComponent implements OnChanges, AfterViewInit, OnDestroy {
  @Input() result: KpiQueryResult | null = null;
  @Input() viewMode: 'chart' | 'grid' = 'grid';
  @Input() chartOptionsJson: string | null = null;
  @Input() gridShowTotalsDefault = true;

  @ViewChild('chartCanvas') private chartCanvas?: ElementRef<HTMLCanvasElement>;

  protected readonly totalsVisible = signal(true);
  protected totalsRow = signal<Record<string, number | null> | null>(null);

  private chart?: Chart;
  private viewReady = false;

  constructor(private readonly transform: KpiDataTransformService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['gridShowTotalsDefault']) {
      this.totalsVisible.set(this.gridShowTotalsDefault);
    }
    if (this.result?.columns?.length) {
      this.totalsRow.set(this.transform.computeColumnTotals(this.result.columns, this.result.rows));
    } else {
      this.totalsRow.set(null);
    }
    this.tryRenderChart();
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.tryRenderChart();
  }

  ngOnDestroy(): void {
    this.destroyChart();
  }

  formatCell(v: unknown): string {
    if (v == null) {
      return '';
    }
    if (typeof v === 'object') {
      return JSON.stringify(v);
    }
    return String(v);
  }

  formatTotal(v: number | null | undefined): string {
    if (v == null) {
      return '—';
    }
    return Number.isInteger(v) ? String(v) : v.toFixed(2);
  }

  private tryRenderChart(): void {
    if (!this.viewReady || this.viewMode !== 'chart' || !this.result?.rows?.length) {
      this.destroyChart();
      return;
    }
    queueMicrotask(() => {
      const canvas = this.chartCanvas?.nativeElement;
      if (!canvas) {
        return;
      }
      this.destroyChart();
      this.renderChart(canvas);
    });
  }

  private renderChart(canvas: HTMLCanvasElement): void {
    const res = this.result;
    if (!res) {
      return;
    }

    const opts = this.transform.parseChartOptionsJson(this.chartOptionsJson);
    const series = this.transform.buildChartSeries(res, opts);
    if (!series) {
      return;
    }

    const ctx = canvas.getContext('2d');
    if (!ctx) {
      return;
    }

    const t = opts.chartType;
    const cfg = this.buildChartConfig(t, series.labels, series.values);
    this.chart = new Chart(ctx, cfg);
  }

  private formatValueLabel(v: number): string {
    if (!Number.isFinite(v)) {
      return '';
    }
    if (Number.isInteger(v)) {
      return String(v);
    }
    return Math.abs(v) >= 1000 ? v.toFixed(1) : v.toFixed(2);
  }

  private buildChartConfig(
    chartType: KpiChartType,
    labels: string[],
    values: number[]
  ): ChartConfiguration {
    const primary = 'rgba(13, 110, 253, 0.9)';
    const primaryFill = 'rgba(13, 110, 253, 0.15)';
    const primaryBar = 'rgba(13, 110, 253, 0.45)';

    const fmt = (value: unknown) => this.formatValueLabel(Number(value));

    /** Legend docked on the right; items stack vertically (one row per category). */
    const legendRight = {
      display: true,
      position: 'right' as const,
      align: 'center' as const,
      labels: {
        padding: 10,
        boxWidth: 14,
        boxHeight: 14,
        usePointStyle: true,
      },
    };

    /** Labels above bars / lines (outside the bar). */
    const barLineDatalabels = {
      display: true,
      anchor: 'end' as const,
      align: 'top' as const,
      offset: 4,
      color: '#212529',
      font: { weight: 'bold' as const, size: 11 },
      formatter: fmt,
    };

    if (chartType === 'pie' || chartType === 'doughnut' || chartType === 'polarArea') {
      const colors = this.transform.segmentBackgroundColors(values.length);
      return {
        type: chartType,
        data: {
          labels,
          datasets: [
            {
              data: values,
              backgroundColor: colors,
              borderColor: '#ffffff',
              borderWidth: 2,
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: legendRight,
            datalabels: {
              display: true,
              anchor: 'center',
              align: 'center',
              color: '#ffffff',
              font: { weight: 'bold' as const, size: 12 },
              textStrokeColor: 'rgba(0,0,0,0.35)',
              textStrokeWidth: 2,
              formatter: fmt,
            },
          },
        },
      };
    }

    if (chartType === 'radar') {
      return {
        type: 'radar',
        data: {
          labels,
          datasets: [
            {
              label: 'Value',
              data: values,
              borderColor: primary,
              backgroundColor: 'rgba(13, 110, 253, 0.2)',
              fill: true,
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: legendRight,
            datalabels: {
              display: true,
              anchor: 'end',
              align: 'center',
              offset: 4,
              color: '#212529',
              backgroundColor: 'rgba(255,255,255,0.92)',
              borderRadius: 4,
              padding: { top: 2, bottom: 2, left: 4, right: 4 },
              font: { weight: 'bold' as const, size: 10 },
              formatter: fmt,
            },
          },
          scales: {
            r: {
              beginAtZero: true,
            },
          },
        },
      };
    }

    // bar | line
    return {
      type: chartType,
      data: {
        labels,
        datasets: [
          {
            label: 'Value',
            data: values,
            borderColor: primary,
            backgroundColor: chartType === 'line' ? primaryFill : primaryBar,
            fill: chartType === 'line',
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            ...legendRight,
            labels: { ...legendRight.labels, usePointStyle: chartType === 'line' },
          },
          datalabels: chartType === 'line' ? { ...barLineDatalabels, offset: 6 } : barLineDatalabels,
        },
        scales: {
          x: { ticks: { maxRotation: 45, minRotation: 0 } },
          y: { beginAtZero: true },
        },
      },
    };
  }

  private destroyChart(): void {
    this.chart?.destroy();
    this.chart = undefined;
  }
}
