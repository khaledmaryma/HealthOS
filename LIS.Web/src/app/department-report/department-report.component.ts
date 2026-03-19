import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

interface DepartmentReportPatient {
  mrn: string;
  patientName: string;
  admissionNumber: string;
  invoiceTotal: number;
  receiptLBP: number;
  receiptUSD: number;
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
  imports: [CommonModule],
  templateUrl: './department-report.component.html',
  styleUrl: './department-report.component.scss'
})
export class DepartmentReportComponent implements OnInit {
  reportData = signal<DepartmentReportGroup[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  private apiUrl = 'http://localhost:5050/api';

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadReport();
  }

  loadReport(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.http.get<DepartmentReportGroup[]>(`${this.apiUrl}/ResidentPatient/report/by-department`).subscribe({
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
}
