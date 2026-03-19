import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DepartmentCash {
  department: string;
  lbp: number;
  usd: number;
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
  private apiUrl = 'http://localhost:5050/api/Accounting';

  getDailyCashByDepartment(): Observable<DepartmentCash[]> {
    return this.http.get<DepartmentCash[]>(`${this.apiUrl}/DailyCashByDepartment`);
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


