import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiUrlService } from '../api/api-url.service';

export interface LabTest {
  id: number;
  code?: string | null;
  testDesciption?: string | null;
  denomination?: number | null;
  defaultTextResult?: string | null;
  displayOrder: string;
  isACollection: boolean;
  hasReferenceRange: boolean;
  referenceRelatesToAge: boolean;
  referencerelatesToGyneco: boolean;
  resultType?: number | null;
  uom?: number | null;
  isDeleted: boolean;
}

export interface ResultType {
  id: number;
  description?: string | null;
}

export interface UnitOfMeasure {
  id: number;
  description?: string | null;
}

export interface Denomination {
  id: number;
  smallDescription: string;
  code?: string | null;
  displayOrder?: number | null;
}

export interface LabTestAge {
  id: number;
  labTest?: number | null;
  description?: string | null;
  displayOrder?: string | null;
  defaultMin?: string | null;
  defaultMax?: string | null;
  errorRangeMin?: string | null;
  errorRangeMax?: string | null;
  prefix?: string | null;
  suffix?: string | null;
  lower?: number | null;
  higher?: number | null;
  machineID?: number | null;
  lowPanicIndex?: number | null;
  highPanicIndex?: number | null;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
}

export interface LabTestGyneco {
  id: number;
  labTest?: number | null;
  description?: string | null;
  displayOrder?: string | null;
  femaleNormalMin?: number | null;
  femaleNormalMax?: number | null;
  errorRangeMin?: number | null;
  errorRangeMax?: number | null;
  prefix?: string | null;
  suffix?: string | null;
  machineID?: number | null;
  lowPanicIndex?: number | null;
  highPanicIndex?: number | null;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
}

export interface LabTestSub {
  id: number;
  labTest?: number | null;
  description?: string | null;
  paragraphHeader?: string | null;
  displayOrder?: string | null;
  uom?: number | null;
  defaultNoramlMin?: string | null;
  defaultNormalMax?: string | null;
  femaleNormalMin?: string | null;
  femaleNormalMax?: string | null;
  maleNormalMin?: string | null;
  maleNormalMax?: string | null;
  errorRangeMin?: string | null;
  errorRangeMax?: string | null;
  prefix?: string | null;
  suffix?: string | null;
  labTestDescription?: string | null;
  isPercentage: boolean;
  isComment: boolean;
  ageNormalMin?: string | null;
  ageNormalMax?: string | null;
  ageType?: number | null;
  machineID?: number | null;
  lowPanicIndex?: number | null;
  highPanicIndex?: number | null;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
}

@Injectable({ providedIn: 'root' })
export class LabTestsService {
  private http = inject(HttpClient);
  private readonly urls = inject(ApiUrlService);
  private readonly labTestsUrl = this.urls.api('/api/labtests');
  private readonly resultTypesUrl = this.urls.api('/api/resulttypes');
  private readonly unitOfMeasuresUrl = this.urls.api('/api/unitofmeasures');
  private readonly denominationUrl = this.urls.api('/api/denomination');
  private readonly labTestAgeUrl = this.urls.api('/api/labtestage');
  private readonly labTestGynecoUrl = this.urls.api('/api/labtestgyneco');
  private readonly labTestSubUrl = this.urls.api('/api/labtestsub');

  readonly items = signal<LabTest[]>([]);
  readonly resultTypes = signal<ResultType[]>([]);
  readonly unitOfMeasures = signal<UnitOfMeasure[]>([]);
  readonly denominations = signal<Denomination[]>([]);

  load() {
    this.http.get<LabTest[]>(this.labTestsUrl).subscribe(d => this.items.set(d));
  }

  loadResultTypes() {
    this.http.get<ResultType[]>(this.resultTypesUrl).subscribe(d => this.resultTypes.set(d));
  }

  loadUnitOfMeasures() {
    this.http.get<UnitOfMeasure[]>(this.unitOfMeasuresUrl).subscribe(d => this.unitOfMeasures.set(d));
  }

  loadDenominations() {
    this.http.get<Denomination[]>(this.denominationUrl).subscribe(d => this.denominations.set(d));
  }

  searchDenominations(query: string) {
    return this.http.get<Denomination[]>(`${this.denominationUrl}/search?query=${query}`);
  }

  create(payload: Partial<LabTest>) {
    return this.http.post<LabTest>(this.labTestsUrl, payload);
  }

  update(id: number, payload: Partial<LabTest>) {
    return this.http.put<void>(`${this.labTestsUrl}/${id}`, { ...payload, id });
  }

  delete(id: number) {
    return this.http.delete<void>(`${this.labTestsUrl}/${id}`);
  }

  // LabTestAge methods
  getLabTestAgeByLabTestId(labTestId: number) {
    return this.http.get<LabTestAge[]>(`${this.labTestAgeUrl}/byLabTest/${labTestId}`);
  }

  createLabTestAge(payload: Partial<LabTestAge>) {
    return this.http.post<LabTestAge>(this.labTestAgeUrl, payload);
  }

  updateLabTestAge(id: number, payload: Partial<LabTestAge>) {
    return this.http.put<void>(`${this.labTestAgeUrl}/${id}`, { ...payload, id });
  }

  deleteLabTestAge(id: number) {
    return this.http.delete<void>(`${this.labTestAgeUrl}/${id}`);
  }

  // LabTestGyneco methods
  getLabTestGynecoByLabTestId(labTestId: number) {
    return this.http.get<LabTestGyneco[]>(`${this.labTestGynecoUrl}/byLabTest/${labTestId}`);
  }

  createLabTestGyneco(payload: Partial<LabTestGyneco>) {
    return this.http.post<LabTestGyneco>(this.labTestGynecoUrl, payload);
  }

  updateLabTestGyneco(id: number, payload: Partial<LabTestGyneco>) {
    return this.http.put<void>(`${this.labTestGynecoUrl}/${id}`, { ...payload, id });
  }

  deleteLabTestGyneco(id: number) {
    return this.http.delete<void>(`${this.labTestGynecoUrl}/${id}`);
  }

  // LabTestSub methods
  getLabTestSubByLabTestId(labTestId: number) {
    return this.http.get<LabTestSub[]>(`${this.labTestSubUrl}/byLabTest/${labTestId}`);
  }

  createLabTestSub(payload: Partial<LabTestSub>) {
    return this.http.post<LabTestSub>(this.labTestSubUrl, payload);
  }

  updateLabTestSub(id: number, payload: Partial<LabTestSub>) {
    return this.http.put<void>(`${this.labTestSubUrl}/${id}`, { ...payload, id });
  }

  deleteLabTestSub(id: number) {
    return this.http.delete<void>(`${this.labTestSubUrl}/${id}`);
  }
}



