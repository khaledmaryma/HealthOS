import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppDefinition, PermissionDefinition, ScreenDefinition, UserDefinition } from '../../models/user-management';
import { UserManagementService } from '../../services/user-management.service';

@Component({
  selector: 'app-user-management-permissions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management-permissions.component.html',
  styleUrl: './user-management-permissions.component.scss'
})
export class UserManagementPermissionsComponent implements OnInit {
  readonly items = signal<PermissionDefinition[]>([]);
  readonly applications = signal<AppDefinition[]>([]);
  readonly screens = signal<ScreenDefinition[]>([]);
  readonly users = signal<UserDefinition[]>([]);
  readonly query = signal('');
  readonly showForm = signal(false);
  readonly editItem = signal<PermissionDefinition | null>(null);
  readonly selectedItem = signal<PermissionDefinition | null>(null);
  readonly applicationInput = signal('');
  readonly screenInput = signal('');
  readonly showApplicationDropdown = signal(false);
  readonly showScreenDropdown = signal(false);

  readonly filtered = computed(() => {
    const q = this.query().toLowerCase().trim();
    const items = this.items();
    if (!q) return items;
    return items.filter(permission =>
      permission.code.toLowerCase().includes(q) ||
      permission.name.toLowerCase().includes(q) ||
      (permission.description ?? '').toLowerCase().includes(q)
    );
  });

  readonly screenOptions = computed(() => {
    const appId = this.editItem()?.applicationId ?? null;
    const screens = this.screens();
    if (!appId) return screens;
    return screens.filter(screen => screen.appId === appId);
  });

  readonly filteredApplications = computed(() => {
    const q = this.applicationInput().toLowerCase().trim();
    const apps = this.applications();
    if (!q) return apps;
    return apps.filter(app =>
      app.code.toLowerCase().includes(q) ||
      app.name.toLowerCase().includes(q)
    );
  });

  readonly filteredScreens = computed(() => {
    const q = this.screenInput().toLowerCase().trim();
    const screens = this.screenOptions();
    if (!q) return screens;
    return screens.filter(screen =>
      screen.code.toLowerCase().includes(q) ||
      screen.name.toLowerCase().includes(q) ||
      (screen.route ?? '').toLowerCase().includes(q)
    );
  });

  constructor(private readonly svc: UserManagementService) {}

  ngOnInit(): void {
    this.load();
    this.loadApplications();
    this.loadScreens();
    this.loadUsers();
  }

  load(): void {
    this.svc.getPermissions().subscribe({
      next: data => this.items.set(data),
      error: err => {
        console.error('Failed to load permissions', err);
        alert('Failed to load permissions.');
      }
    });
  }

  loadApplications(): void {
    this.svc.getApplications().subscribe({
      next: data => {
        this.applications.set(data);
        this.syncAutocompleteInputs();
      },
      error: err => {
        console.error('Failed to load applications', err);
        alert('Failed to load applications.');
      }
    });
  }

  loadScreens(): void {
    this.svc.getScreens().subscribe({
      next: data => {
        this.screens.set(data);
        this.syncAutocompleteInputs();
      },
      error: err => {
        console.error('Failed to load screens', err);
        alert('Failed to load screens.');
      }
    });
  }

  loadUsers(): void {
    this.svc.getUsers().subscribe({
      next: data => this.users.set(data),
      error: err => {
        console.error('Failed to load users', err);
        alert('Failed to load users.');
      }
    });
  }

  onSearchChange(value: string): void {
    this.query.set(value);
  }

  selectRow(item: PermissionDefinition): void {
    this.selectedItem.set(item);
  }

  isSelected(item: PermissionDefinition): boolean {
    return this.selectedItem()?.id === item.id;
  }

  clickAdd(): void {
    this.editItem.set({
      id: 0,
      applicationId: null,
      screenId: null,
      code: '',
      name: '',
      description: '',
      isDeleted: false,
      createdBy: 0,
      createdDate: new Date().toISOString()
    });
    this.applicationInput.set('');
    this.screenInput.set('');
    this.showForm.set(true);
  }

  clickEdit(): void {
    const selected = this.selectedItem();
    if (!selected) return;
    this.editItem.set({ ...selected });
    this.syncAutocompleteInputs();
    this.showForm.set(true);
  }

  cancel(): void {
    this.showForm.set(false);
    this.editItem.set(null);
    this.applicationInput.set('');
    this.screenInput.set('');
  }

  save(event?: Event): void {
    event?.preventDefault();
    const model = this.editItem();
    if (!model) return;
    if (!model.code.trim() || !model.name.trim()) {
      alert('Code and name are required.');
      return;
    }

    if (model.id && model.id > 0) {
      this.svc.updatePermission(model.id, model).subscribe({
        next: () => {
          this.load();
          this.cancel();
        },
        error: err => {
          console.error('Failed to update permission', err);
          alert('Failed to update permission.');
        }
      });
    } else {
      const { id, ...payload } = model;
      this.svc.createPermission(payload).subscribe({
        next: () => {
          this.load();
          this.cancel();
        },
        error: err => {
          console.error('Failed to create permission', err);
          alert('Failed to create permission.');
        }
      });
    }
  }

  onApplicationInput(value: string): void {
    this.applicationInput.set(value);
    this.showApplicationDropdown.set(true);
    const match = this.applications().find(app => this.formatApplication(app) === value);
    const model = this.editItem();
    if (!model) return;

    const nextAppId = match?.id ?? null;
    const nextModel: PermissionDefinition = { ...model, applicationId: nextAppId };

    if (!nextAppId) {
      nextModel.screenId = null;
      this.screenInput.set('');
    } else if (model.screenId) {
      const screen = this.screens().find(s => s.id === model.screenId);
      if (screen && screen.appId !== nextAppId) {
        nextModel.screenId = null;
        this.screenInput.set('');
      }
    }

    this.editItem.set(nextModel);
  }

  onApplicationFocus(): void {
    this.showApplicationDropdown.set(true);
  }

  hideApplicationDropdown(): void {
    setTimeout(() => this.showApplicationDropdown.set(false), 200);
  }

  onApplicationIconClick(): void {
    this.showApplicationDropdown.set(true);
  }

  selectApplication(app: AppDefinition, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }

    const model = this.editItem();
    if (!model) return;

    const nextModel: PermissionDefinition = {
      ...model,
      applicationId: app.id
    };

    if (model.screenId) {
      const screen = this.screens().find(s => s.id === model.screenId);
      if (screen && screen.appId !== app.id) {
        nextModel.screenId = null;
        this.screenInput.set('');
      }
    }

    this.applicationInput.set(this.formatApplication(app));
    this.editItem.set(nextModel);
    this.showApplicationDropdown.set(false);
  }

  onScreenInput(value: string): void {
    this.screenInput.set(value);
    this.showScreenDropdown.set(true);
    const match = this.screens().find(screen => this.formatScreen(screen) === value);
    const model = this.editItem();
    if (!model) return;

    if (!match) {
      this.editItem.set({ ...model, screenId: null });
      return;
    }

    const app = this.applications().find(a => a.id === match.appId);
    if (app) {
      this.applicationInput.set(this.formatApplication(app));
    }

    this.editItem.set({
      ...model,
      screenId: match.id,
      applicationId: match.appId
    });
  }

  onScreenFocus(): void {
    this.showScreenDropdown.set(true);
  }

  hideScreenDropdown(): void {
    setTimeout(() => this.showScreenDropdown.set(false), 200);
  }

  onScreenIconClick(): void {
    this.showScreenDropdown.set(true);
  }

  selectScreen(screen: ScreenDefinition, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }

    const model = this.editItem();
    if (!model) return;

    const app = this.applications().find(a => a.id === screen.appId);
    if (app) {
      this.applicationInput.set(this.formatApplication(app));
    }

    this.screenInput.set(this.formatScreen(screen));
    this.editItem.set({
      ...model,
      screenId: screen.id,
      applicationId: screen.appId
    });
    this.showScreenDropdown.set(false);
  }

  private syncAutocompleteInputs(): void {
    const model = this.editItem();
    if (!model) return;
    const app = this.applications().find(a => a.id === model.applicationId);
    const screen = this.screens().find(s => s.id === model.screenId);
    this.applicationInput.set(app ? this.formatApplication(app) : '');
    this.screenInput.set(screen ? this.formatScreen(screen) : '');
  }

  formatApplication(app: AppDefinition): string {
    return `${app.code} - ${app.name}`;
  }

  formatScreen(screen: ScreenDefinition): string {
    return `${screen.code} - ${screen.name}`;
  }

  getApplicationLabel(applicationId?: number | null): string {
    if (!applicationId) return '-';
    const app = this.applications().find(item => item.id === applicationId);
    return app ? `${app.code} - ${app.name}` : applicationId.toString();
  }

  getScreenLabel(screenId?: number | null): string {
    if (!screenId) return '-';
    const screen = this.screens().find(item => item.id === screenId);
    return screen ? `${screen.code} - ${screen.name}` : screenId.toString();
  }

  getCreatedByLabel(createdBy?: number | null): string {
    if (!createdBy || createdBy <= 0) return 'System';
    const user = this.users().find(item => item.id === createdBy);
    return user?.fullName || user?.username || createdBy.toString();
  }

  remove(): void {
    const selected = this.selectedItem();
    if (!selected) return;
    if (!confirm(`Delete permission "${selected.name}"?`)) return;
    this.svc.deletePermission(selected.id).subscribe({
      next: () => {
        this.selectedItem.set(null);
        this.load();
      },
      error: err => {
        console.error('Failed to delete permission', err);
        alert('Failed to delete permission.');
      }
    });
  }

}
