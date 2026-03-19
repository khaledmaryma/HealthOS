import { Component, computed, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountingService, AccountStatementFilter, AccountStatementRow, FinancialRow } from '../services/accounting.service';

@Component({
  selector: 'app-accounting-account-statement',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './accounting-account-statement.component.html',
  styleUrls: ['./accounting-account-statement.component.scss']
})
export class AccountingAccountStatementComponent {
  // filter signals
  fromDate = signal('');
  toDate = signal('');
  isDueDate = signal(false);
  accountText = signal('');
  accountCode = signal<string | null>(null);
  jobId = signal<number | null>(null);
  groupId = signal<number | null>(null);
  voucherTypeIds = signal<number[]>([]);
  accountCurrencyId = signal<number | null>(null);
  comment = signal('');

  rows = signal<AccountStatementRow[]>([]);
  isLoading = signal(false);
  errorMsg = signal('');

  // Pagination
  currentPage = signal(1);
  pageSize = signal(20);
  totalItems = signal(0);

  readonly paginatedRows = computed(() => {
    const allRows = this.rowsWithBalances();
    const start = (this.currentPage() - 1) * this.pageSize();
    const end = start + this.pageSize();
    return allRows.slice(start, end);
  });

  readonly totalPages = computed(() => Math.ceil(this.totalItems() / this.pageSize()));

  readonly rowsWithBalances = computed(() => {
    const rs = this.rows();
    let runningBalanceLocal = 0;
    let runningBalanceMain = 0;

    return rs.map(r => {
      runningBalanceLocal += ((r as any)?.debitLocal ?? 0) - ((r as any)?.creditLocal ?? 0);
      runningBalanceMain += ((r as any)?.debitMain ?? 0) - ((r as any)?.creditMain ?? 0);
      
      return {
        ...r,
        balanceLocal: runningBalanceLocal,
        balanceMain: runningBalanceMain
      };
    });
  });

  readonly totals = computed(() => {
    const rs = this.rows();
    return rs.reduce(
      (acc, r) => {
        acc.debitLocal += (r as any)?.debitLocal ?? 0;
        acc.creditLocal += (r as any)?.creditLocal ?? 0;
        acc.debitMain += (r as any)?.debitMain ?? 0;
        acc.creditMain += (r as any)?.creditMain ?? 0;
        return acc;
      },
      { debitLocal: 0, creditLocal: 0, debitMain: 0, creditMain: 0 }
    );
  });

  private accountingService = inject(AccountingService);

  readonly accountsList = signal<FinancialRow[]>([]);
  readonly voucherTypesList = signal<FinancialRow[]>([]);

  readonly currencyOptions = [
    { id: 1, label: 'L.L' },
    { id: 2, label: '$$' },
    { id: 3, label: 'Euro' }
  ];

  onVoucherTypeChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const vals = Array.from(select.selectedOptions).map(o => +o.value);
    this.voucherTypeIds.set(vals);
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.currentPage.set(1); // Reset to first page
  }

  constructor() {
    // set default date range to current month
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth(), 1);
    this.fromDate.set(this.formatDate(start));
    this.toDate.set(this.formatDate(now));

    // preload lookups
    this.accountingService.getAccounts().subscribe({ next: a => this.accountsList.set(a) });
    this.accountingService.getVoucherTypes().subscribe({ next: v => this.voucherTypesList.set(v) });
  }

  private formatDate(d: Date): string {
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${mm}-${dd}`;
  }

  search(): void {
    this.errorMsg.set('');
    // if user typed an account code/text map to accountCode if possible
    if (this.accountText()) {
      const txt = this.accountText().toLowerCase();
      const accountcodetxt = txt.split(' - ')[0].trim(); // in case user typed "code - description"
      this.accountCode.set(accountcodetxt);
    }

    const filter: AccountStatementFilter = {
      FromDate: this.fromDate(),
      ToDate: this.toDate(),
      IsDueDate: this.isDueDate(),
      AccountCode: this.accountCode() ?? undefined,
      JobId: this.jobId() ?? undefined,
      GroupId: this.groupId() ?? undefined,
      VoucherTypeIds: this.voucherTypeIds(),
      AccountCurrencyId: this.accountCurrencyId() ?? undefined,
      Comment: this.comment() || undefined
    };

    this.isLoading.set(true);
    this.accountingService.getAccountStatement(filter).subscribe({
      next: (data: AccountStatementRow[]) => {
        this.rows.set(data);
        this.totalItems.set(data.length);
        this.currentPage.set(1); // Reset to first page
        this.isLoading.set(false);
      },
      error: (err: any) => {
        console.error('account statement failed', err);
        this.errorMsg.set('Failed to load statement');
        this.isLoading.set(false);
      }
    });
  }

  clear(): void {
    this.fromDate.set('');
    this.toDate.set('');
    this.isDueDate.set(false);
    this.accountCode.set(null);
    this.jobId.set(null);
    this.groupId.set(null);
    this.voucherTypeIds.set([]);
    this.accountCurrencyId.set(null);
    this.comment.set('');
    this.rows.set([]);
    this.errorMsg.set('');
    this.currentPage.set(1);
    this.totalItems.set(0);
  }

  private pickValue(row: FinancialRow, keys: string[]): unknown {
    for (const key of keys) {
      if (row[key] !== undefined && row[key] !== null) {
        return row[key];
      }
    }
    return null;
  }

  formatAccount(row: FinancialRow): string {
    const code = this.pickValue(row, ['Code', 'AccountCode', 'Account'])?.toString() ?? '';
    const description = this.pickValue(row, ['Description', 'Desciption', 'description'])?.toString() ?? '';
    return description ? `${code} - ${description}` : code;
  }

  getMin(a: number, b: number): number {
    return Math.min(a, b);
  }

  printReport(): void {
    const selectedAccount = this.accountsList().find(a => this.pickValue(a, ['Code', 'AccountCode']) === this.accountCode());
    const accountDescription = selectedAccount ? this.pickValue(selectedAccount, ['Description', 'Desciption'])?.toString() ?? '' : '';
    const currencyLabel = this.currencyOptions.find(c => c.id === this.accountCurrencyId())?.label ?? 'All Currencies';
    
    // Determine which currency columns to use
    const isLocalCurrency = this.accountCurrencyId() === 1; // L.L
    const currencySuffix = isLocalCurrency ? 'Local' : 'Main';
    
    // Build filter summary
    const filterSummary = [];
    if (this.fromDate()) filterSummary.push(`From: ${this.fromDate()}`);
    if (this.toDate()) filterSummary.push(`To: ${this.toDate()}`);
    if (this.isDueDate()) filterSummary.push('Using Due Date');
    if (this.accountCode()) filterSummary.push(`Account: ${this.accountCode()}`);
    if (this.jobId()) filterSummary.push(`Job: ${this.jobId()}`);
    if (this.groupId()) filterSummary.push(`Group: ${this.groupId()}`);
    if (this.voucherTypeIds().length > 0) filterSummary.push(`Voucher Types: ${this.voucherTypeIds().join(', ')}`);
    if (this.comment()) filterSummary.push(`Comment: ${this.comment()}`);
    
    const printWindow = window.open('', '_blank');
    if (!printWindow) return;
    
    const rows = this.rowsWithBalances();
    
    const reportHtml = `
      <!DOCTYPE html>
      <html>
      <head>
        <title>Account Statement Report</title>
        <style>
          body { font-family: Arial, sans-serif; margin: 20px; font-size: 12px; }
          .header { display: flex; justify-content: space-between; margin-bottom: 10px; }
          .header-left { text-align: left; }
          .header-right { text-align: right; }
          .report-title { text-align: center; font-size: 16px; font-weight: bold; margin: 20px 0; }
          .account-info { text-align: center; margin-bottom: 20px; font-weight: bold; }
          .filter-summary { margin-bottom: 20px; padding: 10px; background-color: #f5f5f5; font-size: 11px; }
          table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 11px; }
          th, td { border: 1px solid #ddd; padding: 6px; text-align: left; }
          th { background-color: #f2f2f2; font-weight: bold; font-size: 11px; }
          .text-end { text-align: right; }
          .text-center { text-align: center; }
          .total-row { font-weight: bold; background-color: #e9ecef; }
          
          @media print { 
            body { margin: 0; }
            .header { position: fixed; top: 0; left: 0; right: 0; background: white; z-index: 1000; padding: 10px 20px; border-bottom: 1px solid #ddd; }
            .report-title { position: fixed; top: 60px; left: 0; right: 0; background: white; z-index: 1000; padding: 0 20px; }
            .account-info { position: fixed; top: 90px; left: 0; right: 0; background: white; z-index: 1000; padding: 0 20px; }
            .filter-summary { position: fixed; top: 120px; left: 0; right: 0; background: white; z-index: 1000; padding: 10px 20px; margin-bottom: 0; }
            table { margin-top: 200px; page-break-inside: avoid; }
            .page-break { page-break-before: always; }
          }
        </style>
      </head>
      <body>
        <div class="header">
          <div class="header-left">
            <div style="font-size: 20px; font-weight: bold;">Company Name</div>
          </div>
          <div class="header-right">
            <div>Accounting Department</div>
          </div>
        </div>
        <div class="report-title">Statement of Account</div>
        
        <div class="account-info">
          Account: ${this.accountCode() || 'All Accounts'} - ${accountDescription} (${currencyLabel})
        </div>
        
        <div class="filter-summary">
          <strong>Filter Summary:</strong> ${filterSummary.join(' | ')}
        </div>
        
        <table>
          <thead>
            <tr>
              <th class="text-center">#</th>
              <th>Voucher Number</th>
              <th>Voucher Date</th>
              <th>Comment</th>
              <th class="text-end">Debit</th>
              <th class="text-end">Credit</th>
              <th class="text-end">Balance</th>
            </tr>
          </thead>
          <tbody>
            ${rows.map((row, index) => {
              const debitValue = (row as any)[`debit${currencySuffix}`] || 0;
              const creditValue = (row as any)[`credit${currencySuffix}`] || 0;
              const balanceValue = (row as any)[`balance${currencySuffix}`] || 0;
              return `
              <tr>
                <td class="text-center">${index + 1}</td>
                <td>${row['voucherNumber'] || ''}</td>
                <td>${row['voucherDate'] ? new Date(row['voucherDate']).toLocaleDateString() : ''}</td>
                <td>${row['comments'] || ''}</td>
                <td class="text-end">${debitValue !== 0 ? debitValue.toFixed(2) : ''}</td>
                <td class="text-end">${creditValue !== 0 ? creditValue.toFixed(2) : ''}</td>
                <td class="text-end">${balanceValue.toFixed(2)}</td>
              </tr>
            `}).join('')}
          </tbody>
          <tfoot>
            <tr class="total-row">
              <td colspan="4"><strong>Totals</strong></td>
              <td class="text-end"><strong>${this.totals()[`debit${currencySuffix}`] !== 0 ? this.totals()[`debit${currencySuffix}`]?.toFixed(2) : ''}</strong></td>
              <td class="text-end"><strong>${this.totals()[`credit${currencySuffix}`] !== 0 ? this.totals()[`credit${currencySuffix}`]?.toFixed(2) : ''}</strong></td>
              <td class="text-end"><strong>${((this.totals()[`debit${currencySuffix}`] || 0) - (this.totals()[`credit${currencySuffix}`] || 0)).toFixed(2)}</strong></td>
            </tr>
          </tfoot>
        </table>
      </body>
      </html>
    `;
    
    printWindow.document.write(reportHtml);
    printWindow.document.close();
    printWindow.focus();
    
    // Wait for content to load then print
    setTimeout(() => {
      printWindow.print();
    }, 500);
  }
}
