/** Application context (matches route `appKey` data). */
export type KpiAppKey = 'LIS' | 'Inventory' | 'UserManagement' | 'Accounting' | 'EMR';

/** Home surface within an app (allows multiple home layouts later). */
export const DEFAULT_KPI_HOME_PAGE_ID = 'main';

/** Persisted user KPI (camelCase from API JSON). */
export interface UserKpiDefinition {
  id: number;
  userId: number;
  appKey: string;
  homePageId: string;
  title: string;
  sqlQuery: string;
  /** 0 = chart, 1 = grid (JSON number from API). */
  displayMode: number;
  gridShowTotals: boolean;
  chartOptionsJson: string | null;
  sortOrder: number;
  createdUtc: string;
  modifiedUtc: string | null;
}

export enum KpiDisplayMode {
  Chart = 0,
  Grid = 1,
}

/** Result of POST /api/user-kpis/execute */
export interface KpiQueryResult {
  columns: string[];
  rows: Record<string, unknown>[];
}

/** Supported Chart.js types for KPI widgets (stored in `chartOptionsJson`). */
export type KpiChartType = 'bar' | 'line' | 'pie' | 'doughnut' | 'radar' | 'polarArea';

/** Parsed from `chartOptionsJson`; optional indexes default via inference. */
export interface KpiChartPresentationOptions {
  chartType: KpiChartType;
  labelColumnIndex?: number;
  valueColumnIndex?: number;
}

export interface ExecuteKpiQueryRequest {
  userId: number;
  kpiId?: number;
  sql?: string;
}
