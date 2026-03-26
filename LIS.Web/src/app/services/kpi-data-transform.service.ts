import { Injectable } from '@angular/core';
import { KpiChartPresentationOptions, KpiChartType, KpiQueryResult } from '../models/user-kpi';

const KPI_CHART_TYPES: readonly KpiChartType[] = [
  'bar',
  'line',
  'pie',
  'doughnut',
  'radar',
  'polarArea',
];

/**
 * Pure transformations from tabular query results to chart series and grid totals.
 * Presentation components stay thin; execution stays in {@link KpiQueryApiService}.
 */
@Injectable({ providedIn: 'root' })
export class KpiDataTransformService {
  /** Distinct fill colors for pie / doughnut / polarArea segments. */
  segmentBackgroundColors(count: number): string[] {
    if (count <= 0) {
      return [];
    }
    const colors: string[] = [];
    for (let i = 0; i < count; i++) {
      const hue = Math.round((360 * i) / Math.max(count, 1)) % 360;
      colors.push(`hsla(${hue}, 65%, 52%, 0.85)`);
    }
    return colors;
  }

  normalizeChartType(raw: unknown): KpiChartType {
    const s = typeof raw === 'string' ? raw.trim().toLowerCase() : '';
    if ((KPI_CHART_TYPES as readonly string[]).includes(s)) {
      return s as KpiChartType;
    }
    return 'bar';
  }

  parseChartOptionsJson(json: string | null | undefined): KpiChartPresentationOptions {
    if (!json?.trim()) {
      return { chartType: 'bar' };
    }
    try {
      const o = JSON.parse(json) as Record<string, unknown>;
      const chartType = this.normalizeChartType(o['chartType']);
      const labelColumnIndex =
        typeof o['labelColumnIndex'] === 'number' ? (o['labelColumnIndex'] as number) : undefined;
      const valueColumnIndex =
        typeof o['valueColumnIndex'] === 'number' ? (o['valueColumnIndex'] as number) : undefined;
      return { chartType, labelColumnIndex, valueColumnIndex };
    } catch {
      return { chartType: 'bar' };
    }
  }

  buildChartSeries(
    result: KpiQueryResult,
    options: KpiChartPresentationOptions
  ): { labels: string[]; values: number[] } | null {
    const { columns, rows } = result;
    if (!columns.length || !rows.length) {
      return null;
    }

    let labelIdx = options.labelColumnIndex ?? -1;
    if (labelIdx < 0 || labelIdx >= columns.length) {
      labelIdx = columns.findIndex(c => !this.columnIsAllNumeric(rows, c));
    }
    if (labelIdx < 0) {
      labelIdx = 0;
    }

    let valueIdx = options.valueColumnIndex ?? -1;
    if (valueIdx < 0 || valueIdx >= columns.length || valueIdx === labelIdx) {
      valueIdx = columns.findIndex((c, i) => i !== labelIdx && this.columnIsAllNumeric(rows, c));
    }
    if (valueIdx < 0) {
      valueIdx = columns.findIndex((_, i) => i !== labelIdx);
    }
    if (valueIdx < 0) {
      return null;
    }

    const labels: string[] = [];
    const values: number[] = [];
    for (const row of rows) {
      const lv = row[columns[labelIdx]];
      const vv = row[columns[valueIdx]];
      labels.push(lv != null ? String(lv) : '');
      const n = this.toNumber(vv);
      values.push(n ?? 0);
    }
    return { labels, values };
  }

  /**
   * Sum numeric columns; non-numeric columns get `null` in the totals row.
   */
  computeColumnTotals(columns: string[], rows: Record<string, unknown>[]): Record<string, number | null> {
    const totals: Record<string, number | null> = {};
    for (const col of columns) {
      if (!this.columnIsAllNumeric(rows, col)) {
        totals[col] = null;
        continue;
      }
      let sum = 0;
      for (const row of rows) {
        const n = this.toNumber(row[col]);
        if (n != null) {
          sum += n;
        }
      }
      totals[col] = sum;
    }
    return totals;
  }

  private columnIsAllNumeric(rows: Record<string, unknown>[], column: string): boolean {
    return rows.every(r => {
      const v = r[column];
      if (v == null) {
        return true;
      }
      return this.toNumber(v) != null;
    });
  }

  private toNumber(v: unknown): number | null {
    if (v == null) {
      return null;
    }
    if (typeof v === 'number' && Number.isFinite(v)) {
      return v;
    }
    if (typeof v === 'string' && v.trim() !== '') {
      const n = Number(v);
      return Number.isFinite(n) ? n : null;
    }
    return null;
  }
}
