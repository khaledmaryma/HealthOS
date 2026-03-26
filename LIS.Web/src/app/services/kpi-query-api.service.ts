import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointsService } from '../api/api-endpoints.service';
import {
  DEFAULT_KPI_HOME_PAGE_ID,
  ExecuteKpiQueryRequest,
  KpiQueryResult,
  UserKpiDefinition,
} from '../models/user-kpi';

/** CRUD + execute against `/api/user-kpis`. */
@Injectable({ providedIn: 'root' })
export class KpiQueryApiService {
  constructor(
    private readonly http: HttpClient,
    private readonly endpoints: ApiEndpointsService
  ) {}

  list(userId: number, appKey: string, homePageId: string = DEFAULT_KPI_HOME_PAGE_ID): Observable<UserKpiDefinition[]> {
    const params = new HttpParams()
      .set('userId', String(userId))
      .set('appKey', appKey)
      .set('homePageId', homePageId);
    return this.http.get<UserKpiDefinition[]>(this.endpoints.userKpis, { params });
  }

  create(body: {
    userId: number;
    appKey: string;
    homePageId: string;
    title: string;
    sqlQuery: string;
    displayMode: number;
    gridShowTotals: boolean;
    chartOptionsJson: string | null;
    sortOrder: number;
  }): Observable<UserKpiDefinition> {
    return this.http.post<UserKpiDefinition>(this.endpoints.userKpis, body);
  }

  update(
    id: number,
    body: {
      userId: number;
      appKey: string;
      homePageId: string;
      title: string;
      sqlQuery: string;
      displayMode: number;
      gridShowTotals: boolean;
      chartOptionsJson: string | null;
      sortOrder: number;
    }
  ): Observable<UserKpiDefinition> {
    return this.http.put<UserKpiDefinition>(`${this.endpoints.userKpis}/${id}`, body);
  }

  delete(id: number, userId: number): Observable<void> {
    const params = new HttpParams().set('userId', String(userId));
    return this.http.delete<void>(`${this.endpoints.userKpis}/${id}`, { params });
  }

  execute(req: ExecuteKpiQueryRequest): Observable<KpiQueryResult> {
    return this.http.post<KpiQueryResult>(`${this.endpoints.userKpis}/execute`, req);
  }
}
