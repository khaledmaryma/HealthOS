import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Insurance } from '../models/insurance';

@Injectable({
  providedIn: 'root'
})
export class InsuranceService {
  private apiUrl = 'http://localhost:5050/api/HospitalDefinition/Insurance';

  constructor(private http: HttpClient) { }

  getAll(): Observable<Insurance[]> {
    return this.http.get<Insurance[]>(this.apiUrl);
  }
}

