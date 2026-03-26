import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiEndpointsService } from '../api/api-endpoints.service';

export interface HospitalConfiguration {
  id: number;
  hospitalName?: string | null;
  hospitalNameArabic?: string | null;
  hospitalAddress?: string | null;
  hospitalAddressArabic?: string | null;
  hospitalPhone?: string | null;
  hospitalFax?: string | null;
  logoBase64?: string | null; // Logo as base64 string
}

@Injectable({
  providedIn: 'root'
})
export class HospitalConfigurationService {
  private http = inject(HttpClient);
  private readonly endpoints = inject(ApiEndpointsService);
  private readonly apiUrl = this.endpoints.hospitalConfiguration;

  readonly config = signal<HospitalConfiguration | null>(null);

  load(): void {
    this.http.get<HospitalConfiguration>(this.apiUrl).subscribe({
      next: (data) => this.config.set(data),
      error: (err) => console.error('Error loading hospital configuration:', err)
    });
  }

  getConfig(): Observable<HospitalConfiguration> {
    return this.http.get<HospitalConfiguration>(this.apiUrl);
  }
}

