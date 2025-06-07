export interface Category {
  id: number;
  displsyOrder?: number;
  name: string;
  color?: string;
  icon?: string;
}

export interface Transaction {
  id?: number;
  value?: number;
  notes?: string;
  date?: Date;
  categoryId?: number;
  user?: string;
  transactionDetails?: TransactionDetails[];
}

export interface TransactionDetails {
  id?: number;
  transactionId?: number;
  value?: number;
  description?: string;
  quantity?: number;
  categoryId?: number;
}

export interface Receipt
{
  items?: Item[];
  total_paid?: string;
  category?: string;
}

export interface Item
{
    name?: string;
    quantity?: string;
    unit_price?: string;
    total_price?: string;
    category?: string;
}

export interface Space {
  id?: string;
  name?: string;
  tint?: string;
  currency?: string; // default currnecy
  createdAt?: Date;
  createdByUserId?: string;

  // Denormalized fields
  countTransactions?: number;
  countCategories?: number;
  totalValue?: number;
}

export interface FilterRequest {
  period?: 'all' | 'today' | 'thisWeek' | 'thisMonth' | 'thisYear' | 'custom';
  startDate?: string;      // ISO 8601 string, e.g., "2025-05-29"
  endDate?: string;        // ISO 8601 string
  category?: number[];
  note?: string;
  user?: string[];
  minValue?: number;
  maxValue?: number;
}
