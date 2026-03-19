import { Component, computed, inject, signal, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InvoiceService } from '../services/invoice.service';
import { InvoiceHeader, CreateInvoiceHeaderRequest } from '../models/invoice-header';
import { InvoiceDetail, CreateInvoiceDetailRequest, InvoiceTotals } from '../models/invoice-detail';
import { Denomination } from '../models/denomination';

@Component({
  selector: 'app-invoice',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './invoice.component.html',
  styleUrls: ['./invoice.component.scss']
})
export class InvoiceComponent implements OnInit {
  private invoiceService = inject(InvoiceService);

  // Expose Math to template
  Math = Math;

  // Signals for state management
  readonly invoiceHeaders = signal<InvoiceHeader[]>([]);
  readonly invoiceDetails = signal<InvoiceDetail[]>([]);
  readonly denominations = signal<Denomination[]>([]);
  readonly selectedInvoice = signal<InvoiceHeader | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly searchQuery = signal('');

  // Form signals
  readonly showInvoiceForm = signal(false);
  readonly showDetailForm = signal(false);
  readonly editingDetail = signal<InvoiceDetail | null>(null);

  // New invoice form
  readonly newInvoice = signal<CreateInvoiceHeaderRequest>({
    invoiceDate: new Date(),
    notes: ''
  });

  // New detail form
  readonly newDetail = signal<CreateInvoiceDetailRequest>({
    invoiceHeaderId: 0,
    itemDescription: '',
    quantity: 1,
    unitPrice: 0,
    denominationId: undefined
  });

  // Denomination search
  readonly denominationSearchQuery = signal('');
  readonly showDenominationDropdown = signal(false);
  readonly filteredDenominations = signal<Denomination[]>([]);

  // Computed totals
  readonly totals = computed(() => {
    const details = this.invoiceDetails();
    return this.invoiceService.calculateTotals(details);
  });

  // Computed filtered invoices
  readonly filteredInvoices = computed(() => {
    const invoices = this.invoiceHeaders();
    const query = this.searchQuery().toLowerCase();

    if (!query) return invoices;

    return invoices.filter(invoice =>
      invoice.invoiceNumber.toLowerCase().includes(query) ||
      invoice.mrn?.toLowerCase().includes(query) ||
      invoice.admissionNumber?.toLowerCase().includes(query) ||
      invoice.status.toLowerCase().includes(query)
    );
  });

  @ViewChild('denominationSearchInput') denominationSearchInput?: ElementRef<HTMLInputElement>;

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.isLoading.set(true);
    this.errorMessage.set('');

    // Load invoice headers
    this.invoiceService.loadInvoiceHeaders().subscribe({
      next: (invoices) => {
        this.invoiceHeaders.set(invoices);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to load invoices: ' + error.message);
        this.isLoading.set(false);
      }
    });

    // Load denominations
    this.invoiceService.loadDenominations().subscribe({
      next: (denominations) => {
        this.denominations.set(denominations);
        this.filteredDenominations.set(denominations);
      },
      error: (error) => {
        console.error('Failed to load denominations:', error);
      }
    });
  }

  selectInvoice(invoice: InvoiceHeader) {
    this.selectedInvoice.set(invoice);
    this.newDetail.set({
      ...this.newDetail(),
      invoiceHeaderId: invoice.id
    });

    // Load invoice details
    this.invoiceService.loadInvoiceDetails(invoice.id).subscribe({
      next: (details) => {
        this.invoiceDetails.set(details);
      },
      error: (error) => {
        this.errorMessage.set('Failed to load invoice details: ' + error.message);
      }
    });
  }

  createInvoice() {
    const invoiceData = this.newInvoice();

    this.invoiceService.createInvoiceHeader(invoiceData).subscribe({
      next: (invoice) => {
        this.invoiceHeaders.set([...this.invoiceHeaders(), invoice]);
        this.showInvoiceForm.set(false);
        this.newInvoice.set({
          invoiceDate: new Date(),
          notes: ''
        });
        this.selectInvoice(invoice);
      },
      error: (error) => {
        this.errorMessage.set('Failed to create invoice: ' + error.message);
      }
    });
  }

  addDetail() {
    const detailData = this.newDetail();

    if (!detailData.itemDescription || detailData.quantity <= 0 || detailData.unitPrice < 0) {
      this.errorMessage.set('Please fill in all required fields with valid values');
      return;
    }

    this.invoiceService.createInvoiceDetail(detailData).subscribe({
      next: (detail) => {
        this.invoiceDetails.set([...this.invoiceDetails(), detail]);
        this.resetDetailForm();
        this.updateInvoiceTotal();
      },
      error: (error) => {
        this.errorMessage.set('Failed to add item: ' + error.message);
      }
    });
  }

  updateDetail(detail: InvoiceDetail) {
    this.editingDetail.set(detail);
    this.newDetail.set({
      invoiceHeaderId: detail.invoiceHeaderId,
      itemDescription: detail.itemDescription,
      quantity: detail.quantity,
      unitPrice: detail.unitPrice,
      denominationId: detail.denominationId,
      discountAmount: detail.discountAmount,
      taxAmount: detail.taxAmount,
      notes: detail.notes
    });
    this.showDetailForm.set(true);
  }

  saveDetailUpdate() {
    const detail = this.editingDetail();
    const detailData = this.newDetail();

    if (!detail || !detailData.itemDescription || detailData.quantity <= 0 || detailData.unitPrice < 0) {
      this.errorMessage.set('Please fill in all required fields with valid values');
      return;
    }

    this.invoiceService.updateInvoiceDetail({
      id: detail.id,
      ...detailData
    }).subscribe({
      next: (updatedDetail) => {
        const details = this.invoiceDetails().map(d => d.id === updatedDetail.id ? updatedDetail : d);
        this.invoiceDetails.set(details);
        this.resetDetailForm();
        this.updateInvoiceTotal();
      },
      error: (error) => {
        this.errorMessage.set('Failed to update item: ' + error.message);
      }
    });
  }

  deleteDetail(detail: InvoiceDetail) {
    if (confirm('Are you sure you want to delete this item?')) {
      this.invoiceService.deleteInvoiceDetail(detail.id).subscribe({
        next: () => {
          const details = this.invoiceDetails().filter(d => d.id !== detail.id);
          this.invoiceDetails.set(details);
          this.updateInvoiceTotal();
        },
        error: (error) => {
          this.errorMessage.set('Failed to delete item: ' + error.message);
        }
      });
    }
  }

  updateInvoiceTotal() {
    const invoice = this.selectedInvoice();
    if (!invoice) return;

    const totals = this.totals();

    this.invoiceService.updateInvoiceHeader({
      id: invoice.id,
      totalAmount: totals.totalAmount
    }).subscribe({
      next: (updatedInvoice) => {
        this.selectedInvoice.set(updatedInvoice);
        const invoices = this.invoiceHeaders().map(i => i.id === updatedInvoice.id ? updatedInvoice : i);
        this.invoiceHeaders.set(invoices);
      },
      error: (error) => {
        console.error('Failed to update invoice total:', error);
      }
    });
  }

  resetDetailForm() {
    const selectedInvoice = this.selectedInvoice();
    this.newDetail.set({
      invoiceHeaderId: selectedInvoice?.id || 0,
      itemDescription: '',
      quantity: 1,
      unitPrice: 0,
      denominationId: undefined
    });
    this.editingDetail.set(null);
    this.showDetailForm.set(false);
  }

  searchDenominations() {
    const query = this.denominationSearchQuery();
    if (query.length < 2) {
      this.filteredDenominations.set(this.denominations());
      return;
    }

    this.invoiceService.searchDenominations(query).subscribe({
      next: (denominations) => {
        this.filteredDenominations.set(denominations);
        this.showDenominationDropdown.set(true);
      },
      error: (error) => {
        console.error('Failed to search denominations:', error);
      }
    });
  }

  selectDenomination(denomination: Denomination) {
    this.newDetail.set({
      ...this.newDetail(),
      itemDescription: denomination.smallDescription,
      unitPrice: 0, // No price in denomination model, user will need to enter manually
      denominationId: denomination.id
    });
    this.showDenominationDropdown.set(false);
    this.denominationSearchQuery.set('');
  }

  calculateLineTotal(detail: InvoiceDetail): number {
    const subtotal = detail.quantity * detail.unitPrice;
    const discount = detail.discountAmount || 0;
    const tax = detail.taxAmount || 0;
    return subtotal - discount + tax;
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }
}
