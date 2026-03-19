import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProfileDefinition, UserDefinition } from '../../models/user-management';
import { UserManagementService } from '../../services/user-management.service';

@Component({
  selector: 'app-user-management-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management-users.component.html',
  styleUrl: './user-management-users.component.scss'
})
export class UserManagementUsersComponent implements OnInit {
  readonly items = signal<UserDefinition[]>([]);
  readonly profiles = signal<ProfileDefinition[]>([]);
  readonly query = signal('');
  readonly showForm = signal(false);
  readonly editItem = signal<UserDefinition | null>(null);
  readonly selectedItem = signal<UserDefinition | null>(null);

  readonly filtered = computed(() => {
    const q = this.query().toLowerCase().trim();
    const items = this.items();
    if (!q) return items;
    return items.filter(u =>
      u.username.toLowerCase().includes(q) ||
      u.fullName.toLowerCase().includes(q) ||
      (u.email ?? '').toLowerCase().includes(q)
    );
  });

  constructor(private readonly svc: UserManagementService) {}

  ngOnInit(): void {
    this.load();
    this.loadProfiles();
  }

  load(): void {
    this.svc.getUsers().subscribe({
      next: data => this.items.set(data),
      error: err => {
        console.error('Failed to load users', err);
        alert('Failed to load users.');
      }
    });
  }

  loadProfiles(): void {
    this.svc.getProfiles().subscribe({
      next: data => this.profiles.set(data),
      error: err => {
        console.error('Failed to load profiles', err);
        alert('Failed to load profiles.');
      }
    });
  }

  onSearchChange(value: string): void {
    this.query.set(value);
  }

  selectRow(item: UserDefinition): void {
    this.selectedItem.set(item);
  }

  isSelected(item: UserDefinition): boolean {
    return this.selectedItem()?.id === item.id;
  }

  clickAdd(): void {
    const firstProfileId = this.profiles()[0]?.id ?? 0;
    this.editItem.set({
      id: 0,
      profileId: firstProfileId,
      username: '',
      fullName: '',
      email: '',
      password: '',
      isActive: true,
      isDeleted: false,
      createdBy: 0,
      createdDate: new Date().toISOString()
    });
    this.showForm.set(true);
  }

  clickEdit(): void {
    const selected = this.selectedItem();
    if (!selected) return;
    this.editItem.set({ ...selected });
    this.showForm.set(true);
  }

  cancel(): void {
    this.showForm.set(false);
    this.editItem.set(null);
  }

  save(event?: Event): void {
    event?.preventDefault();
    const model = this.editItem();
    if (!model) return;

    if (!model.username.trim() || !model.fullName.trim() || model.profileId <= 0) {
      alert('Username, full name, and profile are required.');
      return;
    }

    if (model.id && model.id > 0) {
      const payload: Partial<UserDefinition> = { ...model };
      if (!payload.password) {
        delete payload.password;
      }
      this.svc.updateUser(model.id, payload).subscribe({
        next: () => {
          this.load();
          this.cancel();
        },
        error: err => {
          console.error('Failed to update user', err);
          alert('Failed to update user.');
        }
      });
    } else {
      if (!model.password) {
        alert('Password is required.');
        return;
      }
      const { id, ...payload } = model;
      this.svc.createUser(payload).subscribe({
        next: () => {
          this.load();
          this.cancel();
        },
        error: err => {
          console.error('Failed to create user', err);
          alert('Failed to create user.');
        }
      });
    }
  }

  remove(): void {
    const selected = this.selectedItem();
    if (!selected) return;
    if (!confirm(`Delete user "${selected.username}"?`)) return;
    this.svc.deleteUser(selected.id).subscribe({
      next: () => {
        this.selectedItem.set(null);
        this.load();
      },
      error: err => {
        console.error('Failed to delete user', err);
        alert('Failed to delete user.');
      }
    });
  }

  getProfileLabel(profileId: number): string {
    return this.profiles().find(p => p.id === profileId)?.name ?? '-';
  }
}
