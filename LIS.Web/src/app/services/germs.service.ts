import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Germs {
  id: number;
  code: string;
  description: string;
  identifier?: string;
  displayOrder?: string;
  createdBy?: number;
  createdDate?: Date;
  modifiedBy?: number;
  modifiedDate?: Date;
  isDeleted: boolean;
}

export interface CreateGermRequest {
  code: string;
  description: string;
  identifier?: string;
  displayOrder?: string;
  createdBy?: number;
}

export interface UpdateGermRequest {
  code: string;
  description: string;
  identifier?: string;
  displayOrder?: string;
  modifiedBy?: number;
}

@Injectable({
  providedIn: 'root'
})
export class GermsService {
  private readonly apiUrl = `${environment.apiUrl}/api/germs`;

  readonly items = signal<Germs[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  constructor(private http: HttpClient) {}

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<Germs[]>(this.apiUrl).subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading germs:', err);
        this.error.set('Failed to load germs');
        this.loading.set(false);
      }
    });
  }

  getById(id: number): Observable<Germs> {
    return this.http.get<Germs>(`${this.apiUrl}/${id}`);
  }

  create(germ: CreateGermRequest): Observable<Germs> {
    return this.http.post<Germs>(this.apiUrl, germ);
  }

  update(id: number, germ: UpdateGermRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, germ);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}














