export interface BillingInvoiceHeader {
  id: number;
  sequenceNumber: number;
  type: string;
  counterTypeId: number;
  counter: number;
  date: Date;
  admission: number;
  comment?: string;
  hospitalAmount: number;
  physicianAmount: number;
  medicamentAmount: number;
  discount: number;
  deniedAmount: number;
  lumpSum: number;
  accountId: number;
  accountDescription: string;
  complementaryAccountId?: number;
  complementaryAccountDescription?: string;
  currencyId: number;
  currency: string;
  exchangeRate: number;
  checkInClassId: number;
  checkInClass: string;
  coverageClassId: number;
  coverageClass: string;
  coverageRate: number;
  referralPhysicianId: number;
  referralPhysician: string;
  attendingPhysicianId?: number;
  attendingPhysician?: string;
  reference?: string;
  mainInvoice?: number;
  oldMainInvoice?: number;
  net: number;
  gross: number;
  netGross: number;
  complementary: number;
  complementaryOtherCurrency: number;
  complementaryDifferenceOtherCurrency: number;
  mrn: string;
  patientName: string;
  admissionNumber: string;
  admissionDate: Date;
  departmentId: number;
  department: string;
  dischargeDate?: Date;
  contextPriceId: number;
  contextPrice: string;
  receiptNumber?: string;
  receiptAmount?: number;
  receiptDate?: Date;
  roundedAmount?: number;
  insurance: number;
  admissionInsuranceCoverage: number;
  isDRG: number;
  collectionScheduleId?: number;
  collectionScheduleNumber?: string;
  collectionScheduleDate?: Date;
  receivedLBP?: number;
  receivedUSD?: number;
  difference?: number;
  receivingDate?: Date;
  voucherNumber?: string;
  splitedInvoice: number;
  primaryDischargeDiagnostic?: string;
  secondaryDischargeDiagnostic?: string;
  requireRegenerate: number;
  lockedBy?: number;
  lockedByName?: string;
  lockedDate?: Date;
  alternateInvoiceId?: number;
  globalDiscount: number;
  differenceAdjust?: number;
  modifiedDate: Date;
  isDeleted: number;
  createdBy: number;
  modifiedBy?: number;
  createdDate: Date;
  group?: number;
  agreementNumber?: string;
  complementaryDifferenceCalculationState?: number;
  isReversed: number;
  creditNoteNumber?: number;
  creditNoteDate?: Date;
  creditNotePaidAmount?: number;
  creditNoteDiscount?: number;
  employeeAccount?: string;
  isEmployee?: number;
  status?: number;
  contextEnumerationId?: number;
  isDirty?: number;
  isFromScratch?: number;
  agreementCreditAmount?: number;
  isSelected?: number;
  creditNoteAssignedAmount?: number;
  creditNoteVoucherNumber?: string;
  prepaymentAmount?: number;
  prepaymentDate?: Date;
  prepaymentNumber?: number;
  diagnosticGroup1?: string;
  diagnosticGroup2?: string;
  diagnosticGroup3?: string;
  diagnosticGroupId1?: number;
  diagnosticGroupId2?: number;
  diagnosticGroupId3?: number;
}












