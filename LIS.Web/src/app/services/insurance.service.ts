import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Insurance } from '../models/insurance';
import { ApiEndpointsService } from '../api/api-endpoints.service';

@Injectable({
  providedIn: 'root'
})
export class InsuranceService {
  private readonly endpoints = inject(ApiEndpointsService);
  private readonly apiUrl = this.endpoints.insurance;

  constructor(private http: HttpClient) { }

  getAll(): Observable<Insurance[]> {
    return this.http.get<Insurance[]>(this.apiUrl);
  }
}

