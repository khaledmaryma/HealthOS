export interface BillingInvoiceDetail {
  id: number;
  prescriptionDate?: Date;
  prescribedBy?: number;
  medicationUnit: number;
  medicationUnitDescription: string;
  admission: number;
  patient: number;
  denomination: number;
  denominationCode: string;
  denominationDescription: string;
  denominationCoeffCode: string;
  denominationCoeffValue: number;
  denominationCoeffPrice: number;
  quantity: number;
  unitPrice: number;
  netPrice: number;
  netUnitPrice: number;
  differenceAmount: number;
  deniedAmount: number;
  discount: number;
  lumpSum: number;
  complementaryAmount: number;
  complementaryAmountOtherCurrency: number;
  complementaryDifferenceOtherCurrency: number;
  operatingPhysician: number;
  isMedicalResultOk?: number;
  medicalResultDate?: Date;
  requireApproval: number;
  approvalReference?: string;
  approvalDate?: Date;
  isDenied: number;
  approvedBy?: string;
  dueDate?: Date;
  executionDate?: Date;
  invoiceHeader: number;
  referralPhysician: number;
  costCenter: number;
  profitCenter: number;
  pacIndex?: number;
  preInvoiceDetail?: number;
  detailDate: Date;
  mainDetailId?: number;
  copyFlag: number;
  detailDateHelper?: Date;
  isDoubtfull: number;
  procedure?: string;
  isDeleted: number;
  createdBy: number;
  modifiedBy?: number;
  createdDate: Date;
  modifiedDate?: Date;
  previousDetailId?: number;
  orderDetailSequenceNumber: number;
  source?: string;
  isCanceled: number;
  cancelComment?: string;
  oldOrderDetailSequenceNumber?: number;
  isApproved?: number;
  invoiceNumber?: number;
  patientAmount?: number;
}












