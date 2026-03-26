import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiUrlService } from '../api/api-url.service';

interface DepartmentReportPatient {
  mrn: string;
  patientName: string;
  admissionNumber: string;
  invoiceHeaderId?: number;
  advanceId?: number;
  receiptNumber?: string | null;
  invoiceTotal: number;
  receiptLBP: number;
  receiptUSD: number;
  hasAdvance?: boolean;
  isAdvanceRow?: boolean;
  canSelect?: boolean;
}

interface DepartmentReportGroup {
  department: string;
  patients: DepartmentReportPatient[];
  departmentTotal: {
    invoiceTotal: number;
    receiptLBP: number;
    receiptUSD: number;
    patientCount: number;
  };
}

@Component({
  selector: 'app-department-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './department-report.component.html',
  styleUrl: './department-report.component.scss'
})
export class DepartmentReportComponent implements OnInit {
  reportData = signal<DepartmentReportGroup[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  transferringDepartment = signal<string | null>(null);
  checkInDateFrom = signal(this.formatDateForInput(new Date()));
  checkInDateTo = signal(this.formatDateForInput(new Date()));
  departmentFilter = signal<string | null>(null);
  showAllDepartments = signal(false);
  private readonly apiUrl = inject(ApiUrlService).api('/api');

  constructor(
    private http: HttpClient,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const today = this.formatDateForInput(new Date());
    this.route.queryParams.subscribe(params => {
      const from = params['from'] ?? params['checkInDateFrom'] ?? today;
      const to = params['to'] ?? params['checkInDateTo'] ?? today;
      this.checkInDateFrom.set(from);
      this.checkInDateTo.set(to);
    });
    const userDept = localStorage.getItem('loggedInUserDepartmentName');
    const hasUserDept = userDept && userDept.trim();
    this.departmentFilter.set(hasUserDept ? userDept.trim() : null);
    this.showAllDepartments.set(!hasUserDept);
    this.loadReport();
  }

  private formatDateForInput(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  onFilterChange(): void {
    this.loadReport();
  }

  loadReport(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const from = this.checkInDateFrom() || this.formatDateForInput(new Date());
    const to = this.checkInDateTo() || this.formatDateForInput(new Date());
    const dept = this.showAllDepartments() ? null : this.departmentFilter();
    let url = `${this.apiUrl}/ResidentPatient/report/by-department?checkInDateFrom=${encodeURIComponent(from)}&checkInDateTo=${encodeURIComponent(to)}`;
    if (dept) {
      url += `&departmentName=${encodeURIComponent(dept)}`;
    }
    this.http.get<DepartmentReportGroup[]>(url).subscribe({
      next: (data) => {
        this.reportData.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading report:', error);
        this.errorMessage.set('Failed to load report. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 3,
      maximumFractionDigits: 3
    }).format(value);
  }

  getTotalDepartments(): number {
    return this.reportData().length;
  }

  getTotalPatients(): number {
    return this.reportData().reduce((sum, dept) => sum + dept.departmentTotal.patientCount, 0);
  }

  getTotalInvoiceAmount(): number {
    return this.reportData().reduce((sum, dept) => sum + dept.departmentTotal.invoiceTotal, 0);
  }

  getTotalReceiptLBP(): number {
    return this.reportData().reduce((sum, dept) => sum + dept.departmentTotal.receiptLBP, 0);
  }

  getTotalReceiptUSD(): number {
    return this.reportData().reduce((sum, dept) => sum + dept.departmentTotal.receiptUSD, 0);
  }

  getCurrentDateString(): string {
    return new Date().toLocaleDateString();
  }

  goBack(): void {
    this.router.navigate(['/resident-patients']);
  }

  printReport(): void {
    window.print();
  }

  selectedItems = new Map<string, Set<string>>();

  toggleSelection(department: string, patient: DepartmentReportPatient): void {
    if (!patient.canSelect) return;
    const key = this.getItemKey(patient);
    const deptSet = this.selectedItems.get(department) ?? new Set<string>();
    if (deptSet.has(key)) deptSet.delete(key);
    else deptSet.add(key);
    if (deptSet.size === 0) this.selectedItems.delete(department);
    else this.selectedItems.set(department, deptSet);
  }

  isSelected(department: string, patient: DepartmentReportPatient): boolean {
    return (this.selectedItems.get(department) ?? new Set()).has(this.getItemKey(patient));
  }

  private getItemKey(p: DepartmentReportPatient): string {
    return p.isAdvanceRow && p.advanceId
      ? `adv-${p.advanceId}`
      : `inv-${p.invoiceHeaderId ?? 0}`;
  }

  getSelectedCount(department: string): number {
    return (this.selectedItems.get(department) ?? new Set()).size;
  }

  getSelectableCount(department: string): number {
    const group = this.reportData().find((g) => g.department === department);
    return group ? group.patients.filter((p) => p.canSelect).length : 0;
  }

  isAllSelected(department: string): boolean {
    const selectable = this.getSelectableCount(department);
    if (selectable === 0) return false;
    return this.getSelectedCount(department) === selectable;
  }

  toggleSelectAll(department: string): void {
    const group = this.reportData().find((g) => g.department === department);
    if (!group) return;
    const selectable = group.patients.filter((p) => p.canSelect);
    if (selectable.length === 0) return;
    if (this.isAllSelected(department)) {
      this.selectedItems.delete(department);
    } else {
      const deptSet = new Set(selectable.map((p) => this.getItemKey(p)));
      this.selectedItems.set(department, deptSet);
    }
  }

  transferToCashier(department: string): void {
    const deptSet = this.selectedItems.get(department);
    if (!deptSet || deptSet.size === 0) return;
    const group = this.reportData().find((g) => g.department === department);
    if (!group) return;
    const items = group.patients.filter((p) => p.canSelect && deptSet.has(this.getItemKey(p)));
    if (items.length === 0) return;
    this.transferringDepartment.set(department);
    this.errorMessage.set(null);
    const body = {
      items: items.map((p) => ({
        invoiceHeaderId: p.isAdvanceRow ? null : (p.invoiceHeaderId ?? null),
        advanceId: p.isAdvanceRow ? (p.advanceId ?? null) : null,
        isAdvanceRow: p.isAdvanceRow ?? false,
      })),
    };
    this.http.post(`${this.apiUrl}/ResidentPatient/report/transfer-to-cashier`, body).subscribe({
      next: () => {
        this.transferringDepartment.set(null);
        this.selectedItems.delete(department);
        this.loadReport();
      },
      error: (err) => {
        this.transferringDepartment.set(null);
        this.errorMessage.set(err?.error?.message || err?.message || 'Transfer failed');
      },
    });
  }
}
