import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { PatientResultsService, PatientLabResult, PatientLabSub } from '../services/patient-results.service';
import { RichTextEditorComponent } from '../shared/rich-text-editor/rich-text-editor.component';

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

export interface Germ {
  id: number;
  description: string;
  code?: string | null;
  identifier?: string | null;
  displayOrder?: string | null;
}

export interface Bacteria {
  id: number;
  germId?: number | null;
  description: string;
  isPanic?: boolean | null;
}

export interface Antibiotic {
  id: number;
  description: string;
  arabicDescription?: string | null;
  code?: string | null;
  commercialName?: string | null;
  antibFamily?: number | null;
  displayOrder?: number | null;
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

// Helper function to get today's date in YYYY-MM-DD format
function getTodayDateString(): string {
  const today = new Date();
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, '0');
  const day = String(today.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

// Helper function to get default from date (January 1, 2024)
function getDefaultFromDate(): string {
  return '2024-01-01';
}

@Component({
  selector: 'app-patient-results',
  standalone: true,
  imports: [CommonModule, FormsModule, RichTextEditorComponent],
  templateUrl: './patient-results.component.html',
  styleUrls: ['./patient-results.component.scss']
})
export class PatientResultsComponent implements OnInit {
  private http = inject(HttpClient);
  private patientResultsService = inject(PatientResultsService);
  private readonly apiUrl = 'http://localhost:5050/api/residentpatients';

  // Expose Math and Array to template
  Math = Math;
  Array = Array;

  // Signals for state management
  readonly patients = signal<ResidentPatient[]>([]);
  readonly selectedPatient = signal<ResidentPatient | null>(null);
  readonly searchQuery = signal('');
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');

  // Date filter signals
  readonly fromDate = signal<string>(getDefaultFromDate());
  readonly toDate = signal<string>(getTodayDateString());

  // Lab results signals
  readonly labResults = signal<PatientLabResult[]>([]);
  readonly isLoadingResults = signal(false);
  readonly resultsErrorMessage = signal('');
  readonly isSavingResults = signal(false);
  readonly showAllAdmissions = signal(false);
  readonly isReadOnlyMode = signal(false);

  // Lab test filter (multi-select)
  readonly selectedLabTestsFilter = signal<Set<number>>(new Set());
  readonly labTestSearchQuery = signal<string>('');
  readonly showLabTestDropdown = signal<boolean>(false);

  // Medical class filter (multi-select)
  readonly selectedMedicalClassesFilter = signal<Set<number>>(new Set());
  readonly medicalClassSearchQuery = signal<string>('');
  readonly showMedicalClassDropdown = signal<boolean>(false);

  // Multi-select for printing
  readonly selectedMedicalClassesForPrint = signal<Set<number>>(new Set());
  readonly selectedLabTestsForPrint = signal<Set<number>>(new Set());
  readonly showPrintDropdown = signal<boolean>(false);

  // Expandable rows
  readonly expandedResultIds = signal<Set<number>>(new Set());
  readonly expandedAntibiogramIds = signal<Set<number>>(new Set());
  readonly subTestsMap = signal<Map<number, PatientLabSub[]>>(new Map());

  // Collapse/Expand sections
  readonly numericResultsCollapsed = signal(false);
  readonly textResultsCollapsed = signal(false);
  readonly antibiogramResultsCollapsed = signal(false);

  // Collapse/Expand admissions (tracks which admissions are collapsed by admission number)
  readonly collapsedAdmissions = signal<Set<string>>(new Set());

  // Germ and Bacteria autocomplete
  readonly allGerms = signal<Germ[]>([]);
  readonly allBacteria = signal<Bacteria[]>([]);
  readonly filteredGerms = signal<Germ[]>([]);
  readonly filteredBacteria = signal<Bacteria[]>([]);
  readonly showGermDropdown = signal<number | null>(null);
  readonly showBacteriaDropdown = signal<number | null>(null);

  // Store selected germ/bacteria per antibiogram result
  readonly antibiogramSelections = signal(new Map<number, {
    germId?: number,
    germName?: string,
    bacteriaId?: number,
    bacteriaName?: string,
    antibiotics?: Antibiotic[],
    bacteriologyRecords?: PatientLabBacteriology[],
    bacteriologyHeaderId?: number
  }>());

  // Computed: Available lab tests for filtering
  readonly availableLabTests = computed(() => {
    const allResults = this.labResults();
    const uniqueTests = new Map<number, string>();

    allResults.forEach(r => {
      if (r.labTestID && !uniqueTests.has(r.labTestID)) {
        uniqueTests.set(r.labTestID, r.labTestDescription || 'Unknown Test');
      }
    });

    return Array.from(uniqueTests.entries())
      .map(([id, description]) => ({ id, description }))
      .sort((a, b) => a.description.localeCompare(b.description));
  });

  // Computed: Available medical classes for filtering
  readonly availableMedicalClasses = computed(() => {
    const allResults = this.labResults();
    const uniqueClasses = new Map<number, string>();

    allResults.forEach(r => {
      if (r.medicalClass && !uniqueClasses.has(r.medicalClass)) {
        uniqueClasses.set(r.medicalClass, r.medicalClassDesc || 'Unknown Class');
      }
    });

    return Array.from(uniqueClasses.entries())
      .map(([id, description]) => ({ id, description }))
      .sort((a, b) => a.id - b.id); // Sort by ID to maintain medical class order
  });

  // Computed: Filtered lab tests based on search query
  readonly filteredAvailableLabTests = computed(() => {
    const allTests = this.availableLabTests();
    const query = this.labTestSearchQuery().toLowerCase().trim();

    if (!query) {
      return allTests;
    }

    return allTests.filter(test =>
      test.description.toLowerCase().includes(query)
    );
  });

  // Computed: Filtered medical classes based on search query
  readonly filteredAvailableMedicalClasses = computed(() => {
    const allClasses = this.availableMedicalClasses();
    const query = this.medicalClassSearchQuery().toLowerCase().trim();

    if (!query) {
      return allClasses;
    }

    return allClasses.filter(medClass =>
      medClass.description.toLowerCase().includes(query)
    );
  });

  // Computed: Filtered results based on selected lab tests and medical classes
  // Results are ordered by MedicalClass then DisplayOrder (from API)
  readonly filteredLabResults = computed(() => {
    let results = this.labResults();
    const filterLabTestIds = this.selectedLabTestsFilter();
    const filterMedicalClassIds = this.selectedMedicalClassesFilter();

    // Apply lab test filter - multiple tests (maintains order)
    if (filterLabTestIds.size > 0) {
      results = results.filter(r => r.labTestID && filterLabTestIds.has(r.labTestID));
    }

    // Apply medical class filter - multiple classes (maintains order)
    if (filterMedicalClassIds.size > 0) {
      results = results.filter(r => filterMedicalClassIds.has(r.medicalClass));
    }

    // Return filtered results in original order: MedicalClass → DisplayOrder
    return results;
  });

  // Computed: Separate numeric, text, and antibiogram results based on ResultType
  // ResultType: 1 = Text, 2 = Numeric, 3 = Antibiogram
  // Order is maintained: MedicalClass → DisplayOrder
  readonly numericResults = computed(() => {
    const results = this.filteredLabResults().filter(r => r.resultType === 2);
    console.log('🔢 Numeric Results (ResultType=2):', results.length, results);
    // Results maintain order: MedicalClass → DisplayOrder
    return results;
  });

  readonly textResults = computed(() => {
    const results = this.filteredLabResults().filter(r => r.resultType === 1);
    console.log('📝 Text Results (ResultType=1):', results.length, results);
    // Results maintain order: MedicalClass → DisplayOrder
    return results;
  });

  readonly antibiogramResults = computed(() => {
    const results = this.filteredLabResults().filter(r => r.resultType === 3);
    console.log('🦠 Antibiogram Results (ResultType=3):', results.length, results);

    // Debug: Show all results with their types
    console.log('📊 ALL Lab Results with ResultType:', this.labResults().map(r => ({
      id: r.id,
      labTestID: r.labTestID,
      resultType: r.resultType,
      resultTypeType: typeof r.resultType,
      description: r.labTestDescription
    })));

    // Results maintain order: MedicalClass → DisplayOrder
    return results;
  });

  // Group results by admission when showing all admissions
  readonly resultsByAdmission = computed(() => {
    if (!this.showAllAdmissions()) {
      return []; // Not used when showing single admission
    }

    const results = this.filteredLabResults();
    const admissionsMap = new Map<string, {
      admissionNumber: string;
      requestDate: string | null;
      isCurrentAdmission: boolean;
      numericResults: PatientLabResult[];
      textResults: PatientLabResult[];
      antibiogramResults: PatientLabResult[];
    }>();

    // Group results by admission
    results.forEach(result => {
      const admissionNumber = result.admissionNumber || 'Unknown';

      if (!admissionsMap.has(admissionNumber)) {
        admissionsMap.set(admissionNumber, {
          admissionNumber,
          requestDate: result.requestDate || null,
          isCurrentAdmission: result.isCurrentAdmission || false,
          numericResults: [],
          textResults: [],
          antibiogramResults: []
        });
      }

      const admission = admissionsMap.get(admissionNumber)!;

      // Group by result type
      if (result.resultType === 2) {
        admission.numericResults.push(result);
      } else if (result.resultType === 1) {
        admission.textResults.push(result);
      } else if (result.resultType === 3) {
        admission.antibiogramResults.push(result);
      }
    });

    // Convert to array and sort (current admission first, then by request date descending)
    return Array.from(admissionsMap.values())
      .sort((a, b) => {
        if (a.isCurrentAdmission && !b.isCurrentAdmission) return -1;
        if (!a.isCurrentAdmission && b.isCurrentAdmission) return 1;

        // Sort by request date (most recent first)
        const dateA = a.requestDate ? new Date(a.requestDate).getTime() : 0;
        const dateB = b.requestDate ? new Date(b.requestDate).getTime() : 0;
        return dateB - dateA;
      });
  });

  // Pagination
  readonly pageSize = signal(20);
  readonly pageIndex = signal(0);

  // Computed filtered and paginated list
  // Note: Date filtering is now done on the server side via API parameters
  readonly filteredPatients = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const allPatients = this.patients();

    // Filter by search query only (date filtering is done server-side)
    if (!query) return allPatients;

    return allPatients.filter(p =>
      p.patientName.toLowerCase().includes(query) ||
      p.medicalRecordNumber.toLowerCase().includes(query) ||
      p.admissionNumber.toLowerCase().includes(query) ||
      (p.arabicFullName && p.arabicFullName.includes(query))
    );
  });

  readonly totalPages = computed(() => {
    const total = this.filteredPatients().length;
    const size = this.pageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  readonly pagedPatients = computed(() => {
    const size = this.pageSize();
    const idx = this.pageIndex();
    const start = idx * size;
    return this.filteredPatients().slice(start, start + size);
  });

  ngOnInit(): void {
    this.loadPatients();
    this.loadGerms();
  }

  loadPatients(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    // Build query parameters with date filters
    const params: any = {};
    if (this.fromDate()) {
      params.fromDate = this.fromDate();
    }
    if (this.toDate()) {
      params.toDate = this.toDate();
    }

    const queryString = new URLSearchParams(params).toString();
    const url = queryString ? `${this.apiUrl}?${queryString}` : this.apiUrl;

    this.http.get<ResidentPatient[]>(url).subscribe({
      next: (data) => {
        this.patients.set(data);
        this.isLoading.set(false);
        console.log(`Loaded ${data.length} patients from ${this.fromDate()} to ${this.toDate()}`);
      },
      error: (err) => {
        console.error('Error loading patients:', err);
        this.errorMessage.set('Failed to load patients. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  searchPatients(): void {
    const query = this.searchQuery().trim();
    if (!query || query.length < 2) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.http.get<ResidentPatient[]>(`${this.apiUrl}/search?query=${encodeURIComponent(query)}`).subscribe({
      next: (data) => {
        this.patients.set(data);
        this.isLoading.set(false);
        this.pageIndex.set(0);
      },
      error: (err) => {
        console.error('Error searching patients:', err);
        this.errorMessage.set('Search failed. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  selectPatient(patient: ResidentPatient): void {
    this.selectedPatient.set(patient);
    this.loadPatientResults(patient.admissionNumber);

    // Generate lab results from invoice asynchronously (don't wait for it)
    // This runs in the background and will reload results when done
    setTimeout(() => {
      this.generateLabResultsFromInvoice(patient.admissionNumber);
    }, 100);
  }

  toggleShowAllAdmissions(): void {
    const newValue = !this.showAllAdmissions();
    this.showAllAdmissions.set(newValue);

    // Reload results with the new setting
    const patient = this.selectedPatient();
    if (patient) {
      this.loadPatientResults(patient.admissionNumber);
    }
  }

  toggleReadOnlyMode(): void {
    const newValue = !this.isReadOnlyMode();
    this.isReadOnlyMode.set(newValue);
  }

  generateDashes(count: number): string {
    // Ensure we don't generate negative dashes
    const dashCount = Math.max(10, count); // Minimum 10 dashes
    return '-'.repeat(dashCount);
  }

  /**
   * Check if a lab result is above the reference range
   */
  isResultHigh(result: PatientLabResult): boolean {
    if (!result.result || !result.max) {
      return false;
    }

    const resultValue = parseFloat(result.result.toString());
    const maxValue = parseFloat(result.max.toString());

    return !isNaN(resultValue) && !isNaN(maxValue) && resultValue > maxValue;
  }

  /**
   * Check if a lab result is below the reference range
   */
  isResultLow(result: PatientLabResult): boolean {
    if (!result.result || !result.min) {
      return false;
    }

    const resultValue = parseFloat(result.result.toString());
    const minValue = parseFloat(result.min.toString());

    return !isNaN(resultValue) && !isNaN(minValue) && resultValue < minValue;
  }

  /**
   * Get CSS classes for result row based on reference range
   */
  getResultRowClasses(result: PatientLabResult, index: number): string {
    let classes = '';

    // Base alternating row classes
    if (this.isReadOnlyMode()) {
      classes = index % 2 === 0 ? 'bg-light' : 'bg-white';
    }

    // Add reference range classes
    if (this.isResultHigh(result)) {
      classes += ' bg-danger bg-opacity-25'; // Light red for high values
    } else if (this.isResultLow(result)) {
      classes += ' bg-warning bg-opacity-25'; // Light yellow for low values
    }

    return classes;
  }

  /**
   * Check if a lab sub-result is above the reference range
   */
  isSubResultHigh(subResult: PatientLabSub): boolean {
    if (!subResult.result || !subResult.max) {
      return false;
    }

    const resultValue = parseFloat(subResult.result.toString());
    const maxValue = parseFloat(subResult.max.toString());

    return !isNaN(resultValue) && !isNaN(maxValue) && resultValue > maxValue;
  }

  /**
   * Check if a lab sub-result is below the reference range
   */
  isSubResultLow(subResult: PatientLabSub): boolean {
    if (!subResult.result || !subResult.min) {
      return false;
    }

    const resultValue = parseFloat(subResult.result.toString());
    const minValue = parseFloat(subResult.min.toString());

    return !isNaN(resultValue) && !isNaN(minValue) && resultValue < minValue;
  }

  /**
   * Get CSS classes for sub-test row based on reference range
   */
  getSubResultRowClasses(subResult: PatientLabSub, index: number): string {
    let classes = '';

    // Base alternating row classes for sub-tests
    if (this.isReadOnlyMode()) {
      classes = index % 2 === 0 ? 'bg-info bg-opacity-10' : 'bg-secondary bg-opacity-10';
    }

    // Add reference range classes
    if (this.isSubResultHigh(subResult)) {
      classes += ' bg-danger bg-opacity-25'; // Light red for high values
    } else if (this.isSubResultLow(subResult)) {
      classes += ' bg-warning bg-opacity-25'; // Light yellow for low values
    }

    return classes;
  }

  isResultReadOnly(result: PatientLabResult): boolean {
    // Result is read-only if:
    // 1. Dedicated read-only mode is enabled, OR
    // 2. Showing all admissions and this is not the current admission
    return this.isReadOnlyMode() || (this.showAllAdmissions() && result.isCurrentAdmission === false);
  }

  loadPatientResults(admissionNumber: string): void {
    this.isLoadingResults.set(true);
    this.resultsErrorMessage.set('');
    this.labResults.set([]);
    this.expandedResultIds.set(new Set());
    this.subTestsMap.set(new Map());

    // Reset lab test filter
    this.selectedLabTestsFilter.set(new Set());
    this.labTestSearchQuery.set('');
    this.showLabTestDropdown.set(false);

    // Reset medical class filter
    this.selectedMedicalClassesFilter.set(new Set());
    this.medicalClassSearchQuery.set('');
    this.showMedicalClassDropdown.set(false);

    // Reset antibiogram selections
    this.antibiogramSelections.set(new Map());

    const patient = this.selectedPatient();
    const useAllAdmissions = this.showAllAdmissions();

    const apiCall = useAllAdmissions && patient
      ? this.patientResultsService.getByMRN(patient.mrn, patient.admissionNumber)
      : this.patientResultsService.getByAdmissionNumber(admissionNumber);

    apiCall.subscribe({
      next: (data) => {
        console.log('🔍 RAW API Response:', data);
        console.log('🔍 First result sample:', data[0]);
        console.log('🔍 ResultType values:', data.map(r => ({
          id: r.id,
          labTestID: r.labTestID,
          resultType: r.resultType,
          typeOf: typeof r.resultType,
          isNull: r.resultType === null,
          isUndefined: r.resultType === undefined
        })));

        this.labResults.set(data);
        // Pre-load sub-tests for all results to determine which ones have sub-tests
        data.forEach(result => {
          this.loadSubTests(result.id);
          // Initialize text editors with existing content
          if (result.defaultTextResult && result.defaultTextResult.trim() !== '') {
            this.initializeTextEditor(result.id, result.result || result.defaultTextResult);
          }
          // Load existing bacteriology records for antibiogram tests (ResultType = 3)
          if (result.resultType === 3) {
            this.loadExistingBacteriologyRecords(result.id);
          }
        });
        this.isLoadingResults.set(false);
      },
      error: (err) => {
        console.error('Error loading lab results:', err);
        this.resultsErrorMessage.set('Failed to load lab results. Please try again.');
        this.isLoadingResults.set(false);
      }
    });
  }

  saveLabResults(): void {
    this.isSavingResults.set(true);
    this.resultsErrorMessage.set('');

    // Explicitly capture content from all rich text editors before saving
    this.textResults().forEach((result) => {
      const editorElement = document.querySelector(`#text-result-${result.id}`) as HTMLElement;
      if (editorElement) {
        const content = editorElement.innerHTML;
        // Update the result in the labResults signal
        const allResults = this.labResults();
        const resultIndex = allResults.findIndex(r => r.id === result.id);
        if (resultIndex !== -1) {
          allResults[resultIndex].result = content;
        }
      }
    });

    const results = this.labResults();

    // Collect all sub-tests from the map
    const allSubTests: PatientLabSub[] = [];
    this.subTestsMap().forEach((subTests) => {
      allSubTests.push(...subTests);
    });

    // Save main results and sub-tests
    const saveResults$ = results.length > 0
      ? this.patientResultsService.batchUpdateResults(results)
      : of(null);

    const saveSubTests$ = allSubTests.length > 0
      ? this.patientResultsService.batchUpdateSubTests(allSubTests)
      : of(null);

    // Execute both save operations
    saveResults$.subscribe({
      next: () => {
        if (allSubTests.length > 0) {
          // Save sub-tests after main results
          saveSubTests$.subscribe({
            next: () => {
              this.isSavingResults.set(false);
              alert('Lab results and sub-tests saved successfully!');
            },
            error: (err) => {
              console.error('Error saving sub-tests:', err);
              this.resultsErrorMessage.set('Failed to save sub-tests. Please try again.');
              this.isSavingResults.set(false);
            }
          });
        } else {
          this.isSavingResults.set(false);
          alert('Lab results saved successfully!');
        }
      },
      error: (err) => {
        console.error('Error saving lab results:', err);
        this.resultsErrorMessage.set('Failed to save lab results. Please try again.');
        this.isSavingResults.set(false);
      }
    });
  }

  toggleExpand(resultId: number): void {
    const expanded = this.expandedResultIds();
    const newSet = new Set(expanded);

    if (newSet.has(resultId)) {
      newSet.delete(resultId);
    } else {
      newSet.add(resultId);
      // Load sub-tests if not already loaded
      if (!this.subTestsMap().has(resultId)) {
        this.loadSubTests(resultId);
      }
    }

    this.expandedResultIds.set(newSet);
  }

  isExpanded(resultId: number): boolean {
    return this.expandedResultIds().has(resultId);
  }

  // Antibiogram expand/collapse methods
  toggleAntibiogramExpanded(resultId: number): void {
    const currentExpanded = this.expandedAntibiogramIds();
    const newExpanded = new Set(currentExpanded);

    if (newExpanded.has(resultId)) {
      newExpanded.delete(resultId);
    } else {
      newExpanded.add(resultId);
    }

    this.expandedAntibiogramIds.set(newExpanded);
  }

  isAntibiogramExpanded(resultId: number): boolean {
    return this.expandedAntibiogramIds().has(resultId);
  }

  loadSubTests(patientLabTestId: number): void {
    this.patientResultsService.getSubTestsByPatientLabTestId(patientLabTestId).subscribe({
      next: (data) => {
        const currentMap = this.subTestsMap();
        const newMap = new Map(currentMap);
        newMap.set(patientLabTestId, data);
        this.subTestsMap.set(newMap);
      },
      error: (err) => {
        console.error('Error loading sub-tests:', err);
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

  isSelected(patient: ResidentPatient): boolean {
    return this.selectedPatient()?.id === patient.id;
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
    this.pageIndex.set(0);
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.pageIndex.set(0);
    this.loadPatients();
  }

  // Date filter methods
  onFromDateChange(date: string): void {
    this.fromDate.set(date);
    this.pageIndex.set(0); // Reset to first page
    this.loadPatients(); // Reload data from API with new date filter
  }

  onToDateChange(date: string): void {
    this.toDate.set(date);
    this.pageIndex.set(0); // Reset to first page
    this.loadPatients(); // Reload data from API with new date filter
  }

  clearDateFilters(): void {
    this.fromDate.set(getDefaultFromDate());
    this.toDate.set(getTodayDateString());
    this.pageIndex.set(0);
    this.loadPatients(); // Reload data from API
  }

  // Pagination methods
  setPageSize(size: number): void {
    this.pageSize.set(size);
    this.pageIndex.set(0);
  }

  firstPage(): void {
    this.pageIndex.set(0);
  }

  prevPage(): void {
    const current = this.pageIndex();
    if (current > 0) this.pageIndex.set(current - 1);
  }

  nextPage(): void {
    const current = this.pageIndex();
    if (current < this.totalPages() - 1) this.pageIndex.set(current + 1);
  }

  lastPage(): void {
    this.pageIndex.set(this.totalPages() - 1);
  }

  // Gender helpers to support multiple backend formats
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

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}-${month}-${year}`;
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

  // Check if this is the first result in a medical class group
  isFirstInMedicalClassGroup(index: number): boolean {
    const results = this.numericResults();
    if (index === 0) return true;
    return results[index].medicalClassDesc !== results[index - 1].medicalClassDesc;
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

  // Initialize text result editors with existing content
  initializeTextEditor(resultId: number, content: string | null | undefined): void {
    setTimeout(() => {
      const editor = document.getElementById(`text-result-${resultId}`) as HTMLElement;
      if (editor && content) {
        editor.innerHTML = content;
      }
    }, 100);
  }

  // Keyboard navigation methods
  focusNextResult(currentIndex: number): void {
    const results = this.numericResults(); // Use filtered results

    // Find the next non-disabled result field
    for (let i = currentIndex + 1; i < results.length; i++) {
      const nextResult = results[i];

      // Skip if it has sub-tests (disabled)
      if (!this.hasSubTests(nextResult.id)) {
        const nextElement = document.getElementById(`result_${i}`) as HTMLInputElement;
        if (nextElement) {
          nextElement.focus();
          nextElement.select();
          return;
        }
      }
    }

    // If we couldn't find a next result, try to focus the first result (wrap around)
    for (let i = 0; i < currentIndex; i++) {
      const result = results[i];
      if (!this.hasSubTests(result.id)) {
        const element = document.getElementById(`result_${i}`) as HTMLInputElement;
        if (element) {
          element.focus();
          element.select();
          return;
        }
      }
    }
  }

  focusNextSubResult(mainIndex: number, currentSubIndex: number): void {
    const mainResult = this.numericResults()[mainIndex]; // Use filtered results
    const subTests = this.getSubTests(mainResult.id);

    // Move to next sub-test in the same main result
    if (currentSubIndex + 1 < subTests.length) {
      const nextElement = document.getElementById(`sub_result_${mainIndex}_${currentSubIndex + 1}`) as HTMLInputElement;
      if (nextElement) {
        nextElement.focus();
        nextElement.select();
        return;
      }
    }

    // We're at the last sub-test, so move to the next main result
    this.focusNextResult(mainIndex);
  }

  // Rich text editor callback
  onTextResultBlur(result: PatientLabResult): void {
    // Ensure content is captured from the editor when it loses focus
    const editorElement = document.querySelector(`#text-result-${result.id}`) as HTMLElement;
    if (editorElement) {
      const content = editorElement.innerHTML;
      // Update the result in the labResults signal
      const allResults = this.labResults();
      const resultIndex = allResults.findIndex(r => r.id === result.id);
      if (resultIndex !== -1) {
        allResults[resultIndex].result = content;
        console.log('Text result updated on blur:', result.id, content);
      }
    }
  }

  // Toggle collapse/expand for result type sections
  toggleNumericResultsCollapsed(): void {
    this.numericResultsCollapsed.update(collapsed => !collapsed);
  }

  toggleTextResultsCollapsed(): void {
    this.textResultsCollapsed.update(collapsed => !collapsed);
  }

  toggleAntibiogramResultsCollapsed(): void {
    this.antibiogramResultsCollapsed.update(collapsed => !collapsed);
  }

  // Toggle admission collapsed state
  toggleAdmissionCollapsed(admissionNumber: string): void {
    const collapsed = new Set(this.collapsedAdmissions());
    if (collapsed.has(admissionNumber)) {
      collapsed.delete(admissionNumber);
    } else {
      collapsed.add(admissionNumber);
    }
    this.collapsedAdmissions.set(collapsed);
  }

  // Check if admission is collapsed
  isAdmissionCollapsed(admissionNumber: string): boolean {
    return this.collapsedAdmissions().has(admissionNumber);
  }

  // Lab test filter methods
  onLabTestSearchInput(query: string): void {
    this.labTestSearchQuery.set(query);
    this.showLabTestDropdown.set(true);
  }

  onLabTestSearchFocus(): void {
    this.showLabTestDropdown.set(true);
  }

  onLabTestSearchBlur(): void {
    // Delay hiding to allow click on dropdown items
    setTimeout(() => {
      this.showLabTestDropdown.set(false);
    }, 200);
  }

  toggleLabTestFilter(labTestId: number): void {
    const current = this.selectedLabTestsFilter();
    const newSet = new Set(current);

    if (newSet.has(labTestId)) {
      newSet.delete(labTestId);
    } else {
      newSet.add(labTestId);
    }

    this.selectedLabTestsFilter.set(newSet);
  }

  isLabTestSelected(labTestId: number): boolean {
    return this.selectedLabTestsFilter().has(labTestId);
  }

  clearLabTestFilter(): void {
    this.selectedLabTestsFilter.set(new Set());
    this.labTestSearchQuery.set('');
    this.showLabTestDropdown.set(false);
  }

  selectAllLabTests(): void {
    const allIds = new Set(this.availableLabTests().map(lt => lt.id));
    this.selectedLabTestsFilter.set(allIds);
  }

  getLabTestName(id: number): string {
    return this.availableLabTests().find(t => t.id === id)?.description || '';
  }

  // Medical class filter methods
  onMedicalClassSearchInput(query: string): void {
    this.medicalClassSearchQuery.set(query);
    this.showMedicalClassDropdown.set(true);
  }

  onMedicalClassSearchFocus(): void {
    this.showMedicalClassDropdown.set(true);
  }

  onMedicalClassSearchBlur(): void {
    // Delay hiding to allow click on dropdown items
    setTimeout(() => {
      this.showMedicalClassDropdown.set(false);
    }, 200);
  }

  toggleMedicalClassFilter(medicalClassId: number): void {
    const current = this.selectedMedicalClassesFilter();
    const newSet = new Set(current);

    if (newSet.has(medicalClassId)) {
      newSet.delete(medicalClassId);
    } else {
      newSet.add(medicalClassId);
    }

    this.selectedMedicalClassesFilter.set(newSet);
  }

  isMedicalClassSelected(medicalClassId: number): boolean {
    return this.selectedMedicalClassesFilter().has(medicalClassId);
  }

  clearMedicalClassFilter(): void {
    this.selectedMedicalClassesFilter.set(new Set());
    this.medicalClassSearchQuery.set('');
    this.showMedicalClassDropdown.set(false);
  }

  selectAllMedicalClasses(): void {
    const allIds = new Set(this.availableMedicalClasses().map(mc => mc.id));
    this.selectedMedicalClassesFilter.set(allIds);
  }

  getSelectedMedicalClassNames(): string[] {
    const selectedIds = Array.from(this.selectedMedicalClassesFilter());
    return selectedIds
      .map(id => this.availableMedicalClasses().find(c => c.id === id)?.description)
      .filter((desc): desc is string => desc !== undefined);
  }

  getMedicalClassName(id: number): string {
    return this.availableMedicalClasses().find(c => c.id === id)?.description || '';
  }

  // Multi-select methods for printing
  toggleMedicalClassForPrint(medicalClassId: number): void {
    const current = this.selectedMedicalClassesForPrint();
    const newSet = new Set(current);

    if (newSet.has(medicalClassId)) {
      newSet.delete(medicalClassId);
    } else {
      newSet.add(medicalClassId);
    }

    this.selectedMedicalClassesForPrint.set(newSet);
  }

  toggleLabTestForPrint(labTestId: number): void {
    const current = this.selectedLabTestsForPrint();
    const newSet = new Set(current);

    if (newSet.has(labTestId)) {
      newSet.delete(labTestId);
    } else {
      newSet.add(labTestId);
    }

    this.selectedLabTestsForPrint.set(newSet);
  }

  isMedicalClassSelectedForPrint(medicalClassId: number): boolean {
    return this.selectedMedicalClassesForPrint().has(medicalClassId);
  }

  isLabTestSelectedForPrint(labTestId: number): boolean {
    return this.selectedLabTestsForPrint().has(labTestId);
  }

  selectAllMedicalClassesForPrint(): void {
    const allIds = new Set(this.availableMedicalClasses().map(mc => mc.id));
    this.selectedMedicalClassesForPrint.set(allIds);
  }

  clearAllMedicalClassesForPrint(): void {
    this.selectedMedicalClassesForPrint.set(new Set());
  }

  selectAllLabTestsForPrint(): void {
    const allIds = new Set(this.availableLabTests().map(lt => lt.id));
    this.selectedLabTestsForPrint.set(allIds);
  }

  clearAllLabTestsForPrint(): void {
    this.selectedLabTestsForPrint.set(new Set());
  }

  togglePrintDropdown(): void {
    this.showPrintDropdown.update(show => !show);
  }

  closePrintDropdown(): void {
    this.showPrintDropdown.set(false);
  }

  getPrintUrl(): string {
    const admissionNumber = this.selectedPatient()?.admissionNumber;
    if (!admissionNumber) return '#';

    const params = new URLSearchParams();

    // Add selected medical classes
    const selectedMedClasses = Array.from(this.selectedMedicalClassesForPrint());
    if (selectedMedClasses.length > 0) {
      params.set('medicalClasses', selectedMedClasses.join(','));
    }

    // Add selected lab tests
    const selectedTests = Array.from(this.selectedLabTestsForPrint());
    if (selectedTests.length > 0) {
      params.set('labTests', selectedTests.join(','));
    }

    const queryString = params.toString();
    return `/print-lab-results/${admissionNumber}${queryString ? '?' + queryString : ''}`;
  }

  openPrintWindow(): void {
    const url = this.getPrintUrl();
    window.open(url, '_blank');
    this.closePrintDropdown();
  }

  // ===== Germ and Bacteria Methods =====

  loadGerms(): void {
    console.log('🔄 Loading germs from API...');
    this.http.get<Germ[]>('http://localhost:5050/api/Germs').subscribe({
      next: (germs) => {
        this.allGerms.set(germs);
        console.log(`✅ Loaded ${germs.length} germs:`, germs.slice(0, 5));
      },
      error: (err) => {
        console.error('❌ Error loading germs:', err);
        alert('Failed to load germs from API. Please check if the API is running on port 5050.');
      }
    });
  }

  onGermFocus(resultId: number): void {
    // Show all germs when focused
    const germs = this.allGerms();
    console.log(`🔍 Germ focus for result ${resultId}. Available germs:`, germs.length);
    this.filteredGerms.set(germs);
    this.showGermDropdown.set(resultId);
    console.log(`👁️ Showing dropdown: ${this.showGermDropdown()}, Filtered germs: ${this.filteredGerms().length}`);
  }

  onGermSearch(resultId: number, query: string): void {
    if (!query || query.trim() === '') {
      this.filteredGerms.set(this.allGerms());
    } else {
      const filtered = this.allGerms().filter(g =>
        g.description.toLowerCase().includes(query.toLowerCase()) ||
        (g.code && g.code.toLowerCase().includes(query.toLowerCase()))
      );
      this.filteredGerms.set(filtered);
    }
    this.showGermDropdown.set(resultId);
  }

  selectGerm(resultId: number, germ: Germ): void {
    // Store selected germ - create new Map for reactivity
    const currentMap = this.antibiogramSelections();
    const selection = currentMap.get(resultId) || {};
    selection.germId = germ.id;
    selection.germName = germ.description;
    // Clear bacteria when germ changes
    selection.bacteriaId = undefined;
    selection.bacteriaName = undefined;
    selection.antibiotics = undefined;

    const newMap = new Map(currentMap);
    newMap.set(resultId, selection);
    this.antibiogramSelections.set(newMap);

    // Hide dropdown
    this.showGermDropdown.set(null);
    this.filteredGerms.set([]);

    // Load bacteria filtered by this germ
    this.loadBacteriaForGerm(germ.id);

    // Load antibiotics and create PatientLabBacteriology records for this germ
    this.loadAntibioticsForGerm(resultId, germ.id);

    console.log(`Selected germ: ${germ.description} for result ${resultId}`);
  }

  loadBacteriaForGerm(germId: number): void {
    this.http.get<Bacteria[]>(`http://localhost:5050/api/Bacteria?germId=${germId}`).subscribe({
      next: (bacteria) => {
        this.allBacteria.set(bacteria);
        console.log(`Loaded ${bacteria.length} bacteria for germ ${germId}`);
      },
      error: (err) => {
        console.error('Error loading bacteria:', err);
      }
    });
  }

  onBacteriaFocus(resultId: number): void {
    // Show all bacteria when focused (filtered by germ if selected)
    this.filteredBacteria.set(this.allBacteria());
    this.showBacteriaDropdown.set(resultId);
  }

  onBacteriaSearch(resultId: number, query: string): void {
    if (!query || query.trim() === '') {
      this.filteredBacteria.set(this.allBacteria());
    } else {
      const filtered = this.allBacteria().filter(b =>
        b.description.toLowerCase().includes(query.toLowerCase())
      );
      this.filteredBacteria.set(filtered);
    }
    this.showBacteriaDropdown.set(resultId);
  }

  selectBacteria(resultId: number, bacteria: Bacteria): void {
    // Store selected bacteria - create new Map for reactivity
    const currentMap = this.antibiogramSelections();
    const selection = currentMap.get(resultId) || {};
    selection.bacteriaId = bacteria.id;
    selection.bacteriaName = bacteria.description;

    const newMap = new Map(currentMap);
    newMap.set(resultId, selection);
    this.antibiogramSelections.set(newMap);

    // Hide dropdown
    this.showBacteriaDropdown.set(null);
    this.filteredBacteria.set([]);

    // If germ is already selected, reload records to save bacteria info to header
    if (selection.germId) {
      this.loadAntibioticsForGerm(resultId, selection.germId);
    }

    console.log(`Selected bacteria: ${bacteria.description} for result ${resultId}`);
  }

  loadExistingBacteriologyRecords(resultId: number): void {
    // Load existing bacteriology records for this result
    this.http.get<{
      header: any,
      details: PatientLabBacteriology[]
    }>(`http://localhost:5050/api/PatientLabBacteriology/byPatientLabResult/${resultId}`).subscribe({
      next: (response) => {
        if (response.header && response.details && response.details.length > 0) {
          const currentMap = this.antibiogramSelections();
          const selection = currentMap.get(resultId) || {};
          selection.bacteriologyRecords = response.details;
          selection.bacteriologyHeaderId = response.header.id;

          // Load germ info from header
          if (response.header.germsId) {
            selection.germId = response.header.germsId;
            selection.germName = response.header.germ;
            console.log(`Loaded germ: ${response.header.germ} (ID: ${response.header.germsId})`);
          }

          // Load bacteria info from header
          if (response.header.bacterieId) {
            selection.bacteriaId = response.header.bacterieId;
            selection.bacteriaName = response.header.bacteria;
            console.log(`Loaded bacteria: ${response.header.bacteria} (ID: ${response.header.bacterieId})`);
          }

          const newMap = new Map(currentMap);
          newMap.set(resultId, selection);
          this.antibiogramSelections.set(newMap);

          console.log(`Loaded ${response.details.length} existing bacteriology records for result ${resultId}`);
        }
      },
      error: (err) => {
        // It's OK if no records exist yet - just log for debugging
        if (err.status !== 404) {
          console.error('Error loading existing bacteriology records:', err);
        }
      }
    });
  }

  loadAntibioticsForGerm(resultId: number, germId: number): void {
    // Get the current patient result (this is the PatientLabResult with ResultType = 3 for Antibiogram)
    const currentResult = this.labResults().find(r => r.id === resultId);
    if (!currentResult) {
      console.error('Result not found:', resultId);
      return;
    }

    // Get bacteria info from current selection if available
    const currentSelection = this.antibiogramSelections().get(resultId);
    const bacteriaId = currentSelection?.bacteriaId;
    const bacteriaName = currentSelection?.bacteriaName;

    // Call the createForGerm endpoint to create PatientLabBacteriology records
    const requestBody = {
      patientLabResultId: currentResult.id,  // This is the PatientLabResult ID (Antibiogram test)
      germId: germId,
      bacteriaId: bacteriaId || null,  // Pass bacteria ID if selected
      bacteriaName: bacteriaName || null,  // Pass bacteria name if selected
      createdBy: 1, // Replace with actual user ID
      comments: null,
      colony: null
    };

    this.http.post<{
      message: string,
      headerId: number,
      detailsCreated: number,
      totalRecords: number,
      details: PatientLabBacteriology[]
    }>('http://localhost:5050/api/PatientLabBacteriology/createForGerm', requestBody).subscribe({
      next: (response) => {
        const currentMap = this.antibiogramSelections();
        const selection = currentMap.get(resultId) || {};
        selection.bacteriologyRecords = response.details;
        selection.bacteriologyHeaderId = response.headerId;

        const newMap = new Map(currentMap);
        newMap.set(resultId, selection);
        this.antibiogramSelections.set(newMap);

        console.log(`Created ${response.detailsCreated} new bacteriology records (total: ${response.totalRecords}) for result ${resultId}, germ ${germId}`);
        console.log('Records:', response.details);
      },
      error: (err) => {
        console.error('Error creating bacteriology records:', err);
      }
    });
  }

  getAntibiogramGermName(resultId: number): string {
    return this.antibiogramSelections().get(resultId)?.germName || '';
  }

  getAntibiogramGermId(resultId: number): number | undefined {
    return this.antibiogramSelections().get(resultId)?.germId;
  }

  getAntibiogramBacteriaName(resultId: number): string {
    return this.antibiogramSelections().get(resultId)?.bacteriaName || '';
  }

  getBacteriologyRecords(resultId: number): PatientLabBacteriology[] {
    return this.antibiogramSelections().get(resultId)?.bacteriologyRecords || [];
  }

  // Handle mutual exclusivity for Resistant, Intermediat, and Sensible checkboxes
  onSensitivityChange(record: PatientLabBacteriology, field: 'resistant' | 'intermediat' | 'sensible'): void {
    // If the selected field is now true, uncheck the other two
    if (record[field]) {
      if (field !== 'resistant') record.resistant = false;
      if (field !== 'intermediat') record.intermediat = false;
      if (field !== 'sensible') record.sensible = false;
    }
  }

  saveBacteriologyRecords(resultId: number): void {
    const records = this.getBacteriologyRecords(resultId);
    if (records.length === 0) {
      console.warn('No bacteriology records to save');
      return;
    }

    // Prepare the batch update payload with actual table structure
    const updates = records.map(record => ({
      id: record.id,
      resistant: record.resistant,
      intermediat: record.intermediat,
      sensible: record.sensible,
      charge: record.charge,
      diameter: record.diameter,
      modifiedBy: 1 // Replace with actual user ID
    }));

    this.http.put('http://localhost:5050/api/PatientLabBacteriology/batchUpdate', updates).subscribe({
      next: (response: any) => {
        console.log('Bacteriology records saved successfully:', response);
        alert(`Saved ${response.count} bacteriology results successfully!`);
      },
      error: (err) => {
        console.error('Error saving bacteriology records:', err);
        alert('Error saving bacteriology results. Please try again.');
      }
    });
  }

  hideGermDropdownDelayed(): void {
    setTimeout(() => {
      this.showGermDropdown.set(null);
      this.filteredGerms.set([]);
    }, 200);
  }

  hideBacteriaDropdownDelayed(): void {
    setTimeout(() => {
      this.showBacteriaDropdown.set(null);
      this.filteredBacteria.set([]);
    }, 200);
  }

  generateLabResultsFromInvoice(admissionNumber: string): void {
    console.log(`🔄 Generating lab results from invoice for admission: ${admissionNumber}`);

    this.patientResultsService.generateFromInvoice(admissionNumber).subscribe({
      next: (response) => {
        console.log('✅ Lab results generated from invoice:', response);

        // Reload patient results to show the newly generated ones
        this.loadPatientResults(admissionNumber);

        // Show success message
        if (response.newResultsCount > 0) {
          console.log(`✅ Generated ${response.newResultsCount} new lab results from invoice`);
        } else {
          console.log('ℹ️ No new lab results generated - all tests already exist');
        }
      },
      error: (err) => {
        console.error('❌ Error generating lab results from invoice:', err);

        // Don't show error to user if it's just "no invoice found" - this is normal
        if (err.status === 404) {
          console.log('ℹ️ No invoice found for admission - this is normal for some patients');
        } else {
          console.error('Failed to generate lab results from invoice:', err.error?.message || err.message);
        }
      }
    });
  }
}

