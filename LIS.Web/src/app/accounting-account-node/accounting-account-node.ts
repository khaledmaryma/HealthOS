import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccountNode } from '../services/accounting.service';

@Component({
  selector: 'app-account-node',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './accounting-account-node.html',
  styleUrl: './accounting-account-node.scss'
})
export class AccountingAccountNode {
  @Input() node!: AccountNode;
  @Input() level = 0;
  @Input() expandedNodes = new Set<string>();
  @Output() toggle = new EventEmitter<string>();

  get isExpanded(): boolean {
    return this.node.code ? this.expandedNodes.has(this.node.code) : false;
  }

  get hasChildren(): boolean {
    return !!(this.node.children && this.node.children.length > 0);
  }

  onToggle() {
    this.toggle.emit(this.node.code);
  }
}
