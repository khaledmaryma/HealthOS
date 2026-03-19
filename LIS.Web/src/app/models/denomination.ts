export interface Denomination {
  id: number;
  code?: string | null;
  displayOrder?: number | null;
  smallDescription: string;
}

export interface CreateDenominationRequest {
  code?: string | null;
  displayOrder?: number | null;
  smallDescription: string;
}

export interface UpdateDenominationRequest {
  id: number;
  code?: string | null;
  displayOrder?: number | null;
  smallDescription?: string;
}














