import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ResidentPatient } from '../models/resident-patient';
import { UnpaidPrivateInvoiceReceivedPatch, UnpaidPrivateInvoiceRow } from '../models/unpaid-private-invoice';
import { ApiEndpointsService } from '../api/api-endpoints.service';

@Injectable({
  providedIn: 'root'
})
export class ResidentPatientService {
  private readonly http = inject(HttpClient);
  private readonly endpoints = inject(ApiEndpointsService);

  constructor() {}

  getAll(page: number = 1, pageSize: number = 50, search?: string, isDischarged?: boolean, currentDateOnly?: boolean, checkInDateFrom?: string, checkInDateTo?: string): Observable<ResidentPatient[]> {
    let params = new HttpParams();

    // Add pagination parameters
    params = params.set('page', page.toString());
    params = params.set('pageSize', pageSize.toString());

    // Add search parameter
    if (search) {
      params = params.set('search', search);
    }

    // Add discharge filter
    if (isDischarged !== undefined) {
      params = params.set('isDischarged', isDischarged.toString());
    }

    // Add current date only filter
    if (currentDateOnly) {
      params = params.set('currentDateOnly', 'true');
    }

    // Add date range filters
    if (checkInDateFrom) {
      params = params.set('checkInDateFrom', checkInDateFrom);
    }

    if (checkInDateTo) {
      params = params.set('checkInDateTo', checkInDateTo);
    }

    return this.http.get<ResidentPatient[]>(this.endpoints.residentPatient, { params });
  }

  getCount(search?: string, isDischarged?: boolean, currentDateOnly?: boolean, checkInDateFrom?: string, checkInDateTo?: string): Observable<number> {
    let params = new HttpParams();

    // Add search parameter
    if (search) {
      params = params.set('search', search);
    }

    // Add discharge filter
    if (isDischarged !== undefined) {
      params = params.set('isDischarged', isDischarged.toString());
    }

    // Add current date only filter
    if (currentDateOnly) {
      params = params.set('currentDateOnly', 'true');
    }

    // Add date range filters
    if (checkInDateFrom) {
      params = params.set('checkInDateFrom', checkInDateFrom);
    }

    if (checkInDateTo) {
      params = params.set('checkInDateTo', checkInDateTo);
    }

    return this.http.get<number>(`${this.endpoints.residentPatient}/count`, { params });
  }

  getById(id: number): Observable<ResidentPatient> {
    return this.http.get<ResidentPatient>(`${this.endpoints.residentPatient}/${id}`);
  }

  /** Unpaid invoices (no receipt, Private auxiliary insurance). Uses logged-in department when set; otherwise all departments. */
  getUnpaidPrivateInvoices(departmentNameOverride?: string | null): Observable<UnpaidPrivateInvoiceRow[]> {
    let headers = new HttpHeaders();
    const dept = localStorage.getItem('loggedInUserDepartmentName');
    if (dept) {
      headers = headers.set('X-User-Department', dept);
    }
    let url = `${this.endpoints.residentPatient}/unpaid-private-invoices`;
    if (departmentNameOverride?.trim()) {
      const p = new HttpParams().set('departmentName', departmentNameOverride.trim());
      url = `${url}?${p.toString()}`;
    }
    return this.http.get<UnpaidPrivateInvoiceRow[]>(url, { headers });
  }

  patchUnpaidPrivateInvoiceReceived(
    invoiceHeaderId: number,
    body: UnpaidPrivateInvoiceReceivedPatch
  ): Observable<UnpaidPrivateInvoiceRow> {
    return this.http.patch<UnpaidPrivateInvoiceRow>(
      `${this.endpoints.residentPatient}/unpaid-private-invoices/${invoiceHeaderId}/received`,
      body
    );
  }
}

