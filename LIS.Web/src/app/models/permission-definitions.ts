export interface PermissionSeed {
  code: string;
  name: string;
  description: string;
}

export const PERMISSION_DEFINITIONS: PermissionSeed[] = [
  { code: 'LAB_TESTS', name: 'Lab Tests', description: 'Lab tests management' },
  { code: 'PATIENT_RESULTS', name: 'Patient Results', description: 'Patient results' },
  { code: 'RESIDENT_PATIENTS', name: 'Resident Patients', description: 'Resident patients list' },
  { code: 'QUICK_ADMISSION', name: 'Quick Admission', description: 'Quick admission workflow' },
  { code: 'PRINT_LAB_RESULTS', name: 'Print Lab Results', description: 'Print lab results' },
  { code: 'ACCOUNTING_DASHBOARD', name: 'Accounting Dashboard', description: 'Accounting dashboard' },
  { code: 'ACCOUNTING_STATEMENT', name: 'Account Statement', description: 'View account statements' },
  { code: 'ACCOUNTING_OPENING_CLOSING', name: 'Opening/Closing Vouchers', description: 'Generate opening and closing vouchers' },
  { code: 'PATIENT_MEDICAL_FILE', name: 'Patient Medical File', description: 'Patient medical file' },
  { code: 'GEMINI_CHAT', name: 'Gemini Chat', description: 'AI assistant chat' },
  { code: 'INVENTORY', name: 'Inventory', description: 'Inventory management' },
  { code: 'INVENTORY_PRODUCT_EDIT', name: 'Inventory Product Edit', description: 'Inventory product edit' },
  { code: 'DEPARTMENT_REPORT', name: 'Department Report', description: 'Department report' },
  { code: 'UM_USERS', name: 'User Management - Users', description: 'Manage users' },
  { code: 'UM_PROFILES', name: 'User Management - Profiles', description: 'Manage profiles' },
  { code: 'UM_PERMISSIONS', name: 'User Management - Permissions', description: 'Manage permissions' },
  { code: 'UM_PROFILE_PERMISSIONS', name: 'User Management - Profile Permissions', description: 'Manage profile permissions' }
];
