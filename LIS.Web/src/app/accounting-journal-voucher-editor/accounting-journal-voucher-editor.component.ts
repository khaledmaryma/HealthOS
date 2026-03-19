import { Component, ElementRef, OnInit, ViewChild, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AccountingService, FinancialRow, VoucherSaveRequest } from '../services/accounting.service';

type DetailRow = {
  accountId: number;
  accountCode: string;
  accountDescription: string;
  accountCurrencyId: number | null;
  accountCurrencyLabel: string;
  rate: number;
  debitLocal: number;
  debitMain: number;
  creditLocal: number;
  creditMain: number;
  comment: string;
};

@Component({
  selector: 'app-accounting-journal-voucher-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './accounting-journal-voucher-editor.component.html',
  styleUrl: './accounting-journal-voucher-editor.component.scss'
})
export class AccountingJournalVoucherEditorComponent implements OnInit {
  readonly isEditMode = signal(false);
  readonly headerId = signal<number | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);

  readonly voucherTypes = signal<FinancialRow[]>([]);
  readonly accounts = signal<FinancialRow[]>([]);

  readonly voucherNumber = signal('');
  readonly voucherDate = signal('');
  readonly voucherDueDate = signal('');
  readonly voucherTypeInput = signal('');
  readonly voucherTypeId = signal<number | null>(null);
  readonly configRowId = signal<number | null>(null);
  readonly configLastVoucherColumn = signal<string | null>(null);
  readonly nextVoucherNumber = signal<number | null>(null);

  readonly showVoucherTypeDropdown = signal(false);
  readonly showAccountDropdown = signal(false);
  readonly accountInput = signal('');
  readonly accountId = signal<number | null>(null);
  readonly accountDescription = signal('');
  readonly accountCurrencyId = signal<number | null>(null);
  readonly accountCurrencyLabel = signal('');
  readonly defaultRate = signal(1);
  readonly rate = signal(1);
  readonly debitLocal = signal('');
  readonly debitMain = signal('');
  readonly creditLocal = signal('');
  readonly creditMain = signal('');
  readonly detailComment = signal('');

  readonly details = signal<DetailRow[]>([]);
  readonly selectedDetailIndex = signal<number | null>(null);

  @ViewChild('accountInputEl') accountInputEl?: ElementRef<HTMLInputElement>;

  readonly filteredVoucherTypes = computed(() => {
    const q = this.voucherTypeInput().toLowerCase().trim();
    const rows = this.voucherTypes();
    if (!q) return rows;
    return rows.filter(row =>
      this.formatVoucherType(row).toLowerCase().includes(q)
    );
  });

  readonly filteredAccounts = computed(() => {
    const q = this.accountInput().toLowerCase().trim();
    const rows = this.accounts();
    if (!q) return rows;
    return rows.filter(row =>
      this.formatAccount(row).toLowerCase().includes(q)
    );
  });

  readonly totals = computed(() => {
    return this.details().reduce(
      (acc, row) => ({
        debitLocal: acc.debitLocal + row.debitLocal,
        debitMain: acc.debitMain + row.debitMain,
        creditLocal: acc.creditLocal + row.creditLocal,
        creditMain: acc.creditMain + row.creditMain
      }),
      { debitLocal: 0, debitMain: 0, creditLocal: 0, creditMain: 0 }
    );
  });

  readonly balanceLocal = computed(() => this.totals().debitLocal - this.totals().creditLocal);
  readonly balanceMain = computed(() => this.totals().debitMain - this.totals().creditMain);

  readonly disableDebit = computed(() => this.toNumber(this.creditLocal()) > 0 || this.toNumber(this.creditMain()) > 0);
  readonly disableCredit = computed(() => this.toNumber(this.debitLocal()) > 0 || this.toNumber(this.debitMain()) > 0);

  constructor(
    private readonly accountingService: AccountingService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam && idParam !== 'new') {
      const id = Number(idParam);
      if (!Number.isNaN(id)) {
        this.isEditMode.set(true);
        this.headerId.set(id);
      }
    }
    this.initDefaultDates();
    this.loadVoucherTypes();
    this.loadAccounts();
    this.loadConfiguration();
    if (this.isEditMode()) {
      this.loadExistingVoucher();
    }
  }

  loadVoucherTypes(): void {
    this.accountingService.getVoucherTypes().subscribe({
      next: rows => this.voucherTypes.set(rows),
      error: err => console.error('Failed to load voucher types', err)
    });
  }

  loadAccounts(): void {
    this.accountingService.getAccounts(1000, true).subscribe({
      next: rows => this.accounts.set(rows),
      error: err => console.error('Failed to load accounts', err)
    });
  }

  loadConfiguration(): void {
    this.accountingService.getConfiguration().subscribe({
      next: rows => {
        const defaultRate = this.getDefaultRate(rows);
        if (defaultRate !== null) {
          this.defaultRate.set(defaultRate);
          this.rate.set(defaultRate);
        }
        const lastVoucherInfo = this.getLastVoucherInfo(rows);
        if (lastVoucherInfo) {
          this.configRowId.set(lastVoucherInfo.rowId);
          this.configLastVoucherColumn.set(lastVoucherInfo.column);
          const nextValue = lastVoucherInfo.value + 1;
          this.nextVoucherNumber.set(nextValue);
          if (!this.isEditMode() && !this.voucherNumber()) {
            this.voucherNumber.set(nextValue.toString());
          }
        }
      },
      error: err => console.error('Failed to load configuration', err)
    });
  }

  loadExistingVoucher(): void {
    const id = this.headerId();
    if (!id) return;
    this.isLoading.set(true);
    this.accountingService.getVoucherHeaders(1000).subscribe({
      next: rows => {
        const header = rows.find(r => this.getRowId(r) === id);
        if (header) {
          this.applyHeader(header);
        }
        this.isLoading.set(false);
      },
      error: err => {
        console.error('Failed to load voucher header', err);
        this.isLoading.set(false);
      }
    });

    this.accountingService.getVoucherDetails(id).subscribe({
      next: rows => {
        this.details.set(rows.map(r => this.mapDetailRow(r)));
      },
      error: err => console.error('Failed to load voucher details', err)
    });
  }

  applyHeader(row: FinancialRow): void {
    this.voucherNumber.set(this.pickValue(row, ['VoucherNumber', 'Number', 'VoucherNo'])?.toString() ?? '');
    this.voucherDate.set(this.toDateInput(this.pickValue(row, ['VoucherDate', 'Date', 'CreatedDate', 'EntryDate'])));
    this.voucherDueDate.set(this.toDateInput(this.pickValue(row, ['VoucherDueDate', 'DueDate'])));
    const typeId = this.toNumber(this.pickValue(row, ['VoucherTypeID', 'VoucherTypeId', 'TypeID', 'TypeId']));
    if (typeId) {
      this.voucherTypeId.set(typeId);
      const type = this.voucherTypes().find(t => this.getRowId(t) === typeId);
      if (type) {
        this.voucherTypeInput.set(this.formatVoucherType(type));
      }
    }
  }

  mapDetailRow(row: FinancialRow): DetailRow {
    const accountId = this.toNumber(this.pickValue(row, ['AccountID', 'AccountId'])) || 0;
    const accountCode = this.pickValue(row, ['AccountCode', 'Account'])?.toString() ?? '';
    const accountDescription = this.pickValue(row, ['AccountDescription', 'Description', 'AccountName', 'Name'])?.toString() ?? '';
    const currencyId = this.toNumber(this.pickValue(row, ['AccountCurreny', 'AccountCurrency', 'CurrencyID'])) || null;
    const rateValue = this.toNumber(this.pickValue(row, ['Rate', 'ExchangeRate', 'FxRate'])) || this.defaultRate();
    return {
      accountId,
      accountCode,
      accountDescription,
      accountCurrencyId: currencyId,
      accountCurrencyLabel: this.currencyLabel(currencyId),
      rate: rateValue,
      debitLocal: this.toNumber(this.pickValue(row, ['DbLocal', 'DebitLocal'])) || 0,
      debitMain: this.toNumber(this.pickValue(row, ['DbMain', 'DebitMain'])) || 0,
      creditLocal: this.toNumber(this.pickValue(row, ['CrLocal', 'CreditLocal'])) || 0,
      creditMain: this.toNumber(this.pickValue(row, ['CrMain', 'CreditMain'])) || 0,
      comment: this.pickValue(row, ['Comments', 'Comment', 'Remarks'])?.toString() ?? ''
    };
  }

  initDefaultDates(): void {
    const now = new Date();
    this.voucherDate.set(this.formatDateInput(now));
    this.voucherDueDate.set(this.formatDateInput(now));
  }

  formatDateInput(value: Date): string {
    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  toDateInput(value: unknown): string {
    if (!value) return '';
    const date = new Date(String(value));
    if (Number.isNaN(date.getTime())) return '';
    return this.formatDateInput(date);
  }

  onVoucherTypeInput(value: string): void {
    this.voucherTypeInput.set(value);
    this.showVoucherTypeDropdown.set(true);
    const match = this.voucherTypes().find(row => this.formatVoucherType(row) === value);
    this.voucherTypeId.set(match ? this.getRowId(match) : null);
  }

  selectVoucherType(row: FinancialRow, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.voucherTypeId.set(this.getRowId(row));
    this.voucherTypeInput.set(this.formatVoucherType(row));
    this.showVoucherTypeDropdown.set(false);
  }

  onAccountInput(value: string): void {
    this.accountInput.set(value);
    this.showAccountDropdown.set(true);
    const match = this.accounts().find(row => this.formatAccount(row) === value);
    if (match) {
      this.applyAccount(match);
    } else {
      this.accountId.set(null);
    }
  }

  selectAccount(row: FinancialRow, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.applyAccount(row);
    this.showAccountDropdown.set(false);
  }

  applyAccount(row: FinancialRow): void {
    this.accountId.set(this.getRowId(row));
    const code = this.pickValue(row, ['Code', 'AccountCode', 'Account'])?.toString() ?? '';
    this.accountInput.set(code);
    this.accountDescription.set(this.getAccountDescription(row));
    const currencyId = this.toNumber(this.pickValue(row, ['CurrencyID', 'AccountCurrency', 'AccountCurreny'])) || null;
    this.accountCurrencyId.set(currencyId);
    this.accountCurrencyLabel.set(this.currencyLabel(currencyId));
  }

  onDebitChange(): void {
    if (this.disableCredit()) {
      this.creditLocal.set('0');
      this.creditMain.set('0');
    }
  }

  onCreditChange(): void {
    if (this.disableDebit()) {
      this.debitLocal.set('0');
      this.debitMain.set('0');
    }
  }

  onDebitLocalInput(value: string): void {
    this.debitLocal.set(value);
    const rate = this.rate();
    if (rate > 0) {
      const local = this.toNumber(value);
      this.debitMain.set(this.formatAmount(local / rate));
    }
    this.onDebitChange();
  }

  onDebitMainInput(value: string): void {
    this.debitMain.set(value);
    const rate = this.rate();
    if (rate > 0) {
      const main = this.toNumber(value);
      this.debitLocal.set(this.formatAmount(main * rate));
    }
    this.onDebitChange();
  }

  onCreditLocalInput(value: string): void {
    this.creditLocal.set(value);
    const rate = this.rate();
    if (rate > 0) {
      const local = this.toNumber(value);
      this.creditMain.set(this.formatAmount(local / rate));
    }
    this.onCreditChange();
  }

  onCreditMainInput(value: string): void {
    this.creditMain.set(value);
    const rate = this.rate();
    if (rate > 0) {
      const main = this.toNumber(value);
      this.creditLocal.set(this.formatAmount(main * rate));
    }
    this.onCreditChange();
  }

  onRateInput(value: string): void {
    const rate = this.toNumber(value);
    if (rate <= 0) {
      this.rate.set(0);
      return;
    }
    this.rate.set(rate);

    const debitLocal = this.toNumber(this.debitLocal());
    const debitMain = this.toNumber(this.debitMain());
    const creditLocal = this.toNumber(this.creditLocal());
    const creditMain = this.toNumber(this.creditMain());

    if (debitLocal > 0) {
      this.debitMain.set(this.formatAmount(debitLocal / rate));
    } else if (debitMain > 0) {
      this.debitLocal.set(this.formatAmount(debitMain * rate));
    }

    if (creditLocal > 0) {
      this.creditMain.set(this.formatAmount(creditLocal / rate));
    } else if (creditMain > 0) {
      this.creditLocal.set(this.formatAmount(creditMain * rate));
    }
  }

  addDetail(): void {
    const accountId = this.accountId();
    if (!accountId) {
      alert('Select an account.');
      return;
    }
    const detail: DetailRow = {
      accountId,
      accountCode: this.accountInput(),
      accountDescription: this.accountDescription(),
      accountCurrencyId: this.accountCurrencyId(),
      accountCurrencyLabel: this.accountCurrencyLabel(),
      rate: this.rate(),
      debitLocal: this.toNumber(this.debitLocal()),
      debitMain: this.toNumber(this.debitMain()),
      creditLocal: this.toNumber(this.creditLocal()),
      creditMain: this.toNumber(this.creditMain()),
      comment: this.detailComment()
    };
    const index = this.selectedDetailIndex();
    if (index !== null) {
      const rows = [...this.details()];
      rows[index] = detail;
      this.details.set(rows);
    } else {
      this.details.set([...this.details(), detail]);
    }
    this.resetDetailInputs();
  }

  removeDetail(index: number): void {
    const rows = [...this.details()];
    rows.splice(index, 1);
    this.details.set(rows);
    if (this.selectedDetailIndex() === index) {
      this.resetDetailInputs();
    }
  }

  selectDetail(index: number): void {
    const row = this.details()[index];
    if (!row) return;
    this.selectedDetailIndex.set(index);
    this.accountId.set(row.accountId);
    this.accountInput.set(row.accountCode);
    this.accountDescription.set(row.accountDescription);
    this.accountCurrencyId.set(row.accountCurrencyId);
    this.accountCurrencyLabel.set(row.accountCurrencyLabel);
    this.rate.set(row.rate || this.defaultRate());
    this.debitLocal.set(row.debitLocal.toString());
    this.debitMain.set(row.debitMain.toString());
    this.creditLocal.set(row.creditLocal.toString());
    this.creditMain.set(row.creditMain.toString());
    this.detailComment.set(row.comment);
    setTimeout(() => this.accountInputEl?.nativeElement.focus(), 0);
  }

  resetDetailInputs(): void {
    this.accountInput.set('');
    this.accountId.set(null);
    this.accountDescription.set('');
    this.accountCurrencyId.set(null);
    this.accountCurrencyLabel.set('');
    this.rate.set(this.defaultRate());
    this.debitLocal.set('0');
    this.debitMain.set('0');
    this.creditLocal.set('0');
    this.creditMain.set('0');
    this.detailComment.set('');
    this.selectedDetailIndex.set(null);
    setTimeout(() => this.accountInputEl?.nativeElement.focus(), 0);
  }

  saveVoucher(): void {
    if (!this.voucherDate() || !this.voucherTypeId()) {
      alert('Voucher date and voucher type are required.');
      return;
    }
    if (!this.details().length) {
      alert('Add at least one detail.');
      return;
    }
    if (Math.abs(this.balanceLocal()) > 0.0001 || Math.abs(this.balanceMain()) > 0.0001) {
      alert('Balance must be zero before saving.');
      return;
    }

    const payload: VoucherSaveRequest = {
      header: {
        id: this.headerId(),
        voucherDate: this.voucherDate(),
        voucherDueDate: this.voucherDueDate() || null,
        voucherTypeId: this.voucherTypeId()!,
        voucherNumber: this.voucherNumber() || null
      },
      details: this.details().map(row => ({
        accountId: row.accountId,
        accountCode: row.accountCode,
        accountCurrencyId: row.accountCurrencyId,
        accountDescription: row.accountDescription,
        rate: row.rate,
        debitLocal: row.debitLocal,
        debitMain: row.debitMain,
        creditLocal: row.creditLocal,
        creditMain: row.creditMain,
        comment: row.comment || null
      }))
    };

    this.isSaving.set(true);
    if (this.isEditMode()) {
      this.accountingService.updateVoucher(this.headerId()!, payload).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.router.navigate(['/accounting/journal-voucher']);
        },
        error: (err: unknown) => {
          console.error('Failed to save voucher', err);
          this.isSaving.set(false);
          alert('Failed to save voucher.');
        }
      });
      return;
    }

    this.accountingService.createVoucher(payload).subscribe({
      next: (result: { id: number }) => {
        this.isSaving.set(false);
        this.headerId.set(result.id);
        this.isEditMode.set(true);
        this.persistLastVoucherNumber();
        this.router.navigate(['/accounting/journal-voucher']);
      },
      error: (err: unknown) => {
        console.error('Failed to save voucher', err);
        this.isSaving.set(false);
        alert('Failed to save voucher.');
      }
    });
  }

  deleteVoucher(): void {
    const id = this.headerId();
    if (!id) return;
    if (!confirm('Delete this voucher?')) return;
    this.accountingService.deleteVoucher(id).subscribe({
      next: () => this.router.navigate(['/accounting/journal-voucher']),
      error: err => {
        console.error('Failed to delete voucher', err);
        alert('Failed to delete voucher.');
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/accounting/journal-voucher']);
  }

  formatVoucherType(row: FinancialRow): string {
    const code = this.pickValue(row, ['Code', 'VoucherTypeCode', 'TypeCode'])?.toString() ?? '';
    const description = this.pickValue(row, ['Description', 'VoucherTypeDescription', 'Name'])?.toString() ?? '';
    if (code && description) {
      return `${code} - ${description}`;
    }
    return code || description;
  }

  formatAccount(row: FinancialRow): string {
    const code = this.pickValue(row, ['Code', 'AccountCode', 'Account'])?.toString() ?? '';
    const description = this.getAccountDescription(row);
    const currencyId = this.toNumber(this.pickValue(row, ['CurrencyID', 'AccountCurrency', 'AccountCurreny'])) || null;
    const currency = this.currencyLabel(currencyId);
    const label = description ? `${code} - ${description}` : code;
    return currency ? `${label} (${currency})` : label;
  }

  getAccountDescription(row: FinancialRow): string {
    return this.pickValue(row, ['Desciption', 'Description', 'description'])?.toString() ?? '';
  }

  getAccountCurrencyLabel(row: FinancialRow): string {
    const currencyId = this.toNumber(this.pickValue(row, ['CurrencyID', 'AccountCurrency', 'AccountCurreny'])) || null;
    return this.currencyLabel(currencyId);
  }

  currencyLabel(currencyId: number | null): string {
    if (!currencyId) return '';
    if (currencyId === 1) return 'L.L';
    if (currencyId === 2) return '$$';
    return currencyId.toString();
  }

  formatAmount(value: number): string {
    if (!Number.isFinite(value)) return '0';
    return value.toFixed(2);
  }

  toNumber(value: unknown): number {
    if (value == null) return 0;
    const numeric = typeof value === 'number'
      ? value
      : Number(String(value).replace(/,/g, '').trim());
    return Number.isFinite(numeric) ? numeric : 0;
  }

  getRowId(row: FinancialRow): number {
    const value = this.pickValue(row, ['ID', 'Id', 'id']);
    return this.toNumber(value);
  }

  pickValue(row: FinancialRow, keys: string[]): unknown {
    for (const key of keys) {
      if (row[key] !== undefined && row[key] !== null) {
        return row[key];
      }
    }
    return null;
  }

  getDefaultRate(rows: FinancialRow[]): number | null {
    for (const row of rows) {
      const value = this.pickValue(row, ['DefaultRate', 'defaultRate', 'Rate', 'rate']);
      if (value != null) {
        const numeric = this.toNumber(value);
        if (numeric) return numeric;
      }
      const code = this.pickValue(row, ['Code', 'Name', 'Key'])?.toString()?.toLowerCase();
      if (code && code.includes('defaultrate')) {
        const fallback = this.pickValue(row, ['Value', 'ConfigValue', 'Val']) ?? null;
        const numeric = this.toNumber(fallback);
        if (numeric) return numeric;
      }
    }
    return null;
  }

  getLastVoucherInfo(rows: FinancialRow[]): { rowId: number; column: string; value: number } | null {
    const columnCandidates = [
      'LastVoucherNumber',
      'LastVoucherNo',
      'LastVoucherNb',
      'LastVoucher',
      'lastVoucherNumber',
      'lastVoucherNo',
      'lastVoucherNb'
    ];
    for (const row of rows) {
      const rowId = this.getRowId(row);
      if (!rowId) continue;
      for (const column of columnCandidates) {
        const raw = row[column];
        if (raw === undefined || raw === null) continue;
        const value = this.toNumber(raw);
        return { rowId, column, value };
      }
    }
    return null;
  }

  persistLastVoucherNumber(): void {
    const rowId = this.configRowId();
    const column = this.configLastVoucherColumn();
    const nextValue = this.nextVoucherNumber();
    if (!rowId || !column || nextValue === null) return;
    this.accountingService.updateConfiguration(rowId, { [column]: nextValue }).subscribe({
      next: () => {},
      error: err => console.error('Failed to update last voucher number', err)
    });
  }
}
