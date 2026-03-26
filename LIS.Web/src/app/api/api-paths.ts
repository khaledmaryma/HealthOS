/**
 * Relative API paths (no base URL). Single source of truth for routes.
 */
export const API_PATHS = {
  // Common
  hospitalConfiguration: '/api/HospitalConfiguration',

  // Core modules
  accounting: '/api/Accounting',
  department: '/api/Department',
  residentPatient: '/api/ResidentPatient',
  invoice: '/api/invoice',

  // Inventory
  inventoryProducts: '/api/inventory/products',

  // User management
  userManagement: '/api/user-management',
  userKpis: '/api/user-kpis',

  // Lab / results
  patientLabResults: '/api/patientlabresults',
  patientLabSub: '/api/patientlabsub',
  patientLabResultsHeaders: '/api/patientlabresultsheaders',
  patientLabBacteriology: '/api/PatientLabBacteriology',

  // Patient-related screens
  patientMedicalFile: '/api/PatientMedicalFile',
  residentPatientsLegacy: '/api/residentpatients',

  // Catalog / master data
  germs: '/api/germs',
  bacteria: '/api/Bacteria',
  denomination: '/api/denomination',
  insurance: '/api/HospitalDefinition/Insurance',

  // Admissions
  quickAdmission: '/api/QuickAdmission',
  quickAdmissionV2: '/api/QuickAdmissionV2',

  // AI / chat
  chatGpt: '/api/ChatGpt',
} as const;

