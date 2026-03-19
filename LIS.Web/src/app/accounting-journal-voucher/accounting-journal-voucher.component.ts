import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AccountingService, FinancialRow } from '../services/accounting.service';

@Component({
  selector: 'app-accounting-journal-voucher',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './accounting-journal-voucher.component.html',
  styleUrl: './accounting-journal-voucher.component.scss'
})
export class AccountingJournalVoucherComponent implements OnInit {
  readonly headers = signal<FinancialRow[]>([]);
  readonly filterNumber = signal('');
  readonly filterFrom = signal('');
  readonly filterTo = signal('');
  readonly details = signal<FinancialRow[]>([]);
  readonly voucherTypes = signal<FinancialRow[]>([]);
  readonly accounts = signal<FinancialRow[]>([]);
  readonly selectedHeader = signal<FinancialRow | null>(null);
  readonly isLoadingHeaders = signal(false);
  readonly isLoadingDetails = signal(false);
  readonly totals = computed<Totals>(() => {
    const rows = this.details();
    const initial: Totals = { debitLocal: 0, debitMain: 0, creditLocal: 0, creditMain: 0 };
    return rows.reduce<Totals>(
      (acc, row) => ({
        debitLocal: acc.debitLocal + this.getDebitLocal(row),
        debitMain: acc.debitMain + this.getDebitMain(row),
        creditLocal: acc.creditLocal + this.getCreditLocal(row),
        creditMain: acc.creditMain + this.getCreditMain(row)
      }),
      initial
    );
  });

  readonly detailColumns = computed(() => [
    { key: 'account', label: 'Account', value: (row: FinancialRow) => this.getAccountLabel(row) },
    { key: 'currency', label: 'Currency', value: (row: FinancialRow) => this.getCurrencyLabel(row) },
    { key: 'accountDescription', label: 'Account Description', value: (row: FinancialRow) => this.getAccountDescription(row) },
    { key: 'debitLocal', label: 'Debit L.L', value: (row: FinancialRow) => this.formatNumber(this.getDebitLocal(row)) },
    { key: 'debitMain', label: 'Debit $$', value: (row: FinancialRow) => this.formatNumber(this.getDebitMain(row)) },
    { key: 'creditLocal', label: 'Credit L.L', value: (row: FinancialRow) => this.formatNumber(this.getCreditLocal(row)) },
    { key: 'creditMain', label: 'Credit $$', value: (row: FinancialRow) => this.formatNumber(this.getCreditMain(row)) },
    { key: 'comment', label: 'Comment', value: (row: FinancialRow) => this.pickValue(row, ['Comments', 'Comment', 'Remarks', 'Description', 'Note']) ?? '-' },
    { key: 'costCenter', label: 'Cost Center', value: (row: FinancialRow) => this.pickValue(row, ['CostCenter', 'CostCenterID', 'CostCenterId', 'CostCenterCode']) ?? '-' }
  ]);


  constructor(
    private readonly accountingService: AccountingService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.initDefaultDateFilter();
    this.loadHeaders();
    this.loadVoucherTypes();
    this.loadAccounts();
  }

  loadHeaders(): void {
    this.isLoadingHeaders.set(true);
    this.accountingService.getVoucherHeaders().subscribe({
      next: rows => {
        this.headers.set(rows);
        this.isLoadingHeaders.set(false);
        this.ensureSelection();
      },
      error: err => {
        console.error('Failed to load voucher headers', err);
        this.isLoadingHeaders.set(false);
        alert('Failed to load voucher headers.');
      }
    });
  }

  createVoucher(): void {
    this.router.navigate(['/accounting/journal-voucher/new']);
  }

  editVoucher(): void {
    const header = this.selectedHeader();
    const id = header ? this.getHeaderId(header) : null;
    if (!id) return;
    this.router.navigate([`/accounting/journal-voucher/${id}`]);
  }

  deleteVoucher(): void {
    const header = this.selectedHeader();
    const id = header ? this.getHeaderId(header) : null;
    if (!id) return;
    if (!confirm('Delete this voucher?')) return;
    this.accountingService.deleteVoucher(id).subscribe({
      next: () => this.loadHeaders(),
      error: err => {
        console.error('Failed to delete voucher', err);
        alert('Failed to delete voucher.');
      }
    });
  }

  loadVoucherTypes(): void {
    this.accountingService.getVoucherTypes().subscribe({
      next: rows => this.voucherTypes.set(rows),
      error: err => {
        console.error('Failed to load voucher types', err);
      }
    });
  }

  loadAccounts(): void {
    this.accountingService.getAccounts().subscribe({
      next: rows => this.accounts.set(rows),
      error: err => {
        console.error('Failed to load accounts', err);
      }
    });
  }

  readonly filteredHeaders = computed(() => {
    const headers = this.headers();
    const numberFilter = this.filterNumber().trim().toLowerCase();
    const fromDate = this.parseDate(this.filterFrom());
    const toDate = this.parseDate(this.filterTo());

    return headers.filter(header => {
      if (numberFilter) {
        const numberText = this.getHeaderTitle(header).toLowerCase();
        if (!numberText.includes(numberFilter)) {
          return false;
        }
      }

      if (fromDate || toDate) {
        const headerDate = this.getHeaderDate(header);
        if (!headerDate) {
          return false;
        }
        if (fromDate && headerDate < fromDate) {
          return false;
        }
        if (toDate && headerDate > toDate) {
          return false;
        }
      }

      return true;
    });
  });

  onFilterChange(): void {
    this.ensureSelection();
  }

  private ensureSelection(): void {
    const current = this.selectedHeader();
    const list = this.filteredHeaders();
    if (!list.length) {
      this.selectedHeader.set(null);
      this.details.set([]);
      return;
    }
    if (!current || !list.some(item => this.getHeaderId(item) === this.getHeaderId(current))) {
      this.selectHeader(list[0]);
    }
  }

  private initDefaultDateFilter(): void {
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth(), 1);
    this.filterFrom.set(this.formatDateInput(start));
    this.filterTo.set(this.formatDateInput(now));
  }

  private formatDateInput(value: Date): string {
    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private parseDate(value: string): Date | null {
    if (!value) return null;
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return null;
    return parsed;
  }

  selectHeader(row: FinancialRow): void {
    this.selectedHeader.set(row);
    const headerId = this.getHeaderId(row);
    if (!headerId) {
      this.details.set([]);
      return;
    }
    this.loadDetails(headerId);
  }

  loadDetails(headerId: number): void {
    this.isLoadingDetails.set(true);
    this.accountingService.getVoucherDetails(headerId).subscribe({
      next: rows => {
        this.details.set(rows);
        this.isLoadingDetails.set(false);
      },
      error: err => {
        console.error('Failed to load voucher details', err);
        this.isLoadingDetails.set(false);
        alert('Failed to load voucher details.');
      }
    });
  }

  getHeaderId(row: FinancialRow): number | null {
    const value = this.pickValue(row, ['ID', 'Id', 'id']);
    return typeof value === 'number' ? value : Number(value) || null;
  }

  getHeaderTitle(row: FinancialRow): string {
    const number = this.pickValue(row, ['VoucherNumber', 'VoucherNo', 'Number', 'Code', 'ID']) ?? '';
    const description = this.pickValue(row, ['Description', 'Comment', 'Remarks', 'Note']) ?? '';
    return description ? `${number} - ${description}` : `${number}`;
  }

  getHeaderSubtitle(row: FinancialRow): string {
    const date = this.pickValue(row, ['VoucherDate', 'Date', 'CreatedDate', 'EntryDate']);
    const typeLabel = this.getVoucherTypeCode(row);
    const dateText = date ? String(date) : '-';
    const typeText = typeLabel ? `${typeLabel}` : 'Type -';
    return `${dateText} | ${typeText}`;
  }

  getHeaderNumber(row: FinancialRow): string {
    const value = this.pickValue(row, ['VoucherNumber', 'VoucherNo', 'Number', 'Code', 'ID']);
    return value ? String(value) : '-';
  }

  getHeaderDateText(row: FinancialRow): string {
    const value = this.pickValue(row, ['VoucherDate', 'Date', 'CreatedDate', 'EntryDate']);
    return value ? String(value) : '-';
  }

  getHeaderDueDateText(row: FinancialRow): string {
    const value = this.pickValue(row, ['VoucherDueDate', 'DueDate', 'VoucherDue', 'Due']);
    return value ? String(value) : '-';
  }

  isSelected(row: FinancialRow): boolean {
    const current = this.selectedHeader();
    return !!current && this.getHeaderId(current) === this.getHeaderId(row);
  }

  private getHeaderDate(row: FinancialRow): Date | null {
    const value = this.pickValue(row, ['VoucherDate', 'Date', 'CreatedDate', 'EntryDate']);
    if (!value) return null;
    const parsed = new Date(String(value));
    if (Number.isNaN(parsed.getTime())) return null;
    return parsed;
  }

  getVoucherTypeLabel(row: FinancialRow): string | null {
    const idValue = this.pickValue(row, ['VoucherTypeID', 'VoucherTypeId', 'TypeID', 'TypeId']);
    const typeId = typeof idValue === 'number' ? idValue : Number(idValue) || null;
    if (!typeId) return null;
    const match = this.voucherTypes().find(item => this.getRowId(item) === typeId);
    if (!match) return typeId.toString();
    return (this.pickValue(match, ['Description', 'Name', 'VoucherTypeDescription']) ?? typeId).toString();
  }

  getVoucherTypeCode(row: FinancialRow): string | null {
    const idValue = this.pickValue(row, ['VoucherTypeID', 'VoucherTypeId', 'TypeID', 'TypeId']);
    const typeId = typeof idValue === 'number' ? idValue : Number(idValue) || null;
    if (!typeId) return null;
    const match = this.voucherTypes().find(item => this.getRowId(item) === typeId);
    if (!match) return typeId.toString();
    return (this.pickValue(match, ['Code', 'VoucherTypeCode', 'TypeCode', 'Name']) ?? typeId).toString();
  }

  getAccountLabel(row: FinancialRow): string {
    const code = this.pickValue(row, ['AccountCode', 'Account', 'AccountNb', 'AccountNB']);
    const idValue = this.pickValue(row, ['AccountID', 'AccountId', 'AccountID']);
    if (code) return String(code);
    const id = typeof idValue === 'number' ? idValue : Number(idValue) || null;
    if (!id) return '-';
    const account = this.accounts().find(item => this.getRowId(item) === id);
    return (this.pickValue(account ?? {}, ['Code', 'AccountCode', 'Account']) ?? id).toString();
  }

  getAccountDescription(row: FinancialRow): string {
    console.log('>>>>>>>>>> row : ', row);
    console.log('>>>>>>>>>> row : ' + row);
    console.log('>>>>>>>>>> row : ', JSON.stringify(row));
    console.log('>>>>>>>>>> row : ' + JSON.stringify(row));
    const description = this.pickValue(row, ['AccountDescription', 'Description', 'AccountName', 'Name']);
    if (description) return String(description);
    const idValue = this.pickValue(row, ['AccountID', 'AccountId']);
    const id = typeof idValue === 'number' ? idValue : Number(idValue) || null;
    if (!id) return '-';
    const account = this.accounts().find(item => this.getRowId(item) === id);
    return (this.pickValue(account ?? {}, ['Description', 'AccountDescription', 'Name', 'AccountName']) ?? '-').toString();
  }

  getCurrencyLabel(row: FinancialRow): string {
    const value = this.pickValue(row, ['AccountCurreny', 'AccountCurrency', 'CurrencyID', 'CurrencyId', 'AccountCurrencyID']);
    const id = typeof value === 'number' ? value : Number(value) || null;
    if (!id) return '-';
    if (id === 1) return 'L.L';
    if (id === 2) return '$$';
    return id.toString();
  }

  getDebitLocal(row: FinancialRow): number {
    return this.pickNumber(row, ['DbLocal', 'DebitLocal', 'DbLocal1']);
  }

  getDebitMain(row: FinancialRow): number {
    return this.pickNumber(row, ['DbMain', 'DebitMain', 'DbMain1']);
  }

  getCreditLocal(row: FinancialRow): number {
    return this.pickNumber(row, ['CrLocal', 'CreditLocal', 'CrLocal1']);
  }

  getCreditMain(row: FinancialRow): number {
    return this.pickNumber(row, ['CrMain', 'CreditMain', 'CrMain1']);
  }

  formatNumber(value: number): string {
    if (!Number.isFinite(value)) return '0';
    return new Intl.NumberFormat('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(value);
  }

  private pickNumber(row: FinancialRow, keys: string[]): number {
    const value = this.pickValue(row, keys);
    if (value == null) return 0;
    const numeric = typeof value === 'number' ? value : Number(value);
    return Number.isFinite(numeric) ? numeric : 0;
  }

  private getRowId(row: FinancialRow): number | null {
    const value = this.pickValue(row, ['ID', 'Id', 'id']);
    return typeof value === 'number' ? value : Number(value) || null;
  }

  private pickValue(row: FinancialRow, keys: string[]): unknown {
    for (const key of keys) {
      if (row[key] !== undefined && row[key] !== null) {
        return row[key];
      }
    }
    return null;
  }
}

type Totals = {
  debitLocal: number;
  debitMain: number;
  creditLocal: number;
  creditMain: number;
};
