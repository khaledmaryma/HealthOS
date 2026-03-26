import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./login/login.component').then(m => m.LoginComponent) },
  // Home screens (one per app)
  { path: 'lis/home', canActivate: [authGuard], loadComponent: () => import('./home/app-home.component').then(m => m.AppHomeComponent), data: { appKey: 'LIS' } },
  { path: 'inventory/home', canActivate: [authGuard], loadComponent: () => import('./home/app-home.component').then(m => m.AppHomeComponent), data: { appKey: 'Inventory' } },
  { path: 'user-management/home', canActivate: [authGuard], loadComponent: () => import('./home/app-home.component').then(m => m.AppHomeComponent), data: { appKey: 'UserManagement' } },
  { path: 'accounting/home', canActivate: [authGuard], loadComponent: () => import('./home/app-home.component').then(m => m.AppHomeComponent), data: { appKey: 'Accounting' } },
  { path: 'emr/home', canActivate: [authGuard], loadComponent: () => import('./home/app-home.component').then(m => m.AppHomeComponent), data: { appKey: 'EMR' } },
  { path: 'labtests', canActivate: [authGuard], loadComponent: () => import('./lab-tests/lab-tests.component').then(m => m.LabTestsComponent) },
  { path: 'patient-results', canActivate: [authGuard], loadComponent: () => import('./patient-results/patient-results.component').then(m => m.PatientResultsComponent) },
  { path: 'resident-patients', canActivate: [authGuard], loadComponent: () => import('./resident-patient-list/resident-patient-list.component').then(m => m.ResidentPatientListComponent) },
  // EMR aliases (reuse same screens, keep all functionality)
  { path: 'emr/resident-patients', canActivate: [authGuard], loadComponent: () => import('./resident-patient-list/resident-patient-list.component').then(m => m.ResidentPatientListComponent) },
  { path: 'quick-admission', canActivate: [authGuard], loadComponent: () => import('./quick-admission/quick-admission.component').then(m => m.QuickAdmissionComponent) },
  { path: 'quick-admission/:id', canActivate: [authGuard], loadComponent: () => import('./quick-admission/quick-admission.component').then(m => m.QuickAdmissionComponent) },
  // new v2 screen
  { path: 'quick-admission-v2', canActivate: [authGuard], loadComponent: () => import('./quick-admission-v2/quick-admission-v2.component').then(m => m.QuickAdmissionV2Component) },
  { path: 'quick-admission-v2/:id', canActivate: [authGuard], loadComponent: () => import('./quick-admission-v2/quick-admission-v2.component').then(m => m.QuickAdmissionV2Component) },
  { path: 'print-lab-results/:admissionNumber', canActivate: [authGuard], loadComponent: () => import('./print-lab-results/print-lab-results.component').then(m => m.PrintLabResultsComponent) },
  { path: 'accounting', canActivate: [authGuard], loadComponent: () => import('./accounting-dashboard/accounting-dashboard.component').then(m => m.AccountingDashboardComponent) },
  { path: 'accounting/journal-voucher', canActivate: [authGuard], loadComponent: () => import('./accounting-journal-voucher/accounting-journal-voucher.component').then(m => m.AccountingJournalVoucherComponent) },
  { path: 'accounting/journal-voucher/new', canActivate: [authGuard], loadComponent: () => import('./accounting-journal-voucher-editor/accounting-journal-voucher-editor.component').then(m => m.AccountingJournalVoucherEditorComponent) },
  { path: 'accounting/journal-voucher/:id', canActivate: [authGuard], loadComponent: () => import('./accounting-journal-voucher-editor/accounting-journal-voucher-editor.component').then(m => m.AccountingJournalVoucherEditorComponent) },
  { path: 'accounting/account-statement', canActivate: [authGuard], loadComponent: () => import('./accounting-account-statement/accounting-account-statement.component').then(m => m.AccountingAccountStatementComponent) },
  { path: 'accounting/trial-balance', canActivate: [authGuard], loadComponent: () => import('./accounting-trial-balance/accounting-trial-balance.component').then(m => m.AccountingTrialBalanceComponent) },
  { path: 'accounting/configuration', canActivate: [authGuard], loadComponent: () => import('./accounting-configuration/accounting-configuration.component').then(m => m.AccountingConfigurationComponent) },
  { path: 'accounting/cashier', canActivate: [authGuard], loadComponent: () => import('./accounting-cashier/accounting-cashier.component').then(m => m.AccountingCashierComponent) },
  { path: 'medical-file/patient/:patientId', canActivate: [authGuard], loadComponent: () => import('./patient-medical-file/patient-medical-file.component').then(m => m.PatientMedicalFileComponent) },
  { path: 'medical-file/admission/:admissionId', canActivate: [authGuard], loadComponent: () => import('./patient-medical-file/patient-medical-file.component').then(m => m.PatientMedicalFileComponent) },
  { path: 'gemini-chat', canActivate: [authGuard], loadComponent: () => import('./gemini-chat/gemini-chat.component').then(m => m.GeminiChatComponent) },
  { path: 'inventory/product/new', canActivate: [authGuard], loadComponent: () => import('./inventory/product-edit.component').then(m => m.ProductEditComponent) },
  { path: 'inventory/product/:id', canActivate: [authGuard], loadComponent: () => import('./inventory/product-edit.component').then(m => m.ProductEditComponent) },
  { path: 'inventory', canActivate: [authGuard], loadComponent: () => import('./inventory/inventory.component').then(m => m.InventoryComponent) },
  { path: 'department-report', canActivate: [authGuard], loadComponent: () => import('./department-report/department-report.component').then(m => m.DepartmentReportComponent) },
  { path: 'user-management/users', canActivate: [authGuard], loadComponent: () => import('./user-management/users/user-management-users.component').then(m => m.UserManagementUsersComponent) },
  { path: 'user-management/profiles', canActivate: [authGuard], loadComponent: () => import('./user-management/profiles/user-management-profiles.component').then(m => m.UserManagementProfilesComponent) },
  { path: 'user-management/permissions', canActivate: [authGuard], loadComponent: () => import('./user-management/permissions/user-management-permissions.component').then(m => m.UserManagementPermissionsComponent) },
  { path: 'user-management/profile-permissions', canActivate: [authGuard], loadComponent: () => import('./user-management/profile-permissions/user-management-profile-permissions.component').then(m => m.UserManagementProfilePermissionsComponent) }
];
