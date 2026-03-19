export interface InvoiceHeader {
  id: number;
  invoiceNumber: string;
  patientId?: number;
  mrn?: string;
  admissionNumber?: string;
  invoiceDate: Date;
  totalAmount: number;
  paidAmount?: number;
  balanceAmount?: number;
  status: string;
  isDeleted: boolean;
  createdDate?: Date;
  modifiedDate?: Date;
  createdBy?: string;
  modifiedBy?: string;
  notes?: string;
  dueDate?: Date;
  paidDate?: Date;
}

export interface CreateInvoiceHeaderRequest {
  patientId?: number;
  mrn?: string;
  admissionNumber?: string;
  invoiceDate: Date;
  notes?: string;
  dueDate?: Date;
}

export interface UpdateInvoiceHeaderRequest {
  id: number;
  totalAmount?: number;
  paidAmount?: number;
  balanceAmount?: number;
  status?: string;
  notes?: string;
  dueDate?: Date;
  paidDate?: Date;
}














