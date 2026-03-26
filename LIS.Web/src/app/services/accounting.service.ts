import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiEndpointsService } from '../api/api-endpoints.service';

export interface DepartmentCash {
  department: string;
  lbp: number;
  usd: number;
}

export interface CashierDetailRow {
  dailyCounter?: number;
  cashierDetailCounter?: number;
  department?: string;
  distributionTypeDescription?: string;
  payedBY?: string;
  mouvementNb?: string;
  voucherNumber?: number;
  amoutToBePayed?: number;
  accountCurrency?: number;
  collectionLBP?: number;
  collectionUSD?: number;
  differenceUSD?: number;
  differenceLBP?: number;
}

export interface CashierDepartmentSummary {
  department: string;
  collectionLBP: number;
  collectionUSD: number;
}

export interface CashierOpenDetailsResponse {
  cashierHeaderId?: number | null;
  openDate?: string;
  details: CashierDetailRow[];
  summary: CashierDepartmentSummary[];
  totals: { totalLBP: number; totalUSD: number };
}

export interface CashierForPrintResponse {
  cashierHeaderId?: number | null;
  openDate?: string | null;
  closeDate?: string | null;
  closeTime?: string | null;
  details: CashierDetailRow[];
  summary: CashierDepartmentSummary[];
  totals: { totalLBP: number; totalUSD: number };
}

export interface AccountIncome {
  accountCode: string;
  accountDescription: string;
  lbp: number;
  usd: number;
}

// types needed by various accounting components
export interface AccountNode {
  code?: string;
  currency?: string;
  description?: string;
  job?: string;
  group?: string;
  debitLocal?: number;
  debitMain?: number;
  creditLocal?: number;
  creditMain?: number;
  children?: AccountNode[];
  [key: string]: any;
}
export interface AccountStatementFilter {
  FromDate?: string;
  ToDate?: string;
  IsDueDate?: boolean;
  AccountCode?: string;
  JobId?: number;
  GroupId?: number;
  VoucherTypeIds?: number[];
  AccountCurrencyId?: number;
  Comment?: string;
}
export interface AccountStatementRow {
  voucherNumber?: string;
  voucherDate?: string;
  accountCode?: string;
  accountDescription?: string;
  debitLocal?: number;
  creditLocal?: number;
  debitMain?: number;
  creditMain?: number;
  comments?: string;
  [key: string]: any;
}
export interface TrialBalanceFilter {
  FromDate?: string;
  ToDate?: string;
  ExcludeOpeningClosing?: boolean;
}

export interface OpeningClosingRequest { [key: string]: any; }
export interface OpeningClosingPreview { [key: string]: any; }

export type FinancialRow = Record<string, any>;

export interface VoucherSaveRequest {
  header: {
    id?: number | null;
    voucherDate: string;
    voucherDueDate?: string | null;
    voucherTypeId: number;
    voucherNumber?: string | null;
  };
  details: Array<{
    id?: number | null;
    accountId: number;
    accountCode?: string | null;
    accountCurrencyId?: number | null;
    accountDescription?: string | null;
    rate?: number | null;
    debitLocal: number;
    debitMain: number;
    creditLocal: number;
    creditMain: number;
    comment?: string | null;
    costCenter?: string | null;
  }>;
}

@Injectable({ providedIn: 'root' })
export class AccountingService {
  private http = inject(HttpClient);
  private endpoints = inject(ApiEndpointsService);
  private readonly apiUrl = this.endpoints.accounting;

  getDailyCashByDepartment(): Observable<DepartmentCash[]> {
    return this.http.get<DepartmentCash[]>(`${this.apiUrl}/DailyCashByDepartment`);
  }

  getCashierOpenDetails(): Observable<CashierOpenDetailsResponse> {
    return this.http.get<CashierOpenDetailsResponse>(`${this.apiUrl}/CashierOpenDetails`);
  }

  /** draft=true: current open cashier; draft=false: last closed cashier */
  getCashierForPrint(draft: boolean): Observable<CashierForPrintResponse> {
    return this.http.get<CashierForPrintResponse>(`${this.apiUrl}/CashierForPrint`, {
      params: { draft: String(draft) },
    });
  }

  closeCashierAndOpenNew(newOpenDate: string): Observable<{ message: string; newOpenDate: string }> {
    return this.http.post<{ message: string; newOpenDate: string }>(
      `${this.apiUrl}/CloseCashierAndOpenNew`,
      { newOpenDate }
    );
  }

  getCurrentMonthIncomeByAccount(): Observable<AccountIncome[]> {
    return this.http.get<AccountIncome[]>(`${this.apiUrl}/CurrentMonthIncomeByAccount`);
  }

  getVoucherHeaders(top = 200): Observable<FinancialRow[]> {
    return this.http.get<FinancialRow[]>(`${this.apiUrl}/financial/voucher-headers`, {
      params: { top }
    });
  }

  getVoucherDetails(voucherHeaderId: number, top = 500): Observable<FinancialRow[]> {
    return this.http.get<FinancialRow[]>(`${this.apiUrl}/financial/voucher-details`, {
      params: { voucherHeaderId, top }
    });
  }

  getVoucherTypes(top = 200): Observable<FinancialRow[]> {
    return this.http.get<FinancialRow[]>(`${this.apiUrl}/financial/voucher-types`, {
      params: { top }
    });
  }

  getAccounts(top = 1000, accessibleOnly = true): Observable<FinancialRow[]> {
    return this.http.get<FinancialRow[]>(`${this.apiUrl}/financial/accounts`, {
      params: { top, accessibleOnly }
    });
  }

  getConfiguration(top = 200): Observable<FinancialRow[]> {
    return this.http.get<FinancialRow[]>(`${this.apiUrl}/financial/configuration`, {
      params: { top }
    });
  }

  updateConfiguration(id: number, payload: FinancialRow) {
    return this.http.put<void>(`${this.apiUrl}/financial/configuration/${id}`, payload);
  }

  createVoucher(payload: VoucherSaveRequest) {
    return this.http.post<{ id: number }>(`${this.apiUrl}/financial/vouchers`, payload);
  }

  updateVoucher(id: number, payload: VoucherSaveRequest) {
    return this.http.put<void>(`${this.apiUrl}/financial/vouchers/${id}`, payload);
  }

  deleteVoucher(id: number) {
    return this.http.delete<void>(`${this.apiUrl}/financial/vouchers/${id}`);
  }

  // stub methods for features referenced by components
  getAccountStatement(filter: AccountStatementFilter): Observable<AccountStatementRow[]> {
    return this.http.post<AccountStatementRow[]>(`${this.apiUrl}/account-statement`, filter);
  }

  getChartOfAccounts(): Observable<AccountNode[]> {
    return this.http.get<AccountNode[]>(`${this.apiUrl}/chart-of-accounts`);
  }

  previewOpeningClosing(req: OpeningClosingRequest): Observable<OpeningClosingPreview[]> {
    return this.http.post<OpeningClosingPreview[]>(`${this.apiUrl}/preview-opening-closing`, req);
  }

  generateOpeningClosing(req: OpeningClosingRequest): Observable<OpeningClosingPreview[]> {
    return this.http.post<OpeningClosingPreview[]>(`${this.apiUrl}/generate-opening-closing`, req);
  }

  getTrialBalance(filter?: TrialBalanceFilter): Observable<AccountNode[]> {
    if (filter && (filter.FromDate || filter.ToDate || filter.ExcludeOpeningClosing)) {
      // Use POST when filters are provided
      return this.http.post<AccountNode[]>(`${this.apiUrl}/trial-balance`, filter);
    } else {
      // Use GET for default current year data
      return this.http.get<AccountNode[]>(`${this.apiUrl}/trial-balance`);
    }
  }
}


