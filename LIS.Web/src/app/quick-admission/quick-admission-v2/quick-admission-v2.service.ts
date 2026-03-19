import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class QuickAdmissionV2Service {
  private apiUrl = 'http://localhost:5050/api';
  private http = inject(HttpClient);

  constructor() { }

  /**
   * Save complete data - patient, admission, and invoice
   */
  saveComplete(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/QuickAdmissionV2/save-complete`, data);
  }

  /**
   * Load existing admission for edit
   */
  loadAdmission(admissionId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/QuickAdmissionV2/load-admission/${admissionId}`);
  }

  /**
   * Load patient data
   */
  loadPatient(patientId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/Patient/${patientId}`);
  }

  /**
   * Save patient only
   */
  savePatient(patientData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Patient`, patientData);
  }

  /**
   * Save admission only
   */
  saveAdmission(admissionData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Admission`, admissionData);
  }

  /**
   * Save invoice only
   */
  saveInvoice(invoiceData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/BillingInvoice`, invoiceData);
  }
}
