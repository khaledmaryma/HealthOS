import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { inject } from '@angular/core';
import { ApiEndpointsService } from '../api/api-endpoints.service';

export interface SaveOptions {
  savePatient: boolean;
  saveAdmission: boolean;
  saveInvoice: boolean;
}

export interface PatientInfo {
  firstName: string;
  lastName: string;
  middleName?: string;
  gender: string;
  dob: string;
  phone?: string;
  arabicFullName?: string;
  existingPatientId?: number | null;
}

export interface AdmissionInfo {
  admissionId?: number | null;
  admissionNumber?: string;
  admissionSite?: number;
  referralPhysicianId?: number | null;
  attendingPhysicianId?: number | null;
  departmentId?: number | null;
  checkInDate?: string;
  checkOutDate?: string | null;
  type?: number;
  group?: number;
}

export interface InvoiceInfo {
  invoiceHeaderId?: number | null;
  hospitalAmount?: number;
  physicianAmount?: number;
  gross?: number;
  net?: number;
}

export interface InvoiceDetail {
  denomination?: number;
  quantity?: number;
  unitPrice?: number;
  discount?: number;
  lumpSum?: number;
  // other fields as needed
}

export interface SaveData {
  saveOption?: SaveOptions;
  patientInfo?: PatientInfo;
  admissionInfo?: AdmissionInfo;
  invoiceInfo?: InvoiceInfo;
  invoiceDetail?: InvoiceDetail[];
  deliveryHeaderInfo?: any;
  deliveryItem?: any;
}

export interface SaveRequest {
  saveData: SaveData;
  saveOption?: SaveOptions;
}

@Injectable({ providedIn: 'root' })
export class QuickAdmissionV2Service {
  private readonly baseUrl = inject(ApiEndpointsService).quickAdmissionV2;

  constructor(private http: HttpClient) { }

  saveComplete(request: SaveRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/SaveComplete`, request);
  }

  loadAdmission(admissionId: number): Observable<SaveData> {
    return this.http.get<SaveData>(`${this.baseUrl}/LoadAdmission/${admissionId}`);
  }
}
