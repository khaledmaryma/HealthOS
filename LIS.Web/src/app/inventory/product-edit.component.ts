import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ApiEndpointsService } from '../api/api-endpoints.service';

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
  selector: 'app-product-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-edit.component.html',
  styleUrl: './product-edit.component.scss'
})
export class ProductEditComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  model: ProductEdit = {
    id: 0,
    code: '',
    smallDescription: '',
    longDescription: '',
    stockType: 0,
    productLine: 0,
    category: 0,
    manufacturer: null,
    hasExpiry: false,
    canBeSold: true,
    canBePurchased: true,
    salePriceLocal: null
  };

  isNew = true;
  isSaving = false;
  loadError: string | null = null;

  private readonly endpoints = inject(ApiEndpointsService);
  private readonly apiBaseUrl = this.endpoints.inventoryProducts;

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? Number(idParam) : 0;

    if (id) {
      this.isNew = false;
      this.loadProduct(id);
    }
  }

  private loadProduct(id: number): void {
    this.http.get<ProductEdit>(`${this.apiBaseUrl}/${id}`).subscribe({
      next: (p) => {
        this.model = { ...p };
        this.loadError = null;
      },
      error: () => {
        this.loadError = 'Failed to load product.';
      }
    });
  }

  save(): void {
    this.isSaving = true;
    const dto = this.model;

    if (this.isNew) {
      this.http.post<ProductEdit>(this.apiBaseUrl, dto).subscribe({
        next: () => {
          this.isSaving = false;
          this.router.navigate(['/inventory']);
        },
        error: () => {
          this.isSaving = false;
        }
      });
    } else {
      this.http.put<void>(`${this.apiBaseUrl}/${dto.id}`, dto).subscribe({
        next: () => {
          this.isSaving = false;
          this.router.navigate(['/inventory']);
        },
        error: () => {
          this.isSaving = false;
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/inventory']);
  }
}


