import { Component, computed, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountingService, AccountNode, TrialBalanceFilter } from '../services/accounting.service';

@Component({
  selector: 'app-accounting-trial-balance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './accounting-trial-balance.component.html',
  styleUrls: ['./accounting-trial-balance.component.scss']
})
export class AccountingTrialBalanceComponent {
  accounts = signal<AccountNode[]>([]);
  filteredAccounts = signal<AccountNode[]>([]);
  isLoading = signal(false);
  errorMsg = signal('');

  // Filter options
  filter = signal<TrialBalanceFilter>({
    ExcludeOpeningClosing: false
  });

  // Search and pagination
  searchTerm = signal('');
  currentPage = signal(1);
  pageSize = signal(25);
  showPrintView = signal(false);

  // Current date for print footer
  currentDate = new Date();

  readonly currencyOptions = [
    { id: 1, label: 'L.L' },
    { id: 2, label: '$$' },
    { id: 3, label: 'Euro' }
  ];

  readonly filteredAndPagedAccounts = computed(() => {
    let accounts = this.filteredAccounts();

    // Apply search filter
    const search = this.searchTerm().toLowerCase().trim();
    if (search) {
      accounts = accounts.filter(account =>
        (account.code?.toLowerCase().includes(search) ?? false) ||
        (account.description?.toLowerCase().includes(search) ?? false)
      );
    }

    return accounts;
  });

  readonly paginatedAccounts = computed(() => {
    const accounts = this.filteredAndPagedAccounts();
    const page = this.currentPage();
    const size = this.pageSize();
    const start = (page - 1) * size;
    const end = start + size;
    return accounts.slice(start, end);
  });

  readonly totalPages = computed(() => {
    const total = this.filteredAndPagedAccounts().length;
    return Math.ceil(total / this.pageSize());
  });

  readonly totals = computed(() => {
    const allAccounts = this.filteredAndPagedAccounts();
    return allAccounts.reduce(
      (acc: { debitLocal: number; creditLocal: number; debitMain: number; creditMain: number }, account) => {
        acc.debitLocal += (account.debitLocal ?? 0);
        acc.creditLocal += (account.creditLocal ?? 0);
        acc.debitMain += (account.debitMain ?? 0);
        acc.creditMain += (account.creditMain ?? 0);
        return acc;
      },
      { debitLocal: 0, creditLocal: 0, debitMain: 0, creditMain: 0 }
    );
  });

  private accountingService = inject(AccountingService);

  constructor() {
    this.loadTrialBalance();
  }

  loadTrialBalance(): void {
    this.errorMsg.set('');
    this.isLoading.set(true);

    this.accountingService.getTrialBalance(this.filter()).subscribe({
      next: (data: AccountNode[]) => {
        this.accounts.set(data);
        this.filteredAccounts.set(data);
        this.currentPage.set(1); // Reset to first page
        this.isLoading.set(false);
      },
      error: (err: any) => {
        console.error('trial balance failed', err);
        this.errorMsg.set('Failed to load trial balance');
        this.isLoading.set(false);
      }
    });
  }

  onFilterChange(): void {
    this.loadTrialBalance();
  }

  onSearchChange(): void {
    this.currentPage.set(1); // Reset to first page when searching
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  onPageSizeChange(): void {
    this.currentPage.set(1); // Reset to first page when changing page size
  }

  printTrialBalance(): void {
    this.showPrintView.set(true);
    setTimeout(() => {
      window.print();
      this.showPrintView.set(false);
    }, 100);
  }

  exportToExcel(): void {
    // Simple CSV export for now
    const accounts = this.filteredAndPagedAccounts();
    const csvContent = this.generateCSV(accounts);
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', 'trial-balance.csv');
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  private generateCSV(accounts: AccountNode[]): string {
    const headers = ['Account Code', 'Currency', 'Description', 'Debit Local', 'Credit Local', 'Balance Local', 'Debit Main', 'Credit Main', 'Balance Main'];
    const rows = accounts.map(account => [
      account.code || '',
      this.getCurrencyLabel(account.currency),
      account.description || '',
      (account.debitLocal || 0).toFixed(2),
      (account.creditLocal || 0).toFixed(2),
      this.getBalance(account.debitLocal, account.creditLocal).toFixed(2),
      (account.debitMain || 0).toFixed(2),
      (account.creditMain || 0).toFixed(2),
      this.getBalance(account.debitMain, account.creditMain).toFixed(2)
    ]);

    const csvData = [headers, ...rows];
    return csvData.map(row => row.map(cell => `"${cell}"`).join(',')).join('\n');
  }

  private flattenAccounts(accounts: AccountNode[]): AccountNode[] {
    const result: AccountNode[] = [];

    const flatten = (accs: AccountNode[]) => {
      for (const acc of accs) {
        result.push(acc);
        if (acc.children && acc.children.length > 0) {
          flatten(acc.children);
        }
      }
    };

    flatten(accounts);
    return result;
  }

  getAccountLevel(account: AccountNode): number {
    // For now, return 0 as we don't have proper hierarchy implementation
    // This would need to be implemented based on your data structure
    return 0;
  }

  getCurrencyLabel(currencyId: number | string | undefined): string {
    if (typeof currencyId === 'string') {
      currencyId = parseInt(currencyId);
    }
    const currency = this.currencyOptions.find(c => c.id === currencyId);
    return currency?.label || '';
  }

  getBalance(debit: number = 0, credit: number = 0): number {
    return debit - credit;
  }
}