import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InvoiceHeader, CreateInvoiceHeaderRequest, UpdateInvoiceHeaderRequest } from '../models/invoice-header';
import { InvoiceDetail, CreateInvoiceDetailRequest, UpdateInvoiceDetailRequest, InvoiceTotals } from '../models/invoice-detail';
import { ApiEndpointsService } from '../api/api-endpoints.service';

// Use the existing Denomination interface from lab-tests service
export interface Denomination {
  id: number;
  smallDescription: string;
  code?: string | null;
  displayOrder?: number | null;
}

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  private http = inject(HttpClient);
  private readonly endpoints = inject(ApiEndpointsService);
  private readonly apiUrl = this.endpoints.invoice;
  private readonly denominationUrl = this.endpoints.denomination;

  // Signals for state management
  readonly invoiceHeaders = signal<InvoiceHeader[]>([]);
  readonly invoiceDetails = signal<InvoiceDetail[]>([]);
  readonly denominations = signal<Denomination[]>([]);
  readonly selectedInvoice = signal<InvoiceHeader | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');

  // Invoice Header Operations
  loadInvoiceHeaders(): Observable<InvoiceHeader[]> {
    this.isLoading.set(true);
    return this.http.get<InvoiceHeader[]>(`${this.apiUrl}/headers`);
  }

  getInvoiceHeader(id: number): Observable<InvoiceHeader> {
    return this.http.get<InvoiceHeader>(`${this.apiUrl}/headers/${id}`);
  }

  createInvoiceHeader(request: CreateInvoiceHeaderRequest): Observable<InvoiceHeader> {
    return this.http.post<InvoiceHeader>(`${this.apiUrl}/headers`, request);
  }

  updateInvoiceHeader(request: UpdateInvoiceHeaderRequest): Observable<InvoiceHeader> {
    return this.http.put<InvoiceHeader>(`${this.apiUrl}/headers/${request.id}`, request);
  }

  deleteInvoiceHeader(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/headers/${id}`);
  }

  // Invoice Detail Operations
  loadInvoiceDetails(invoiceHeaderId: number): Observable<InvoiceDetail[]> {
    return this.http.get<InvoiceDetail[]>(`${this.apiUrl}/details/${invoiceHeaderId}`);
  }

  getInvoiceDetail(id: number): Observable<InvoiceDetail> {
    return this.http.get<InvoiceDetail>(`${this.apiUrl}/details/${id}`);
  }

  createInvoiceDetail(request: CreateInvoiceDetailRequest): Observable<InvoiceDetail> {
    return this.http.post<InvoiceDetail>(`${this.apiUrl}/details`, request);
  }

  updateInvoiceDetail(request: UpdateInvoiceDetailRequest): Observable<InvoiceDetail> {
    return this.http.put<InvoiceDetail>(`${this.apiUrl}/details/${request.id}`, request);
  }

  deleteInvoiceDetail(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/details/${id}`);
  }

  // Denomination Operations
  loadDenominations(): Observable<Denomination[]> {
    return this.http.get<Denomination[]>(this.denominationUrl);
  }

  searchDenominations(query: string): Observable<Denomination[]> {
    return this.http.get<Denomination[]>(`${this.denominationUrl}/search?query=${query}`);
  }

  // Totals Calculation
  calculateTotals(details: InvoiceDetail[]): InvoiceTotals {
    const subtotal = details.reduce((sum, detail) => sum + (detail.quantity * detail.unitPrice), 0);
    const totalDiscount = details.reduce((sum, detail) => sum + (detail.discountAmount || 0), 0);
    const totalTax = details.reduce((sum, detail) => sum + (detail.taxAmount || 0), 0);
    const totalAmount = subtotal - totalDiscount + totalTax;
    const itemCount = details.length;

    return {
      subtotal,
      totalDiscount,
      totalTax,
      totalAmount,
      itemCount
    };
  }

  // Search and Filter
  searchInvoices(query: string): Observable<InvoiceHeader[]> {
    return this.http.get<InvoiceHeader[]>(`${this.apiUrl}/headers/search?query=${query}`);
  }

  getInvoicesByPatient(patientId: number): Observable<InvoiceHeader[]> {
    return this.http.get<InvoiceHeader[]>(`${this.apiUrl}/headers/patient/${patientId}`);
  }

  getInvoicesByAdmission(admissionNumber: string): Observable<InvoiceHeader[]> {
    return this.http.get<InvoiceHeader[]>(`${this.apiUrl}/headers/admission/${admissionNumber}`);
  }
}
