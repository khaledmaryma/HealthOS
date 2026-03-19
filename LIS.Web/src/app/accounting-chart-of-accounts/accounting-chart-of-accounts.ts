import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccountingService, AccountNode } from '../services/accounting.service';
import { AccountingAccountNode } from '../accounting-account-node/accounting-account-node';

@Component({
  selector: 'app-accounting-chart-of-accounts',
  standalone: true,
  imports: [CommonModule, AccountingAccountNode],
  templateUrl: './accounting-chart-of-accounts.html',
  styleUrl: './accounting-chart-of-accounts.scss'
})
export class AccountingChartOfAccounts implements OnInit {
  private accountingService = inject(AccountingService);
  
  accounts: AccountNode[] = [];
  expandedNodes = new Set<string>();
  loading = false;

  ngOnInit() {
    this.loadChartOfAccounts();
  }

  loadChartOfAccounts() {
    this.loading = true;
    this.accountingService.getChartOfAccounts().subscribe({
      next: (data) => {
        this.accounts = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading chart of accounts:', error);
        this.loading = false;
      }
    });
  }

  toggleNode(code: string) {
    if (this.expandedNodes.has(code)) {
      this.expandedNodes.delete(code);
    } else {
      this.expandedNodes.add(code);
    }
  }

  isExpanded(code: string): boolean {
    return this.expandedNodes.has(code);
  }

  hasChildren(node: AccountNode): boolean {
    return !!(node.children && node.children.length > 0);
  }
}
