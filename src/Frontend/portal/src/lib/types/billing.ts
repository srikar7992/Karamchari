export type CollectionStatus = 'Active' | 'Escalated' | 'Disputed' | 'Closed';

export interface CollectionCase {
  id: string;
  invoiceId: string;
  outstandingAmount: number;
  daysOutstanding: number;
  status: CollectionStatus;
  currentStage: string;
  reminderCount: number;
  lastActionAt: string | null;
  createdAt: string;
}

export interface ARSummary {
  totalOutstanding: number;
  overdue30: number;
  overdue60: number;
  overdue90: number;
  overduePlus: number;
  daysSalesOutstanding: number;
}

export interface MonthlyForecast {
  month: string;
  revenue: number;
  cash: number;
}

export interface ForecastSummary {
  totalUnbilledRevenue: number;
  expectedCashNext30Days: number;
  highRiskAR: number;
  trends: MonthlyForecast[];
}
