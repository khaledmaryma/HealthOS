import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ResidentPatient } from '../models/resident-patient';

@Injectable({
  providedIn: 'root'
})
export class ResidentPatientService {
  private apiUrl = 'http://localhost:5050/api/ResidentPatient';

  constructor(private http: HttpClient) { }

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

    return this.http.get<ResidentPatient[]>(this.apiUrl, { params });
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

    return this.http.get<number>(`${this.apiUrl}/count`, { params });
  }

  getById(id: number): Observable<ResidentPatient> {
    return this.http.get<ResidentPatient>(`${this.apiUrl}/${id}`);
  }
}

