import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BillingInvoiceHeader } from '../models/billing-invoice-header';
import { BillingInvoiceDetail } from '../models/billing-invoice-detail';
import { HospitalDenomination } from '../models/hospital-denomination';
import { DenominationSearchResult } from '../models/denomination-search-result';

@Injectable({
  providedIn: 'root'
})
export class BillingService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5050/api';

  // Invoice Header methods
  getInvoiceHeaders(): Observable<BillingInvoiceHeader[]> {
    return this.http.get<BillingInvoiceHeader[]>(`${this.apiUrl}/BillingInvoiceHeader`);
  }

  getInvoiceHeader(id: number): Observable<BillingInvoiceHeader> {
    return this.http.get<BillingInvoiceHeader>(`${this.apiUrl}/BillingInvoiceHeader/${id}`);
  }

  createInvoiceHeader(header: BillingInvoiceHeader): Observable<BillingInvoiceHeader> {
    return this.http.post<BillingInvoiceHeader>(`${this.apiUrl}/BillingInvoiceHeader`, header);
  }

  updateInvoiceHeader(id: number, header: BillingInvoiceHeader): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/BillingInvoiceHeader/${id}`, header);
  }

  deleteInvoiceHeader(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/BillingInvoiceHeader/${id}`);
  }

  // Invoice Detail methods
  getInvoiceDetails(): Observable<BillingInvoiceDetail[]> {
    return this.http.get<BillingInvoiceDetail[]>(`${this.apiUrl}/BillingInvoiceDetail`);
  }

  getInvoiceDetailsByHeaderId(headerId: number): Observable<BillingInvoiceDetail[]> {
    return this.http.get<BillingInvoiceDetail[]>(`${this.apiUrl}/BillingInvoiceDetail/ByHeader/${headerId}`);
  }

  getInvoiceDetail(id: number): Observable<BillingInvoiceDetail> {
    return this.http.get<BillingInvoiceDetail>(`${this.apiUrl}/BillingInvoiceDetail/${id}`);
  }

  createInvoiceDetail(detail: BillingInvoiceDetail): Observable<BillingInvoiceDetail> {
    return this.http.post<BillingInvoiceDetail>(`${this.apiUrl}/BillingInvoiceDetail`, detail);
  }

  updateInvoiceDetail(id: number, detail: BillingInvoiceDetail): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/BillingInvoiceDetail/${id}`, detail);
  }

  deleteInvoiceDetail(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/BillingInvoiceDetail/${id}`);
  }

  // Denomination methods
  getDenominations(): Observable<HospitalDenomination[]> {
    return this.http.get<HospitalDenomination[]>(`${this.apiUrl}/HospitalDefinition/Denomination`);
  }

  getDenomination(id: number): Observable<HospitalDenomination> {
    return this.http.get<HospitalDenomination>(`${this.apiUrl}/HospitalDefinition/Denomination/${id}`);
  }

  searchDenominations(query: string, costCenterFilter?: string): Observable<HospitalDenomination[]> {
    let url = `${this.apiUrl}/HospitalDefinition/Denomination/Search?query=${encodeURIComponent(query)}`;
    if (costCenterFilter) {
      url += `&costCenterFilter=${encodeURIComponent(costCenterFilter)}`;
    }
    return this.http.get<HospitalDenomination[]>(url);
  }

  getDenominationsForQuickAdmission(): Observable<HospitalDenomination[]> {
    return this.http.get<HospitalDenomination[]>(`${this.apiUrl}/HospitalDefinition/Denomination/QuickAdmission`);
  }

  createDenomination(denomination: HospitalDenomination): Observable<HospitalDenomination> {
    return this.http.post<HospitalDenomination>(`${this.apiUrl}/HospitalDefinition/Denomination`, denomination);
  }

  updateDenomination(id: number, denomination: HospitalDenomination): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/HospitalDefinition/Denomination/${id}`, denomination);
  }

  deleteDenomination(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/HospitalDefinition/Denomination/${id}`);
  }

  // Advanced denomination search with new query
  searchDenominationsAdvanced(
    searchQuery?: string,
    insuranceId: number = 5,
    costCenterIds?: string
  ): Observable<DenominationSearchResult[]> {
    let params = new HttpParams();
    if (searchQuery) {
      params = params.set('searchQuery', searchQuery);
    }
    params = params.set('insuranceId', insuranceId.toString());
    if (costCenterIds) {
      params = params.set('costCenterIds', costCenterIds);
    }
    return this.http.get<DenominationSearchResult[]>(
      `${this.apiUrl}/HospitalDefinition/Denomination/SearchAdvanced`,
      { params }
    );
  }
}
