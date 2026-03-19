import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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
  private readonly apiUrl = 'http://localhost:5050/api/HospitalConfiguration';

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

