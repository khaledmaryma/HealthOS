import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import {
  AccountingService,
  CashierDetailRow,
  CashierDepartmentSummary,
  CashierOpenDetailsResponse,
  CashierForPrintResponse,
} from '../services/accounting.service';
import { ApiEndpointsService } from '../api/api-endpoints.service';

interface HospitalConfig {
  hospitalName?: string | null;
  hospitalNameArabic?: string | null;
  logoBase64?: string | null;
}

@Component({
  selector: 'app-accounting-cashier',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './accounting-cashier.component.html',
  styleUrl: './accounting-cashier.component.scss',
})
export class AccountingCashierComponent implements OnInit {
  private accountingService = inject(AccountingService);
  private http = inject(HttpClient);
  private endpoints = inject(ApiEndpointsService);
  hospitalConfig = signal<HospitalConfig | null>(null);

  openDate = signal<string | null>(null);
  details = signal<CashierDetailRow[]>([]);
  searchTerm = signal('');
  summary = signal<CashierDepartmentSummary[]>([]);
  totalLBP = signal(0);
  totalUSD = signal(0);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  showCloseDateModal = signal(false);
  newOpenDate = signal('');
  closeDateLoading = signal(false);
  closeDateError = signal<string | null>(null);

  filteredDetails = computed(() => {
    const list = this.details();
    const q = this.searchTerm().toLowerCase().trim();
    if (!q) return list;
    return list.filter(
      (d) =>
        (d.department ?? '').toLowerCase().includes(q) ||
        (d.distributionTypeDescription ?? '').toLowerCase().includes(q) ||
        (d.payedBY ?? '').toLowerCase().includes(q) ||
        (d.mouvementNb ?? '').toLowerCase().includes(q) ||
        String(d.voucherNumber ?? '').includes(q) ||
        String(d.dailyCounter ?? '').includes(q) ||
        String(d.cashierDetailCounter ?? '').includes(q)
    );
  });

  totalAmountToPay = computed(() =>
    this.filteredDetails().reduce((s, d) => s + (d.amoutToBePayed ?? 0), 0)
  );
  totalAmountLBP = computed(() =>
    this.filteredDetails().reduce(
      (s, d) => s + ((d.accountCurrency === 1 ? d.amoutToBePayed : 0) ?? 0),
      0
    )
  );
  totalAmountUSD = computed(() =>
    this.filteredDetails().reduce(
      (s, d) => s + ((d.accountCurrency !== 1 ? d.amoutToBePayed : 0) ?? 0),
      0
    )
  );
  totalDiffUSD = computed(() =>
    this.filteredDetails().reduce((s, d) => s + (d.differenceUSD ?? 0), 0)
  );
  totalDiffLBP = computed(() =>
    this.filteredDetails().reduce((s, d) => s + (d.differenceLBP ?? 0), 0)
  );
  totalCollLBP = computed(() =>
    this.filteredDetails().reduce((s, d) => s + (d.collectionLBP ?? 0), 0)
  );
  totalCollUSD = computed(() =>
    this.filteredDetails().reduce((s, d) => s + (d.collectionUSD ?? 0), 0)
  );

  ngOnInit(): void {
    this.load();
    this.http.get<HospitalConfig>(this.endpoints.hospitalConfiguration).subscribe({
      next: (c) => this.hospitalConfig.set(c),
      error: () => {},
    });
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.accountingService.getCashierOpenDetails().subscribe({
      next: (res: CashierOpenDetailsResponse) => {
        this.openDate.set(res.openDate ?? null);
        this.details.set(res.details ?? []);
        this.summary.set(res.summary ?? []);
        this.totalLBP.set(res.totals?.totalLBP ?? 0);
        this.totalUSD.set(res.totals?.totalUSD ?? 0);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Cashier load failed', err);
        this.errorMessage.set(err?.message || 'Failed to load cashier data');
        this.isLoading.set(false);
      },
    });
  }

  formatNumber(value: number | null | undefined, decimals = 2): string {
    if (value == null) return '-';
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    }).format(value);
  }

  formatInt(value: number | null | undefined): string {
    if (value == null) return '-';
    return String(value);
  }

  formatOpenDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '-';
    try {
      return new Date(dateStr).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
      });
    } catch {
      return dateStr;
    }
  }

  printDraft(): void {
    this.errorMessage.set(null);
    const w = window.open('', '_blank', 'width=900,height=700');
    if (!w) {
      this.errorMessage.set('Please allow popups to print');
      return;
    }
    w.document.write('<html><body><p>Loading...</p></body></html>');
    this.accountingService.getCashierForPrint(true).subscribe({
      next: (res) => this.printCashier(res, true, w),
      error: (err) => {
        try { w.document.body!.innerHTML = `<p style="color:red">${err?.message || 'Failed to load cashier for print'}</p>`; } catch { w.close(); }
        this.errorMessage.set(err?.error?.message || err?.message || 'Failed to load cashier for print');
      },
    });
  }

  printLastClosed(): void {
    this.errorMessage.set(null);
    const w = window.open('', '_blank', 'width=900,height=700');
    if (!w) {
      this.errorMessage.set('Please allow popups to print');
      return;
    }
    w.document.write('<html><body><p>Loading...</p></body></html>');
    this.accountingService.getCashierForPrint(false).subscribe({
      next: (res) => this.printCashier(res, false, w),
      error: (err) => {
        try { w.document.body!.innerHTML = `<p style="color:red">${err?.message || 'Failed to load last closed cashier'}</p>`; } catch { w.close(); }
        this.errorMessage.set(err?.error?.message || err?.message || 'Failed to load last closed cashier');
      },
    });
  }

  private printCashier(res: CashierForPrintResponse, isDraft: boolean, w: Window): void {
    const cfg = this.hospitalConfig();
    const hospitalName = cfg?.hospitalName ?? 'Medical Center';
    const hospitalNameAr = cfg?.hospitalNameArabic ?? '';
    const logoSrc = cfg?.logoBase64 ? `data:image/png;base64,${cfg.logoBase64}` : '';

    const asOfDate = (() => {
      const dateStr = res.closeDate || res.openDate;
      if (!dateStr) return new Date();
      let timeStr = '12:00:00';
      if (res.closeDate && res.closeTime && /^\d{1,2}:\d{2}$/.test(res.closeTime)) {
        timeStr = res.closeTime + ':00';
      } else if (res.closeDate && res.closeTime && /^\d{1,2}:\d{2}:\d{2}$/.test(res.closeTime)) {
        timeStr = res.closeTime;
      }
      return new Date(dateStr + 'T' + timeStr);
    })();
    const asOfStr = asOfDate.toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
    });

    const rows = (res.details ?? []).map(
      (d) =>
        `<tr>
          <td class="col-num">${d.dailyCounter ?? ''}</td>
          <td class="col-type">${d.distributionTypeDescription ?? ''}</td>
          <td class="col-paidby">${d.payedBY ?? ''}</td>
          <td class="te br col-amt">${this.formatNumber(d.amoutToBePayed)}</td>
          <td class="te col-ll">${this.formatNumber(d.collectionLBP, 0)}</td>
          <td class="te col-usd">${this.formatNumber(d.collectionUSD)}</td>
        </tr>`
    );

    const byType = new Map<string, { lbp: number; usd: number }>();
    (res.details ?? []).forEach((d) => {
      const t = d.distributionTypeDescription ?? '(Blank)';
      const cur = byType.get(t) ?? { lbp: 0, usd: 0 };
      cur.lbp += d.collectionLBP ?? 0;
      cur.usd += d.collectionUSD ?? 0;
      byType.set(t, cur);
    });
    const typeRows = Array.from(byType.entries())
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([t, v]) => `<tr><td>${t}</td><td class="te">${this.formatNumber(v.lbp, 0)}</td><td class="te">${this.formatNumber(v.usd)}</td></tr>`);

    const summaryRows = (res.summary ?? []).map(
      (s) =>
        `<tr><td>${s.department}</td><td class="te">${this.formatNumber(s.collectionLBP, 0)}</td><td class="te">${this.formatNumber(s.collectionUSD)}</td></tr>`
    );

    const totalLBP = res.totals?.totalLBP ?? 0;
    const totalUSD = res.totals?.totalUSD ?? 0;
    const draftWatermark = isDraft
      ? '<div class="watermark">DRAFT</div>'
      : '';

    w.document.open();
    w.document.write(`
<!DOCTYPE html>
<html>
<head><title>Cashier List</title>
<style>
  * { box-sizing: border-box; }
  body { font-family: 'Times New Roman', serif; padding: 12px; margin: 0; position: relative; font-size: 10px; }
  .watermark { position: fixed; top: 30%; left: 20%; font-size: 100px; color: rgba(200,0,0,0.15); font-weight: bold; transform: rotate(-45deg); pointer-events: none; z-index: 1; }
  .print-header { display: table-header-group; }
  .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 8px; }
  .header-left, .header-right { flex: 1; font-size: 11px; }
  .header-right { text-align: right; direction: rtl; }
  .header-center { flex: 0 0 auto; text-align: center; }
  .header-center img { max-height: 50px; max-width: 90px; }
  .header-center .dmc { font-size: 10px; margin-top: 2px; }
  .report-title { font-size: 12px; font-weight: bold; margin: 6px 0 2px 0; border-top: 1px solid #000; border-bottom: 1px solid #000; padding: 4px 0; }
  .as-of { text-align: center; margin-bottom: 8px; font-size: 10px; border-top: 1px solid #000; border-bottom: 1px solid #000; padding: 4px 0; }
  table { width: 100%; border-collapse: collapse; font-size: 9px; }
  .main-table th, .main-table td { padding: 3px 6px; text-align: left; white-space: nowrap; }
  .main-table th { font-size: 9px; border-top: 1px solid #000; border-bottom: 1px solid #000; }
  .main-table .te { text-align: right; }
  .main-table .br { border-right: 1px solid #000; }
  .main-table .col-num { width: 3%; }
  .main-table .col-type { width: 12%; }
  .main-table .col-paidby { width: 35%; }
  .main-table .col-amt { width: 15%; }
  .main-table .col-ll { width: 17%; }
  .main-table .col-usd { width: 18%; }
  .main-table .title-row td { border-top: 1px solid #000; border-bottom: 1px solid #000; }
  .summary-wrap { display: flex; gap: 30px; margin-top: 16px; font-size: 9px; }
  .summary-box { flex: 1; }
  .summary-box table { width: 100%; }
  .summary-box th, .summary-box td { padding: 3px 6px; text-align: left; white-space: nowrap; }
  .summary-box th { border-top: 1px solid #000; border-bottom: 1px solid #000; }
  .summary-box .te { text-align: right; }
  .summary-box .balance { font-weight: bold; border-top: 1px solid #000; border-bottom: 1px solid #000; }
  .balance-detail { margin-top: 16px; font-size: 9px; }
  .balance-detail-title { font-weight: bold; margin-bottom: 8px; border-top: 1px solid #000; border-bottom: 1px solid #000; padding: 4px 0; }
  .balance-detail .row { display: flex; gap: 16px; margin: 2px 0; }
  .balance-detail .label { min-width: 80px; }
  .signatures { display: flex; justify-content: space-between; margin-top: 24px; padding-top: 12px; }
  .sig { flex: 1; text-align: center; border-top: 1px solid #000; padding-top: 4px; font-size: 9px; max-width: 160px; }
  @media print {
    body { padding: 6px; }
    .watermark { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    .print-header { display: table-header-group; }
  }
</style>
</head>
<body>
  ${draftWatermark}
  <table class="main-table">
    <thead class="print-header">
      <tr><td colspan="6" style="padding:0; border:none;">
        <div class="header">
          <div class="header-left">${hospitalName}</div>
          <div class="header-center">
            ${logoSrc ? `<img src="${logoSrc}" alt="Logo">` : '<div style="width:50px;height:50px;border:1px solid #ccc;margin:0 auto;"></div>'}
            <div class="dmc">${hospitalNameAr || 'D.M.C'}</div>
          </div>
          <div class="header-right">${hospitalNameAr || hospitalName}</div>
        </div>
        <div class="report-title">Cashier List</div>
        <div class="as-of">As Of : ${asOfStr}</div>
      </td></tr>
      <tr>
        <th class="col-num">#</th><th class="col-type">Type</th><th class="col-paidby">Patient — A/C Description (Paidby)</th>
        <th class="te br col-amt">Inv. Amount</th><th class="te col-ll">Amt L.L</th><th class="te col-usd">Amt $$</th>
      </tr>
    </thead>
    <tbody>${rows.join('')}</tbody>
    <tfoot>
      <tr class="title-row">
        <td colspan="3"><strong>Balance</strong></td>
        <td class="te br col-amt">&nbsp;</td>
        <td class="te col-ll"><strong>${this.formatNumber(totalLBP, 0)}</strong></td>
        <td class="te col-usd"><strong>${this.formatNumber(totalUSD)}</strong></td>
      </tr>
    </tfoot>
  </table>

  <div class="summary-wrap">
    <div class="summary-box">
      <table><thead><tr><th>Summary / Type</th><th class="te">L.L</th><th class="te">$$</th></tr></thead>
        <tbody>${typeRows.join('')}</tbody>
        <tfoot><tr class="balance"><td><strong>Balance</strong></td><td class="te"><strong>${this.formatNumber(totalLBP, 0)}</strong></td><td class="te"><strong>${this.formatNumber(totalUSD)}</strong></td></tr></tfoot>
      </table>
    </div>
    <div class="summary-box">
      <table><thead><tr><th>Dep</th><th class="te">L.L</th><th class="te">$$</th></tr></thead>
        <tbody>${summaryRows.join('')}</tbody>
        <tfoot><tr class="balance"><td><strong>Balance</strong></td><td class="te"><strong>${this.formatNumber(totalLBP, 0)}</strong></td><td class="te"><strong>${this.formatNumber(totalUSD)}</strong></td></tr></tfoot>
      </table>
    </div>
  </div>

  <div class="balance-detail">
    <div class="balance-detail-title">Balance Detail</div>
    <div class="row"><span class="label">Cash</span><span>L.L: ${this.formatNumber(totalLBP, 0)}</span><span>$$: ${this.formatNumber(totalUSD)}</span></div>
  </div>

  <div class="signatures">
    <div class="sig">Cashier Signature</div>
    <div class="sig">Controler Signature</div>
    <div class="sig">Manager Signature</div>
  </div>

  <script>window.onload=function(){window.print();}</script>
</body>
</html>
    `);
    w.document.close();
    w.focus();
  }

  openCloseDateModal(): void {
    this.newOpenDate.set(new Date().toISOString().slice(0, 10));
    this.closeDateError.set(null);
    this.showCloseDateModal.set(true);
  }

  closeCloseDateModal(): void {
    this.showCloseDateModal.set(false);
  }

  confirmCloseDate(): void {
    const date = this.newOpenDate().trim();
    if (!date) {
      this.closeDateError.set('Please select a date');
      return;
    }
    this.closeDateLoading.set(true);
    this.closeDateError.set(null);
    this.accountingService.closeCashierAndOpenNew(date).subscribe({
      next: () => {
        this.closeDateLoading.set(false);
        this.showCloseDateModal.set(false);
        this.load();
      },
      error: (err) => {
        this.closeDateLoading.set(false);
        this.closeDateError.set(err?.error?.message || err?.message || 'Failed to close cashier');
      },
    });
  }
}
