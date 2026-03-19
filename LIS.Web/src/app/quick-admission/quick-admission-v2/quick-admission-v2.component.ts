import { Component, OnInit, signal, computed, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { QuickAdmissionV2Service } from './quick-admission-v2.service';
import { BillingService } from '../../services/billing.service';
import { DepartmentService } from '../../services/department.service';
import { BillingInvoiceDetail } from '../../models/billing-invoice-detail';
import { HospitalDenomination } from '../../models/hospital-denomination';
import { Department } from '../../models/department';
import { DenominationSearchResult } from '../../models/denomination-search-result';

interface NameOption {
  id: number;
  name: string;
  arabicName: string;
  gender?: string;
}

interface EditingDetailState {
  denominationSearchQuery: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  lumpSum: number;
  showDropdown: boolean;
  searchResults: DenominationSearchResult[];
  selectedDropdownIndex: number;
  operatingPhysicianName: string;
  operatingPhysicianId: number | null;
  hasOperatingPhysician: boolean;
  showOperatingPhysicianDropdown: boolean;
  operatingPhysicianOptions: any[];
  selectedOperatingPhysicianIndex: number;
}

@Component({
  selector: 'app-quick-admission-v2',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './quick-admission-v2.component.html',
  styleUrl: './quick-admission-v2.component.scss'
})
export class QuickAdmissionV2Component implements OnInit {
  private apiUrl = 'http://localhost:5050/api';
  private v2Service = inject(QuickAdmissionV2Service);
  private billingService = inject(BillingService);
  private departmentService = inject(DepartmentService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);

  // Core signals
  admissionId = signal<number | null>(null);
  isEditMode = signal(false);

  // Patient fields
  mrn = signal<string>('');
  firstName = signal<string>('');
  lastName = signal<string>('');
  middleName = signal<string>('');
  arabicFullName = computed(() => {
    const parts = [
      this.firstNameArabic(),
      this.middleNameArabic(),
      this.lastNameArabic()
    ].filter(p => p);
    return parts.join(' ');
  });
  gender = signal<string>('M');
  dob = signal<string>('');
  age = signal<number | null>(null);
  phone = signal<string>('');
  maritalStatus = signal<number | null>(null);

  // Arabic name parts
  firstNameArabic = signal<string>('');
  middleNameArabic = signal<string>('');
  lastNameArabic = signal<string>('');

  // Autocomplete options
  firstNameOptions = signal<NameOption[]>([]);
  middleNameOptions = signal<NameOption[]>([]);
  lastNameOptions = signal<NameOption[]>([]);

  // Autocomplete visibility
  showFirstNameDropdown = signal(false);
  showMiddleNameDropdown = signal(false);
  showLastNameDropdown = signal(false);

  // Selected index for keyboard navigation
  selectedFirstNameIndex = signal(-1);
  selectedLastNameIndex = signal(-1);
  selectedMiddleNameIndex = signal(-1);
  selectedPhysicianIndex = signal(-1);

  // Loading states
  isLoadingMRN = signal(false);
  isSaving = signal(false);

  // Save options modal state
  showSaveOptionsModal = signal(false);
  saveMedicalFile = signal(true);
  saveAdmission = signal(true);
  saveInvoice = signal(false);

  // Duplicate check
  showDuplicateModal = signal(false);
  duplicatePatients = signal<any[]>([]);
  isExistingPatient = signal(false);
  existingPatientId = signal<number | null>(null);

  // Validation
  dobValidationError = signal<string | null>(null);

  // Invoice related signals
  invoiceDetails = signal<any[]>([]);
  denominations = signal<HospitalDenomination[]>([]);
  filteredDenominations = signal<HospitalDenomination[]>([]);
  denominationSearchQuery = signal<string>('');
  showDenominationDropdown = false;
  newInvoiceDetail = signal<Partial<BillingInvoiceDetail>>({
    denomination: 0,
    denominationCode: '',
    denominationDescription: '',
    quantity: 1,
    unitPrice: 0,
    netPrice: 0,
    netUnitPrice: 0,
    denominationCoeffCode: '',
    denominationCoeffValue: 1,
    denominationCoeffPrice: 0,
    discount: 0,
    lumpSum: 0
  });
  editingInvoiceDetail = signal<BillingInvoiceDetail | null>(null);
  showInvoiceDetailForm = signal(false);

  // Inline editing for invoice details
  inlineEditingDetails = signal<Map<number, EditingDetailState>>(new Map());

  // Admission fields
  admissionNumber = signal<string>('');
  admissionSite = signal<number>(3);
  referralPhysician = signal<string>('');
  referralPhysicianId = signal<number | null>(null);
  attendingPhysician = signal<string>('');
  attendingPhysicianId = signal<number | null>(null);
  mainInsurance = signal<number>(5);
  mainInsuranceClass = signal<number>(4);
  insured = signal<number>(0);
  auxiliaryInsurance = signal<number>(5);
  auxiliaryInsuranceClass = signal<number>(4);
  checkInClass = signal<number>(5);
  department = signal<string>('');
  checkInDate = signal<string>(new Date().toISOString().split('T')[0]);
  checkOutDate = signal<string | null>(null);
  patientId = signal<number | null>(null);
  type = signal<number>(3);
  isWorkAccident = signal<number>(0);
  isExtended = signal<number>(0);
  group = signal<number>(22);

  // Physician autocomplete
  physicianOptions = signal<any[]>([]);
  showPhysicianDropdown = signal(false);

  // Department autocomplete (replaces simple select)
  departmentSearchQuery = signal<string>('');
  filteredDepartments = signal<any[]>([]);
  showDepartmentDropdown = signal(false);
  selectedDepartmentIndex = signal(-1);

  // Departments (raw data from service)
  departments = signal<Department[]>([]);

  // Computed property for departments with display text
  departmentsWithDisplay = computed(() => {
    return this.departments().map(dept => ({
      ...dept,
      displayText: this.getDepartmentDisplayText(dept),
      value: this.getDepartmentValue(dept)
    }));
  });

  // Invoice header signals
  invoiceNet = signal<number>(0);
  invoiceGross = signal<number>(0);
  invoiceDiscount = signal<number>(0);
  invoiceTotal = signal<number>(0);
  invoiceCurrency = signal<string>('USD');
  receiptAmount = signal<number>(0);
  receiptLocal = signal<number>(0);

  // Computed properties for invoice totals
  invoiceTotals = computed(() => {
    const details = this.invoiceDetails();
    const subtotal = details.reduce((sum, detail) => sum + (detail.quantity * detail.unitPrice), 0);
    const totalDiscount = details.reduce((sum, detail) => sum + (detail.discount || 0), 0);
    const totalLumpSum = details.reduce((sum, detail) => sum + (detail.lumpSum || 0), 0);
    const gross = subtotal;
    const net = subtotal - totalDiscount;
    const total = net + totalLumpSum;

    return {
      subtotal,
      totalDiscount,
      totalLumpSum,
      totalTax: 0,
      totalAmount: total,
      itemCount: details.length,
      gross,
      net
    };
  });

  // Validation computed properties for save options
  canSaveMedicalFile = computed(() => {
    return this.firstName() && this.lastName() && this.gender() && this.dob();
  });

  canSaveAdmission = computed(() => {
    const hasPhysician = this.referralPhysicianId() || this.attendingPhysicianId();
    const hasDept = this.department();
    const hasCheckInDate = this.checkInDate();
    return hasPhysician && hasDept && hasCheckInDate;
  });

  canSaveInvoice = computed(() => {
    return this.invoiceDetails().length > 0;
  });

  hasValidSelection = computed(() => {
    return (this.saveMedicalFile() && this.canSaveMedicalFile()) ||
           (this.saveAdmission() && this.canSaveAdmission()) ||
           (this.saveInvoice() && this.canSaveInvoice());
  });

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.admissionId.set(+params['id']);
        this.isEditMode.set(true);
        this.loadAdmissionData(+params['id']);
      } else {
        this.generateMRN();
      }
    });

    this.loadDepartments();
    this.testPhysicianAPI();
  }

  /**
   * Load existing admission data for edit mode
   */
  loadAdmissionData(admissionId: number): void {
    this.v2Service.loadAdmission(admissionId).subscribe({
      next: (data: any) => {
        console.log('Loaded admission data:', data);
        // Populate form with data
        this.mrn.set(data.patientMrn || '');
        this.firstName.set(data.patientFirstName || '');
        this.lastName.set(data.patientLastName || '');
        this.middleName.set(data.patientMiddleName || '');
        this.gender.set(data.patientGender || 'M');
        this.dob.set(data.patientDob ? data.patientDob.split('T')[0] : '');
        this.phone.set(data.patientPhone || '');
        this.patientId.set(data.patientId);

        this.admissionNumber.set(data.admissionNumber || '');
        this.department.set(data.department || '');
        this.checkInDate.set(data.checkInDate ? data.checkInDate.split('T')[0] : '');
        this.referralPhysician.set(data.referralPhysicianName || '');
        this.referralPhysicianId.set(data.referralPhysicianId);
        this.attendingPhysician.set(data.attendingPhysicianName || '');
        this.attendingPhysicianId.set(data.attendingPhysicianId);

        // Load invoice details if any
        if (data.invoiceDetails && data.invoiceDetails.length > 0) {
          this.invoiceDetails.set(data.invoiceDetails);
          this.initializeInlineEditingDetails();
        }
      },
      error: (error) => {
        console.error('Error loading admission data:', error);
        alert('Error loading admission data');
      }
    });
  }

  /**
   * Initialize inline editing details map
   */
  initializeInlineEditingDetails(): void {
    const map = new Map<number, EditingDetailState>();
    this.invoiceDetails().forEach((detail, index) => {
      map.set(index, {
        denominationSearchQuery: detail.denominationDescription || '',
        quantity: detail.quantity || 1,
        unitPrice: detail.unitPrice || 0,
        discount: detail.discount || 0,
        lumpSum: detail.lumpSum || 0,
        showDropdown: false,
        searchResults: [],
        selectedDropdownIndex: -1,
        operatingPhysicianName: '',
        operatingPhysicianId: null,
        hasOperatingPhysician: false,
        showOperatingPhysicianDropdown: false,
        operatingPhysicianOptions: [],
        selectedOperatingPhysicianIndex: -1
      });
    });
    this.inlineEditingDetails.set(map);
  }

  testPhysicianAPI(): void {
    console.log('Testing Physician API...');
    this.http.get<any[]>(`${this.apiUrl}/Physician/search?query=test`).subscribe({
      next: (response) => {
        console.log('Physician API test successful:', response);
      },
      error: (error) => {
        console.error('Physician API test failed:', error);
      }
    });
  }

  generateMRN(): void {
    this.isLoadingMRN.set(true);
    this.http.get<string>(`${this.apiUrl}/Patient/next-mrn`).subscribe({
      next: (mrn) => {
        this.mrn.set(mrn);
        this.isLoadingMRN.set(false);
      },
      error: (error) => {
        console.error('Error generating MRN:', error);
        alert('Error generating MRN');
        this.isLoadingMRN.set(false);
      }
    });
  }

  resetForm(): void {
    this.mrn.set('');
    this.firstName.set('');
    this.lastName.set('');
    this.middleName.set('');
    this.gender.set('M');
    this.dob.set('');
    this.age.set(null);
    this.phone.set('');
    this.maritalStatus.set(null);
    this.firstNameArabic.set('');
    this.middleNameArabic.set('');
    this.lastNameArabic.set('');

    this.admissionNumber.set('');
    this.referralPhysician.set('');
    this.referralPhysicianId.set(null);
    this.attendingPhysician.set('');
    this.attendingPhysicianId.set(null);
    this.checkInDate.set(this.getTodayDateString());
    this.department.set('');
    this.departmentSearchQuery.set('');
    this.filteredDepartments.set([]);
    this.showDepartmentDropdown.set(false);
    this.selectedDepartmentIndex.set(-1);

    this.isExistingPatient.set(false);
    this.existingPatientId.set(null);
    this.patientId.set(null);
    this.showDuplicateModal.set(false);
    this.duplicatePatients.set([]);
    this.dobValidationError.set(null);

    this.showFirstNameDropdown.set(false);
    this.showMiddleNameDropdown.set(false);
    this.showLastNameDropdown.set(false);
    this.showPhysicianDropdown.set(false);

    console.log('Form reset - admission number cleared');
  }

  // ===== NAME SEARCH METHODS =====

  searchFirstName(query: string): void {
    if (query.length < 2) {
      this.firstNameOptions.set([]);
      this.showFirstNameDropdown.set(false);
      return;
    }

    this.http.get<NameOption[]>(`${this.apiUrl}/Name/search?query=${encodeURIComponent(query)}`).subscribe({
      next: (options) => {
        this.firstNameOptions.set(options);
        this.showFirstNameDropdown.set(true);
      },
      error: (error) => console.error('Error searching first names:', error)
    });
  }

  selectFirstName(option: NameOption, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.firstName.set(option.name);
    this.firstNameArabic.set(option.arabicName);
    if (option.gender) {
      this.gender.set(option.gender);
    }
    this.showFirstNameDropdown.set(false);

    if (this.lastName()) {
      setTimeout(() => this.checkForDuplicates(), 300);
    }
  }

  hideFirstNameDropdown(): void {
    setTimeout(() => {
      this.showFirstNameDropdown.set(false);
      this.selectedFirstNameIndex.set(-1);
    }, 200);
  }

  onFirstNameKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (this.showFirstNameDropdown() && this.firstNameOptions().length > 0) {
        const newIndex = Math.min(this.selectedFirstNameIndex() + 1, this.firstNameOptions().length - 1);
        this.selectedFirstNameIndex.set(newIndex);
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.showFirstNameDropdown() && this.firstNameOptions().length > 0) {
        const newIndex = Math.max(this.selectedFirstNameIndex() - 1, 0);
        this.selectedFirstNameIndex.set(newIndex);
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.showFirstNameDropdown() && this.firstNameOptions().length > 0) {
        const idx = this.selectedFirstNameIndex() >= 0 ? this.selectedFirstNameIndex() : 0;
        this.selectFirstName(this.firstNameOptions()[idx]);
      }
    } else if (event.key === 'Escape') {
      this.showFirstNameDropdown.set(false);
    }
  }

  searchLastName(query: string): void {
    if (query.length < 2) {
      this.lastNameOptions.set([]);
      this.showLastNameDropdown.set(false);
      return;
    }

    this.http.get<NameOption[]>(`${this.apiUrl}/Family/search?query=${encodeURIComponent(query)}`).subscribe({
      next: (options) => {
        this.lastNameOptions.set(options);
        this.showLastNameDropdown.set(true);
      },
      error: (error) => console.error('Error searching last names:', error)
    });
  }

  selectLastName(option: NameOption, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.lastName.set(option.name);
    this.lastNameArabic.set(option.arabicName);
    this.showLastNameDropdown.set(false);

    if (this.firstName()) {
      setTimeout(() => this.checkForDuplicates(), 300);
    }
  }

  hideLastNameDropdown(): void {
    setTimeout(() => {
      this.showLastNameDropdown.set(false);
      this.selectedLastNameIndex.set(-1);
    }, 200);
  }

  onLastNameKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (this.showLastNameDropdown() && this.lastNameOptions().length > 0) {
        const newIndex = Math.min(this.selectedLastNameIndex() + 1, this.lastNameOptions().length - 1);
        this.selectedLastNameIndex.set(newIndex);
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.showLastNameDropdown() && this.lastNameOptions().length > 0) {
        const newIndex = Math.max(this.selectedLastNameIndex() - 1, 0);
        this.selectedLastNameIndex.set(newIndex);
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.showLastNameDropdown() && this.lastNameOptions().length > 0) {
        const idx = this.selectedLastNameIndex() >= 0 ? this.selectedLastNameIndex() : 0;
        this.selectLastName(this.lastNameOptions()[idx]);
      }
    } else if (event.key === 'Escape') {
      this.showLastNameDropdown.set(false);
    }
  }

  searchMiddleName(query: string): void {
    if (query.length < 2) {
      this.middleNameOptions.set([]);
      this.showMiddleNameDropdown.set(false);
      return;
    }

    this.http.get<NameOption[]>(`${this.apiUrl}/Name/search?query=${encodeURIComponent(query)}`).subscribe({
      next: (options) => {
        this.middleNameOptions.set(options);
        this.showMiddleNameDropdown.set(true);
      },
      error: (error) => console.error('Error searching middle names:', error)
    });
  }

  selectMiddleName(option: NameOption, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.middleName.set(option.name);
    this.middleNameArabic.set(option.arabicName);
    this.showMiddleNameDropdown.set(false);
  }

  hideMiddleNameDropdown(): void {
    setTimeout(() => {
      this.showMiddleNameDropdown.set(false);
      this.selectedMiddleNameIndex.set(-1);
    }, 200);
  }

  onMiddleNameKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (this.showMiddleNameDropdown() && this.middleNameOptions().length > 0) {
        const newIndex = Math.min(this.selectedMiddleNameIndex() + 1, this.middleNameOptions().length - 1);
        this.selectedMiddleNameIndex.set(newIndex);
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.showMiddleNameDropdown() && this.middleNameOptions().length > 0) {
        const newIndex = Math.max(this.selectedMiddleNameIndex() - 1, 0);
        this.selectedMiddleNameIndex.set(newIndex);
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.showMiddleNameDropdown() && this.middleNameOptions().length > 0) {
        const idx = this.selectedMiddleNameIndex() >= 0 ? this.selectedMiddleNameIndex() : 0;
        this.selectMiddleName(this.middleNameOptions()[idx]);
      }
    } else if (event.key === 'Escape') {
      this.showMiddleNameDropdown.set(false);
    }
  }

  // ===== PHYSICIAN SEARCH METHODS =====

  searchPhysician(query: string): void {
    if (query.length < 2) {
      this.physicianOptions.set([]);
      this.showPhysicianDropdown.set(false);
      return;
    }

    const url = `${this.apiUrl}/Physician/search?query=${encodeURIComponent(query)}`;
    this.http.get<any[]>(url).subscribe({
      next: (options) => {
        this.physicianOptions.set(options || []);
        this.showPhysicianDropdown.set(true);
      },
      error: (error) => {
        console.error('Error searching physicians:', error);
        this.physicianOptions.set([]);
      }
    });
  }

  selectPhysician(physician: any, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.referralPhysician.set(physician.name);
    this.referralPhysicianId.set(physician.id);
    this.attendingPhysician.set(physician.name);
    this.attendingPhysicianId.set(physician.id);
    this.showPhysicianDropdown.set(false);
  }

  hidePhysicianDropdown(): void {
    setTimeout(() => {
      this.showPhysicianDropdown.set(false);
      this.selectedPhysicianIndex.set(-1);
    }, 200);
  }

  onReferralPhysicianKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (this.showPhysicianDropdown() && this.physicianOptions().length > 0) {
        const newIndex = Math.min(this.selectedPhysicianIndex() + 1, this.physicianOptions().length - 1);
        this.selectedPhysicianIndex.set(newIndex);
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.showPhysicianDropdown() && this.physicianOptions().length > 0) {
        const newIndex = Math.max(this.selectedPhysicianIndex() - 1, 0);
        this.selectedPhysicianIndex.set(newIndex);
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.showPhysicianDropdown() && this.physicianOptions().length > 0) {
        const idx = this.selectedPhysicianIndex() >= 0 ? this.selectedPhysicianIndex() : 0;
        this.selectPhysician(this.physicianOptions()[idx]);
      }
    } else if (event.key === 'Escape') {
      this.showPhysicianDropdown.set(false);
    }
  }

  onReferralPhysicianInput(value: string): void {
    this.referralPhysician.set(value);
    this.searchPhysician(value);
  }

  onReferralPhysicianFocus(): void {
    if (!this.showPhysicianDropdown() && this.referralPhysician().length >= 2 && this.physicianOptions().length > 0) {
      this.showPhysicianDropdown.set(true);
    }
  }

  trackByPhysicianId(index: number, physician: any): any {
    return physician.id;
  }

  // ===== DEPARTMENT AUTOCOMPLETE METHODS =====

  searchDepartment(query: string): void {
    if (!query || query.trim().length === 0) {
      this.filteredDepartments.set([]);
      this.showDepartmentDropdown.set(false);
      this.selectedDepartmentIndex.set(-1);
      return;
    }
    const lower = query.toLowerCase();
    const results = this.departmentsWithDisplay()
      .filter(d => d.displayText.toLowerCase().includes(lower));
    this.filteredDepartments.set(results);
    this.showDepartmentDropdown.set(results.length > 0);
    this.selectedDepartmentIndex.set(results.length > 0 ? 0 : -1);
  }

  selectDepartment(dept: any, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.department.set(dept.value);
    this.showDepartmentDropdown.set(false);
    this.selectedDepartmentIndex.set(-1);
  }

  hideDepartmentDropdown(): void {
    setTimeout(() => {
      this.showDepartmentDropdown.set(false);
      this.selectedDepartmentIndex.set(-1);
    }, 200);
  }

  onDepartmentKeyDown(event: KeyboardEvent): void {
    if (!this.showDepartmentDropdown() || this.filteredDepartments().length === 0) return;
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      const index = this.selectedDepartmentIndex();
      const max = this.filteredDepartments().length - 1;
      this.selectedDepartmentIndex.set(index < max ? index + 1 : index);
      this.scrollToSelectedItem('department');
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      const index = this.selectedDepartmentIndex();
      this.selectedDepartmentIndex.set(index > 0 ? index - 1 : 0);
      this.scrollToSelectedItem('department');
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const idx = this.selectedDepartmentIndex() >= 0 ? this.selectedDepartmentIndex() : 0;
      const choice = this.filteredDepartments()[idx];
      if (choice) {
        this.selectDepartment(choice);
      }
    } else if (event.key === 'Escape') {
      this.showDepartmentDropdown.set(false);
      this.selectedDepartmentIndex.set(-1);
    }
  }

  onDepartmentIconClick(): void {
    if (this.showDepartmentDropdown() && this.filteredDepartments().length > 0) {
      this.showDepartmentDropdown.set(false);
      this.selectedDepartmentIndex.set(-1);
    } else {
      const query = this.departmentSearchQuery() || '';
      this.searchDepartment(query);
    }
  }

  onDepartmentInput(value: string): void {
    this.departmentSearchQuery.set(value);
    this.searchDepartment(value);
  }

  onDepartmentFocus(): void {
    if (!this.showDepartmentDropdown() && this.departmentSearchQuery().length >= 1) {
      this.showDepartmentDropdown.set(true);
      // dropdown is inline; no reposition needed
    }
  }

  /**
   * Scroll the active item in whichever dropdown-type is being used into view.
   * Used for keyboard navigation of autocomplete lists.
   */
  scrollToSelectedItem(dropdownType: 'firstName' | 'lastName' | 'middleName' | 'physician' | 'department'): void {
    setTimeout(() => {
      let selector = '.autocomplete-dropdown';
      // all our dropdowns use same class; just rely on active item
      const dropdowns = document.querySelectorAll(selector);
      if (dropdowns.length > 0) {
        const activeItem = Array.from(dropdowns).find(dropdown => {
          return dropdown.querySelector('.autocomplete-item.active') !== null;
        })?.querySelector('.autocomplete-item.active') as HTMLElement;
        if (activeItem) {
          activeItem.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
      }
    }, 0);
  }

  /**
   * For repositioning floating dropdowns (currently only referral physician).
   */
  getDropdownStyle(): any {
    const input = document.getElementById('referralPhysicianInput') as HTMLInputElement;
    if (!input) return {};
    const rect = input.getBoundingClientRect();
    return {
      position: 'fixed',
      top: `${rect.bottom + window.scrollY + 2}px`,
      left: `${rect.left + window.scrollX}px`,
      width: `${rect.width}px`,
      zIndex: 99999
    };
  }

  // ===== DUPLICATE CHECK METHODS =====

  checkForDuplicates(): void {
    const fName = this.firstName()?.trim();
    const lName = this.lastName()?.trim();

    if (!fName || !lName || fName.length < 2 || lName.length < 2) {
      return;
    }

    const params = new URLSearchParams();
    params.append('firstName', fName);
    params.append('lastName', lName);
    const mName = this.middleName()?.trim();
    if (mName) {
      params.append('middleName', mName);
    }

    const url = `${this.apiUrl}/Patient/check-duplicate?${params.toString()}`;
    this.http.get<any>(url).subscribe({
      next: (response) => {
        if (response.found && response.patients.length > 0) {
          this.duplicatePatients.set(response.patients);
          this.showDuplicateModal.set(true);
        }
      },
      error: (error) => {
        console.error('Error checking duplicates:', error);
      }
    });
  }

  loadExistingPatient(patient: any): void {
    this.mrn.set(patient.mrn);
    this.firstName.set(patient.firstName);
    this.lastName.set(patient.lastName);
    this.middleName.set(patient.middleName || '');
    this.gender.set(patient.gender || 'M');
    this.dob.set(patient.dob ? patient.dob.split('T')[0] : '');
    this.phone.set(patient.phone || '');

    if (patient.dob) {
      const dobString = patient.dob.split('T')[0];
      this.age.set(this.calculateAgeFromDob(dobString));
    }

    this.isExistingPatient.set(true);
    this.existingPatientId.set(patient.id);
    this.patientId.set(patient.id);

    this.showDuplicateModal.set(false);
    alert(`Loaded existing patient: ${patient.mrn} - ${patient.firstName} ${patient.lastName}`);
  }

  continueAsNew(): void {
    this.showDuplicateModal.set(false);
    this.isExistingPatient.set(false);
    this.existingPatientId.set(null);
    this.patientId.set(null);
  }

  cancelDuplicateCheck(): void {
    this.showDuplicateModal.set(false);
  }

  // ===== DOB/AGE METHODS =====

  onDobChange(dobValue: string): void {
    this.dob.set(dobValue);
    this.dobValidationError.set(null);

    if (dobValue) {
      if (!this.isValidDob(dobValue)) {
        this.dobValidationError.set('Date of birth cannot be in the future');
        this.age.set(null);
        return;
      }
      this.age.set(this.calculateAgeFromDob(dobValue));
    } else {
      this.age.set(null);
    }
  }

  onAgeChange(ageValue: string): void {
    const age = ageValue ? parseInt(ageValue, 10) : null;
    this.age.set(age);
    this.dobValidationError.set(null);

    if (age !== null && age >= 0 && age <= 150) {
      const calculatedDob = this.calculateDobFromAge(age);
      if (this.isValidDob(calculatedDob)) {
        this.dob.set(calculatedDob);
      } else {
        this.dobValidationError.set('Calculated date of birth is in the future');
        this.dob.set('');
      }
    } else if (age !== null) {
      this.dobValidationError.set('Age must be between 0 and 150 years');
    }
  }

  calculateAgeFromDob(dobString: string): number {
    const dob = new Date(dobString);
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const monthDiff = today.getMonth() - dob.getMonth();

    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
      age--;
    }

    return age;
  }

  calculateDobFromAge(age: number): string {
    const today = new Date();
    const birthYear = today.getFullYear() - age;
    const dob = new Date(birthYear, today.getMonth(), today.getDate());
    return dob.toISOString().split('T')[0];
  }

  isValidDob(dobString: string): boolean {
    const dob = new Date(dobString);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    dob.setHours(0, 0, 0, 0);
    return dob <= today;
  }

  // ===== DEPARTMENT METHODS =====

  loadDepartments(): void {
    this.departmentService.getAll().subscribe({
      next: (departments) => {
        // sort alphabetically by name/code/description to make autocomplete predictable
        departments.sort((a, b) => {
          const aText = (a.name || a.code || a.description || '').toString().toLowerCase();
          const bText = (b.name || b.code || b.description || '').toString().toLowerCase();
          return aText.localeCompare(bText);
        });
        this.departments.set(departments);

        // initialize filtered list so icon click shows them all
        this.filteredDepartments.set(this.departmentsWithDisplay());
      },
      error: (error) => {
        console.error('Error loading departments:', error);
      }
    });
  }

  getDepartmentValue(dept: Department): string {
    if (dept.name && dept.name.toString().trim().length > 0) {
      return dept.name.toString().trim();
    }
    if (dept.code && dept.code.toString().trim().length > 0) {
      return dept.code.toString().trim();
    }
    return dept.id.toString();
  }

  getDepartmentDisplayText(dept: Department): string {
    const name = dept?.name?.toString()?.trim() || '';
    const code = dept?.code?.toString()?.trim() || '';
    const description = dept?.description?.toString()?.trim() || '';

    if (name) return name;
    if (code) return code;
    if (description) return description;
    return `Department ${dept?.id || ''}`;
  }

  trackByDepartmentId(index: number, dept: any): any {
    return dept?.id || index;
  }

  // ===== INVOICE METHODS =====

  getCostCenterFilter(): string | undefined {
    const deptName = this.department();
    if (!deptName) return undefined;

    // Map department names to cost center filters
    const costCenterMap: { [key: string]: string } = {
      'Clinic': 'CC1,CC2',
      'Laboratory': 'CC3',
      'Imaging': 'CC4'
    };

    return costCenterMap[deptName];
  }

  hasDenominationRequiringOperatingPhysician(): boolean {
    return this.invoiceDetails().some(detail => {
      return detail.denominationCode && detail.denominationCode.includes('SUR');
    });
  }

  searchDenominationsForDetail(query: string, rowIndex: number): void {
    const map = new Map(this.inlineEditingDetails());
    const detail = map.get(rowIndex) || this.getEditingDetailForRow(rowIndex);

    const costCenterFilter = this.getCostCenterFilter();

    this.billingService.searchDenominationsAdvanced(
      query && query.trim().length > 0 ? query.trim() : undefined,
      5,
      costCenterFilter || undefined
    ).subscribe({
      next: (results) => {
        detail.searchResults = results;
        detail.showDropdown = results.length > 0;
        map.set(rowIndex, detail);
        this.inlineEditingDetails.set(map);
      },
      error: (error) => {
        console.error('Error searching denominations:', error);
        detail.showDropdown = false;
        map.set(rowIndex, detail);
        this.inlineEditingDetails.set(map);
      }
    });
  }

  selectDenominationForDetail(result: DenominationSearchResult, rowIndex: number, updateUnitPrice = false): void {
    const details = this.invoiceDetails();
    if (rowIndex >= 0 && rowIndex < details.length) {
      const detail = { ...details[rowIndex] };
      detail.denominationCode = result.actName || '';
      detail.denominationDescription = result.actName || '';
      
      if (updateUnitPrice) {
        detail.unitPrice = result.priceUsd ?? detail.unitPrice;
      }

      details[rowIndex] = detail;
      this.invoiceDetails.set([...details]);

      const map = new Map(this.inlineEditingDetails());
      const editingDetail = map.get(rowIndex) || this.getEditingDetailForRow(rowIndex);
      editingDetail.denominationSearchQuery = result.actName || '';
      editingDetail.showDropdown = false;
      map.set(rowIndex, editingDetail);
      this.inlineEditingDetails.set(map);
    }
  }

  searchOperatingPhysician(query: string, rowIndex: number): void {
    if (query.length < 2) return;

    const url = `${this.apiUrl}/Physician/search?query=${encodeURIComponent(query)}`;
    this.http.get<any[]>(url).subscribe({
      next: (physicians) => {
        const map = new Map(this.inlineEditingDetails());
        const detail = map.get(rowIndex) || this.getEditingDetailForRow(rowIndex);
        detail.operatingPhysicianOptions = physicians;
        detail.showOperatingPhysicianDropdown = true;
        map.set(rowIndex, detail);
        this.inlineEditingDetails.set(map);
      },
      error: (error) => {
        console.error('Error searching physicians:', error);
      }
    });
  }

  selectOperatingPhysician(physician: any, rowIndex: number): void {
    const map = new Map(this.inlineEditingDetails());
    const detail = map.get(rowIndex) || this.getEditingDetailForRow(rowIndex);
    detail.operatingPhysicianName = physician.name;
    detail.operatingPhysicianId = physician.id;
    detail.showOperatingPhysicianDropdown = false;
    map.set(rowIndex, detail);
    this.inlineEditingDetails.set(map);
  }

  onOperatingPhysicianKeyDown(event: KeyboardEvent, rowIndex: number): void {
    if (event.key === 'Escape') {
      const map = new Map(this.inlineEditingDetails());
      const detail = map.get(rowIndex) || this.getEditingDetailForRow(rowIndex);
      detail.showOperatingPhysicianDropdown = false;
      map.set(rowIndex, detail);
      this.inlineEditingDetails.set(map);
    }
  }

  onOperatingPhysicianFocus(rowIndex: number): void {
    // Can add logic to show options on focus
  }

  onDenominationInputFocusForDetail(rowIndex: number): void {
    const currentDetail = this.getEditingDetailForRow(rowIndex);
    if (currentDetail.denominationSearchQuery && currentDetail.denominationSearchQuery.length > 0) {
      this.searchDenominationsForDetail(currentDetail.denominationSearchQuery, rowIndex);
    }
  }

  onDenominationKeyDownForDetail(event: KeyboardEvent, rowIndex: number): void {
    if (event.key === 'Escape') {
      const map = new Map(this.inlineEditingDetails());
      const detail = map.get(rowIndex) || this.getEditingDetailForRow(rowIndex);
      detail.showDropdown = false;
      map.set(rowIndex, detail);
      this.inlineEditingDetails.set(map);
    }
  }

  getEditingDetailForRow(rowIndex: number): EditingDetailState {
    const map = this.inlineEditingDetails();
    return map.get(rowIndex) || {
      denominationSearchQuery: '',
      quantity: 1,
      unitPrice: 0,
      discount: 0,
      lumpSum: 0,
      showDropdown: false,
      searchResults: [],
      selectedDropdownIndex: -1,
      operatingPhysicianName: '',
      operatingPhysicianId: null,
      hasOperatingPhysician: false,
      showOperatingPhysicianDropdown: false,
      operatingPhysicianOptions: [],
      selectedOperatingPhysicianIndex: -1
    };
  }

  updateEditingDetailFieldForRow(field: string, value: any, rowIndex: number): void {
    const map = new Map(this.inlineEditingDetails());
    const detail = map.get(rowIndex) || this.getEditingDetailForRow(rowIndex);
    (detail as any)[field] = value;
    map.set(rowIndex, detail);
    this.inlineEditingDetails.set(map);

    // Update the actual invoice detail
    const details = this.invoiceDetails();
    if (rowIndex >= 0 && rowIndex < details.length) {
      const invoiceDetail = { ...details[rowIndex] };
      if (field === 'quantity') invoiceDetail.quantity = value;
      if (field === 'unitPrice') invoiceDetail.unitPrice = value;
      if (field === 'discount') invoiceDetail.discount = value;
      if (field === 'lumpSum') invoiceDetail.lumpSum = value;
      details[rowIndex] = invoiceDetail;
      this.invoiceDetails.set([...details]);
    }
  }

  updateDetailTotalForRow(rowIndex: number): void {
    const details = this.invoiceDetails();
    if (rowIndex >= 0 && rowIndex < details.length) {
      const detail = details[rowIndex];
      detail.netPrice = (detail.quantity * detail.unitPrice) - (detail.discount || 0) + (detail.lumpSum || 0);
      details[rowIndex] = detail;
      this.invoiceDetails.set([...details]);
    }
  }

  calculateDetailTotalForRow(rowIndex: number): number {
    const detail = this.invoiceDetails()[rowIndex];
    if (!detail) return 0;
    return (detail.quantity * detail.unitPrice) - (detail.discount || 0) + (detail.lumpSum || 0);
  }

  addInvoiceDetailInline(): void {
    if (!this.department()) {
      alert('Please select a Department before adding invoice items.');
      return;
    }

    const newDetail: any = {
      id: Date.now(),
      medicationUnit: 113,
      medicationUnitDescription: 'Clinics',
      admission: 0,
      patient: 0,
      denomination: 0,
      denominationCode: '',
      denominationDescription: '',
      denominationCoeffCode: '',
      denominationCoeffValue: 1,
      denominationCoeffPrice: 0,
      quantity: 1,
      unitPrice: 0,
      netPrice: 0,
      netUnitPrice: 0,
      discount: 0,
      lumpSum: 0
    };

    const details = this.invoiceDetails();
    const newIndex = details.length;
    details.push(newDetail);
    this.invoiceDetails.set([...details]);

    const map = new Map(this.inlineEditingDetails());
    map.set(newIndex, {
      denominationSearchQuery: '',
      quantity: 1,
      unitPrice: 0,
      discount: 0,
      lumpSum: 0,
      showDropdown: false,
      searchResults: [],
      selectedDropdownIndex: -1,
      operatingPhysicianName: '',
      operatingPhysicianId: null,
      hasOperatingPhysician: false,
      showOperatingPhysicianDropdown: false,
      operatingPhysicianOptions: [],
      selectedOperatingPhysicianIndex: -1
    });
    this.inlineEditingDetails.set(map);
  }

  deleteInvoiceDetail(index: number): void {
    const details = this.invoiceDetails();
    details.splice(index, 1);
    this.invoiceDetails.set([...details]);

    const map = new Map(this.inlineEditingDetails());
    map.delete(index);
    this.inlineEditingDetails.set(map);
  }

  onQuantityTab(event: any, rowIndex: number): void {
    // Auto-focus next field on Tab
    setTimeout(() => {
      const nextInput = document.querySelector(`[data-row-index="${rowIndex}"] input[type="number"]`) as HTMLInputElement;
      if (nextInput) {
        nextInput.focus();
      }
    }, 0);
  }

  // ===== SAVE METHODS =====

  savePatient(): void {
    if (!this.firstName() || !this.lastName() || !this.gender() || !this.dob()) {
      alert('Please fill in all required fields (First Name, Last Name, Gender, DOB)');
      return;
    }

    if (this.dobValidationError()) {
      alert('Please fix the Date of Birth validation error before saving');
      return;
    }

    // Show save options modal
    this.showSaveOptionsModal.set(true);
  }

  performSelectiveSave(): void {
    this.isSaving.set(true);

    const saveRequest: any = {
      saveMedicalFile: this.saveMedicalFile(),
      saveAdmission: this.saveAdmission(),
      saveInvoice: this.saveInvoice(),
      patient: this.buildPatientObject(),
      admission: this.saveAdmission() ? this.buildAdmissionObject() : null,
      invoiceDetails: this.saveInvoice() ? this.invoiceDetails() : []
    };

    console.log('Save request:', saveRequest);

    this.v2Service.saveComplete(saveRequest).subscribe({
      next: (response: any) => {
        this.isSaving.set(false);
        this.showSaveOptionsModal.set(false);
        alert('Data saved successfully!');
        this.router.navigate(['/resident-patients']);
      },
      error: (error) => {
        console.error('Error saving data:', error);
        this.isSaving.set(false);
        alert('Error saving data: ' + (error.error?.message || error.message));
      }
    });
  }

  cancelSaveOptions(): void {
    this.showSaveOptionsModal.set(false);
  }

  private buildPatientObject(): any {
    return {
      FirstName: this.firstName()?.trim() || null,
      LastName: this.lastName()?.trim() || null,
      MiddleName: this.middleName()?.trim() || null,
      Gender: this.gender() || null,
      Phone: this.phone()?.trim() || null,
      ArabicFullName: this.arabicFullName()?.trim() || null,
      DOB: this.dob() ? new Date(this.dob()).toISOString() : null,
      MaritalStatus: this.maritalStatus(),
      CreatedBy: 338
    };
  }

  private buildAdmissionObject(): any {
    return {
      Number: null,
      AdmissionSite: this.admissionSite(),
      ReferralPhysician: this.referralPhysicianId(),
      AttendingPhysician: this.attendingPhysicianId() || this.referralPhysicianId(),
      MainInsurance: this.mainInsurance(),
      MainInsuranceClass: this.mainInsuranceClass(),
      Insured: this.insured(),
      AuxiliaryInsurance: this.auxiliaryInsurance(),
      AuxiliaryInsuranceClass: this.auxiliaryInsuranceClass(),
      CheckInClass: this.checkInClass(),
      Department: this.department(),
      CheckInDate: this.checkInDate() ? new Date(this.checkInDate()).toISOString().split('T')[0] : null,
      CheckOutDate: this.checkOutDate(),
      Patient: this.patientId() || this.existingPatientId(),
      Type: this.type(),
      IsWorkAccident: this.isWorkAccident(),
      IsExtended: this.isExtended(),
      Group: this.group(),
      CreatedBy: 338
    };
  }

  // ===== UTILITY METHODS =====

  getTodayDateString(): string {
    return new Date().toISOString().split('T')[0];
  }

  formatCurrency(value: number): string {
    return `$${value.toFixed(2)}`;
  }

  truncateText(text: string | null | undefined, maxLength: number): string {
    if (!text) return '';
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
  }

  formatDate(date: Date | string | null | undefined): string {
    if (!date) return '-';
    const d = new Date(date);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  goBack(): void {
    this.router.navigate(['/resident-patients']);
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: Event): void {
    if (this.showDuplicateModal()) {
      event.preventDefault();
      event.stopPropagation();
    }
  }

  onFirstNameIconClick(): void {
    if (this.showFirstNameDropdown() && this.firstNameOptions().length > 0) {
      this.showFirstNameDropdown.set(false);
    } else {
      const currentValue = this.firstName();
      if (currentValue && currentValue.length >= 2) {
        this.searchFirstName(currentValue);
      } else {
        this.http.get<NameOption[]>(`${this.apiUrl}/Name/search?query=a`).subscribe({
          next: (options) => {
            this.firstNameOptions.set(options.slice(0, 50));
            this.showFirstNameDropdown.set(true);
          },
          error: (error) => console.error('Error:', error)
        });
      }
    }
  }

  onLastNameIconClick(): void {
    if (this.showLastNameDropdown() && this.lastNameOptions().length > 0) {
      this.showLastNameDropdown.set(false);
    } else {
      const currentValue = this.lastName();
      if (currentValue && currentValue.length >= 2) {
        this.searchLastName(currentValue);
      } else {
        this.http.get<NameOption[]>(`${this.apiUrl}/Family/search?query=a`).subscribe({
          next: (options) => {
            this.lastNameOptions.set(options.slice(0, 50));
            this.showLastNameDropdown.set(true);
          },
          error: (error) => console.error('Error:', error)
        });
      }
    }
  }

  onMiddleNameIconClick(): void {
    if (this.showMiddleNameDropdown() && this.middleNameOptions().length > 0) {
      this.showMiddleNameDropdown.set(false);
    } else {
      const currentValue = this.middleName();
      if (currentValue && currentValue.length >= 2) {
        this.searchMiddleName(currentValue);
      } else {
        this.http.get<NameOption[]>(`${this.apiUrl}/Name/search?query=a`).subscribe({
          next: (options) => {
            this.middleNameOptions.set(options.slice(0, 50));
            this.showMiddleNameDropdown.set(true);
          },
          error: (error) => console.error('Error:', error)
        });
      }
    }
  }

  onReferralPhysicianIconClick(): void {
    if (this.showPhysicianDropdown() && this.physicianOptions().length > 0) {
      this.showPhysicianDropdown.set(false);
    } else {
      const currentValue = this.referralPhysician();
      if (currentValue && currentValue.length >= 2) {
        this.searchPhysician(currentValue);
      } else {
        const url = `${this.apiUrl}/Physician/search?query=a`;
        this.http.get<any[]>(url).subscribe({
          next: (options) => {
            this.physicianOptions.set(options.slice(0, 50));
            this.showPhysicianDropdown.set(true);
          },
          error: (error) => console.error('Error:', error)
        });
      }
    }
  }

  onDenominationIconClickForDetail(rowIndex: number): void {
    const detail = this.getEditingDetailForRow(rowIndex);
    if (detail.showDropdown && detail.searchResults.length > 0) {
      detail.showDropdown = false;
      const map = new Map(this.inlineEditingDetails());
      map.set(rowIndex, detail);
      this.inlineEditingDetails.set(map);
    } else {
      this.searchDenominationsForDetail('', rowIndex);
    }
  }

  onOperatingPhysicianIconClick(rowIndex: number): void {
    const detail = this.getEditingDetailForRow(rowIndex);
    if (!detail.hasOperatingPhysician) return;

    if (detail.showOperatingPhysicianDropdown && detail.operatingPhysicianOptions.length > 0) {
      detail.showOperatingPhysicianDropdown = false;
      const map = new Map(this.inlineEditingDetails());
      map.set(rowIndex, detail);
      this.inlineEditingDetails.set(map);
    } else {
      const url = `${this.apiUrl}/Physician/search?query=a`;
      this.http.get<any[]>(url).subscribe({
        next: (physicians) => {
          const map = new Map(this.inlineEditingDetails());
          const editingDetail = map.get(rowIndex) || this.getEditingDetailForRow(rowIndex);
          editingDetail.operatingPhysicianOptions = physicians.slice(0, 50);
          editingDetail.showOperatingPhysicianDropdown = true;
          map.set(rowIndex, editingDetail);
          this.inlineEditingDetails.set(map);
        },
        error: (error) => console.error('Error:', error)
      });
    }
  }
}
