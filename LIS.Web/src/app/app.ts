import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthorizationService } from './auth/authorization.service';
import { LoginAccess } from './models/user-management';

type AppName = 'LIS' | 'Inventory' | 'UserManagement' | 'Accounting' | 'EMR';

const ALL_APPS: readonly AppName[] = ['LIS', 'Inventory', 'UserManagement', 'Accounting', 'EMR'];

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  protected readonly title = signal('LIS.Web');

  protected readonly activeApp = signal<AppName>('LIS');
  protected readonly isLauncherOpen = signal(false);
  protected readonly loggedInUser = signal('Guest');
  protected readonly loggedInUserDepartment = signal<string | null>(null);
  protected readonly allowedApps = signal<Set<AppName>>(new Set([...ALL_APPS]));
  protected readonly fontScale = signal(1);
  protected readonly controlScale = signal(1);
  protected readonly currentTime = signal('');
  private timeIntervalId: ReturnType<typeof setInterval> | null = null;

  constructor(
    private readonly router: Router,
    private readonly auth: AuthorizationService
  ) {}

  ngOnInit(): void {
    const storedFontScale = Number(localStorage.getItem('appFontScale') || '1');
    const storedControlScale = Number(localStorage.getItem('appControlScale') || '1');
    const storedUser = localStorage.getItem('loggedInUser');
    this.fontScale.set(Number.isFinite(storedFontScale) ? storedFontScale : 1);
    this.controlScale.set(Number.isFinite(storedControlScale) ? storedControlScale : 1);
    if (storedUser) {
      this.loggedInUser.set(storedUser);
    }
    const storedDept = localStorage.getItem('loggedInUserDepartmentName');
    this.loggedInUserDepartment.set(storedDept || null);
    this.allowedApps.set(this.resolveAllowedApps());
    const allowed = this.allowedApps();
    if (!allowed.has(this.activeApp())) {
      const first = allowed.values().next().value as AppName | undefined;
      if (first) {
        this.activeApp.set(first);
      }
    }
    this.applyUiScales();
    this.updateCurrentTime();
    this.timeIntervalId = setInterval(() => this.updateCurrentTime(), 1000);
  }

  ngOnDestroy(): void {
    if (this.timeIntervalId) {
      clearInterval(this.timeIntervalId);
      this.timeIntervalId = null;
    }
  }

  toggleLauncher(): void {
    this.isLauncherOpen.update(open => !open);
  }

  selectApp(app: AppName): void {
    if (!this.isAppAllowed(app)) {
      return;
    }
    this.activeApp.set(app);
    this.isLauncherOpen.set(false);

    if (app === 'LIS') this.router.navigate(['/lis/home']);
    else if (app === 'Inventory') this.router.navigate(['/inventory/home']);
    else if (app === 'UserManagement') this.router.navigate(['/user-management/home']);
    else if (app === 'Accounting') this.router.navigate(['/accounting/home']);
    else if (app === 'EMR') this.router.navigate(['/emr/home']);
  }

  isAppAllowed(app: AppName): boolean {
    return this.allowedApps().has(app);
  }

  onFontScaleChange(value: string): void {
    const parsed = Number(value);
    this.fontScale.set(Number.isFinite(parsed) ? parsed : 1);
    localStorage.setItem('appFontScale', this.fontScale().toString());
    this.applyUiScales();
  }

  onControlScaleChange(value: string): void {
    const parsed = Number(value);
    this.controlScale.set(Number.isFinite(parsed) ? parsed : 1);
    localStorage.setItem('appControlScale', this.controlScale().toString());
    this.applyUiScales();
  }

  private applyUiScales(): void {
    const root = document.documentElement;
    root.style.setProperty('--app-font-scale', this.fontScale().toString());
    root.style.setProperty('--app-control-scale', this.controlScale().toString());
  }

  private updateCurrentTime(): void {
    this.currentTime.set(new Date().toLocaleTimeString());
  }

  private resolveAllowedApps(): Set<AppName> {
    if (this.auth.isAdmin()) {
      return new Set(ALL_APPS);
    }

    const raw = localStorage.getItem('userAccess');
    if (!raw) {
      return new Set(ALL_APPS);
    }

    try {
      const access = JSON.parse(raw) as LoginAccess;
      const allowed = new Set<AppName>();
      (access.applications || []).forEach(app => {
        if (!app.hasAccessToApp) return;
        const name = this.mapAppName(app.code, app.name);
        if (name) {
          allowed.add(name);
        }
      });
      if (allowed.size) {
        return allowed;
      }
      // If access exists but no app matched, allow all to avoid locking users out
      return new Set(ALL_APPS);
    } catch {
      return new Set(ALL_APPS);
    }
  }

  private mapAppName(code: string, name: string): AppName | null {
    const normalized = `${code} ${name}`.toLowerCase().replace(/\s+/g, '');
    if (normalized.includes('lis')) return 'LIS';
    if (normalized.includes('inventory') || normalized.includes('inv')) return 'Inventory';
    if (normalized.includes('usermanagement') || normalized.includes('user') || normalized.includes('um')) return 'UserManagement';
    if (normalized.includes('accounting') || normalized.includes('acc')) return 'Accounting';
    if (normalized.includes('emr')) return 'EMR';
    return null;
  }
}
