import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiUrlService } from '../api/api-url.service';

export interface Bilan {
  id: number;
  description?: string;
  type?: number;
  isDeleted?: boolean;
}

export interface BilanDetail {
  id: number;
  bilanId: number;
  labTestId?: number;
  operationId?: number;
  denominationId?: number;
  denominationCode?: string;
  denominationDescription?: string;
  description?: string;
  typeDescription?: string;
  /** Price in USD - use this if set, else fall back to denomination */
  priceUsd?: number | null;
  /** Price in LL (Lebanese Lira) - use if priceUsd not set, else fall back to denomination */
  priceLlbp?: number | null;
}

@Injectable({
  providedIn: 'root'
})
export class BilanService {
  private readonly http = inject(HttpClient);
  private readonly urls = inject(ApiUrlService);
  private readonly apiUrl = this.urls.api('/api');

  getAll(): Observable<Bilan[]> {
    return this.http.get<Bilan[]>(`${this.apiUrl}/Bilans`);
  }

  getDetails(bilanId: number): Observable<BilanDetail[]> {
    return this.http.get<BilanDetail[]>(`${this.apiUrl}/Bilans/${bilanId}/details`);
  }
}
