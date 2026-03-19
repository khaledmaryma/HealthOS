export interface InvoiceDetail {
  id: number;
  invoiceHeaderId: number;
  itemDescription: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  serviceId?: number;
  labTestId?: number;
  denominationId?: number;
  isDeleted: boolean;
  createdDate?: Date;
  modifiedDate?: Date;
  createdBy?: string;
  modifiedBy?: string;
  sequence?: number;
  discountAmount?: number;
  taxAmount?: number;
  notes?: string;
}

export interface CreateInvoiceDetailRequest {
  invoiceHeaderId: number;
  itemDescription: string;
  quantity: number;
  unitPrice: number;
  serviceId?: number;
  labTestId?: number;
  denominationId?: number;
  sequence?: number;
  discountAmount?: number;
  taxAmount?: number;
  notes?: string;
}

export interface UpdateInvoiceDetailRequest {
  id: number;
  itemDescription?: string;
  quantity?: number;
  unitPrice?: number;
  serviceId?: number;
  labTestId?: number;
  denominationId?: number;
  sequence?: number;
  discountAmount?: number;
  taxAmount?: number;
  notes?: string;
}

export interface InvoiceTotals {
  subtotal: number;
  totalDiscount: number;
  totalTax: number;
  totalAmount: number;
  itemCount: number;
}














