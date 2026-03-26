import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { ApiEndpointsService } from '../api/api-endpoints.service';

interface ProductListItem {
  id: number;
  code: string;
  smallDescription: string;
  category: number;
  manufacturer?: number | null;
  hasExpiry: boolean;
  canBeSold: boolean;
  canBePurchased: boolean;
  salePriceLocal?: number | null;
}

interface ProductEdit {
  id: number;
  code: string;
  smallDescription: string;
  longDescription?: string | null;
  stockType: number;
  productLine: number;
  category: number;
  manufacturer?: number | null;
  hasExpiry: boolean;
  canBeSold: boolean;
  canBePurchased: boolean;
  salePriceLocal?: number | null;
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss'
})
export class InventoryComponent implements OnInit {
  private readonly http = inject(HttpClient);

  products: ProductListItem[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  isLoading = false;

  private readonly endpoints = inject(ApiEndpointsService);
  private readonly apiBaseUrl = this.endpoints.inventoryProducts;

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(page: number = this.page): void {
    this.isLoading = true;
    this.page = page;

    this.http
      .get<{ totalCount: number; items: ProductListItem[] }>(
        `${this.apiBaseUrl}?page=${this.page}&pageSize=${this.pageSize}`
      )
      .subscribe({
        next: (response) => {
          this.totalCount = response.totalCount;
          this.products = response.items;
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
        }
      });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  canGoPrev(): boolean {
    return this.page > 1;
  }

  canGoNext(): boolean {
    return this.page < this.totalPages;
  }
}



