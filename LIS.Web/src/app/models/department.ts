export interface Department {
  id: number;
  name?: string | null;
  description?: string | null;
  code?: string | null;
  isActive?: boolean | null;
  isDeleted?: boolean | null;
  createdBy?: number | null;
  modifiedBy?: number | null;
  createdDate?: Date | null;
  modifiedDate?: Date | null;
}

