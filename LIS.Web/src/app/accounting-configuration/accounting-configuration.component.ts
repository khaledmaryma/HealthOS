import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountingService, FinancialRow } from '../services/accounting.service';

@Component({
  selector: 'app-accounting-configuration',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './accounting-configuration.component.html',
  styleUrl: './accounting-configuration.component.scss'
})
export class AccountingConfigurationComponent implements OnInit {
  readonly rows = signal<FinancialRow[]>([]);
  readonly isLoading = signal(false);

  readonly columns = computed(() => {
    const data = this.rows();
    if (!data.length) return [];
    return Object.keys(data[0]).filter(key => key.toLowerCase() !== 'id');
  });

  constructor(private readonly accountingService: AccountingService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.accountingService.getConfiguration().subscribe({
      next: data => {
        this.rows.set(data);
        this.isLoading.set(false);
      },
      error: err => {
        console.error('Failed to load configuration', err);
        this.isLoading.set(false);
        alert('Failed to load configuration.');
      }
    });
  }

  updateRowValue(row: FinancialRow, column: string, value: string): void {
    const rows = [...this.rows()];
    const index = rows.findIndex(item => this.getRowId(item) === this.getRowId(row));
    if (index === -1) return;
    rows[index] = { ...rows[index], [column]: value };
    this.rows.set(rows);
  }

  saveRow(row: FinancialRow): void {
    const id = this.getRowId(row);
    if (!id) return;
    const payload: FinancialRow = { ...row };
    this.accountingService.updateConfiguration(id, payload).subscribe({
      next: () => this.load(),
      error: err => {
        console.error('Failed to update configuration', err);
        alert('Failed to update configuration.');
      }
    });
  }

  private getRowId(row: FinancialRow): number {
    const value = row['ID'] ?? row['Id'] ?? row['id'];
    return typeof value === 'number' ? value : Number(value) || 0;
  }
}
