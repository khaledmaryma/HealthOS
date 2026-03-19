import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { PermissionDefinition, ProfileDefinition, ProfilePermission } from '../../models/user-management';
import { UserManagementService } from '../../services/user-management.service';

@Component({
  selector: 'app-user-management-profile-permissions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management-profile-permissions.component.html',
  styleUrl: './user-management-profile-permissions.component.scss'
})
export class UserManagementProfilePermissionsComponent implements OnInit {
  readonly profiles = signal<ProfileDefinition[]>([]);
  readonly permissions = signal<PermissionDefinition[]>([]);
  readonly profilePermissions = signal<ProfilePermission[]>([]);
  readonly selectedProfileId = signal<number | null>(null);
  readonly isSaving = signal(false);

  readonly rows = computed(() => {
    const profileId = this.selectedProfileId();
    const permissions = this.permissions();
    const assignments = this.profilePermissions();
    return permissions.map(permission => {
      const entry = assignments.find(pp => pp.permissionId === permission.id);
      const isScreenPermission = !!permission.screenId;
      const isAppPermission = !isScreenPermission && !!permission.applicationId;
      return {
        permission,
        entry,
        canAdd: entry?.canAdd ?? false,
        canModify: entry?.canModify ?? false,
        canDelete: entry?.canDelete ?? false,
        canSee: entry?.canSee ?? false,
        hasAccessToMenu: entry?.hasAccessToMenu ?? false,
        hasAccessToApp: entry?.hasAccessToApp ?? false,
        isAppPermission,
        isScreenPermission,
        profileId
      };
    });
  });

  constructor(
    private readonly svc: UserManagementService,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const profileIdParam = this.route.snapshot.queryParamMap.get('profileId');
    if (profileIdParam) {
      const profileId = Number(profileIdParam);
      if (!Number.isNaN(profileId)) {
        this.selectedProfileId.set(profileId);
      }
    }
    this.loadProfiles();
    this.loadPermissions();
  }

  loadProfiles(): void {
    this.svc.getProfiles().subscribe({
      next: data => {
        this.profiles.set(data);
        if (!data.length) {
          return;
        }

        const selectedId = this.selectedProfileId();
        if (selectedId === null) {
          this.onProfileChange(data[0].id);
          return;
        }

        const exists = data.some(profile => profile.id === selectedId);
        this.onProfileChange(exists ? selectedId : data[0].id);
      },
      error: err => {
        console.error('Failed to load profiles', err);
        alert('Failed to load profiles.');
      }
    });
  }

  loadPermissions(): void {
    this.svc.getPermissions().subscribe({
      next: data => this.permissions.set(data),
      error: err => {
        console.error('Failed to load permissions', err);
        alert('Failed to load permissions.');
      }
    });
  }

  onProfileChange(profileId: number): void {
    this.selectedProfileId.set(profileId);
    this.svc.getProfilePermissions(profileId).subscribe({
      next: data => this.profilePermissions.set(data),
      error: err => {
        console.error('Failed to load profile permissions', err);
        alert('Failed to load profile permissions.');
      }
    });
  }

  toggle(
    permissionId: number,
    field: 'canAdd' | 'canModify' | 'canDelete' | 'canSee' | 'hasAccessToMenu' | 'hasAccessToApp',
    checked: boolean
  ): void {
    const profileId = this.selectedProfileId();
    if (!profileId) return;

    const existing = this.profilePermissions().find(pp => pp.permissionId === permissionId);
    if (existing) {
      const updated: ProfilePermission = { ...existing, [field]: checked };
      this.svc.updateProfilePermission(existing.id, updated).subscribe({
        next: () => {
          this.profilePermissions.set(this.profilePermissions().map(pp => pp.id === existing.id ? updated : pp));
        },
        error: err => {
          console.error('Failed to update profile permission', err);
          alert('Failed to update profile permission.');
        }
      });
      return;
    }

    const payload: Partial<ProfilePermission> = {
      profileId,
      permissionId,
      canAdd: field === 'canAdd' ? checked : false,
      canModify: field === 'canModify' ? checked : false,
      canDelete: field === 'canDelete' ? checked : false,
      canSee: field === 'canSee' ? checked : false,
      hasAccessToMenu: field === 'hasAccessToMenu' ? checked : false,
      hasAccessToApp: field === 'hasAccessToApp' ? checked : false,
      isDeleted: false,
      createdBy: 0,
      createdDate: new Date().toISOString()
    };

    this.svc.createProfilePermission(payload).subscribe({
      next: created => {
        this.profilePermissions.set([...this.profilePermissions(), created]);
      },
      error: err => {
        console.error('Failed to create profile permission', err);
        alert('Failed to create profile permission.');
      }
    });
  }

  saveAll(): void {
    const profileId = this.selectedProfileId();
    if (!profileId || this.isSaving()) return;

    const rows = this.rows();
    const existing = this.profilePermissions();
    const createdDate = new Date().toISOString();
    const requests = rows.flatMap(row => {
      const entry = existing.find(pp => pp.permissionId === row.permission.id);
      if (!entry) {
        return this.svc.createProfilePermission({
          profileId,
          permissionId: row.permission.id,
          canAdd: row.canAdd,
          canModify: row.canModify,
          canDelete: row.canDelete,
          canSee: row.canSee,
          hasAccessToMenu: row.hasAccessToMenu,
          hasAccessToApp: row.hasAccessToApp,
          isDeleted: false,
          createdBy: 0,
          createdDate
        } as Partial<ProfilePermission>);
      }

      const hasChanges =
        entry.canAdd !== row.canAdd ||
        entry.canModify !== row.canModify ||
        entry.canDelete !== row.canDelete ||
        entry.canSee !== row.canSee ||
        entry.hasAccessToMenu !== row.hasAccessToMenu ||
        entry.hasAccessToApp !== row.hasAccessToApp;

      if (!hasChanges) {
        return [];
      }

      const updated: ProfilePermission = {
        ...entry,
        canAdd: row.canAdd,
        canModify: row.canModify,
        canDelete: row.canDelete,
        canSee: row.canSee,
        hasAccessToMenu: row.hasAccessToMenu,
        hasAccessToApp: row.hasAccessToApp
      };

      return this.svc.updateProfilePermission(entry.id, updated);
    });

    if (!requests.length) {
      return;
    }

    this.isSaving.set(true);
    forkJoin(requests).subscribe({
      next: () => this.onProfileChange(profileId),
      error: err => {
        console.error('Failed to save profile permissions', err);
        alert('Failed to save profile permissions.');
      },
      complete: () => this.isSaving.set(false)
    });
  }
}
