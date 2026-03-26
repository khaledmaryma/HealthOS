import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiEndpointsService } from '../api/api-endpoints.service';

export interface MedicamentProduct {
  prdId: number;
  prdCode: string;
  prdName: string;
  SalePriceLocal?: number;
  SalePriceMain?: number;
  salePriceLocal?: number;
  salePriceMain?: number;
  CatId: number;
  CatCode: string;
  CatName: string;
  prdPackId?: number;
  prdPack?: number;
  IsMain: boolean;
  PLId: number;
  PLCode: string;
  PLDescription?: string;
  plDescription?: string;
  StId: number;
  StCode: string;
  StName: string;
}

@Injectable({
  providedIn: 'root'
})
export class InventoryProductService {
  private readonly http = inject(HttpClient);
  private readonly endpoints = inject(ApiEndpointsService);
  private readonly apiBaseUrl = this.endpoints.inventoryProducts;

  getProductsForMedicament(): Observable<MedicamentProduct[]> {
    return this.http.get<MedicamentProduct[]>(`${this.apiBaseUrl}/for-medicament`);
  }
}
