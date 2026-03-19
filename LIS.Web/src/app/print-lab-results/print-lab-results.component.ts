import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { PatientResultsService, PatientLabResult, PatientLabSub } from '../services/patient-results.service';

export interface ResidentPatient {
  id: number;
  patientID: number;
  admission: number;
  mrn: number;
  admissionNumber: string;
  patientName: string;
  arabicFullName?: string | null;
  medicalRecordNumber: string;
  patientDOB: string;
  age?: number | null;
  patientGender: string;
  checkInDate: string;
  checkInClassDescription: string;
  mainInsuranceDescription: string;
  referralPhysicianName: string;
  attendingPhysicianName?: string | null;
  roomDescription?: string | null;
  bedDescription?: string | null;
  contact?: string | null;
  isDischarged: boolean;
  isDeleted: boolean;
}

export interface PatientLabResultsHeader {
  id: number;
  caseNumber: number;
  patientID: number;
  mrn: number;
  admissionNB: number;
  admissionNumber: string;
  resultNB?: number | null;
  requestDate?: string | null;
  isApproved: boolean;
  approvedDate?: string | null;
  isCompleted: boolean;
  completedDate?: string | null;
  reason?: string | null;
  room?: string | null;
  floor?: string | null;
  bed?: string | null;
  gender?: string | null;
  age?: number | null;
  department?: string | null;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
  isDeleted: boolean;
  comment?: string | null;
  printedStatus?: number | null;
  reportedDelivered?: boolean | null;
}

export interface PatientLabBacteriology {
  id: number;
  patientHeader: number;
  code?: string | null;
  antibioticId?: number | null;
  antibioticDescription?: string | null;
  dateTime?: string | null;
  resistant: boolean;
  intermediat: boolean;
  sensible: boolean;
  charge: string;
  diameter: string;
  displayOrder?: string | null;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
}

export interface BacteriologyHeader {
  id: number;
  patientLabResultID: number;
  germsId?: number | null;
  germ?: string | null;
  bacterieId?: number | null;
  bacteria?: string | null;
  number?: number | null;
  prelevement?: string | null;
  prelevementId?: number | null;
  dateTime?: string | null;
  comments?: string | null;
}

export interface HospitalConfiguration {
  id: number;
  hospitalName?: string | null;
  hospitalNameArabic?: string | null;
  hospitalAddress?: string | null;
  hospitalAddressArabic?: string | null;
  hospitalPhone?: string | null;
  hospitalFax?: string | null;
  logoBase64?: string | null; // Logo as base64 string
}

@Component({
  selector: 'app-print-lab-results',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './print-lab-results.component.html',
  styleUrls: ['./print-lab-results.component.scss']
})
export class PrintLabResultsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private patientResultsService = inject(PatientResultsService);
  private readonly apiUrl = 'http://localhost:5050/api/residentpatients';
  private readonly headerApiUrl = 'http://localhost:5050/api/patientlabresultsheaders';

  readonly patient = signal<ResidentPatient | null>(null);
  readonly labResults = signal<PatientLabResult[]>([]);
  readonly subTestsMap = signal<Map<number, PatientLabSub[]>>(new Map());
  readonly bacteriologyMap = signal<Map<number, { header: BacteriologyHeader | null, details: PatientLabBacteriology[] }>>(new Map());
  readonly header = signal<PatientLabResultsHeader | null>(null);
  readonly hospitalConfig = signal<HospitalConfiguration | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal('');
  readonly currentDate = new Date();

  // Filter parameters from query string
  readonly selectedMedicalClasses = signal<number[]>([]);
  readonly selectedLabTests = signal<number[]>([]);

  // Computed: Filtered results based on query parameters
  readonly filteredLabResults = computed(() => {
    let results = this.labResults();
    const medClasses = this.selectedMedicalClasses();
    const labTests = this.selectedLabTests();

    // Filter by medical classes if any selected
    if (medClasses.length > 0) {
      results = results.filter(r => medClasses.includes(r.medicalClass));
    }

    // Filter by lab tests if any selected
    if (labTests.length > 0) {
      results = results.filter(r => r.labTestID && labTests.includes(r.labTestID));
    }

    return results;
  });

  // Computed signals to separate numeric, text, and antibiogram results based on ResultType
  // ResultType: 1 = Text, 2 = Numeric, 3 = Antibiogram
  readonly numericResults = computed(() => {
    return this.filteredLabResults().filter(r => r.resultType === 2);
  });

  readonly textResults = computed(() => {
    return this.filteredLabResults().filter(r => r.resultType === 1);
  });

  readonly antibiogramResults = computed(() => {
    return this.filteredLabResults().filter(r => r.resultType === 3);
  });

  ngOnInit(): void {
    const admissionNumber = this.route.snapshot.paramMap.get('admissionNumber');

    // Parse query parameters for filters
    const queryParams = this.route.snapshot.queryParamMap;

    // Parse medical classes
    const medClassesParam = queryParams.get('medicalClasses');
    if (medClassesParam) {
      const medClasses = medClassesParam.split(',').map(id => parseInt(id, 10)).filter(id => !isNaN(id));
      this.selectedMedicalClasses.set(medClasses);
    }

    // Parse lab tests
    const labTestsParam = queryParams.get('labTests');
    if (labTestsParam) {
      const labTests = labTestsParam.split(',').map(id => parseInt(id, 10)).filter(id => !isNaN(id));
      this.selectedLabTests.set(labTests);
    }

    // Load hospital configuration
    this.loadHospitalConfiguration();

    if (admissionNumber) {
      this.loadPrintData(admissionNumber);
    } else {
      this.errorMessage.set('No admission number provided');
      this.isLoading.set(false);
    }
  }

  loadHospitalConfiguration(): void {
    console.log('🏥 Loading hospital configuration...');
    this.http.get<HospitalConfiguration>('http://localhost:5050/api/HospitalConfiguration').subscribe({
      next: (config) => {
        console.log('✅ Hospital configuration loaded:', config);
        console.log('Hospital Name:', config.hospitalName);
        console.log('Hospital Name Arabic:', config.hospitalNameArabic);
        console.log('Has Logo:', !!config.logoBase64);
        this.hospitalConfig.set(config);
      },
      error: (err) => {
        console.error('❌ Error loading hospital configuration:', err);
        // Don't fail the print if hospital config fails to load
      }
    });
  }

  loadPrintData(admissionNumber: string): void {
    this.isLoading.set(true);

    // Load patient data
    this.http.get<ResidentPatient[]>(`${this.apiUrl}/search?query=${encodeURIComponent(admissionNumber)}`).subscribe({
      next: (patients) => {
        if (patients && patients.length > 0) {
          this.patient.set(patients[0]);
          this.loadLabResults(admissionNumber);
        } else {
          this.errorMessage.set('Patient not found');
          this.isLoading.set(false);
        }
      },
      error: (err) => {
        console.error('Error loading patient:', err);
        this.errorMessage.set('Failed to load patient data');
        this.isLoading.set(false);
      }
    });
  }

  loadLabResults(admissionNumber: string): void {
    this.patientResultsService.getByAdmissionNumber(admissionNumber).subscribe({
      next: (results) => {
        this.labResults.set(results);

        // Load all sub-tests and bacteriology data
        results.forEach(result => {
          // Load sub-tests for collections
          this.patientResultsService.getSubTestsByPatientLabTestId(result.id).subscribe({
            next: (subTests) => {
              if (subTests && subTests.length > 0) {
                const currentMap = this.subTestsMap();
                const newMap = new Map(currentMap);
                newMap.set(result.id, subTests);
                this.subTestsMap.set(newMap);
              }
            }
          });

          // Load bacteriology data for antibiogram results (ResultType = 3)
          if (result.resultType === 3) {
            this.loadBacteriologyData(result.id);
          }
        });

        this.isLoading.set(false);

        // Auto-print after all data loads (including hospital config)
        // Wait longer to ensure hospital config is loaded
        setTimeout(() => {
          console.log('Auto-printing with hospital config:', this.hospitalConfig());
          window.print();
        }, 2000);
      },
      error: (err) => {
        console.error('Error loading lab results:', err);
        this.errorMessage.set('Failed to load lab results');
        this.isLoading.set(false);
      }
    });
  }

  loadBacteriologyData(resultId: number): void {
    this.http.get<{
      header: BacteriologyHeader,
      details: PatientLabBacteriology[]
    }>(`http://localhost:5050/api/PatientLabBacteriology/byPatientLabResult/${resultId}`).subscribe({
      next: (response) => {
        const currentMap = this.bacteriologyMap();
        const newMap = new Map(currentMap);
        newMap.set(resultId, {
          header: response.header || null,
          details: response.details || []
        });
        this.bacteriologyMap.set(newMap);
      },
      error: (err) => {
        console.error(`Error loading bacteriology data for result ${resultId}:`, err);
        // Set empty data even on error
        const currentMap = this.bacteriologyMap();
        const newMap = new Map(currentMap);
        newMap.set(resultId, { header: null, details: [] });
        this.bacteriologyMap.set(newMap);
      }
    });
  }

  getSubTests(resultId: number): PatientLabSub[] {
    return this.subTestsMap().get(resultId) || [];
  }

  hasSubTests(resultId: number): boolean {
    const subTests = this.subTestsMap().get(resultId);
    return subTests !== undefined && subTests.length > 0;
  }

  getBacteriologyData(resultId: number): { header: BacteriologyHeader | null, details: PatientLabBacteriology[] } {
    return this.bacteriologyMap().get(resultId) || { header: null, details: [] };
  }

  hasBacteriologyData(resultId: number): boolean {
    const data = this.bacteriologyMap().get(resultId);
    return data !== undefined && data.details.length > 0;
  }

  getSensitivitySymbol(record: PatientLabBacteriology): string {
    if (record.sensible) return 'S';
    if (record.resistant) return 'R';
    if (record.intermediat) return 'I';
    return '-';
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}-${month}-${year}`;
  }

  formatDateTime(dateString: string): string {
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${day}-${month}-${year} ${hours}:${minutes}`;
  }

  getAge(dobString: string): number {
    const dob = new Date(dobString);
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const monthDiff = today.getMonth() - dob.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
      age--;
    }
    return age;
  }

  isMale(gender: string | null | undefined): boolean {
    if (!gender) return false;
    const g = String(gender).toLowerCase().trim();
    return g === 'm' || g === 'male' || g === '1' || g === 'masculin' || g === 'man';
  }

  isFemale(gender: string | null | undefined): boolean {
    if (!gender) return false;
    const g = String(gender).toLowerCase().trim();
    return g === 'f' || g === 'female' || g === '2' || g === 'feminin' || g === 'woman';
  }

  getGenderText(gender: string): string {
    return this.isMale(gender) ? 'Male' : this.isFemale(gender) ? 'Female' : 'Unknown';
  }

  // Check if this is the first result in a medical class group
  isFirstInMedicalClassGroup(index: number): boolean {
    const results = this.numericResults(); // Use filtered results
    if (index === 0) return true;
    return results[index].medicalClassDesc !== results[index - 1].medicalClassDesc;
  }

  print(): void {
    window.print();
  }

  close(): void {
    window.close();
  }

  // Check if a main result is abnormal (outside reference range)
  isResultAbnormal(result: PatientLabResult): boolean {
    if (!result.result) return false;

    const resultValue = parseFloat(result.result);
    if (isNaN(resultValue)) return false; // If not a number, can't determine

    const min = result.min ? parseFloat(result.min) : null;
    const max = result.max ? parseFloat(result.max) : null;

    if (min !== null && resultValue < min) return true;
    if (max !== null && resultValue > max) return true;

    return false;
  }

  // Check if a sub-test result is abnormal (outside reference range)
  isSubTestAbnormal(subTest: PatientLabSub): boolean {
    if (!subTest.result) return false;

    const result = parseFloat(subTest.result);
    if (isNaN(result)) return false; // If not a number, can't determine

    const min = subTest.min ? parseFloat(subTest.min) : null;
    const max = subTest.max ? parseFloat(subTest.max) : null;

    if (min !== null && result < min) return true;
    if (max !== null && result > max) return true;

    return false;
  }
}

