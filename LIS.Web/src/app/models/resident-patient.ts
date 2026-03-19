export interface ResidentPatient {
  id: number;
  patientID: number;
  admission: number;
  mrn: number;
  admissionNumber: string;
  patientName: string;
  arabicFullName?: string;
  medicalRecordNumber: string;
  patientDOB: Date;
  age?: number;
  patientGender: string;
  checkInDate: Date;
  checkInClassID: number;
  checkInClassDescription: string;
  mainInsuranceID: number;
  mainInsuranceDescription: string;
  mainInsuranceClassID: number;
  mainInsuranceClassDescription: string;
  referralPhysicianID: number;
  referralPhysicianName: string;
  attendingPhysicianID?: number;
  attendingPhysicianName?: string;
  medicationUnitID: number;
  medicationUnitDescription: string;
  roomID?: number;
  roomDescription?: string;
  bedID?: number;
  bedDescription?: string;
  floorID?: number;
  floorDescription?: string;
  insuranceID: number;
  insuranceDescription: string;
  guarantorID: number;
  guarantorDescription: string;
  currencyID: number;
  currencyDescription: string;
  classID: number;
  classDescription: string;
  contextPriceID: number;
  contextPriceDescription: string;
  contextEnumerationID: number;
  contextEnumerationDescription: string;
  admissionType: number;
  admissionTypeDescription: string;
  contact?: string;
  insuredName?: string;
  insuredNameArabic?: string;
  insuredPhone?: string;
  auxiliaryInsuranceID?: number;
  auxiliaryInsuranceDescription?: string;
  auxiliaryInsuranceClassID?: number;
  auxiliaryInsuranceClassDescription?: string;
  isDischarged: boolean;
  dischargeDate?: Date;
  comment?: string;
  totalAdvanceLBP?: number;
  totalAdvanceUSD?: number;
  diagnostic?: string;
  visaNumber?: string;
  totalUncollectedAdvanceLBP?: number;
  totalUncollectedAdvanceUSD?: number;
  invoiceGrossAmountLBP?: number;
  invoiceGrossAmountUSD?: number;
  mainInvoiceNumber?: string;
  isPharmDisch: boolean;
  pharmDischDate?: Date;
  isDeleted: boolean;
  createdBy: number;
  modifiedBy?: number;
  createdDate: Date;
  modifiedDate?: Date;
  admissionSite?: number;
  isNersingDischarge: boolean;
  nersingDischargeComment?: string;
  oldBedID?: number;
  group?: number;
  patientShortName?: string;
  patientFormattedName?: string;
  status?: number;
  isRecheckIn: boolean;
  hasInvoices?: boolean;
  requireRegenerate?: boolean;
  diagnosticGroup1?: string;
  diagnosticGroup2?: string;
  diagnosticGroup3?: string;
}




















