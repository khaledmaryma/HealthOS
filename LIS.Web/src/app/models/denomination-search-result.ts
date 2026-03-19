export interface DenominationSearchResult {
  insurance?: string | null;
  insId: number;
  costCenterName?: string | null;
  costCenterId: number;
  denId: number;
  actCode?: string | null;
  ActCode?: string | null;
  code?: string | null;
  actName?: string | null; // SmallDescription
  labTest?: string | null;
  coefficientValue?: number | null;
  outLL: number;
  outUsd: number;
  priceLL: number;
  priceUsd: number;
  hasOperatingPhysician?: boolean | null;
}

