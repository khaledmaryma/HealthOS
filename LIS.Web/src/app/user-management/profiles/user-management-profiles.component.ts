import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { forkJoin, map, of, switchMap } from 'rxjs';
import { PermissionDefinition, ProfileDefinition, ProfilePermission } from '../../models/user-management';
import { UserManagementService } from '../../services/user-management.service';

@Component({
  selector: 'app-user-management-profiles',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './user-management-profiles.component.html',
  styleUrl: './user-management-profiles.component.scss'
})
export class UserManagementProfilesComponent implements OnInit {
  readonly items = signal<ProfileDefinition[]>([]);
  readonly query = signal('');
  readonly showForm = signal(false);
  readonly editItem = signal<ProfileDefinition | null>(null);
  readonly selectedItem = signal<ProfileDefinition | null>(null);

  readonly filtered = computed(() => {
    const q = this.query().toLowerCase().trim();
    const items = this.items();
    if (!q) return items;
    return items.filter(profile =>
      profile.name.toLowerCase().includes(q)
    );
  });

  constructor(private readonly svc: UserManagementService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.svc.getProfiles().subscribe({
      next: data => this.items.set(data),
      error: err => {
        console.error('Failed to load profiles', err);
        alert('Failed to load profiles.');
      }
    });
  }

  onSearchChange(value: string): void {
    this.query.set(value);
  }

  selectRow(item: ProfileDefinition): void {
    this.selectedItem.set(item);
  }

  isSelected(item: ProfileDefinition): boolean {
    return this.selectedItem()?.id === item.id;
  }

  clickAdd(): void {
    this.editItem.set({
      id: 0,
      name: '',
      isAdmin: false,
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
    if (!model.name.trim()) {
      alert('Profile name is required.');
      return;
    }

    if (model.id && model.id > 0) {
      this.svc.updateProfile(model.id, model).subscribe({
        next: () => {
          this.load();
          this.cancel();
        },
        error: err => {
          console.error('Failed to update profile', err);
          alert('Failed to update profile.');
        }
      });
    } else {
      const { id, ...payload } = model;
      this.svc.createProfile(payload).pipe(
        switchMap(created =>
          this.svc.getPermissions().pipe(
            switchMap((permissions: PermissionDefinition[]) => {
              if (!permissions.length) {
                return of(created);
              }
              const createdDate = new Date().toISOString();
              const requests = permissions.map(permission =>
                this.svc.createProfilePermission({
                  profileId: created.id,
                  permissionId: permission.id,
                  canAdd: false,
                  canModify: false,
                  canDelete: false,
                  canSee: false,
                  hasAccessToMenu: false,
                  hasAccessToApp: false,
                  isDeleted: false,
                  createdBy: 0,
                  createdDate
                } as Partial<ProfilePermission>)
              );
              return forkJoin(requests).pipe(map(() => created));
            })
          )
        )
      ).subscribe({
        next: () => {
          this.load();
          this.cancel();
        },
        error: err => {
          console.error('Failed to create profile permissions', err);
          alert('Failed to create profile permissions.');
        }
      });
    }
  }

  remove(): void {
    const selected = this.selectedItem();
    if (!selected) return;
    if (!confirm(`Delete profile "${selected.name}"?`)) return;
    this.svc.deleteProfile(selected.id).subscribe({
      next: () => {
        this.selectedItem.set(null);
        this.load();
      },
      error: err => {
        console.error('Failed to delete profile', err);
        alert('Failed to delete profile.');
      }
    });
  }
}
