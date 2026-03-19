import { Component, OnInit, signal, computed, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { BillingService } from '../services/billing.service';
import { DepartmentService } from '../services/department.service';
import { BillingInvoiceHeader } from '../models/billing-invoice-header';
import { BillingInvoiceDetail } from '../models/billing-invoice-detail';
import { HospitalDenomination } from '../models/hospital-denomination';
import { Department } from '../models/department';
import { DenominationSearchResult } from '../models/denomination-search-result';
import { Insurance } from '../models/insurance';
import { InsuranceService } from '../services/insurance.service';

interface NameOption {
  id: number;
  name: string;
  arabicName: string;
  gender?: string;
}

@Component({
  selector: 'app-quick-admission',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './quick-admission.component.html',
  styleUrl: './quick-admission.component.scss'
})
export class QuickAdmissionComponent implements OnInit {
  private apiUrl = 'http://localhost:5050/api';
  private billingService = inject(BillingService);
  private departmentService = inject(DepartmentService);
  private insuranceService = inject(InsuranceService);

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

  // Arabic name parts (from selection)
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

  // Duplicate check (only for name changes, not save)
  showDuplicateModal = signal(false);
  duplicatePatients = signal<any[]>([]);
  isExistingPatient = signal(false);
  existingPatientId = signal<number | null>(null);

  // Validation
  dobValidationError = signal<string | null>(null);

  // Invoice related signals
  invoiceDetails = signal<BillingInvoiceDetail[]>([]);
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
  private denominationCodeById = signal<Map<number, string>>(new Map<number, string>());
  private denominationCodeFetchInProgress = new Set<number>();

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

  // Insurance / account autocomplete
  insurances = signal<Insurance[]>([]);
  filteredInsurancesForInsurance = signal<Insurance[]>([]);
  filteredInsurancesForAccount = signal<Insurance[]>([]);
  insuranceSearchText = signal<string>('');
  accountSearchText = signal<string>('');
  showInsuranceDropdown = signal<boolean>(false);
  showAccountDropdown = signal<boolean>(false);
  selectedInsuranceIndex = signal<number>(-1);
  selectedAccountIndex = signal<number>(-1);

  // Physician autocomplete
  physicianOptions = signal<any[]>([]);
  showPhysicianDropdown = signal(false);

  // Departments
  departments = signal<Department[]>([]);

  // Computed property for departments with display text
  departmentsWithDisplay = computed(() => {
    return this.departments().map(dept => ({
      ...dept,
      displayText: this.getDepartmentDisplayText(dept),
      value: this.getDepartmentValue(dept)
    }));
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) { }

  // Invoice header signals
  invoiceHeaderId = signal<number | null>(null);
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

    // NOTE: Cannot write to signals inside computed - removed signal updates
    // The template will use invoiceTotals() directly instead of separate signals

    return {
      subtotal,
      totalDiscount,
      totalLumpSum,
      totalTax: 0, // No tax in this system
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

  // Track which detail is being edited inline
  editingDetailIndex = signal<number | null>(null);

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.admissionId.set(+params['id']);
        this.isEditMode.set(true);
        this.loadAdmissionForEdit(+params['id']);
      } else {
        // Generate MRN for new patient
        this.generateMRN();
      }
    });

    this.route.queryParams.subscribe(queryParams => {
      const patientId = queryParams['patientId'];
      if (!this.isEditMode() && patientId) {
        this.loadPatientForNewAdmission(+patientId);
      }
    });

    // Load departments
    this.loadDepartments();

    // Load insurances for admission autocomplete
    this.loadInsurances();

    // Note: Denominations are now loaded on-demand via searchDenominations() using the new advanced query

    // Invoice section is ready
    console.log('🔄 Quick Admission ngOnInit - Invoice section ready');

    // Test physician API on component init
    this.testPhysicianAPI();
  }

  resetForm(): void {
    // Reset all form fields
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

    // Reset admission fields
    this.admissionNumber.set(''); // Clear admission number for new admission
    this.referralPhysician.set('');
    this.referralPhysicianId.set(null);
    this.attendingPhysician.set('');
    this.attendingPhysicianId.set(null);
    this.checkInDate.set(this.getTodayDateString());
    this.department.set('');
    this.mainInsurance.set(5);
    this.auxiliaryInsurance.set(5);
    this.invoiceHeaderId.set(null);
    this.invoiceDetails.set([]);
    this.inlineEditingDetails.set(new Map());
    this.receiptAmount.set(0);
    this.receiptLocal.set(0);
    this.invoiceCurrency.set('USD');

    // Reset flags
    this.isExistingPatient.set(false);
    this.existingPatientId.set(null);
    this.patientId.set(null);
    this.showDuplicateModal.set(false);
    this.duplicatePatients.set([]);
    this.dobValidationError.set(null);

    // Reset dropdowns
    this.showFirstNameDropdown.set(false);
    this.showMiddleNameDropdown.set(false);
    this.showLastNameDropdown.set(false);
    this.showPhysicianDropdown.set(false);

    console.log('Form reset - admission number cleared');
  }

  private loadInsurances(): void {
    this.insuranceService.getAll().subscribe({
      next: (list) => {
        this.insurances.set(list || []);
        // Default selection: insurance id = 5 for both fields
        const defaultIns = list?.find(i => i.id === 5);
        if (defaultIns) {
          this.mainInsurance.set(defaultIns.id);
          this.auxiliaryInsurance.set(defaultIns.id);
          this.insuranceSearchText.set(defaultIns.description);
          this.accountSearchText.set(defaultIns.description);
        }
        this.filteredInsurancesForInsurance.set(list || []);
        this.filteredInsurancesForAccount.set(list || []);
      },
      error: (error) => {
        console.error('Error loading insurances:', error);
      }
    });
  }

  private filterInsurances(term: string): Insurance[] {
    const query = (term || '').toLowerCase();
    if (!query) {
      return this.insurances();
    }
    return this.insurances().filter(i =>
      (i.description && i.description.toLowerCase().includes(query)) ||
      (i.arabicDescription && i.arabicDescription.toLowerCase().includes(query))
    );
  }

  onInsuranceSearchInput(value: string): void {
    this.insuranceSearchText.set(value);
    this.filteredInsurancesForInsurance.set(this.filterInsurances(value));
    this.showInsuranceDropdown.set(true);
    this.selectedInsuranceIndex.set(this.filteredInsurancesForInsurance().length > 0 ? 0 : -1);
  }

  onAccountSearchInput(value: string): void {
    this.accountSearchText.set(value);
    this.filteredInsurancesForAccount.set(this.filterInsurances(value));
    this.showAccountDropdown.set(true);
    this.selectedAccountIndex.set(this.filteredInsurancesForAccount().length > 0 ? 0 : -1);
  }

  toggleInsuranceDropdown(): void {
    const newValue = !this.showInsuranceDropdown();
    this.showInsuranceDropdown.set(newValue);
    if (newValue && this.filteredInsurancesForInsurance().length === 0) {
      this.filteredInsurancesForInsurance.set(this.insurances());
    }
  }

  toggleAccountDropdown(): void {
    const newValue = !this.showAccountDropdown();
    this.showAccountDropdown.set(newValue);
    if (newValue && this.filteredInsurancesForAccount().length === 0) {
      this.filteredInsurancesForAccount.set(this.insurances());
    }
  }

  hideInsuranceDropdown(): void {
    setTimeout(() => {
      this.showInsuranceDropdown.set(false);
      this.selectedInsuranceIndex.set(-1);
    }, 150);
  }

  hideAccountDropdown(): void {
    setTimeout(() => {
      this.showAccountDropdown.set(false);
      this.selectedAccountIndex.set(-1);
    }, 150);
  }

  selectInsurance(ins: Insurance, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.mainInsurance.set(ins.id);
    this.insuranceSearchText.set(ins.description);
    this.showInsuranceDropdown.set(false);
  }

  selectAccount(ins: Insurance, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.auxiliaryInsurance.set(ins.id);
    this.accountSearchText.set(ins.description);
    this.showAccountDropdown.set(false);
  }

  onInsuranceKeyDown(event: KeyboardEvent): void {
    if (!this.showInsuranceDropdown()) {
      return;
    }
    const list = this.filteredInsurancesForInsurance();
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      const next = Math.min(this.selectedInsuranceIndex() + 1, list.length - 1);
      this.selectedInsuranceIndex.set(next);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      const prev = Math.max(this.selectedInsuranceIndex() - 1, 0);
      this.selectedInsuranceIndex.set(prev);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const idx = this.selectedInsuranceIndex();
      if (idx >= 0 && idx < list.length) {
        this.selectInsurance(list[idx]);
      }
    } else if (event.key === 'Escape') {
      this.showInsuranceDropdown.set(false);
    }
  }

  onAccountKeyDown(event: KeyboardEvent): void {
    if (!this.showAccountDropdown()) {
      return;
    }
    const list = this.filteredInsurancesForAccount();
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      const next = Math.min(this.selectedAccountIndex() + 1, list.length - 1);
      this.selectedAccountIndex.set(next);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      const prev = Math.max(this.selectedAccountIndex() - 1, 0);
      this.selectedAccountIndex.set(prev);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const idx = this.selectedAccountIndex();
      if (idx >= 0 && idx < list.length) {
        this.selectAccount(list[idx]);
      }
    } else if (event.key === 'Escape') {
      this.showAccountDropdown.set(false);
    }
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
    console.log('selectFirstName called:', option);
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.firstName.set(option.name);
    this.firstNameArabic.set(option.arabicName);

    // Auto-set gender if available from the name
    if (option.gender) {
      console.log('Setting gender from first name:', option.gender);
      this.gender.set(option.gender);
    }

    this.showFirstNameDropdown.set(false);

    // Check for duplicates if both first and last names are now set
    if (this.lastName()) {
      console.log('Last name exists, scheduling duplicate check');
      setTimeout(() => this.checkForDuplicates(), 300);
    } else {
      console.log('Last name not set yet, skipping duplicate check');
    }
  }

  hideFirstNameDropdown(): void {
    setTimeout(() => {
      this.showFirstNameDropdown.set(false);
      this.selectedFirstNameIndex.set(-1);
    }, 200);
  }

  /**
   * Handle keyboard navigation for First Name autocomplete
   */
  onFirstNameKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (this.showFirstNameDropdown() && this.firstNameOptions().length > 0) {
        const newIndex = this.selectedFirstNameIndex() < this.firstNameOptions().length - 1
          ? this.selectedFirstNameIndex() + 1
          : this.selectedFirstNameIndex();
        this.selectedFirstNameIndex.set(newIndex);
        this.scrollToSelectedItem('firstName');
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.showFirstNameDropdown() && this.firstNameOptions().length > 0) {
        const newIndex = this.selectedFirstNameIndex() > 0
          ? this.selectedFirstNameIndex() - 1
          : 0;
        this.selectedFirstNameIndex.set(newIndex);
        this.scrollToSelectedItem('firstName');
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.showFirstNameDropdown() && this.firstNameOptions().length > 0) {
        const selectedIndex = this.selectedFirstNameIndex() >= 0
          ? this.selectedFirstNameIndex()
          : 0;
        const selectedOption = this.firstNameOptions()[selectedIndex];
        if (selectedOption) {
          this.selectFirstName(selectedOption);
        }
      }
    } else if (event.key === 'Escape') {
      this.showFirstNameDropdown.set(false);
      this.selectedFirstNameIndex.set(-1);
    }
  }

  searchMiddleName(query: string): void {
    if (query.length < 2) {
      this.middleNameOptions.set([]);
      this.showMiddleNameDropdown.set(false);
      this.selectedMiddleNameIndex.set(-1);
      return;
    }

    this.http.get<NameOption[]>(`${this.apiUrl}/Name/search?query=${encodeURIComponent(query)}`).subscribe({
      next: (options) => {
        this.middleNameOptions.set(options);
        this.showMiddleNameDropdown.set(true);
        this.selectedMiddleNameIndex.set(options.length > 0 ? 0 : -1);
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

    // Don't change gender for middle name

    this.showMiddleNameDropdown.set(false);
    this.selectedMiddleNameIndex.set(-1);
  }

  hideMiddleNameDropdown(): void {
    setTimeout(() => {
      this.showMiddleNameDropdown.set(false);
      this.selectedMiddleNameIndex.set(-1);
    }, 200);
  }

  /**
   * Handle keyboard navigation for Middle Name autocomplete
   */
  onMiddleNameKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (this.showMiddleNameDropdown() && this.middleNameOptions().length > 0) {
        const newIndex = this.selectedMiddleNameIndex() < this.middleNameOptions().length - 1
          ? this.selectedMiddleNameIndex() + 1
          : this.selectedMiddleNameIndex();
        this.selectedMiddleNameIndex.set(newIndex);
        this.scrollToSelectedItem('middleName');
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.showMiddleNameDropdown() && this.middleNameOptions().length > 0) {
        const newIndex = this.selectedMiddleNameIndex() > 0
          ? this.selectedMiddleNameIndex() - 1
          : 0;
        this.selectedMiddleNameIndex.set(newIndex);
        this.scrollToSelectedItem('middleName');
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.showMiddleNameDropdown() && this.middleNameOptions().length > 0) {
        const selectedIndex = this.selectedMiddleNameIndex() >= 0
          ? this.selectedMiddleNameIndex()
          : 0;
        const selectedOption = this.middleNameOptions()[selectedIndex];
        if (selectedOption) {
          this.selectMiddleName(selectedOption);
        }
      }
    } else if (event.key === 'Escape') {
      this.showMiddleNameDropdown.set(false);
      this.selectedMiddleNameIndex.set(-1);
    }
  }

  searchLastName(query: string): void {
    if (query.length < 2) {
      this.lastNameOptions.set([]);
      this.showLastNameDropdown.set(false);
      this.selectedLastNameIndex.set(-1);
      return;
    }

    this.http.get<NameOption[]>(`${this.apiUrl}/Family/search?query=${encodeURIComponent(query)}`).subscribe({
      next: (options) => {
        this.lastNameOptions.set(options);
        this.showLastNameDropdown.set(true);
        this.selectedLastNameIndex.set(options.length > 0 ? 0 : -1);
      },
      error: (error) => console.error('Error searching last names:', error)
    });
  }

  selectLastName(option: NameOption, event?: Event): void {
    console.log('selectLastName called:', option);
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.lastName.set(option.name);
    this.lastNameArabic.set(option.arabicName);
    this.showLastNameDropdown.set(false);
    this.selectedLastNameIndex.set(-1);

    // Check for duplicates if both first and last names are now set
    if (this.firstName()) {
      console.log('First name exists, scheduling duplicate check');
      setTimeout(() => this.checkForDuplicates(), 300);
    } else {
      console.log('First name not set yet, skipping duplicate check');
    }
  }

  hideLastNameDropdown(): void {
    setTimeout(() => {
      this.showLastNameDropdown.set(false);
      this.selectedLastNameIndex.set(-1);
    }, 200);
  }

  /**
   * Handle keyboard navigation for Last Name autocomplete
   */
  onLastNameKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (this.showLastNameDropdown() && this.lastNameOptions().length > 0) {
        const newIndex = this.selectedLastNameIndex() < this.lastNameOptions().length - 1
          ? this.selectedLastNameIndex() + 1
          : this.selectedLastNameIndex();
        this.selectedLastNameIndex.set(newIndex);
        this.scrollToSelectedItem('lastName');
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.showLastNameDropdown() && this.lastNameOptions().length > 0) {
        const newIndex = this.selectedLastNameIndex() > 0
          ? this.selectedLastNameIndex() - 1
          : 0;
        this.selectedLastNameIndex.set(newIndex);
        this.scrollToSelectedItem('lastName');
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.showLastNameDropdown() && this.lastNameOptions().length > 0) {
        const selectedIndex = this.selectedLastNameIndex() >= 0
          ? this.selectedLastNameIndex()
          : 0;
        const selectedOption = this.lastNameOptions()[selectedIndex];
        if (selectedOption) {
          this.selectLastName(selectedOption);
        }
      }
    } else if (event.key === 'Escape') {
      this.showLastNameDropdown.set(false);
      this.selectedLastNameIndex.set(-1);
    }
  }

  checkForDuplicates(): void {
    console.log('checkForDuplicates called', {
      firstName: this.firstName(),
      lastName: this.lastName()
    });

    const fName = this.firstName()?.trim();
    const lName = this.lastName()?.trim();

    if (!fName || !lName || fName.length < 2 || lName.length < 2) {
      console.log('Missing or invalid firstName/lastName, skipping duplicate check');
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
    console.log('Checking duplicates at:', url);

    this.http.get<any>(url).subscribe({
      next: (response) => {
        console.log('Duplicate check response:', response);
        if (response.found && response.patients.length > 0) {
          console.log('Duplicates found, showing modal');
          this.duplicatePatients.set(response.patients);
          this.showDuplicateModal.set(true);
        } else {
          console.log('No duplicates found');
        }
      },
      error: (error) => {
        console.error('Error checking duplicates:', error);
        console.error('Error details:', error.error);
      }
    });
  }

  loadExistingPatient(patient: any): void {
    this.applyPatientData(patient, { markExisting: true });
    this.saveMedicalFile.set(false);
    this.saveAdmission.set(true);
    this.showDuplicateModal.set(false);
    alert(`Loaded existing patient: ${patient.mrn} - ${patient.firstName} ${patient.lastName}`);
  }

  continueAsNew(): void {
    this.showDuplicateModal.set(false);
    // Reset existing patient flags when continuing as new
    this.isExistingPatient.set(false);
    this.existingPatientId.set(null);
    this.patientId.set(null);
    this.saveMedicalFile.set(true);
  }

  cancelDuplicateCheck(): void {
    this.showDuplicateModal.set(false);
  }



  savePatient(): void {
    if (this.dobValidationError()) {
      alert('Please fix the Date of Birth validation error before saving');
      return;
    }

    this.showSaveOptionsModal.set(true);
  }

  private loadPatientForNewAdmission(patientId: number): void {
    if (!patientId || Number.isNaN(patientId)) {
      return;
    }

    this.http.get<any>(`${this.apiUrl}/Patient/${patientId}`).subscribe({
      next: (patient) => {
        this.applyPatientData(patient, { markExisting: true });
        this.saveMedicalFile.set(false);
        this.saveAdmission.set(true);
      },
      error: (error) => {
        console.error('Error loading patient for new admission:', error);
        alert('Failed to load the selected patient for a new admission.');
      }
    });
  }

  private loadAdmissionForEdit(admissionId: number): void {
    this.http.get<any>(`${this.apiUrl}/ResidentPatient/${admissionId}`).subscribe({
      next: (residentPatient) => {
        this.applyResidentAdmissionData(residentPatient);
        const patientId = residentPatient?.patientID ?? residentPatient?.patientId;
        if (patientId) {
          this.http.get<any>(`${this.apiUrl}/Patient/${patientId}`).subscribe({
            next: (patient) => this.applyPatientData(patient, { markExisting: true }),
            error: (error) => console.error('Error loading patient for edit mode:', error)
          });
        }

        this.loadInvoiceForAdmission(admissionId);
      },
      error: (error) => {
        console.error('Error loading admission for edit mode:', error);
        alert('Failed to load the admission data.');
      }
    });
  }

  private loadInvoiceForAdmission(admissionId: number): void {
    this.http.get<BillingInvoiceHeader[]>(`${this.apiUrl}/BillingInvoiceHeader/ByAdmission/${admissionId}`).subscribe({
      next: (headers) => {
        const header = headers?.[0];
        if (!header) {
          this.invoiceHeaderId.set(null);
          this.invoiceDetails.set([]);
          this.inlineEditingDetails.set(new Map());
          return;
        }

        this.invoiceHeaderId.set(header.id);
        this.invoiceCurrency.set(header.currency || 'USD');
        this.receiptAmount.set(header.receiptAmount || 0);
        this.receiptLocal.set(header.receivedLBP || 0);

        this.http.get<BillingInvoiceDetail[]>(`${this.apiUrl}/BillingInvoiceDetail/ByHeader/${header.id}`).subscribe({
          next: (details) => {
            this.invoiceDetails.set(details || []);
            this.inlineEditingDetails.set(new Map());
            this.saveInvoice.set((details?.length || 0) > 0);
          },
          error: (error) => {
            console.error('Error loading invoice details:', error);
            this.invoiceDetails.set([]);
            this.inlineEditingDetails.set(new Map());
          }
        });
      },
      error: (error) => {
        console.error('Error loading invoice header:', error);
        this.invoiceHeaderId.set(null);
        this.invoiceDetails.set([]);
        this.inlineEditingDetails.set(new Map());
      }
    });
  }

  private applyResidentAdmissionData(residentPatient: any): void {
    this.patientId.set(residentPatient?.patientID ?? residentPatient?.patientId ?? null);
    this.admissionNumber.set(residentPatient?.admissionNumber || '');
    this.admissionSite.set(residentPatient?.admissionSite ?? 3);
    this.referralPhysicianId.set(residentPatient?.referralPhysicianID ?? residentPatient?.referralPhysicianId ?? null);
    this.referralPhysician.set(residentPatient?.referralPhysicianName || '');
    this.attendingPhysicianId.set(residentPatient?.attendingPhysicianID ?? residentPatient?.attendingPhysicianId ?? null);
    this.attendingPhysician.set(residentPatient?.attendingPhysicianName || residentPatient?.referralPhysicianName || '');
    this.mainInsurance.set(residentPatient?.mainInsuranceID ?? residentPatient?.mainInsuranceId ?? 5);
    this.mainInsuranceClass.set(residentPatient?.mainInsuranceClassID ?? residentPatient?.mainInsuranceClassId ?? 4);
    this.auxiliaryInsurance.set(residentPatient?.auxiliaryInsuranceID ?? residentPatient?.auxiliaryInsuranceId ?? 5);
    this.auxiliaryInsuranceClass.set(residentPatient?.auxiliaryInsuranceClassID ?? residentPatient?.auxiliaryInsuranceClassId ?? 4);
    this.checkInClass.set(residentPatient?.checkInClassID ?? residentPatient?.checkInClassId ?? 5);
    this.department.set(residentPatient?.medicationUnitDescription || '');
    this.checkInDate.set(this.toDateInputValue(residentPatient?.checkInDate));
    this.group.set(residentPatient?.group ?? 22);
    this.type.set(residentPatient?.admissionType ?? 3);
    this.saveMedicalFile.set(false);
    this.saveAdmission.set(true);
    this.saveInvoice.set((residentPatient?.hasInvoices ?? false) || this.invoiceDetails().length > 0);
  }

  private applyPatientData(
    patient: any,
    options: { markExisting?: boolean } = {}
  ): void {
    const patientId = patient?.id ?? patient?.Id ?? null;
    const mrn = patient?.mrn ?? patient?.medicalRecordNumber ?? patient?.MedicalRecordNumber ?? '';
    const firstName = patient?.firstName ?? patient?.FirstName ?? '';
    const lastName = patient?.lastName ?? patient?.LastName ?? '';
    const middleName = patient?.middleName ?? patient?.MiddleName ?? '';
    const gender = patient?.gender ?? patient?.Gender ?? 'M';
    const dob = patient?.dob ?? patient?.DOB ?? '';
    const phone = patient?.phone ?? patient?.Phone ?? '';
    const maritalStatus = patient?.maritalStatus ?? patient?.MaritalStatus ?? null;
    const arabicFullName = patient?.arabicFullName ?? patient?.ArabicFullName ?? '';

    this.mrn.set(mrn);
    this.firstName.set(firstName);
    this.lastName.set(lastName);
    this.middleName.set(middleName);
    this.gender.set(gender);
    this.dob.set(this.toDateInputValue(dob));
    this.phone.set(phone);
    this.maritalStatus.set(maritalStatus);

    const normalizedDob = this.toDateInputValue(dob);
    this.age.set(normalizedDob ? this.calculateAgeFromDob(normalizedDob) : null);

    this.firstNameArabic.set('');
    this.middleNameArabic.set('');
    this.lastNameArabic.set('');
    if (arabicFullName) {
      const arabicParts = arabicFullName.split(' ');
      if (arabicParts.length >= 1) this.firstNameArabic.set(arabicParts[0]);
      if (arabicParts.length >= 2) this.middleNameArabic.set(arabicParts[1]);
      if (arabicParts.length >= 3) this.lastNameArabic.set(arabicParts[2]);
    }

    if (options.markExisting && patientId) {
      this.isExistingPatient.set(true);
      this.existingPatientId.set(patientId);
      this.patientId.set(patientId);
    }
  }

  private toDateInputValue(value: string | Date | null | undefined): string {
    if (!value) {
      return '';
    }

    if (typeof value === 'string') {
      return value.includes('T') ? value.split('T')[0] : value.substring(0, 10);
    }

    return value.toISOString().split('T')[0];
  }

  private buildStoredProcedurePayload(): { saveData: string; saveOptions: string; existingPatientId: number | null } {
    const saveData = {
      patient: {
        FirstName: this.firstName()?.trim() || null,
        LastName: this.lastName()?.trim() || null,
        MiddleName: this.middleName()?.trim() || null,
        Gender: this.gender() || null,
        Phone: this.phone()?.trim() || null,
        ArabicFullName: this.arabicFullName()?.trim() || null,
        DOB: this.dob() ? new Date(this.dob()).toISOString() : null,
        MaritalStatus: this.maritalStatus(),
        CreatedBy: 338
      },
      admission: {
        AdmissionSite: this.admissionSite(),
        ReferralPhysician: this.referralPhysicianId(),
        AttendingPhysician: this.attendingPhysicianId() || this.referralPhysicianId(),
        MainInsurance: this.mainInsurance(),
        MainInsuranceClass: this.mainInsuranceClass(),
        Insured: this.insured(),
        AuxiliaryInsurance: this.auxiliaryInsurance(),
        AuxiliaryInsuranceClass: this.auxiliaryInsuranceClass(),
        CheckInClass: this.checkInClass(),
        Department: this.department() || null,
        CheckInDate: this.checkInDate() || null,
        CheckOutDate: this.checkOutDate(),
        Type: this.type(),
        IsWorkAccident: this.isWorkAccident() === 1,
        IsExtended: this.isExtended() === 1,
        Group: this.group(),
        CreatedBy: 338
      },
      invoice: this.invoiceDetails().map(detail => ({
        MedicationUnit: detail.medicationUnit || 113,
        MedicationUnitDescription: detail.medicationUnitDescription || this.department() || 'Clinics',
        Denomination: detail.denomination || 0,
        DenominationCode: detail.denominationCode || '',
        DenominationDescription: detail.denominationDescription || '',
        Quantity: detail.quantity || 1,
        UnitPrice: detail.unitPrice || 0,
        NetPrice: detail.netPrice || 0,
        NetUnitPrice: detail.netUnitPrice || 0,
        Discount: detail.discount || 0,
        LumpSum: detail.lumpSum || 0,
        OperatingPhysician: detail.operatingPhysician || 0,
        CostCenter: detail.costCenter || 12,
        ProfitCenter: detail.profitCenter || 3,
        CreatedBy: detail.createdBy || 338
      }))
    };

    const saveOptions = {
      saveMedicalFile: this.saveMedicalFile(),
      saveAdmission: this.saveAdmission(),
      saveInvoice: this.saveInvoice()
    };

    return {
      saveData: JSON.stringify(saveData),
      saveOptions: JSON.stringify(saveOptions),
      existingPatientId: this.isExistingPatient() ? this.existingPatientId() : null
    };
  }

  private saveViaStoredProcedure(): void {
    if (this.isEditMode()) {
      alert('Edit-mode saving is not wired to update APIs yet. Create/new-admission flows have been restored first.');
      this.isSaving.set(false);
      return;
    }

    const payload = this.buildStoredProcedurePayload();

    this.http.post<any>(`${this.apiUrl}/QuickAdmission/Save_V1`, payload).subscribe({
      next: (response) => {
        const mrn = response?.mrn ?? response?.MRN;
        const patientId = response?.patientID ?? response?.patientId ?? response?.PatientID;
        const admissionId = response?.admissionID ?? response?.admissionId ?? response?.AdmissionID;
        const admissionNumber = response?.admissionNumber ?? response?.AdmissionNumber;
        const invoiceHeaderId = response?.invoiceHeaderID ?? response?.invoiceHeaderId ?? response?.InvoiceHeaderID;

        this.isSaving.set(false);
        this.showSaveOptionsModal.set(false);

        if (mrn) {
          this.mrn.set(mrn);
        }
        if (patientId) {
          this.patientId.set(patientId);
          this.existingPatientId.set(patientId);
          this.isExistingPatient.set(true);
        }
        if (admissionId) {
          this.admissionId.set(admissionId);
        }
        if (admissionNumber) {
          this.updateAdmissionNumberDisplay(admissionNumber);
        }
        if (invoiceHeaderId) {
          this.invoiceHeaderId.set(invoiceHeaderId);
        }

        alert(`Quick admission saved successfully.\nMRN: ${mrn || this.mrn()}\nAdmission: ${admissionNumber || this.admissionNumber() || 'N/A'}`);
        this.goBack();
      },
      error: (error) => {
        console.error('Error saving quick admission via stored procedure:', error);
        this.isSaving.set(false);
        const message = error?.error?.error || error?.error?.message || error?.message || 'Unknown error';
        alert(`Error saving quick admission: ${message}`);
      }
    });
  }

  performSave(): void {
    this.isSaving.set(true);

    // Check if this is an existing patient
    if (this.isExistingPatient() && this.existingPatientId()) {
      console.log('=== EXISTING PATIENT DETECTED ===');
      console.log('Patient ID:', this.existingPatientId());
      console.log('Skipping patient creation, proceeding directly to admission');

      // Create admission for existing patient
      this.createAdmission(this.existingPatientId()!);
      return;
    }

    // Prepare patient data - don't send MRN as it's auto-generated by trigger
    const patientData: any = {
      FirstName: this.firstName()?.trim() || null,
      LastName: this.lastName()?.trim() || null,
      MiddleName: this.middleName()?.trim() || null,
      Gender: this.gender() || null,
      Phone: this.phone()?.trim() || null,
      ArabicFullName: this.arabicFullName()?.trim() || null,
      CreatedBy: 338
    };

    // Add DOB if provided (ensure it's in ISO format)
    if (this.dob()) {
      try {
        const dobDate = new Date(this.dob());
        // Format as ISO string for the API
        patientData.DOB = dobDate.toISOString();
      } catch (e) {
        console.error('Invalid date format:', this.dob());
        alert('Invalid date format. Please check the Date of Birth.');
        this.isSaving.set(false);
        return;
      }
    } else {
      patientData.DOB = null;
    }

    // Add marital status if provided
    if (this.maritalStatus() !== null && this.maritalStatus() !== undefined) {
      patientData.MaritalStatus = this.maritalStatus();
    } else {
      patientData.MaritalStatus = null;
    }

    console.log('Sending patient data:', JSON.stringify(patientData, null, 2));

    this.http.post(`${this.apiUrl}/Patient`, patientData).subscribe({
      next: (response: any) => {
        console.log('Patient created successfully:', response);
        this.mrn.set(response.mrn); // Update MRN with the actual generated value
        this.patientId.set(response.id); // Store patient ID for admission
        console.log('=== PATIENT SAVED SUCCESSFULLY ===');
        console.log('Patient ID from response:', response.id);
        console.log('Patient response:', response);

        // Now create admission
        console.log('=== CALLING CREATE ADMISSION ===');
        this.createAdmission(response.id);
      },
      error: (error) => {
        console.error('Error saving patient:', error);
        console.error('Error status:', error.status);
        console.error('Error details:', error.error);

        let errorMsg = 'Error saving patient: ';
        console.log('[Line 395] Initial errorMsg:', errorMsg);

        if (error.error?.errors) {
          // Model validation errors
          const validationErrors = Object.entries(error.error.errors)
            .map(([key, value]) => `${key}: ${value}`)
            .join('\n');
          errorMsg += '\n[Line 400] Validation errors:\n' + validationErrors;
          console.log('[Line 402] errorMsg after validation errors:', errorMsg);
        } else if (error.error?.message) {
          errorMsg += '\n[Line 403] ' + error.error.message;
          console.log('[Line 404] errorMsg after error.error.message:', errorMsg);
        } else if (error.message) {
          errorMsg += '\n[Line 405] ' + error.message;
          console.log('[Line 406] errorMsg after error.message:', errorMsg);
        } else {
          errorMsg += '\n[Line 407] Unknown error';
          console.log('[Line 408] errorMsg for unknown error:', errorMsg);
        }

        console.log('[Line 411] Final errorMsg:', errorMsg);
        alert(errorMsg);
        this.isSaving.set(false);
      }
    });
  }

  createAdmission(patientId: number): void {
    console.log('=== CREATE ADMISSION START ===');
    console.log('Patient ID received:', patientId);

    // Log all current signal values
    console.log('Current signal values:');
    console.log('- admissionNumber:', this.admissionNumber());
    console.log('- admissionSite:', this.admissionSite());
    console.log('- referralPhysician:', this.referralPhysician());
    console.log('- referralPhysicianId:', this.referralPhysicianId());
    console.log('- attendingPhysician:', this.attendingPhysician());
    console.log('- attendingPhysicianId:', this.attendingPhysicianId());
    console.log('- mainInsurance:', this.mainInsurance());
    console.log('- mainInsuranceClass:', this.mainInsuranceClass());
    console.log('- insured:', this.insured());
    console.log('- auxiliaryInsurance:', this.auxiliaryInsurance());
    console.log('- auxiliaryInsuranceClass:', this.auxiliaryInsuranceClass());
    console.log('- checkInClass:', this.checkInClass());
    console.log('- department:', this.department());
    console.log('- checkInDate:', this.checkInDate());
    console.log('- checkOutDate:', this.checkOutDate());
    console.log('- type:', this.type());
    console.log('- isWorkAccident:', this.isWorkAccident());
    console.log('- isExtended:', this.isExtended());
    console.log('- group:', this.group());

    // Prepare admission data
    const admissionData: any = {
      Number: null, // Let database trigger generate this
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
      Patient: patientId,
      Type: this.type(),
      IsWorkAccident: this.isWorkAccident(),
      IsExtended: this.isExtended(),
      Group: this.group(),
      CreatedBy: 338
    };

    console.log('=== ADMISSION DATA TO SEND ===');
    console.log('Raw admission data:', admissionData);
    console.log('JSON stringified admission data:', JSON.stringify(admissionData, null, 2));

    // Validate required fields
    const requiredFields = ['ReferralPhysician', 'Department', 'CheckInDate', 'Patient'];
    const missingFields = requiredFields.filter(field => !admissionData[field]);
    if (missingFields.length > 0) {
      console.error('Missing required fields:', missingFields);
      alert(`Missing required fields: ${missingFields.join(', ')}`);
      this.isSaving.set(false);
      return;
    }

    console.log('API URL:', `${this.apiUrl}/Admission`);
    console.log('=== SENDING ADMISSION REQUEST ===');

    this.http.post(`${this.apiUrl}/Admission`, admissionData).subscribe({
      next: (response: any) => {
        console.log('Admission created successfully:', response);
        console.log('Response number field:', response.number);
        console.log('Response ID field:', response.id);

        // Update admission number from the response (generated by database trigger)
        if (response.number) {
          this.updateAdmissionNumberDisplay(response.number);
        } else {
          console.warn('No admission number in response');
        }

        this.isSaving.set(false);
        alert(`Patient and admission created successfully!\nMRN: ${this.mrn()}\nAdmission: ${response.number || 'Generated by system'}`);
      },
      error: (error) => {
        console.error('=== ADMISSION SAVE ERROR ===');
        console.error('Full error object:', error);
        console.error('Error status:', error.status);
        console.error('Error status text:', error.statusText);
        console.error('Error details:', error.error);
        console.error('Error message:', error.message);
        console.error('Error URL:', error.url);
        console.error('Error headers:', error.headers);

        // Log the request data that failed
        console.error('Failed admission data:', admissionData);
        console.error('API URL that failed:', `${this.apiUrl}/Admission`);

        let errorMsg = 'Patient saved but error creating admission:\n\n';

        if (error.error?.errors) {
          // Model validation errors
          console.error('Validation errors:', error.error.errors);
          const validationErrors = Object.entries(error.error.errors)
            .map(([key, value]) => `${key}: ${Array.isArray(value) ? value.join(', ') : value}`)
            .join('\n');
          errorMsg += 'Validation errors:\n' + validationErrors;
        } else if (error.error?.message) {
          console.error('Error message from server:', error.error.message);
          errorMsg += 'Server error: ' + error.error.message;
        } else if (error.message) {
          console.error('Client error message:', error.message);
          errorMsg += 'Client error: ' + error.message;
        } else {
          console.error('Unknown error type');
          errorMsg += 'Unknown error occurred';
        }

        console.error('Final error message to show user:', errorMsg);
        alert(errorMsg);
        this.isSaving.set(false);
      }
    });
  }

  formatDate(date: Date | string | null | undefined): string {
    if (!date) return '-';
    const d = new Date(date);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  goBack(): void {
    this.router.navigate(['/resident-patients']);
  }

  onDobChange(dobValue: string): void {
    this.dob.set(dobValue);

    // Clear previous validation error
    this.dobValidationError.set(null);

    if (dobValue) {
      // Validate DOB
      if (!this.isValidDob(dobValue)) {
        this.dobValidationError.set('Date of birth cannot be in the future');
        this.age.set(null);
        return;
      }

      const calculatedAge = this.calculateAgeFromDob(dobValue);
      this.age.set(calculatedAge);
    } else {
      this.age.set(null);
    }
  }

  onAgeChange(ageValue: string): void {
    const age = ageValue ? parseInt(ageValue, 10) : null;
    this.age.set(age);

    // Clear previous validation error
    this.dobValidationError.set(null);

    if (age !== null && age >= 0 && age <= 150) {
      const calculatedDob = this.calculateDobFromAge(age);

      // Validate the calculated DOB
      if (this.isValidDob(calculatedDob)) {
        this.dob.set(calculatedDob);
      } else {
        this.dobValidationError.set('Calculated date of birth is in the future');
        this.dob.set('');
      }
    } else if (age === null) {
      this.dob.set('');
    } else {
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

    // Set time to start of day for accurate comparison
    today.setHours(0, 0, 0, 0);
    dob.setHours(0, 0, 0, 0);

    return dob <= today;
  }

  getTodayDateString(): string {
    return new Date().toISOString().split('T')[0];
  }

  // Physician search methods
  searchPhysician(query: string): void {
    console.log('searchPhysician called with query:', query);

    if (query.length < 2) {
      this.physicianOptions.set([]);
      this.showPhysicianDropdown.set(false);
      this.selectedPhysicianIndex.set(-1);
      return;
    }

    const url = `${this.apiUrl}/Physician/search?query=${encodeURIComponent(query)}`;
    console.log('Searching physicians at URL:', url);

    this.http.get<any[]>(url).subscribe({
      next: (options) => {
        console.log('Physician search response:', options);
        this.physicianOptions.set(options || []);
        this.showPhysicianDropdown.set(true);
        this.selectedPhysicianIndex.set((options || []).length > 0 ? 0 : -1);
      },
      error: (error) => {
        console.error('Error searching physicians:', error);
        console.error('Error details:', error.error);
        console.error('Error status:', error.status);

        // Show error message in dropdown
        this.physicianOptions.set([]);
        this.showPhysicianDropdown.set(true);
        this.selectedPhysicianIndex.set(-1);
      }
    });
  }

  selectPhysician(physician: any, event?: Event): void {
    console.log('selectPhysician called with:', physician);

    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.referralPhysician.set(physician.name);
    this.referralPhysicianId.set(physician.id);
    this.attendingPhysician.set(physician.name);
    this.attendingPhysicianId.set(physician.id);
    this.showPhysicianDropdown.set(false);
    this.selectedPhysicianIndex.set(-1);

    console.log('Physician selected:', {
      referralPhysician: this.referralPhysician(),
      referralPhysicianId: this.referralPhysicianId(),
      attendingPhysician: this.attendingPhysician(),
      attendingPhysicianId: this.attendingPhysicianId()
    });
  }

  hidePhysicianDropdown(): void {
    setTimeout(() => {
      this.showPhysicianDropdown.set(false);
      this.selectedPhysicianIndex.set(-1);
    }, 200);
  }

  /**
   * Handle keyboard navigation for Referral Physician autocomplete
   */
  onReferralPhysicianKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (this.showPhysicianDropdown() && this.physicianOptions().length > 0) {
        const newIndex = this.selectedPhysicianIndex() < this.physicianOptions().length - 1
          ? this.selectedPhysicianIndex() + 1
          : this.selectedPhysicianIndex();
        this.selectedPhysicianIndex.set(newIndex);
        this.scrollToSelectedItem('physician');
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.showPhysicianDropdown() && this.physicianOptions().length > 0) {
        const newIndex = this.selectedPhysicianIndex() > 0
          ? this.selectedPhysicianIndex() - 1
          : 0;
        this.selectedPhysicianIndex.set(newIndex);
        this.scrollToSelectedItem('physician');
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.showPhysicianDropdown() && this.physicianOptions().length > 0) {
        const selectedIndex = this.selectedPhysicianIndex() >= 0
          ? this.selectedPhysicianIndex()
          : 0;
        const selectedOption = this.physicianOptions()[selectedIndex];
        if (selectedOption) {
          this.selectPhysician(selectedOption);
        }
      }
    } else if (event.key === 'Escape') {
      this.showPhysicianDropdown.set(false);
      this.selectedPhysicianIndex.set(-1);
    }
  }

  /**
   * Scroll to selected item in dropdown
   */
  scrollToSelectedItem(dropdownType: 'firstName' | 'lastName' | 'middleName' | 'physician'): void {
    setTimeout(() => {
      let selector = '';
      switch (dropdownType) {
        case 'firstName':
          selector = '.autocomplete-dropdown';
          break;
        case 'lastName':
          selector = '.autocomplete-dropdown';
          break;
        case 'middleName':
          selector = '.autocomplete-dropdown';
          break;
        case 'physician':
          selector = '.autocomplete-dropdown';
          break;
      }

      // Find the dropdown container - need to find the one closest to the input
      const dropdowns = document.querySelectorAll(selector);
      if (dropdowns.length > 0) {
        // Find the active item
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
   * Handle icon click for first name autocomplete
   */
  onFirstNameIconClick(): void {
    // Toggle dropdown visibility
    if (this.showFirstNameDropdown() && this.firstNameOptions().length > 0) {
      // Hide dropdown
      this.showFirstNameDropdown.set(false);
      this.selectedFirstNameIndex.set(-1);
    } else {
      // Show dropdown
      const currentValue = this.firstName();
      if (currentValue && currentValue.length >= 2) {
        this.searchFirstName(currentValue);
      } else {
        // Trigger search with a single character to get initial results
        this.http.get<NameOption[]>(`${this.apiUrl}/Name/search?query=a`).subscribe({
          next: (options) => {
            this.firstNameOptions.set(options.slice(0, 50)); // Show first 50 results
            this.showFirstNameDropdown.set(true);
            this.selectedFirstNameIndex.set(options.length > 0 ? 0 : -1);
          },
          error: (error) => console.error('Error searching first names:', error)
        });
      }
    }
  }

  /**
   * Handle icon click for last name autocomplete (toggle dropdown)
   */
  onLastNameIconClick(): void {
    // Toggle dropdown visibility
    if (this.showLastNameDropdown() && this.lastNameOptions().length > 0) {
      // Hide dropdown
      this.showLastNameDropdown.set(false);
      this.selectedLastNameIndex.set(-1);
    } else {
      // Show dropdown - always trigger search to show all options
      const currentValue = this.lastName();
      if (currentValue && currentValue.length >= 2) {
        this.searchLastName(currentValue);
      } else {
        // Trigger search with a single character to get initial results
        this.http.get<NameOption[]>(`${this.apiUrl}/Family/search?query=a`).subscribe({
          next: (options) => {
            this.lastNameOptions.set(options.slice(0, 50)); // Show first 50 results
            this.showLastNameDropdown.set(true);
            this.selectedLastNameIndex.set(options.length > 0 ? 0 : -1);
          },
          error: (error) => console.error('Error searching last names:', error)
        });
      }
    }
  }

  /**
   * Handle icon click for middle name autocomplete (toggle dropdown)
   */
  onMiddleNameIconClick(): void {
    // Toggle dropdown visibility
    if (this.showMiddleNameDropdown() && this.middleNameOptions().length > 0) {
      // Hide dropdown
      this.showMiddleNameDropdown.set(false);
      this.selectedMiddleNameIndex.set(-1);
    } else {
      // Show dropdown - always trigger search to show all options
      const currentValue = this.middleName();
      if (currentValue && currentValue.length >= 2) {
        this.searchMiddleName(currentValue);
      } else {
        // Trigger search with a single character to get initial results
        this.http.get<NameOption[]>(`${this.apiUrl}/Name/search?query=a`).subscribe({
          next: (options) => {
            this.middleNameOptions.set(options.slice(0, 50)); // Show first 50 results
            this.showMiddleNameDropdown.set(true);
            this.selectedMiddleNameIndex.set(options.length > 0 ? 0 : -1);
          },
          error: (error) => console.error('Error searching middle names:', error)
        });
      }
    }
  }

  /**
   * Handle icon click for referral physician autocomplete (toggle dropdown)
   */
  onReferralPhysicianIconClick(): void {
    // Toggle dropdown visibility
    if (this.showPhysicianDropdown() && this.physicianOptions().length > 0) {
      // Hide dropdown
      this.showPhysicianDropdown.set(false);
      this.selectedPhysicianIndex.set(-1);
    } else {
      // Show dropdown - always trigger search to show all options
      const currentValue = this.referralPhysician();
      if (currentValue && currentValue.length >= 2) {
        this.searchPhysician(currentValue);
      } else {
        // Trigger search with a single character to get initial results
        const url = `${this.apiUrl}/Physician/search?query=a`;
        this.http.get<any[]>(url).subscribe({
          next: (options) => {
            this.physicianOptions.set(options.slice(0, 50)); // Show first 50 results
            this.showPhysicianDropdown.set(true);
            this.selectedPhysicianIndex.set(options.length > 0 ? 0 : -1);
          },
          error: (error) => {
            console.error('Error searching physicians:', error);
            this.physicianOptions.set([]);
            this.showPhysicianDropdown.set(true);
            this.selectedPhysicianIndex.set(-1);
          }
        });
      }
    }
  }

  /**
   * Handle icon click for denomination autocomplete in detail (toggle dropdown)
   */
  onDenominationIconClickForDetail(index: number): void {
    const editingDetail = this.getEditingDetailForRow(index);
    const map = new Map(this.inlineEditingDetails());
    const detail = map.get(index) || editingDetail;

    // Toggle dropdown visibility
    if (detail.showDropdown && detail.searchResults && detail.searchResults.length > 0) {
      // Hide dropdown
      detail.showDropdown = false;
      detail.selectedDropdownIndex = -1;
    } else {
      // Show dropdown - always trigger search to show all options
      detail.showDropdown = true;
      const currentValue = editingDetail.denominationSearchQuery || '';
      // Always trigger search to ensure we have results
      this.searchDenominationsForDetail(currentValue || '', index);
    }

    map.set(index, detail);
    this.inlineEditingDetails.set(map);
  }

  /**
   * Handle icon click for operating physician autocomplete in detail (toggle dropdown)
   */
  onOperatingPhysicianIconClick(index: number): void {
    const editingDetail = this.getEditingDetailForRow(index);
    if (!editingDetail.hasOperatingPhysician) return;

    const map = new Map(this.inlineEditingDetails());
    const detail = map.get(index) || editingDetail;

    // Toggle dropdown visibility
    if (detail.showOperatingPhysicianDropdown && detail.operatingPhysicianOptions && detail.operatingPhysicianOptions.length > 0) {
      // Hide dropdown
      detail.showOperatingPhysicianDropdown = false;
      detail.selectedOperatingPhysicianIndex = -1;
    } else {
      // Show dropdown - always trigger search to show all options
      detail.showOperatingPhysicianDropdown = true;
      const currentValue = editingDetail.operatingPhysicianName || '';

      if (currentValue && currentValue.length >= 2) {
        this.searchOperatingPhysician(currentValue, index);
      } else {
        // Trigger search with a single character to get initial results
        const url = `${this.apiUrl}/Physician/search?query=a`;
        this.http.get<any[]>(url).subscribe({
          next: (physicians) => {
            const map = new Map(this.inlineEditingDetails());
            const editingDetail = map.get(index) || this.getEditingDetailForRow(index);
            editingDetail.operatingPhysicianOptions = physicians.slice(0, 50);
            editingDetail.showOperatingPhysicianDropdown = physicians.length > 0;
            editingDetail.selectedOperatingPhysicianIndex = physicians.length > 0 ? 0 : -1;
            map.set(index, editingDetail);
            this.inlineEditingDetails.set(map);
          },
          error: (error) => {
            console.error('Error searching physicians:', error);
            const map = new Map(this.inlineEditingDetails());
            const editingDetail = map.get(index) || this.getEditingDetailForRow(index);
            editingDetail.operatingPhysicianOptions = [];
            editingDetail.showOperatingPhysicianDropdown = false;
            editingDetail.selectedOperatingPhysicianIndex = -1;
            map.set(index, editingDetail);
            this.inlineEditingDetails.set(map);
          }
        });
      }
    }

    map.set(index, detail);
    this.inlineEditingDetails.set(map);
  }

  onReferralPhysicianInput(value: string): void {
    console.log('onReferralPhysicianInput called with value:', value);
    this.referralPhysician.set(value);
    this.searchPhysician(value);
  }

  onReferralPhysicianFocus(): void {
    console.log('onReferralPhysicianFocus called');
    // Only show dropdown on focus if it's not already visible (don't interfere with toggle)
    if (!this.showPhysicianDropdown() && this.referralPhysician().length >= 2 && this.physicianOptions().length > 0) {
      this.showPhysicianDropdown.set(true);
      // Trigger change detection to recalculate position
      setTimeout(() => {
        // Force position recalculation
        this.getDropdownStyle();
      }, 0);
    }
  }

  trackByPhysicianId(index: number, physician: any): any {
    return physician.id;
  }

  getDropdownStyle(): any {
    // Find the Referral Physician input field specifically by ID
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

  /**
   * Prevent Escape key from closing the duplicate modal
   * The modal can only be closed by clicking action buttons
   */
  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: Event): void {
    if (this.showDuplicateModal()) {
      event.preventDefault();
      event.stopPropagation();
    }
  }

  /**
   * Update admission number display after successful creation
   */
  updateAdmissionNumberDisplay(admissionNumber: string): void {
    console.log('Updating admission number display to:', admissionNumber);
    this.admissionNumber.set(admissionNumber);

    // Force UI update
    setTimeout(() => {
      console.log('Admission number display updated:', this.admissionNumber());
    }, 50);
  }

  // ===========================================
  // INVOICE METHODS
  // ===========================================

  /**
   * Load denominations for invoice
   */
  loadDenominations(): void {
    // No longer needed - denominations are now loaded on-demand via searchDenominations() using the new advanced query
    console.log('ℹ️ Denominations are now loaded on-demand via search using the new advanced query');
  }

  /**
   * Load departments from database
   */
  loadDepartments(): void {
    console.log('🔄 Loading departments...');
    this.departmentService.getAll().subscribe({
      next: (departments) => {
        console.log('📥 Raw departments response:', departments);
        console.log('📥 First department:', departments[0]);
        console.log('📥 First department name:', departments[0]?.name);
        console.log('📥 First department name type:', typeof departments[0]?.name);

        // Don't filter - show all departments, even if some fields are empty
        this.departments.set(departments);
        console.log('✅ Loaded departments:', departments.length);
        console.log('📋 All departments:', departments.map(d => ({
          id: d.id,
          name: d.name,
          nameType: typeof d.name,
          code: d.code,
          description: d.description
        })));
      },
      error: (error) => {
        console.error('❌ Error loading departments:', error);
        console.error('Error details:', JSON.stringify(error, null, 2));
      }
    });
  }

  /**
   * Get department value for select option
   */
  getDepartmentValue(dept: Department): string {
    if (dept.name && dept.name.toString().trim().length > 0) {
      return dept.name.toString().trim();
    }
    if (dept.code && dept.code.toString().trim().length > 0) {
      return dept.code.toString().trim();
    }
    if (dept.description && dept.description.toString().trim().length > 0) {
      return dept.description.toString().trim();
    }
    // Fallback to ID if nothing else is available
    return dept.id.toString();
  }

  /**
   * Get department display text
   */
  getDepartmentDisplay(dept: Department): string {
    if (dept.name && dept.name.toString().trim().length > 0) {
      return dept.name.toString().trim();
    }
    if (dept.code && dept.code.toString().trim().length > 0) {
      return dept.code.toString().trim();
    }
    if (dept.description && dept.description.toString().trim().length > 0) {
      return dept.description.toString().trim();
    }
    // Fallback to ID if nothing else is available
    return `Department ${dept.id}`;
  }

  /**
   * Get department display text for template (simplified)
   */
  getDepartmentDisplayText(dept: Department): string {
    const name = dept?.name?.toString()?.trim() || '';
    const code = dept?.code?.toString()?.trim() || '';
    const description = dept?.description?.toString()?.trim() || '';

    if (name) return name;
    if (code) return code;
    if (description) return description;
    return `Department ${dept?.id || ''}`;
  }

  /**
   * Track by function for department list
   */
  trackByDepartmentId(index: number, dept: any): any {
    return dept?.id || index;
  }

  /**
   * Check if denomination matches CostCenter filter
   */
  matchesCostCenterFilter(den: HospitalDenomination): boolean {
    if (!this.department()) return true; // No filter if no department selected

    const filter = this.getCostCenterFilter();
    if (!filter) return true; // No filter if department doesn't have one

    const allowedCostCenters = filter.split(',').map(cc => cc.trim());
    const denCostCenter = den.costCenter?.toString().trim() || '';

    return allowedCostCenters.includes(denCostCenter);
  }

  /**
   * Search denominations using the new advanced query
   */
  searchDenominations(query: string): void {
    console.log('🔍 Searching denominations for:', query);
    this.denominationSearchQuery.set(query || '');

    // Get CostCenter filter based on department
    const costCenterFilter = this.getCostCenterFilter();
    console.log('🏥 Department:', this.department(), 'CostCenter filter:', costCenterFilter);

    // Use the new advanced search endpoint (allow empty query to show all results)
    this.billingService.searchDenominationsAdvanced(
      query && query.trim().length > 0 ? query.trim() : undefined, // Pass undefined for empty query to get all results
      5, // Default insurance ID
      costCenterFilter || undefined
    ).subscribe({
      next: (results) => {
        console.log('🌐 Advanced search results:', results.length);
        if (results.length > 0) {
          console.log('📊 Sample result:', JSON.stringify(results[0], null, 2));
          console.log('💰 Price USD:', results[0].priceUsd);
        }
        this.showDenominationDropdown = results.length > 0;
      },
      error: (error) => {
        console.error('❌ Error searching denominations:', error);
        this.showDenominationDropdown = false;
      }
    });
  }

  /**
   * Select denomination for invoice item (from new search results)
   */
  selectDenominationFromSearchResult(result: DenominationSearchResult): void {
    // Convert DenominationSearchResult to invoice detail
    const unitPrice = result.priceUsd ?? 0;
    const fallbackCode = result.actName || '';
    const initialCode = this.getDenominationCodeFromSearchResult(result) || fallbackCode;
    this.newInvoiceDetail.set({
      ...this.newInvoiceDetail(),
      denomination: result.denId,
      denominationCode: initialCode,
      denominationDescription: result.actName || '',
      unitPrice: unitPrice,
      netPrice: unitPrice * (this.newInvoiceDetail().quantity || 1),
      netUnitPrice: unitPrice
    });

    // Update search query to show selected item
    this.denominationSearchQuery.set(this.formatDenominationDisplay(initialCode, result.actName || ''));
    this.showDenominationDropdown = false;

    // If code is not present in search result, resolve by denomination id.
    if (!this.getDenominationCodeFromSearchResult(result)) {
      this.resolveDenominationCode(result, fallbackCode, (resolvedCode) => {
        this.newInvoiceDetail.set({
          ...this.newInvoiceDetail(),
          denominationCode: resolvedCode
        });
        this.denominationSearchQuery.set(this.formatDenominationDisplay(resolvedCode, result.actName || ''));
      });
    }

    console.log('✅ Selected denomination from search result:', result);
  }

  /**
   * Select denomination for invoice item
   */
  selectDenomination(denomination: HospitalDenomination): void {
    const unitPrice = denomination.cashPriceUsd ?? 0;
    this.newInvoiceDetail.set({
      ...this.newInvoiceDetail(),
      denomination: denomination.id,
      denominationCode: denomination.code || '',
      denominationDescription: denomination.smallDescription || '',
      denominationCoeffCode: denomination.coefficientCode || '',
      denominationCoeffValue: denomination.coefficientValue ?? 1,
      denominationCoeffPrice: (denomination.coefficientValue ?? 1) * unitPrice,
      unitPrice: unitPrice,
      netUnitPrice: unitPrice
    });
    this.showDenominationDropdown = false;
    this.denominationSearchQuery.set((denomination.code || '') + ' - ' + (denomination.smallDescription || ''));
  }

  /**
   * Clear selected denomination
   */
  clearDenomination(): void {
    this.newInvoiceDetail.set({
      ...this.newInvoiceDetail(),
      denomination: 0,
      denominationCode: '',
      denominationDescription: ''
    });
  }

  /**
   * Select denomination for inline adding
   */
  selectDenominationInline(denomination: HospitalDenomination): void {
    console.log('✅ Selected denomination:', denomination);
    const unitPrice = denomination.cashPriceUsd ?? 0;
    this.newInvoiceDetail.set({
      ...this.newInvoiceDetail(),
      denomination: denomination.id,
      denominationCode: denomination.code || '',
      denominationDescription: denomination.smallDescription || '',
      denominationCoeffCode: denomination.coefficientCode || '',
      denominationCoeffValue: denomination.coefficientValue ?? 1,
      denominationCoeffPrice: (denomination.coefficientValue ?? 1) * unitPrice,
      unitPrice: unitPrice,
      netUnitPrice: unitPrice
    });
    this.showDenominationDropdown = false;
    this.denominationSearchQuery.set((denomination.code || '') + ' - ' + (denomination.smallDescription || ''));
  }

  /**
   * Close denomination dropdown
   */
  closeDenominationDropdown(): void {
    this.showDenominationDropdown = false;
  }

  /**
   * Handle input focus
   */
  onDenominationInputFocus(): void {
    console.log('🎯 Focus on denomination input');

    const query = this.denominationSearchQuery();
    if (query && query.length > 0) {
      // Re-search with current query
      this.searchDenominations(query);
    } else {
      // If no query, trigger search with empty string to show all available denominations
      this.searchDenominations('');
    }
  }

  /**
   * Handle keyboard events for autocomplete
   */
  onDenominationKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.closeDenominationDropdown();
    }
  }

  /**
   * Handle mouse enter for autocomplete items
   */
  onMouseEnter(event: Event): void {
    const target = event.target as HTMLElement;
    if (target) {
      target.style.backgroundColor = '#f8f9fa';
    }
  }

  /**
   * Handle mouse leave for autocomplete items
   */
  onMouseLeave(event: Event): void {
    const target = event.target as HTMLElement;
    if (target) {
      target.style.backgroundColor = 'transparent';
    }
  }

  /**
   * Add invoice item inline
   */
  addInvoiceItemInline(): void {
    // Check if department is selected
    if (!this.department()) {
      alert('Please select a Department before adding invoice items.');
      return;
    }

    const detailData = this.newInvoiceDetail();

    if (!detailData.denominationDescription || (detailData.quantity ?? 0) <= 0 || (detailData.unitPrice ?? 0) < 0) {
      console.error('❌ Please fill in all required fields with valid values');
      return;
    }

    // Calculate net price and other values
    const netPrice = ((detailData.quantity ?? 0) * (detailData.unitPrice ?? 0)) - (detailData.discount || 0) + (detailData.lumpSum || 0);
    const netUnitPrice = (detailData.unitPrice ?? 0) - (detailData.discount || 0) + (detailData.lumpSum || 0);

    const newDetail: BillingInvoiceDetail = {
      id: Date.now(),
      prescriptionDate: undefined,
      prescribedBy: undefined,
      medicationUnit: 113,
      medicationUnitDescription: 'Clinics',
      admission: 0,
      patient: 0,
      denomination: detailData.denomination || 0,
      denominationCode: detailData.denominationCode || '',
      denominationDescription: detailData.denominationDescription || '',
      denominationCoeffCode: detailData.denominationCoeffCode || '',
      denominationCoeffValue: detailData.denominationCoeffValue || 1,
      denominationCoeffPrice: detailData.denominationCoeffPrice || 0,
      quantity: detailData.quantity || 1,
      unitPrice: detailData.unitPrice || 0,
      netPrice: netPrice,
      netUnitPrice: netUnitPrice,
      differenceAmount: 0,
      deniedAmount: 0,
      discount: detailData.discount || 0,
      lumpSum: detailData.lumpSum || 0,
      complementaryAmount: 0,
      complementaryAmountOtherCurrency: 0,
      complementaryDifferenceOtherCurrency: 0,
      operatingPhysician: 0,
      isMedicalResultOk: undefined,
      medicalResultDate: undefined,
      requireApproval: 0,
      approvalReference: undefined,
      approvalDate: undefined,
      isDenied: 0,
      approvedBy: undefined,
      dueDate: undefined,
      executionDate: undefined,
      invoiceHeader: 0,
      referralPhysician: 0,
      costCenter: 12,
      profitCenter: 3,
      pacIndex: undefined,
      preInvoiceDetail: undefined,
      detailDate: new Date(),
      mainDetailId: undefined,
      copyFlag: 0,
      detailDateHelper: undefined,
      isDoubtfull: 0,
      procedure: undefined,
      isDeleted: 0,
      createdBy: 298,
      modifiedBy: undefined,
      createdDate: new Date(),
      modifiedDate: undefined,
      previousDetailId: undefined,
      orderDetailSequenceNumber: this.invoiceDetails().length + 1,
      source: 'O',
      isCanceled: 0,
      cancelComment: undefined,
      oldOrderDetailSequenceNumber: undefined,
      isApproved: undefined,
      invoiceNumber: undefined,
      patientAmount: undefined
    };

    this.invoiceDetails.set([...this.invoiceDetails(), newDetail]);
    this.resetInvoiceDetailForm();
    console.log('✅ Invoice item added inline');
  }

  /**
   * Update invoice item inline
   */
  updateInvoiceItemInline(detail: BillingInvoiceDetail, index: number): void {
    // Recalculate net price
    const netPrice = (detail.quantity * detail.unitPrice) - (detail.discount || 0) + (detail.lumpSum || 0);
    const netUnitPrice = detail.unitPrice - (detail.discount || 0) + (detail.lumpSum || 0);

    const updatedDetail: BillingInvoiceDetail = {
      ...detail,
      netPrice: netPrice,
      netUnitPrice: netUnitPrice,
      modifiedDate: new Date()
    };

    const details = [...this.invoiceDetails()];
    details[index] = updatedDetail;
    this.invoiceDetails.set(details);
  }

  /**
   * Calculate total for new item
   */
  calculateNewItemTotal(): number {
    const detail = this.newInvoiceDetail();
    const quantity = detail.quantity || 0;
    const unitPrice = detail.unitPrice || 0;
    const discount = detail.discount || 0;
    const lumpSum = detail.lumpSum || 0;
    return (quantity * unitPrice) - discount + lumpSum;
  }

  /**
   * Check if new item can be added
   */
  canAddNewItem(): boolean {
    const detail = this.newInvoiceDetail();
    return !!(detail.denominationDescription &&
      detail.quantity &&
      detail.quantity > 0 &&
      detail.unitPrice &&
      detail.unitPrice > 0);
  }

  /**
   * Get CostCenter filter based on department
   */
  getCostCenterFilter(): string | undefined {
    const dept = this.department().toLowerCase();
    switch (dept) {
      case 'clinic':
        return '5,11,12';
      case 'lab':
        return '1';
      case 'radio':
        return '2,6,7';
      case 'beauty':
        return '54';
      default:
        return undefined;
    }
  }

  // Inline editing state for invoice details
  inlineEditingDetails = signal<Map<number, {
    denominationSearchQuery: string;
    searchResults: DenominationSearchResult[];
    showDropdown: boolean;
    selectedDropdownIndex: number;
    quantity: number;
    unitPrice: number;
    discount: number;
    denominationId?: number;
    denominationCode?: string;
    denominationDescription?: string;
    hasOperatingPhysician?: boolean;
    operatingPhysician?: number;
    operatingPhysicianName?: string;
    operatingPhysicianOptions?: any[];
    showOperatingPhysicianDropdown?: boolean;
  }>>(new Map());

  /**
   * Get editing detail for inline edit (legacy method for backward compatibility)
   */
  getEditingDetail(): any {
    const index = this.editingDetailIndex();
    if (index === null) {
      return {
        denominationSearchQuery: '',
        searchResults: [],
        showDropdown: false,
        selectedDropdownIndex: -1,
        quantity: 1,
        unitPrice: 0,
        discount: 0
      };
    }
    return this.getEditingDetailForRow(index);
  }

  /**
   * Get editing detail for a specific row (always editable mode)
   */
  getEditingDetailForRow(index: number): any {
    const map = this.inlineEditingDetails();
    if (!map.has(index)) {
      const detail = this.invoiceDetails()[index];
      if (detail) {
        // Initialize with detail values
        const hasOperatingPhysician = this.checkDenominationHasOperatingPhysician(detail.denomination);
        const editingDetail = {
          denominationSearchQuery: detail.denominationDescription || '',
          searchResults: [],
          showDropdown: false,
          selectedDropdownIndex: -1,
          quantity: detail.quantity,
          unitPrice: detail.unitPrice,
          discount: detail.discount || 0,
          denominationId: detail.denomination,
          denominationCode: detail.denominationCode,
          denominationDescription: detail.denominationDescription,
          hasOperatingPhysician: hasOperatingPhysician,
          operatingPhysician: detail.operatingPhysician || 0,
          operatingPhysicianName: '',
          operatingPhysicianOptions: [],
          showOperatingPhysicianDropdown: false
        };
        map.set(index, editingDetail);
        this.inlineEditingDetails.set(map);

        // Load operating physician name if exists
        if (detail.operatingPhysician && detail.operatingPhysician > 0) {
          let physicianName = '';
          if (this.referralPhysicianId() === detail.operatingPhysician) {
            physicianName = this.referralPhysician();
          } else if (this.attendingPhysicianId() === detail.operatingPhysician) {
            physicianName = this.attendingPhysician();
          }
          if (physicianName) {
            editingDetail.operatingPhysicianName = physicianName;
            map.set(index, editingDetail);
            this.inlineEditingDetails.set(map);
          } else {
            this.searchPhysicianForOperatingPosition(detail.operatingPhysician, index);
          }
        }

        return editingDetail;
      } else {
        const defaultDetail = {
          denominationSearchQuery: '',
          searchResults: [],
          showDropdown: false,
          selectedDropdownIndex: -1,
          quantity: 1,
          unitPrice: 0,
          discount: 0,
          hasOperatingPhysician: false,
          operatingPhysician: 0,
          operatingPhysicianName: '',
          operatingPhysicianOptions: [],
          showOperatingPhysicianDropdown: false
        };
        map.set(index, defaultDetail);
        this.inlineEditingDetails.set(map);
        return defaultDetail;
      }
    }
    return map.get(index)!;
  }

  /**
   * Update editing detail field for a specific row
   */
  updateEditingDetailFieldForRow(field: string, value: any, index: number): void {
    const map = new Map(this.inlineEditingDetails());
    const editingDetail = map.get(index) || this.getEditingDetailForRow(index);
    (editingDetail as any)[field] = value;
    map.set(index, editingDetail);
    this.inlineEditingDetails.set(map);

    // Auto-save when denomination, quantity, unitPrice, or discount changes
    if (field === 'denominationId' || field === 'quantity' || field === 'unitPrice' || field === 'discount') {
      this.saveInvoiceDetailInline(index);
    }
  }

  /**
   * Calculate detail total for a specific row
   */
  calculateDetailTotalForRow(index: number): number {
    const editingDetail = this.getEditingDetailForRow(index);
    const quantity = editingDetail.quantity || 0;
    const unitPrice = editingDetail.unitPrice || 0;
    const discount = editingDetail.discount || 0;
    return (quantity * unitPrice) - discount;
  }

  /**
   * Update detail total for a specific row (triggers auto-save)
   */
  updateDetailTotalForRow(index: number): void {
    this.saveInvoiceDetailInline(index);
  }

  /**
   * Add new invoice detail inline
   */
  addInvoiceDetailInline(): void {
    if (!this.department()) {
      alert('Please select a Department before adding invoice items.');
      return;
    }

    const newIndex = this.invoiceDetails().length;
    const newDetail: BillingInvoiceDetail = {
      id: Date.now(),
      prescriptionDate: undefined,
      prescribedBy: undefined,
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
      differenceAmount: 0,
      deniedAmount: 0,
      discount: 0,
      lumpSum: 0,
      complementaryAmount: 0,
      complementaryAmountOtherCurrency: 0,
      complementaryDifferenceOtherCurrency: 0,
      operatingPhysician: 0,
      isMedicalResultOk: undefined,
      medicalResultDate: undefined,
      requireApproval: 0,
      approvalReference: undefined,
      approvalDate: undefined,
      isDenied: 0,
      approvedBy: undefined,
      dueDate: undefined,
      executionDate: undefined,
      invoiceHeader: 0,
      referralPhysician: 0,
      costCenter: 12,
      profitCenter: 3,
      pacIndex: undefined,
      preInvoiceDetail: undefined,
      detailDate: new Date(),
      mainDetailId: undefined,
      copyFlag: 0,
      detailDateHelper: undefined,
      isDoubtfull: 0,
      procedure: undefined,
      isDeleted: 0,
      createdBy: 298,
      modifiedBy: undefined,
      createdDate: new Date(),
      modifiedDate: undefined,
      previousDetailId: undefined,
      orderDetailSequenceNumber: this.invoiceDetails().length + 1,
      source: 'O',
      isCanceled: 0,
      cancelComment: undefined,
      oldOrderDetailSequenceNumber: undefined,
      isApproved: undefined,
      invoiceNumber: undefined,
      patientAmount: undefined
    };

    this.invoiceDetails.set([...this.invoiceDetails(), newDetail]);

    // Initialize inline editing state (always editable mode)
    const map = new Map(this.inlineEditingDetails());
    // Set default operating physician to referral physician if available
    const defaultOperatingPhysician = this.referralPhysicianId();
    const defaultOperatingPhysicianName = this.referralPhysician();

    map.set(newIndex, {
      denominationSearchQuery: '',
      searchResults: [],
      showDropdown: false,
      selectedDropdownIndex: -1,
      quantity: 1,
      unitPrice: 0,
      discount: 0,
      hasOperatingPhysician: false,
      operatingPhysician: defaultOperatingPhysician || 0,
      operatingPhysicianName: defaultOperatingPhysicianName || '',
      operatingPhysicianOptions: [],
      showOperatingPhysicianDropdown: false
    });
    this.inlineEditingDetails.set(map);

    // Auto-focus on denomination input after a short delay to ensure DOM is ready
    setTimeout(() => {
      const input = document.querySelector(`input[data-row-index="${newIndex}"]`) as HTMLInputElement;
      if (input) {
        input.focus();
        // Trigger search to show dropdown
        this.searchDenominationsForDetail('', newIndex);
      }
    }, 100);
  }

  /**
   * Edit invoice detail inline
   */
  editInvoiceDetailInline(index: number): void {
    this.editingDetailIndex.set(index);
    const detail = this.invoiceDetails()[index];
    const map = new Map(this.inlineEditingDetails());
    // Check if denomination has operating physician
    const hasOperatingPhysician = this.checkDenominationHasOperatingPhysician(detail.denomination);

    map.set(index, {
      denominationSearchQuery: detail.denominationDescription || '',
      searchResults: [],
      showDropdown: false,
      selectedDropdownIndex: -1,
      quantity: detail.quantity,
      unitPrice: detail.unitPrice,
      discount: detail.discount || 0,
      denominationId: detail.denomination,
      denominationCode: detail.denominationCode,
      denominationDescription: detail.denominationDescription,
      hasOperatingPhysician: hasOperatingPhysician,
      operatingPhysician: detail.operatingPhysician || 0,
      operatingPhysicianName: '',
      operatingPhysicianOptions: [],
      showOperatingPhysicianDropdown: false
    });
    // Load operating physician name if exists
    if (detail.operatingPhysician && detail.operatingPhysician > 0) {
      // Try to get name from referral/attending physician first
      let physicianName = '';
      if (this.referralPhysicianId() === detail.operatingPhysician) {
        physicianName = this.referralPhysician();
      } else if (this.attendingPhysicianId() === detail.operatingPhysician) {
        physicianName = this.attendingPhysician();
      }

      if (physicianName) {
        const editingDetail = map.get(index);
        if (editingDetail) {
          editingDetail.operatingPhysicianName = physicianName;
          map.set(index, editingDetail);
        }
      } else {
        // Fallback to search using the same search method as referral physician
        this.searchPhysicianForOperatingPosition(detail.operatingPhysician, index);
      }
    }

    this.inlineEditingDetails.set(map);
  }

  /**
   * Save invoice detail inline (auto-save in always-editable mode)
   */
  saveInvoiceDetailInline(index: number): void {
    const editingDetail = this.getEditingDetailForRow(index);

    // Don't save if denomination is not set yet (allow empty state)
    if (!editingDetail.denominationId && editingDetail.denominationSearchQuery.length === 0) {
      return;
    }

    // Validate if denomination is set
    if (editingDetail.denominationId && (editingDetail.quantity <= 0 || editingDetail.unitPrice < 0)) {
      // Don't show alert on every keystroke, just return
      return;
    }

    const details = [...this.invoiceDetails()];
    if (index >= details.length) {
      return; // Index out of bounds
    }

    const detail = details[index];

    const netPrice = (editingDetail.quantity * editingDetail.unitPrice) - (editingDetail.discount || 0);

    // Get operating physician ID - use the one from editing detail, or default to referral physician if hasOperatingPhysician is true
    let operatingPhysicianId = editingDetail.operatingPhysician || 0;
    if (editingDetail.hasOperatingPhysician && (!operatingPhysicianId || operatingPhysicianId === 0)) {
      operatingPhysicianId = this.referralPhysicianId() || 0;
    }

    details[index] = {
      ...detail,
      denomination: editingDetail.denominationId || detail.denomination,
      denominationCode: editingDetail.denominationCode || detail.denominationCode || '',
      denominationDescription: editingDetail.denominationDescription || detail.denominationDescription || '',
      quantity: editingDetail.quantity,
      unitPrice: editingDetail.unitPrice,
      discount: editingDetail.discount || 0,
      netPrice: netPrice,
      netUnitPrice: editingDetail.unitPrice - (editingDetail.discount || 0),
      operatingPhysician: operatingPhysicianId
    };

    this.invoiceDetails.set(details);

    // Keep editing state - don't clear it in always-editable mode
  }

  /**
   * Cancel edit detail
   */
  cancelEditDetail(): void {
    const index = this.editingDetailIndex();
    if (index !== null && index >= this.invoiceDetails().length) {
      // If it's a new item, remove it
      const details = [...this.invoiceDetails()];
      details.splice(index, 1);
      this.invoiceDetails.set(details);
    }
    this.editingDetailIndex.set(null);
  }

  /**
   * Delete invoice detail
   */
  deleteInvoiceDetail(index: number): void {
    if (confirm('Are you sure you want to delete this item?')) {
      const details = [...this.invoiceDetails()];
      details.splice(index, 1);
      this.invoiceDetails.set(details);

      // Clear inline editing state if this was being edited
      if (this.editingDetailIndex() === index) {
        this.editingDetailIndex.set(null);
      }
      const map = new Map(this.inlineEditingDetails());
      map.delete(index);
      this.inlineEditingDetails.set(map);
    }
  }

  /**
   * Calculate line total for invoice detail
   */
  calculateLineTotal(detail: BillingInvoiceDetail): number {
    const subtotal = detail.quantity * detail.unitPrice;
    const discount = detail.discount || 0;
    return subtotal - discount;
  }

  /**
   * Calculate total for editing detail
   */
  calculateDetailTotal(index: number): number {
    const editingDetail = this.getEditingDetail();
    const subtotal = editingDetail.quantity * editingDetail.unitPrice;
    const discount = editingDetail.discount || 0;
    return subtotal - discount;
  }

  /**
   * Update detail total when fields change
   */
  updateDetailTotal(index: number): void {
    // This will trigger recalculation in the template
  }

  /**
   * Update editing detail field
   */
  updateEditingDetailField(field: string, value: any): void {
    const index = this.editingDetailIndex();
    if (index === null) return;

    const map = new Map(this.inlineEditingDetails());
    const editingDetail = map.get(index) || this.getEditingDetail();
    (editingDetail as any)[field] = value;
    map.set(index, editingDetail);
    this.inlineEditingDetails.set(map);
  }

  /**
   * Search denominations for inline detail
   */
  searchDenominationsForDetail(query: string, index: number): void {
    const costCenterFilter = this.getCostCenterFilter();

    this.billingService.searchDenominationsAdvanced(
      query && query.trim().length > 0 ? query.trim() : undefined,
      5, // Default insurance ID
      costCenterFilter || undefined
    ).subscribe({
      next: (results) => {
        const map = new Map(this.inlineEditingDetails());
        const editingDetail = map.get(index) || this.getEditingDetailForRow(index);
        editingDetail.searchResults = results.slice(0, 50);
        this.resolveMissingCodesForResults(editingDetail.searchResults);
        editingDetail.showDropdown = results.length > 0;
        // Reset selected index when search results change
        editingDetail.selectedDropdownIndex = results.length > 0 ? 0 : -1;
        map.set(index, editingDetail);
        this.inlineEditingDetails.set(map);
      },
      error: (error) => {
        console.error('❌ Error searching denominations:', error);
        const map = new Map(this.inlineEditingDetails());
        const editingDetail = map.get(index) || this.getEditingDetailForRow(index);
        editingDetail.searchResults = [];
        editingDetail.showDropdown = false;
        editingDetail.selectedDropdownIndex = -1;
        map.set(index, editingDetail);
        this.inlineEditingDetails.set(map);
      }
    });
  }

  /**
   * Handle denomination input focus for inline detail
   */
  onDenominationInputFocusForDetail(index: number): void {
    const map = new Map(this.inlineEditingDetails());
    const editingDetail = map.get(index) || this.getEditingDetailForRow(index);
    const query = editingDetail.denominationSearchQuery;

    // Only show dropdown on focus if it's not already visible (don't interfere with toggle)
    if (!editingDetail.showDropdown) {
      editingDetail.showDropdown = true;
      map.set(index, editingDetail);
      this.inlineEditingDetails.set(map);
      // Trigger search on focus to show dropdown
      this.searchDenominationsForDetail(query || '', index);
    }
  }

  /**
   * Handle keydown events on denomination input for inline detail
   */
  onDenominationKeyDownForDetail(event: KeyboardEvent, index: number): void {
    const editingDetail = this.getEditingDetailForRow(index);

    if (event.key === 'Enter') {
      event.preventDefault();
      event.stopPropagation();

      // If dropdown is visible and has results
      if (editingDetail.showDropdown && editingDetail.searchResults.length > 0) {
        // Use selected index or first item
        const selectedIndex = editingDetail.selectedDropdownIndex >= 0
          ? editingDetail.selectedDropdownIndex
          : 0;
        const selectedResult = editingDetail.searchResults[selectedIndex];

        if (selectedResult) {
          // Select denomination and create new row
          this.selectDenominationForDetail(selectedResult, index, true);
        }
      } else if (editingDetail.denominationId) {
        // If denomination is already selected, just create new row
        this.updateDetailTotalForRow(index);
        setTimeout(() => {
          this.addInvoiceDetailInline();
        }, 50);
      }
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (editingDetail.showDropdown && editingDetail.searchResults.length > 0) {
        const currentIndex = editingDetail.selectedDropdownIndex ?? -1;
        const newIndex = currentIndex < editingDetail.searchResults.length - 1
          ? currentIndex + 1
          : currentIndex;
        this.updateEditingDetailFieldForRow('selectedDropdownIndex', newIndex, index);
        // Scroll to selected item
        this.scrollToSelectedDenomination(index);
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (editingDetail.showDropdown && editingDetail.searchResults.length > 0) {
        const currentIndex = editingDetail.selectedDropdownIndex ?? -1;
        const newIndex = currentIndex > 0
          ? currentIndex - 1
          : 0;
        this.updateEditingDetailFieldForRow('selectedDropdownIndex', newIndex, index);
        // Scroll to selected item
        this.scrollToSelectedDenomination(index);
      }
    } else if (event.key === 'Tab' && !event.shiftKey) {
      // Tab key - move to quantity field
      event.preventDefault();
      const rowElement = document.querySelector(`input[data-row-index="${index}"]`)?.closest('tr');
      const quantityInput = rowElement?.querySelector('input[type="number"]') as HTMLInputElement;
      if (quantityInput) {
        quantityInput.focus();
        quantityInput.select();
      }
    } else if (event.key === 'Escape') {
      // Close dropdown on Escape
      this.updateEditingDetailFieldForRow('showDropdown', false, index);
      this.updateEditingDetailFieldForRow('selectedDropdownIndex', -1, index);
    }
  }

  /**
   * Scroll to selected denomination item in dropdown
   */
  scrollToSelectedDenomination(index: number): void {
    setTimeout(() => {
      const rowElement = document.querySelector(`input[data-row-index="${index}"]`)?.closest('tr');
      const dropdown = rowElement?.nextElementSibling?.querySelector('.denomination-dropdown-fullwidth') as HTMLElement;
      if (dropdown) {
        const activeItem = dropdown.querySelector('.dropdown-item.active') as HTMLElement;
        if (activeItem) {
          activeItem.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
      }
    }, 0);
  }

  /**
   * Handle Tab key on quantity input
   */
  onQuantityTab(event: KeyboardEvent, index: number): void {
    // Allow default Tab behavior to move to next field
    // Don't prevent default
  }

  /**
   * Select denomination for inline detail
   * @param createNewRow - If true, creates a new row after selection
   */
  selectDenominationForDetail(result: DenominationSearchResult, index: number, createNewRow: boolean = false): void {
    const map = new Map(this.inlineEditingDetails());
    const editingDetail = map.get(index) || this.getEditingDetailForRow(index);
    const fallbackCode = result.actName || '';
    const initialCode = this.getDenominationCodeFromSearchResult(result) || fallbackCode;

    editingDetail.denominationId = result.denId;
    editingDetail.denominationCode = initialCode;
    editingDetail.denominationDescription = result.actName || '';
    editingDetail.unitPrice = result.priceUsd ?? 0;
    editingDetail.denominationSearchQuery = this.formatDenominationDisplay(initialCode, result.actName || '');
    editingDetail.showDropdown = false;
    editingDetail.selectedDropdownIndex = -1;

    // Set hasOperatingPhysician based on the selected denomination
    editingDetail.hasOperatingPhysician = result.hasOperatingPhysician === true || (result.hasOperatingPhysician as any) === 1;

    // Set default operating physician to referral physician if hasOperatingPhysician is true
    if (editingDetail.hasOperatingPhysician) {
      // Only set if not already set
      if (!editingDetail.operatingPhysician || editingDetail.operatingPhysician === 0) {
        const referralPhysicianId = this.referralPhysicianId();
        if (referralPhysicianId) {
          editingDetail.operatingPhysician = referralPhysicianId;
          editingDetail.operatingPhysicianName = this.referralPhysician() || '';
        }
      }
    } else {
      // If denomination doesn't require operating physician, clear it
      editingDetail.operatingPhysician = 0;
      editingDetail.operatingPhysicianName = '';
    }

    map.set(index, editingDetail);
    this.inlineEditingDetails.set(map);

    this.resolveDenominationCode(result, fallbackCode, (resolvedCode) => {
      const updatedMap = new Map(this.inlineEditingDetails());
      const updatedDetail = updatedMap.get(index) || editingDetail;
      updatedDetail.denominationCode = resolvedCode;
      updatedDetail.denominationSearchQuery = this.formatDenominationDisplay(resolvedCode, result.actName || '');
      updatedMap.set(index, updatedDetail);
      this.inlineEditingDetails.set(updatedMap);

      // Save the current row after denomination code is resolved
      this.saveInvoiceDetailInline(index);

      // If createNewRow is true, create a new row and focus on it
      if (createNewRow) {
        setTimeout(() => {
          this.addInvoiceDetailInline();
        }, 50);
      }
    });
  }

  /**
   * Add consultation for clinic department
   */
  addConsultation(): void {
    if (!this.department() || this.department().toLowerCase() !== 'clinic') {
      return;
    }

    if (!this.referralPhysicianId() && !this.attendingPhysicianId()) {
      alert('Please select a physician (Referral or Attending) before adding consultation.');
      return;
    }

    // Search for consultation denomination
    const costCenterFilter = this.getCostCenterFilter();
    this.billingService.searchDenominationsAdvanced(
      'consultation',
      5,
      costCenterFilter || undefined
    ).subscribe({
      next: (results) => {
        // Find consultation denomination (you may need to adjust this logic)
        const consultation = results.find(r =>
          r.actName?.toLowerCase().includes('consultation') ||
          r.actName?.toLowerCase().includes('consult')
        ) || results[0];

        if (!consultation) {
          alert('Consultation denomination not found. Please add it manually.');
          return;
        }

        const newDetail: BillingInvoiceDetail = {
          id: Date.now(),
          prescriptionDate: undefined,
          prescribedBy: this.referralPhysicianId() || this.attendingPhysicianId() || undefined,
          medicationUnit: 113,
          medicationUnitDescription: 'Clinics',
          admission: 0,
          patient: 0,
          denomination: consultation.denId,
          denominationCode: this.getDenominationCodeFromSearchResult(consultation) || consultation.actName || '',
          denominationDescription: consultation.actName || '',
          denominationCoeffCode: '',
          denominationCoeffValue: consultation.coefficientValue || 1,
          denominationCoeffPrice: consultation.priceUsd || 0,
          quantity: 1,
          unitPrice: consultation.priceUsd || 0,
          netPrice: consultation.priceUsd || 0,
          netUnitPrice: consultation.priceUsd || 0,
          differenceAmount: 0,
          deniedAmount: 0,
          discount: 0,
          lumpSum: 0,
          complementaryAmount: 0,
          complementaryAmountOtherCurrency: 0,
          complementaryDifferenceOtherCurrency: 0,
          operatingPhysician: consultation.hasOperatingPhysician ? (this.referralPhysicianId() || this.attendingPhysicianId() || 0) : 0,
          isMedicalResultOk: undefined,
          medicalResultDate: undefined,
          requireApproval: 0,
          approvalReference: undefined,
          approvalDate: undefined,
          isDenied: 0,
          approvedBy: undefined,
          dueDate: undefined,
          executionDate: undefined,
          invoiceHeader: 0,
          referralPhysician: this.referralPhysicianId() || 0,
          costCenter: consultation.costCenterId,
          profitCenter: 3,
          pacIndex: undefined,
          preInvoiceDetail: undefined,
          detailDate: new Date(),
          mainDetailId: undefined,
          copyFlag: 0,
          detailDateHelper: undefined,
          isDoubtfull: 0,
          procedure: undefined,
          isDeleted: 0,
          createdBy: 298,
          modifiedBy: undefined,
          createdDate: new Date(),
          modifiedDate: undefined,
          previousDetailId: undefined,
          orderDetailSequenceNumber: this.invoiceDetails().length + 1,
          source: 'O',
          isCanceled: 0,
          cancelComment: undefined,
          oldOrderDetailSequenceNumber: undefined,
          isApproved: undefined,
          invoiceNumber: undefined,
          patientAmount: undefined
        };

        this.resolveDenominationCode(consultation, consultation.actName || '', (resolvedCode) => {
          newDetail.denominationCode = resolvedCode;
          this.invoiceDetails.set([...this.invoiceDetails(), newDetail]);
          console.log('✅ Consultation added:', newDetail);
        });
      },
      error: (error) => {
        console.error('❌ Error adding consultation:', error);
        alert('Error adding consultation. Please try again.');
      }
    });
  }

  private getDenominationCodeFromSearchResult(result: DenominationSearchResult): string {
    return (result.actCode || result.ActCode || result.code || '').trim();
  }

  getDenominationDisplayForResult(result: DenominationSearchResult): string {
    const directCode = this.getDenominationCodeFromSearchResult(result);
    if (directCode && result.denId > 0) {
      this.cacheDenominationCode(result.denId, directCode);
    }
    const resolvedCode = directCode || this.getCachedDenominationCode(result.denId);
    return this.formatDenominationDisplay(resolvedCode, result.actName || '');
  }

  formatDenominationDisplay(code: string | null | undefined, description: string | null | undefined): string {
    const safeCode = (code || '').trim();
    const safeDescription = (description || '').trim();
    if (safeCode && safeDescription) {
      return `${safeCode} - ${safeDescription}`;
    }
    return safeCode || safeDescription;
  }

  private resolveDenominationCode(
    result: DenominationSearchResult,
    fallbackCode: string,
    callback: (code: string) => void
  ): void {
    const directCode = this.getDenominationCodeFromSearchResult(result);
    if (directCode) {
      callback(directCode);
      return;
    }

    if (!result.denId || result.denId <= 0) {
      callback(fallbackCode);
      return;
    }

    this.billingService.getDenomination(result.denId).subscribe({
      next: (denomination) => {
        const resolvedCode = (denomination?.code || fallbackCode || '').trim();
        if (result.denId > 0 && resolvedCode) {
          this.cacheDenominationCode(result.denId, resolvedCode);
        }
        callback(resolvedCode);
      },
      error: (error) => {
        console.warn('⚠️ Could not resolve denomination code from Denomination endpoint:', error);
        callback(fallbackCode);
      }
    });
  }

  private resolveMissingCodesForResults(results: DenominationSearchResult[]): void {
    for (const result of results) {
      if (!result?.denId || result.denId <= 0) {
        continue;
      }

      const directCode = this.getDenominationCodeFromSearchResult(result);
      if (directCode) {
        this.cacheDenominationCode(result.denId, directCode);
        continue;
      }

      if (this.getCachedDenominationCode(result.denId) || this.denominationCodeFetchInProgress.has(result.denId)) {
        continue;
      }

      this.denominationCodeFetchInProgress.add(result.denId);
      this.billingService.getDenomination(result.denId).subscribe({
        next: (denomination) => {
          const resolvedCode = (denomination?.code || '').trim();
          if (resolvedCode) {
            this.cacheDenominationCode(result.denId, resolvedCode);
          }
          this.denominationCodeFetchInProgress.delete(result.denId);
        },
        error: () => {
          this.denominationCodeFetchInProgress.delete(result.denId);
        }
      });
    }
  }

  private getCachedDenominationCode(denId: number): string {
    if (!denId || denId <= 0) {
      return '';
    }
    return this.denominationCodeById().get(denId) || '';
  }

  private cacheDenominationCode(denId: number, code: string): void {
    const normalizedCode = (code || '').trim();
    if (!denId || denId <= 0 || !normalizedCode) {
      return;
    }

    const currentMap = this.denominationCodeById();
    if (currentMap.get(denId) === normalizedCode) {
      return;
    }

    const nextMap = new Map(currentMap);
    nextMap.set(denId, normalizedCode);
    this.denominationCodeById.set(nextMap);
  }

  /**
   * Format currency for display
   */
  formatCurrency(amount: number): string {
    return `$$${amount.toFixed(2)}`;
  }

  /**
   * Truncate text to specified length with ellipsis
   */
  truncateText(text: string | null | undefined, maxLength: number = 60): string {
    if (!text) return '';
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
  }

  /**
   * Check if denomination has operating physician
   */
  checkDenominationHasOperatingPhysician(denominationId: number): boolean {
    // Check in the search results first
    const editingDetail = this.getEditingDetail();
    if (editingDetail.searchResults && editingDetail.searchResults.length > 0) {
      const result = editingDetail.searchResults.find((r: DenominationSearchResult) => r.denId === denominationId);
      if (result) {
        return result.hasOperatingPhysician === true || (result.hasOperatingPhysician as any) === 1;
      }
    }
    // Check in invoice details if we have the denomination
    const detail = this.invoiceDetails().find(d => d.denomination === denominationId);
    if (detail) {
      // We need to check the denomination search result for this
      // For now, return false if we can't determine
      return false;
    }
    return false;
  }

  /**
   * Get if detail has operating physician
   */
  getDetailHasOperatingPhysician(index: number): boolean {
    const detail = this.invoiceDetails()[index];
    if (!detail || !detail.denomination) return false;

    // Check in editing detail if it's being edited
    if (this.editingDetailIndex() === index) {
      const editingDetail = this.getEditingDetail();
      return editingDetail.hasOperatingPhysician === true;
    }

    // Otherwise check by denomination ID
    return this.checkDenominationHasOperatingPhysician(detail.denomination);
  }

  /**
   * Get operating physician name
   */
  getOperatingPhysicianName(physicianId: number): string {
    if (!physicianId || physicianId === 0) return '';
    // Try to find in physician options first
    const physician = this.physicianOptions().find(p => p.id === physicianId);
    if (physician?.name) return physician.name;

    // If not found, we need to load it - but for display purposes, we'll return empty
    // The name should be loaded when the detail is loaded
    return '';
  }

  /**
   * Get operating physician name from detail (checks if name is stored in detail)
   */
  getOperatingPhysicianNameFromDetail(detail: BillingInvoiceDetail): string {
    if (!detail.operatingPhysician || detail.operatingPhysician === 0) return '';

    // First check if we have the referral physician ID and it matches
    if (this.referralPhysicianId() === detail.operatingPhysician && this.referralPhysician()) {
      return this.referralPhysician();
    }

    // Check if we have the attending physician ID and it matches
    if (this.attendingPhysicianId() === detail.operatingPhysician && this.attendingPhysician()) {
      return this.attendingPhysician();
    }

    // Try to find in physician options (same as referral physician dropdown)
    const physician = this.physicianOptions().find(p => p.id === detail.operatingPhysician);
    if (physician?.name) return physician.name;

    return '';
  }

  /**
   * Search operating physician (uses same search as referral physician)
   */
  searchOperatingPhysician(query: string, index: number): void {
    const map = new Map(this.inlineEditingDetails());
    const editingDetail = map.get(index) || this.getEditingDetail();

    editingDetail.operatingPhysicianName = query;
    map.set(index, editingDetail);
    this.inlineEditingDetails.set(map);

    if (!query || query.trim().length < 2) {
      editingDetail.showOperatingPhysicianDropdown = false;
      editingDetail.operatingPhysicianOptions = [];
      map.set(index, editingDetail);
      this.inlineEditingDetails.set(map);
      return;
    }

    // Use the exact same physician search method as referral physician
    const url = `${this.apiUrl}/Physician/search?query=${encodeURIComponent(query.trim())}`;
    this.http.get<any[]>(url).subscribe({
      next: (physicians) => {
        const map = new Map(this.inlineEditingDetails());
        const editingDetail = map.get(index) || this.getEditingDetail();
        editingDetail.operatingPhysicianOptions = physicians.slice(0, 20);
        editingDetail.showOperatingPhysicianDropdown = physicians.length > 0;
        map.set(index, editingDetail);
        this.inlineEditingDetails.set(map);
      },
      error: (error) => {
        console.error('Error searching physicians:', error);
        const map = new Map(this.inlineEditingDetails());
        const editingDetail = map.get(index) || this.getEditingDetail();
        editingDetail.operatingPhysicianOptions = [];
        editingDetail.showOperatingPhysicianDropdown = false;
        map.set(index, editingDetail);
        this.inlineEditingDetails.set(map);
      }
    });
  }

  /**
   * Handle operating physician input focus
   */
  onOperatingPhysicianFocus(index: number): void {
    const map = new Map(this.inlineEditingDetails());
    const editingDetail = map.get(index) || this.getEditingDetailForRow(index);

    // Only show dropdown on focus if it's not already visible (don't interfere with toggle)
    if (!editingDetail.showOperatingPhysicianDropdown) {
      // Trigger search if there's text, or show dropdown if empty
      if (editingDetail.operatingPhysicianName && editingDetail.operatingPhysicianName.length >= 2) {
        this.searchOperatingPhysician(editingDetail.operatingPhysicianName, index);
      } else {
        // If empty, trigger a search with empty string to populate options from referral physician
        // This ensures the dropdown has data from the same source
        this.searchPhysician('');
        setTimeout(() => {
          const map = new Map(this.inlineEditingDetails());
          const editingDetail = map.get(index) || this.getEditingDetailForRow(index);
          const physicians = this.physicianOptions();
          editingDetail.operatingPhysicianOptions = physicians.slice(0, 20);
          editingDetail.showOperatingPhysicianDropdown = physicians.length > 0;
          editingDetail.selectedOperatingPhysicianIndex = physicians.length > 0 ? 0 : -1;
          map.set(index, editingDetail);
          this.inlineEditingDetails.set(map);
        }, 100);
      }
    }
  }

  /**
   * Select operating physician
   */
  selectOperatingPhysician(physician: any, index: number): void {
    const map = new Map(this.inlineEditingDetails());
    const editingDetail = map.get(index) || this.getEditingDetailForRow(index);

    editingDetail.operatingPhysician = physician.id;
    editingDetail.operatingPhysicianName = physician.name;
    editingDetail.showOperatingPhysicianDropdown = false;
    editingDetail.selectedOperatingPhysicianIndex = -1;

    map.set(index, editingDetail);
    this.inlineEditingDetails.set(map);

    // Update the invoiceDetails array
    this.updateDetailTotalForRow(index);
  }

  /**
   * Handle keyboard navigation for Operating Physician autocomplete in grid
   */
  onOperatingPhysicianKeyDown(event: KeyboardEvent, index: number): void {
    const editingDetail = this.getEditingDetailForRow(index);
    if (!editingDetail.hasOperatingPhysician) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (editingDetail.showOperatingPhysicianDropdown && editingDetail.operatingPhysicianOptions && editingDetail.operatingPhysicianOptions.length > 0) {
        const currentIndex = editingDetail.selectedOperatingPhysicianIndex ?? -1;
        const newIndex = currentIndex < editingDetail.operatingPhysicianOptions.length - 1
          ? currentIndex + 1
          : currentIndex;
        this.updateEditingDetailFieldForRow('selectedOperatingPhysicianIndex', newIndex, index);
        this.scrollToSelectedOperatingPhysician(index);
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (editingDetail.showOperatingPhysicianDropdown && editingDetail.operatingPhysicianOptions && editingDetail.operatingPhysicianOptions.length > 0) {
        const currentIndex = editingDetail.selectedOperatingPhysicianIndex ?? -1;
        const newIndex = currentIndex > 0
          ? currentIndex - 1
          : 0;
        this.updateEditingDetailFieldForRow('selectedOperatingPhysicianIndex', newIndex, index);
        this.scrollToSelectedOperatingPhysician(index);
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (editingDetail.showOperatingPhysicianDropdown && editingDetail.operatingPhysicianOptions && editingDetail.operatingPhysicianOptions.length > 0) {
        const currentIndex = editingDetail.selectedOperatingPhysicianIndex ?? -1;
        const selectedIndex = currentIndex >= 0 ? currentIndex : 0;
        const selectedOption = editingDetail.operatingPhysicianOptions[selectedIndex];
        if (selectedOption) {
          this.selectOperatingPhysician(selectedOption, index);
        }
      }
    } else if (event.key === 'Escape') {
      this.updateEditingDetailFieldForRow('showOperatingPhysicianDropdown', false, index);
      this.updateEditingDetailFieldForRow('selectedOperatingPhysicianIndex', -1, index);
    }
  }

  /**
   * Scroll to selected operating physician item
   */
  scrollToSelectedOperatingPhysician(index: number): void {
    setTimeout(() => {
      const rowElement = document.querySelector(`input[data-row-index="${index}"]`)?.closest('tr');
      const dropdown = rowElement?.querySelector('.dropdown-menu.show');
      if (dropdown) {
        const activeItem = dropdown.querySelector('.dropdown-item.active') as HTMLElement;
        if (activeItem) {
          activeItem.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
      }
    }, 0);
  }

  /**
   * Load operating physician name
   */
  loadOperatingPhysicianName(physicianId: number, index: number): void {
    // Search for the physician by ID - we'll need to search by name or use a different approach
    // For now, we'll search with a common query and find the matching ID
    this.http.get<any[]>(`${this.apiUrl}/Physician/search`, {
      params: { query: '' }
    }).subscribe({
      next: (physicians) => {
        const physician = physicians.find(p => p.id === physicianId);
        if (physician) {
          const map = new Map(this.inlineEditingDetails());
          const editingDetail = map.get(index) || this.getEditingDetailForRow(index);
          editingDetail.operatingPhysicianName = physician.name || '';
          map.set(index, editingDetail);
          this.inlineEditingDetails.set(map);
        }
      },
      error: (error) => {
        console.error('Error loading physician:', error);
      }
    });
  }

  /**
   * Load operating physician name for editing (when setting default)
   */
  loadOperatingPhysicianNameForEditing(physicianId: number, index: number): void {
    // Use the referral physician name if available, otherwise search
    const referralPhysicianName = this.referralPhysician();
    if (referralPhysicianName) {
      const map = new Map(this.inlineEditingDetails());
      const editingDetail = map.get(index) || this.getEditingDetail();
      editingDetail.operatingPhysicianName = referralPhysicianName;
      map.set(index, editingDetail);
      this.inlineEditingDetails.set(map);
    } else {
      // Fallback to search using the same search method as referral physician
      this.searchPhysicianForOperatingPosition(physicianId, index);
    }
  }

  /**
   * Search for physician by ID (for operating position) using the same method as referral physician
   */
  searchPhysicianForOperatingPosition(physicianId: number, index: number): void {
    // We need to search physicians - but we can't search by ID directly
    // Instead, we'll try to find it in the physicianOptions if available
    const physician = this.physicianOptions().find(p => p.id === physicianId);
    if (physician) {
      const map = new Map(this.inlineEditingDetails());
      const editingDetail = map.get(index) || this.getEditingDetail();
      editingDetail.operatingPhysicianName = physician.name || '';
      map.set(index, editingDetail);
      this.inlineEditingDetails.set(map);
    } else {
      // If not in options, we can't easily get the name by ID without a new API endpoint
      // For now, leave it empty and let the user search
      console.log('Physician ID not found in options:', physicianId);
    }
  }

  /**
   * Add new invoice item
   */
  addInvoiceItem(): void {
    console.log('🔄 Adding invoice item...', this.newInvoiceDetail());

    // Check if department is selected
    if (!this.department()) {
      alert('Please select a Department before adding invoice items.');
      return;
    }

    const detailData = this.newInvoiceDetail();

    if (!detailData.denomination || detailData.denomination === 0 || !detailData.denominationDescription || (detailData.quantity ?? 0) <= 0 || (detailData.unitPrice ?? 0) < 0) {
      console.error('❌ Please select a denomination and fill in all required fields with valid values');
      alert('Please select a denomination/service and ensure quantity and unit price are valid.');
      return;
    }

    // Calculate net price and other values
    const netPrice = ((detailData.quantity ?? 0) * (detailData.unitPrice ?? 0)) - (detailData.discount || 0) + (detailData.lumpSum || 0);
    const netUnitPrice = (detailData.unitPrice ?? 0) - (detailData.discount || 0) + (detailData.lumpSum || 0);

    // For now, we'll add to local array since we don't have an invoice header yet
    const newDetail: BillingInvoiceDetail = {
      id: Date.now(), // Temporary ID
      prescriptionDate: undefined,
      prescribedBy: undefined,
      medicationUnit: 113, // Default to Clinics
      medicationUnitDescription: 'Clinics',
      admission: 0, // Will be set when saving
      patient: 0, // Will be set when saving
      denomination: detailData.denomination || 0,
      denominationCode: detailData.denominationCode || '',
      denominationDescription: detailData.denominationDescription || '',
      denominationCoeffCode: detailData.denominationCoeffCode || '',
      denominationCoeffValue: detailData.denominationCoeffValue || 1,
      denominationCoeffPrice: detailData.denominationCoeffPrice || 0,
      quantity: detailData.quantity || 1,
      unitPrice: detailData.unitPrice || 0,
      netPrice: netPrice,
      netUnitPrice: netUnitPrice,
      differenceAmount: 0,
      deniedAmount: 0,
      discount: detailData.discount || 0,
      lumpSum: detailData.lumpSum || 0,
      complementaryAmount: 0,
      complementaryAmountOtherCurrency: 0,
      complementaryDifferenceOtherCurrency: 0,
      operatingPhysician: 0,
      isMedicalResultOk: undefined,
      medicalResultDate: undefined,
      requireApproval: 0,
      approvalReference: undefined,
      approvalDate: undefined,
      isDenied: 0,
      approvedBy: undefined,
      dueDate: undefined,
      executionDate: undefined,
      invoiceHeader: 0, // Will be set when saving
      referralPhysician: 0,
      costCenter: 12,
      profitCenter: 3,
      pacIndex: undefined,
      preInvoiceDetail: undefined,
      detailDate: new Date(),
      mainDetailId: undefined,
      copyFlag: 0,
      detailDateHelper: undefined,
      isDoubtfull: 0,
      procedure: undefined,
      isDeleted: 0,
      createdBy: 298, // Default user
      modifiedBy: undefined,
      createdDate: new Date(),
      modifiedDate: undefined,
      previousDetailId: undefined,
      orderDetailSequenceNumber: this.invoiceDetails().length + 1,
      source: 'O',
      isCanceled: 0,
      cancelComment: undefined,
      oldOrderDetailSequenceNumber: undefined,
      isApproved: undefined,
      invoiceNumber: undefined,
      patientAmount: undefined
    };

    console.log('✅ Adding new detail:', newDetail);
    this.invoiceDetails.set([...this.invoiceDetails(), newDetail]);
    console.log('📊 Current invoice details:', this.invoiceDetails());
    this.resetInvoiceDetailForm();
  }

  /**
   * Edit invoice item
   */
  editInvoiceItem(detail: BillingInvoiceDetail): void {
    this.editingInvoiceDetail.set(detail);
    this.newInvoiceDetail.set({
      denomination: detail.denomination,
      denominationCode: detail.denominationCode,
      denominationDescription: detail.denominationDescription,
      quantity: detail.quantity,
      unitPrice: detail.unitPrice,
      discount: detail.discount,
      lumpSum: detail.lumpSum,
      denominationCoeffCode: detail.denominationCoeffCode,
      denominationCoeffValue: detail.denominationCoeffValue,
      denominationCoeffPrice: detail.denominationCoeffPrice
    });
    this.showInvoiceDetailForm.set(true);
  }

  /**
   * Update invoice item
   */
  updateInvoiceItem(): void {
    const detail = this.editingInvoiceDetail();
    const detailData = this.newInvoiceDetail();

    if (!detail || !detailData.denominationDescription || (detailData.quantity ?? 0) <= 0 || (detailData.unitPrice ?? 0) < 0) {
      console.error('Please fill in all required fields with valid values');
      return;
    }

    // Calculate net price and other values
    const netPrice = ((detailData.quantity ?? 0) * (detailData.unitPrice ?? 0)) - (detailData.discount || 0) + (detailData.lumpSum || 0);
    const netUnitPrice = (detailData.unitPrice ?? 0) - (detailData.discount || 0) + (detailData.lumpSum || 0);

    const updatedDetail: BillingInvoiceDetail = {
      ...detail,
      denomination: detailData.denomination || 0,
      denominationCode: detailData.denominationCode || '',
      denominationDescription: detailData.denominationDescription || '',
      denominationCoeffCode: detailData.denominationCoeffCode || '',
      denominationCoeffValue: detailData.denominationCoeffValue || 1,
      denominationCoeffPrice: detailData.denominationCoeffPrice || 0,
      quantity: detailData.quantity || 1,
      unitPrice: detailData.unitPrice || 0,
      netPrice: netPrice,
      netUnitPrice: netUnitPrice,
      discount: detailData.discount || 0,
      lumpSum: detailData.lumpSum || 0,
      modifiedDate: new Date()
    };

    const details = this.invoiceDetails().map(d => d.id === updatedDetail.id ? updatedDetail : d);
    this.invoiceDetails.set(details);
    this.resetInvoiceDetailForm();
  }

  /**
   * Delete invoice item
   */
  deleteInvoiceItem(detail: BillingInvoiceDetail): void {
    if (confirm('Are you sure you want to delete this item?')) {
      const details = this.invoiceDetails().filter(d => d.id !== detail.id);
      this.invoiceDetails.set(details);
    }
  }

  /**
   * Reset invoice detail form
   */
  resetInvoiceDetailForm(): void {
    this.denominationSearchQuery.set('');
    this.showDenominationDropdown = false;
    this.newInvoiceDetail.set({
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
    this.editingInvoiceDetail.set(null);
    this.showInvoiceDetailForm.set(false);
  }


  /**
   * Add a test item for debugging
   */
  addTestItem(): void {
    console.log('🧪 Adding test item...');
    const testDetail: BillingInvoiceDetail = {
      id: Date.now(),
      prescriptionDate: undefined,
      prescribedBy: undefined,
      medicationUnit: 113,
      medicationUnitDescription: 'Clinics',
      admission: 0,
      patient: 0,
      denomination: 999,
      denominationCode: 'TEST001',
      denominationDescription: 'Test Lab Service',
      denominationCoeffCode: 'TEST',
      denominationCoeffValue: 1,
      denominationCoeffPrice: 25.00,
      quantity: 1,
      unitPrice: 25.00,
      netPrice: 25.00,
      netUnitPrice: 25.00,
      differenceAmount: 0,
      deniedAmount: 0,
      discount: 0,
      lumpSum: 0,
      complementaryAmount: 0,
      complementaryAmountOtherCurrency: 0,
      complementaryDifferenceOtherCurrency: 0,
      operatingPhysician: 0,
      isMedicalResultOk: undefined,
      medicalResultDate: undefined,
      requireApproval: 0,
      approvalReference: undefined,
      approvalDate: undefined,
      isDenied: 0,
      approvedBy: undefined,
      dueDate: undefined,
      executionDate: undefined,
      invoiceHeader: 0,
      referralPhysician: 0,
      costCenter: 12,
      profitCenter: 3,
      pacIndex: undefined,
      preInvoiceDetail: undefined,
      detailDate: new Date(),
      mainDetailId: undefined,
      copyFlag: 0,
      detailDateHelper: undefined,
      isDoubtfull: 0,
      procedure: undefined,
      isDeleted: 0,
      createdBy: 298,
      modifiedBy: undefined,
      createdDate: new Date(),
      modifiedDate: undefined,
      previousDetailId: undefined,
      orderDetailSequenceNumber: this.invoiceDetails().length + 1,
      source: 'O',
      isCanceled: 0,
      cancelComment: undefined,
      oldOrderDetailSequenceNumber: undefined,
      isApproved: undefined,
      invoiceNumber: undefined,
      patientAmount: undefined
    };

    console.log('✅ Adding test detail:', testDetail);
    this.invoiceDetails.set([...this.invoiceDetails(), testDetail]);
    console.log('📊 Current invoice details after test:', this.invoiceDetails());
  }

  // Save options modal methods
  hasDenominationRequiringOperatingPhysician(): boolean {
    return this.invoiceDetails().some(detail => detail.operatingPhysician || false);
  }

  performSelectiveSave(): void {
    if (!this.hasValidSelection()) {
      alert('Please select at least one valid section to save.');
      return;
    }

    if (this.isEditMode()) {
      alert('Edit-mode saving is not wired to update APIs yet. Create/new-admission flows have been restored first.');
      return;
    }

    if (!this.isExistingPatient() && !this.saveMedicalFile() && (this.saveAdmission() || this.saveInvoice())) {
      alert('For a new patient flow, you must save the medical file before saving admission or invoice.');
      return;
    }

    if (this.saveInvoice() && !this.saveAdmission()) {
      alert('Invoice saving currently requires admission saving in the same operation.');
      return;
    }

    if (this.saveMedicalFile() && !this.canSaveMedicalFile()) {
      alert('Medical file information is incomplete.');
      return;
    }

    if (this.saveAdmission() && !this.canSaveAdmission()) {
      alert('Admission information is incomplete.');
      return;
    }

    if (this.saveInvoice() && !this.canSaveInvoice()) {
      alert('Invoice information is incomplete.');
      return;
    }

    this.isSaving.set(true);
    this.saveViaStoredProcedure();
  }

  cancelSaveOptions(): void {
    this.showSaveOptionsModal.set(false);
  }
}


