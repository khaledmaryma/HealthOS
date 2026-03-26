import { Injectable } from '@angular/core';
import { ConfigService } from '../config/config.service';

@Injectable({ providedIn: 'root' })
export class ApiUrlService {
  constructor(private config: ConfigService) {}

  get baseUrl(): string {
    return this.config.getApiBaseUrl();
  }

  api(path: string): string {
    const p = path.startsWith('/') ? path : `/${path}`;
    return `${this.baseUrl}${p}`;
  }
}

