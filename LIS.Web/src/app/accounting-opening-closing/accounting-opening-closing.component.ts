import { Component, computed, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccountingService, OpeningClosingRequest, OpeningClosingPreview } from '../services/accounting.service';

@Component({
  selector: 'app-accounting-opening-closing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './accounting-opening-closing.component.html',
  styleUrls: ['./accounting-opening-closing.component.scss']
})
export class AccountingOpeningClosingComponent {
  year = signal(new Date().getFullYear());
  preview = signal<OpeningClosingPreview | null>(null);
  isLoading = signal(false);
  message = signal('');

  private accountingService = inject(AccountingService);

  // Computed properties for preview data
  closingRows = computed(() => {
    const p = this.preview();
    return (p ? p['closing'] : null) as any[] | null || [];
  });
  openingRows = computed(() => {
    const p = this.preview();
    return (p ? p['opening'] : null) as any[] | null || [];
  });

  generatePreview(): void {
    this.message.set('');
    this.isLoading.set(true);
    const req: OpeningClosingRequest = { year: this.year() };
    this.accountingService.previewOpeningClosing(req).subscribe({
      next: p => {
        this.preview.set(p);
        this.isLoading.set(false);
      },
      error: err => {
        console.error('preview failed', err);
        this.message.set('Failed to compute preview');
        this.isLoading.set(false);
      }
    });
  }

  save(): void {
    if (!this.preview()) {
      this.message.set('Please generate preview first');
      return;
    }
    this.isLoading.set(true);
    const req: OpeningClosingRequest = { year: this.year() };
    this.accountingService.generateOpeningClosing(req).subscribe({
      next: (res: any) => {
        this.message.set(`Saved closing voucher ${res['closingVoucherId']} and opening voucher ${res['openingVoucherId']}`);
        this.isLoading.set(false);
      },
      error: (err: any) => {
        console.error('save failed', err);
        this.message.set('Failed to save vouchers');
        this.isLoading.set(false);
      }
    });
  }

  readonly closingTotals = computed(() => {
    const p = this.preview();
    return p
      ? { debit: p['closingTotalDebit'], credit: p['closingTotalCredit'] }
      : { debit: 0, credit: 0 };
  });

  readonly openingTotals = computed(() => {
    const p = this.preview();
    return p
      ? { debit: p['openingTotalDebit'], credit: p['openingTotalCredit'] }
      : { debit: 0, credit: 0 };
  });
}
