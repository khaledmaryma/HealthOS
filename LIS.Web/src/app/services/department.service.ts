import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Department } from '../models/department';
import { ApiEndpointsService } from '../api/api-endpoints.service';

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private readonly endpoints = inject(ApiEndpointsService);
  private readonly apiUrl = this.endpoints.department;

  constructor(private http: HttpClient) { }

  getAll(): Observable<Department[]> {
    return this.http.get<Department[]>(this.apiUrl);
  }

  getById(id: number): Observable<Department> {
    return this.http.get<Department>(`${this.apiUrl}/${id}`);
  }
}

