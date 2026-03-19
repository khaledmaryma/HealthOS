import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ResidentPatient } from '../models/resident-patient';
import { ResidentPatientService } from '../services/resident-patient.service';

@Component({
  selector: 'app-resident-patient-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './resident-patient-list.component.html',
  styleUrl: './resident-patient-list.component.scss'
})
export class ResidentPatientListComponent implements OnInit {
  patients = signal<ResidentPatient[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  selectedPatient = signal<ResidentPatient | null>(null);

  // Pagination
  currentPage = signal(1);
  pageSize = signal(50);
  totalCount = signal(0);

  // Filters
  searchTerm = signal('');
  dischargeFilter = signal<boolean | undefined>(false);
  currentDateOnly = signal(false);
  showDateRange = signal(true);
  checkInDateFrom = signal<string>('2026-01-01');
  checkInDateTo = signal<string>(this.formatDateForInput(new Date()));

  // Computed
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
  hasNextPage = computed(() => this.currentPage() < this.totalPages());
  hasPrevPage = computed(() => this.currentPage() > 1);

  constructor(
    private residentPatientService: ResidentPatientService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadPatients();
    this.loadCount();
  }

  loadPatients(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.residentPatientService.getAll(
      this.currentPage(),
      this.pageSize(),
      this.searchTerm() || undefined,
      this.dischargeFilter(),
      this.currentDateOnly(),
      this.checkInDateFrom() || undefined,
      this.checkInDateTo() || undefined
    ).subscribe({
      next: (data) => {
        this.patients.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading patients:', error);
        this.errorMessage.set('Failed to load patients. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  loadCount(): void {
    this.residentPatientService.getCount(
      this.searchTerm() || undefined,
      this.dischargeFilter(),
      this.currentDateOnly(),
      this.checkInDateFrom() || undefined,
      this.checkInDateTo() || undefined
    ).subscribe({
      next: (count) => {
        this.totalCount.set(count);
      },
      error: (error) => {
        console.error('Error loading patient count:', error);
      }
    });
  }

  onSearch(): void {
    this.currentPage.set(1);
    this.loadPatients();
    this.loadCount();
  }

  onDischargeFilterChange(value: string): void {
    if (value === 'all') {
      this.dischargeFilter.set(undefined);
      this.currentDateOnly.set(false);
      this.showDateRange.set(false);
    } else if (value === 'active') {
      this.dischargeFilter.set(false);
      this.currentDateOnly.set(false);
      this.showDateRange.set(false);
    } else if (value === 'current') {
      this.dischargeFilter.set(false);
      this.currentDateOnly.set(true);
      this.showDateRange.set(false);
    } else if (value === 'checkin') {
      this.dischargeFilter.set(false);
      this.currentDateOnly.set(false);
      this.showDateRange.set(true);

      // Set default dates: from = today - 7 days, to = today
      const today = new Date();
      const sevenDaysAgo = new Date();
      sevenDaysAgo.setDate(today.getDate() - 7);

      this.checkInDateFrom.set(this.formatDateForInput(sevenDaysAgo));
      this.checkInDateTo.set(this.formatDateForInput(today));
    } else if (value === 'discharged') {
      this.dischargeFilter.set(true);
      this.currentDateOnly.set(false);
      this.showDateRange.set(false);
    }
    this.currentPage.set(1);
    this.loadPatients();
    this.loadCount();
  }

  nextPage(): void {
    if (this.hasNextPage()) {
      this.currentPage.update(p => p + 1);
      this.loadPatients();
    }
  }

  prevPage(): void {
    if (this.hasPrevPage()) {
      this.currentPage.update(p => p - 1);
      this.loadPatients();
    }
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadPatients();
    }
  }

  getPageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: number[] = [];

    // Show max 7 page numbers
    const maxPages = 7;
    let startPage = Math.max(1, current - Math.floor(maxPages / 2));
    let endPage = Math.min(total, startPage + maxPages - 1);

    if (endPage - startPage < maxPages - 1) {
      startPage = Math.max(1, endPage - maxPages + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return pages;
  }

  calculateAge(dob: Date): number {
    const birthDate = new Date(dob);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();

    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }

    return age;
  }

  formatDate(date: Date | string | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  formatDateTime(date: Date | string | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // Select patient row
  selectPatient(patient: ResidentPatient): void {
    this.selectedPatient.set(patient);
  }

  // New Patient - V2 Screen with new UI and API integration
  newPatientV2(): void {
    this.router.navigate(['/quick-admission-v2']);
  }

  // New Admission V2 - For selected row, load patient info and open quick-admission-v2 page
  newAdmissionV2(): void {
    const patient = this.selectedPatient();
    if (patient && patient.patientID) {
      this.router.navigate(['/quick-admission-v2'], {
        queryParams: { patientId: patient.patientID }
      });
    } else {
      console.error('No patient selected or Patient ID not available for new admission');
      alert('Please select a patient row first');
    }
  }

  // Edit Admission V2 - Load quick-admission-v2 with patient, admission, and invoice data
  editAdmissionV2(): void {
    const patient = this.selectedPatient();
    if (patient && patient.admission) {
      this.router.navigate(['/quick-admission-v2', patient.admission]);
    } else {
      console.error('No patient selected or Admission ID not available for edit');
      alert('Please select a patient row with an admission first');
    }
  }

  // New Patient - Create patient, admission, and invoice from scratch
  newPatient(): void {
    this.router.navigate(['/quick-admission']);
  }

  // New Admission - For selected row, load patient info and open quick-admission page
  newAdmission(): void {
    const patient = this.selectedPatient();
    if (patient && patient.patientID) {
      this.router.navigate(['/quick-admission'], {
        queryParams: { patientId: patient.patientID }
      });
    } else {
      console.error('No patient selected or Patient ID not available for new admission');
      alert('Please select a patient row first');
    }
  }

  // Edit Admission - Load quick-admission with patient, admission, and invoice data
  editAdmission(): void {
    const patient = this.selectedPatient();
    if (patient && patient.admission) {
      this.router.navigate(['/quick-admission', patient.admission]);
    } else {
      console.error('No patient selected or Admission ID not available for edit');
      alert('Please select a patient row with an admission first');
    }
  }

  // Open Department Report
  openDepartmentReport(): void {
    this.router.navigate(['/department-report']);
  }

  openMedicalFile(patient: ResidentPatient): void {
    const queryParams: any = {};
    if (patient.patientName) {
      queryParams.patientName = patient.patientName;
    }
    if (patient.admissionNumber) {
      queryParams.admissionNumber = patient.admissionNumber;
    }

    // Use admission ID if available, otherwise use patient ID
    if (patient.admission) {
      this.router.navigate(['/medical-file/admission', patient.admission], { queryParams });
    } else if (patient.patientID) {
      this.router.navigate(['/medical-file/patient', patient.patientID], { queryParams });
    }
  }
}

