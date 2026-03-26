import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiEndpointsService } from '../api/api-endpoints.service';

export interface PatientLabResult {
  id: number;
  patientHeaderID: number;
  admissionNumber?: string;
  requestDate?: string | null;
  isCurrentAdmission?: boolean;
  labTestID?: number | null;
  resultType?: number | null;
  referenceRelatesToAge?: boolean;
  ageBasedReferenceRange?: string | null;
  labTestDescription: string;
  medicalClass: number;
  medicalClassDesc: string;
  paragraph?: string | null;
  min?: string | null;
  max?: string | null;
  prefix?: string | null;
  suffix?: string | null;
  errorMin?: string | null;
  errorMax?: string | null;
  uom?: number | null;
  uomDescription?: string | null;
  result?: string | null;
  last?: string | null;
  lastResultDate?: string | null;
  defaultTextResult?: string | null;
  comments?: string | null;
  displayOrder?: string | null;
  statusID?: number | null;
  isResultok: boolean;
  guid?: string | null;
  resultDate?: string | null;
  ref_Range?: string | null;
  tempHelperID?: number | null;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
  isDeleted: boolean;
  preInvoiceDetailID?: number | null;
  preInvoiceDetailSequence?: number | null;
  lowPanicIndex?: number | null;
  highPanicIndex?: number | null;
  isPanic: boolean;
  isNotified: boolean;
  panicDate?: string | null;
  panicComment?: string | null;
  printed?: boolean | null;
}

export interface PatientLabSub {
  id: number;
  patientLabTestID: number;
  labTestDescription: string;
  labTestSubID: number;
  labTestSubDescription: string;
  paragraph?: string | null;
  min?: string | null;
  max?: string | null;
  uom: number;
  uomDescription: string;
  result?: string | null;
  comments?: string | null;
  displayOrder?: string | null;
  percentage?: string | null;
  lastResult?: string | null;
  lastResultDate?: string | null;
  statusID?: number | null;
  prefix?: string | null;
  suffix?: string | null;
  errorMin?: string | null;
  errorMax?: string | null;
  ref_Range?: string | null;
  lowPanicIndex?: number | null;
  highPanicIndex?: number | null;
  isPanic: boolean;
  isNotified: boolean;
  panicDate?: string | null;
  panicComment?: string | null;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
  isDeleted: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class PatientResultsService {
  private http = inject(HttpClient);
  private readonly endpoints = inject(ApiEndpointsService);
  private readonly apiUrl = this.endpoints.patientLabResults;
  private readonly subApiUrl = this.endpoints.patientLabSub;

  getByAdmissionNumber(admissionNumber: string): Observable<PatientLabResult[]> {
    return this.http.get<PatientLabResult[]>(`${this.apiUrl}/byAdmission/${admissionNumber}`);
  }

  getByMRN(mrn: number, currentAdmission?: string): Observable<PatientLabResult[]> {
    const params = currentAdmission ? `?currentAdmission=${currentAdmission}` : '';
    return this.http.get<PatientLabResult[]>(`${this.apiUrl}/byMRN/${mrn}${params}`);
  }

  getByHeaderId(headerId: number): Observable<PatientLabResult[]> {
    return this.http.get<PatientLabResult[]>(`${this.apiUrl}/byHeader/${headerId}`);
  }

  getById(id: number): Observable<PatientLabResult> {
    return this.http.get<PatientLabResult>(`${this.apiUrl}/${id}`);
  }

  getSubTestsByPatientLabTestId(patientLabTestId: number): Observable<PatientLabSub[]> {
    return this.http.get<PatientLabSub[]>(`${this.subApiUrl}/byPatientLabTest/${patientLabTestId}`);
  }

  batchUpdateResults(results: PatientLabResult[]): Observable<any> {
    return this.http.put(`${this.apiUrl}/batch`, results);
  }

  batchUpdateSubTests(subTests: PatientLabSub[]): Observable<any> {
    return this.http.put(`${this.subApiUrl}/batch`, subTests);
  }

  generateFromInvoice(admissionNumber: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/generateFromInvoice/${admissionNumber}`, {});
  }
}

