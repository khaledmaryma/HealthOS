import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { KpiHomeSectionComponent } from '../kpi/kpi-home-section.component';

type AppKey = 'LIS' | 'Inventory' | 'UserManagement' | 'Accounting' | 'EMR';

@Component({
  selector: 'app-app-home',
  standalone: true,
  imports: [CommonModule, KpiHomeSectionComponent],
  template: `
    <div class="container-fluid py-3">
      <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
        <div>
          <h4 class="mb-1">{{ title() }}</h4>
          <div class="text-muted small">
            Welcome, <strong>{{ user() }}</strong
            >{{ department() ? ' — ' + department() : '' }}.
          </div>
        </div>
        <div class="text-muted small">Today: {{ now() | date : 'medium' }}</div>
      </div>

      <div class="row g-3">
        <div class="col-12">
          <app-kpi-home-section [appKey]="appKey()" />
        </div>
      </div>
    </div>
  `,
})
export class AppHomeComponent {
  private route = inject(ActivatedRoute);
  private readonly _now = signal(new Date());

  readonly appKey = computed(() => (this.route.snapshot.data['appKey'] as AppKey) ?? 'LIS');

  readonly user = computed(() => localStorage.getItem('loggedInUser') ?? 'Guest');
  readonly department = computed(() => localStorage.getItem('loggedInUserDepartmentName') ?? '');
  readonly now = computed(() => this._now());

  readonly title = computed(() => {
    const k = this.appKey();
    if (k === 'LIS') return 'LIS Home';
    if (k === 'Inventory') return 'Inventory Home';
    if (k === 'UserManagement') return 'User Management Home';
    if (k === 'Accounting') return 'Accounting Home';
    if (k === 'EMR') return 'EMR Home';
    return 'Home';
  });
}

