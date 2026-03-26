import { Injectable } from '@angular/core';
import { LoginAccess } from '../models/user-management';

const STORAGE_KEY = 'loggedInUserIsAdmin';

/**
 * Central client authorization. Profile IsAdmin bypasses permission- and role-based checks.
 */
@Injectable({ providedIn: 'root' })
export class AuthorizationService {
  isAdmin(): boolean {
    return localStorage.getItem(STORAGE_KEY) === '1';
  }

  setAdmin(isAdmin: boolean): void {
    if (isAdmin) {
      localStorage.setItem(STORAGE_KEY, '1');
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }

  /** Clear persisted admin flag (call from logout together with other session keys). */
  clearAdminFlag(): void {
    localStorage.removeItem(STORAGE_KEY);
  }

  /**
   * If admin, always true. Otherwise checks `LoginAccess.permissions` for `canSee` on the code.
   */
  hasPermission(permissionCode: string): boolean {
    if (this.isAdmin()) {
      return true;
    }
    const access = this.readAccess();
    if (!access) {
      return false;
    }
    return (access.permissions || []).some(p => p.code === permissionCode && p.canSee);
  }

  /**
   * If admin, always true. Otherwise checks `LoginAccess.screens` for `hasAccessToMenu` matching route.
   */
  hasScreenRouteAccess(route: string): boolean {
    if (this.isAdmin()) {
      return true;
    }
    const access = this.readAccess();
    if (!access) {
      return false;
    }
    const normalized = route.replace(/^\//, '').toLowerCase();
    return (access.screens || []).some(
      s =>
        s.hasAccessToMenu &&
        (s.route || '').replace(/^\//, '').toLowerCase() === normalized
    );
  }

  private readAccess(): LoginAccess | null {
    const raw = localStorage.getItem('userAccess');
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as LoginAccess;
    } catch {
      return null;
    }
  }
}
