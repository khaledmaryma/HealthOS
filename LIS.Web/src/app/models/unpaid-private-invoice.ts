export interface UnpaidPrivateInvoiceRow {
  department?: string;
  checkInDate?: string | null;
  mrn: string;
  admissionNumber: string;
  patientName: string;
  patientPhone?: string;
  invoiceHeaderId: number;
  invoiceNet: number;
  paidAdvance: number;
  restToPay: number;
  currency?: string | null;
  receivedLbp: number;
  receivedUsd: number;
}

export interface UnpaidPrivateInvoiceReceivedPatch {
  receivedLbp: number;
  receivedUsd: number;
}
