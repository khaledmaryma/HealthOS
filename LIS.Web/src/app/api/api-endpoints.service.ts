import { Injectable } from '@angular/core';
import { API_PATHS } from './api-paths';
import { ApiUrlService } from './api-url.service';

@Injectable({ providedIn: 'root' })
export class ApiEndpointsService {
  constructor(private urls: ApiUrlService) {}

  get accounting() {
    return this.urls.api(API_PATHS.accounting);
  }
  get department() {
    return this.urls.api(API_PATHS.department);
  }
  get residentPatient() {
    return this.urls.api(API_PATHS.residentPatient);
  }
  get invoice() {
    return this.urls.api(API_PATHS.invoice);
  }
  get denomination() {
    return this.urls.api(API_PATHS.denomination);
  }
  get inventoryProducts() {
    return this.urls.api(API_PATHS.inventoryProducts);
  }
  get userManagement() {
    return this.urls.api(API_PATHS.userManagement);
  }
  get userKpis() {
    return this.urls.api(API_PATHS.userKpis);
  }
  get hospitalConfiguration() {
    return this.urls.api(API_PATHS.hospitalConfiguration);
  }
  get patientLabResults() {
    return this.urls.api(API_PATHS.patientLabResults);
  }
  get patientLabSub() {
    return this.urls.api(API_PATHS.patientLabSub);
  }
  get patientLabResultsHeaders() {
    return this.urls.api(API_PATHS.patientLabResultsHeaders);
  }
  get patientLabBacteriology() {
    return this.urls.api(API_PATHS.patientLabBacteriology);
  }
  get patientMedicalFile() {
    return this.urls.api(API_PATHS.patientMedicalFile);
  }
  get residentPatientsLegacy() {
    return this.urls.api(API_PATHS.residentPatientsLegacy);
  }
  get germs() {
    return this.urls.api(API_PATHS.germs);
  }
  get bacteria() {
    return this.urls.api(API_PATHS.bacteria);
  }
  get insurance() {
    return this.urls.api(API_PATHS.insurance);
  }
  get quickAdmission() {
    return this.urls.api(API_PATHS.quickAdmission);
  }
  get quickAdmissionV2() {
    return this.urls.api(API_PATHS.quickAdmissionV2);
  }
  get chatGpt() {
    return this.urls.api(API_PATHS.chatGpt);
  }
}

