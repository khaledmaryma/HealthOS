import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RuntimeConfig {
  apiBaseUrl?: string;
}

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private config: RuntimeConfig = {};

  constructor(private http: HttpClient) {}

  async load(): Promise<void> {
    try {
      // Served from `public/config.json` (see angular.json assets)
      const url = `/config.json?v=${Date.now()}`;
      const cfg = await firstValueFrom(this.http.get<RuntimeConfig>(url));
      this.config = cfg ?? {};
    } catch (err) {
      // Safe fallback: keep empty config (getters will fall back)
      // eslint-disable-next-line no-console
      console.warn('Failed to load runtime config.json; falling back to environment defaults.', err);
      this.config = {};
    }
  }

  getApiBaseUrl(): string {
    const fromConfig = (this.config.apiBaseUrl ?? '').trim();
    if (fromConfig) return fromConfig.replace(/\/+$/, '');

    const fromEnv = ((environment as any).apiBaseUrl ?? (environment as any).apiUrl ?? '').trim();
    if (fromEnv) return fromEnv.replace(/\/+$/, '');

    // Last-resort fallback (keeps app usable in most hosting scenarios)
    return window.location.origin;
  }
}

