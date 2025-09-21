import type { Category, Transaction } from "@/lib/types";

export interface DailyCategorySummary {
  date: string; // ISO date string for consistent grouping
  [categoryName: string]: number | string;
}

export interface CategoryStats {
  total: number;
  average: number;
}

export interface CategoryAnalysis {
  [categoryName: string]: CategoryStats;
}

export interface TimeframeAnalysis {
  daily: CategoryAnalysis;
  weekly: CategoryAnalysis;
  monthly: CategoryAnalysis;
}

/**
 * Aggregates all categories in a CategoryAnalysis into a single CategoryStats object
 * @param categoryAnalysis Analysis data with stats per category
 * @returns Single CategoryStats object representing totals/averages across all categories
 */
export function aggregateAllCategories(categoryAnalysis: CategoryAnalysis): CategoryStats {
  // If no categories, return zeros
  const categoryNames = Object.keys(categoryAnalysis);
  if (categoryNames.length === 0) {
    return {
      total: 0,
      average: 0,
    };
  }

  // Calculate totals across all categories
  const total = categoryNames.reduce((sum, categoryName) => {
    return sum + categoryAnalysis[categoryName].total;
  }, 0);

  const average = categoryNames.reduce((sum, categoryName) => {
    return sum + categoryAnalysis[categoryName].average;
  }, 0);

  return {
    total,
    average,
  };
}

// Helper function to get sorted categories by total (descending)
export const getSortedCategories = (analysis: TimeframeAnalysis): { name: string; stats: { total: number; dailyAverage: number; weeklyAverage: number; monthlyAverage: number;} }[] => {
  // Use daily analysis as the source of truth (all timeframes should have same categories and totals)
  const dailyAnalysis = analysis.daily;
  const weeklyAnalysis = analysis.weekly;
  const monthlyAnalysis = analysis.monthly;
  const categories = Object.keys(dailyAnalysis);
  
  if (categories.length === 0) {
    return [];
  }
  
  // Sort by total descending
  return categories
    .map(name => ({
      name,
      stats:  {
        total: dailyAnalysis[name].total,
        dailyAverage: dailyAnalysis[name].average,
        weeklyAverage: weeklyAnalysis[name].average,
        monthlyAverage: monthlyAnalysis[name].average,
      }
    }))
    .sort((a, b) => b.stats.total - a.stats.total);
};

/**
 * Analyzes DailyCategorySummary data to calculate totals and averages per category
 * across different timeframes
 * @param data Array of daily category summaries
 * @returns Object containing daily, weekly, and monthly analysis per category
 */
export function analyzeCategoryData(data: DailyCategorySummary[]): TimeframeAnalysis {
  if (data.length === 0) {
    return {
      daily: {},
      weekly: {},
      monthly: {}
    };
  }

  // Extract category names (excluding 'date')
  const categoryNames = Object.keys(data[0]).filter(key => key !== 'date');
  
  // Group data by month and week for monthly/weekly calculations
  const monthlyGroups = new Map<string, DailyCategorySummary[]>();
  const weeklyGroups = new Map<string, DailyCategorySummary[]>();
  
  data.forEach(item => {
    const date = new Date(item.date);
    
    // Format: YYYY-MM for monthly grouping
    const monthKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
    if (!monthlyGroups.has(monthKey)) {
      monthlyGroups.set(monthKey, []);
    }
    monthlyGroups.get(monthKey)!.push(item);
    
    // Format: YYYY-WW for weekly grouping (ISO week)
    const weekKey = getISOWeekKey(date);
    if (!weeklyGroups.has(weekKey)) {
      weeklyGroups.set(weekKey, []);
    }
    weeklyGroups.get(weekKey)!.push(item);
  });

  // Calculate stats for each category
  const result: TimeframeAnalysis = {
    daily: {},
    weekly: {},
    monthly: {}
  };

  categoryNames.forEach(categoryName => {
    // Get all values for this category
    const values = data.map(item => item[categoryName] as number);
    const total = values.reduce((sum, val) => sum + val, 0);
    const dailyAverage = total / data.length;
    
    // Weekly calculations
    const weeklyTotals = Array.from(weeklyGroups.values()).map(weekData => 
      weekData.reduce((sum, item) => sum + (item[categoryName] as number), 0)
    );
    const weeklyAverage = weeklyTotals.length > 0 
      ? weeklyTotals.reduce((sum, val) => sum + val, 0) / weeklyTotals.length 
      : 0;
    
    // Monthly calculations
    const monthlyTotals = Array.from(monthlyGroups.values()).map(monthData => 
      monthData.reduce((sum, item) => sum + (item[categoryName] as number), 0)
    );
    const monthlyAverage = monthlyTotals.length > 0 
      ? monthlyTotals.reduce((sum, val) => sum + val, 0) / monthlyTotals.length 
      : 0;
    
    result.daily[categoryName] = {
      total,
      average: dailyAverage,
    };
    
    result.weekly[categoryName] = {
      total,
      average: weeklyAverage,
    };
    
    result.monthly[categoryName] = {
      total,
      average: monthlyAverage
    };
  });

  return result;
}

/**
 * Helper function to get ISO week key (YYYY-WW)
 */
function getISOWeekKey(date: Date): string {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  const dayNum = d.getUTCDay() || 7;
  d.setUTCDate(d.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
  const weekNo = Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
  return `${d.getUTCFullYear()}-W${String(weekNo).padStart(2, '0')}`;
}

/**
 * Groups transactions by date and aggregates values per category
 * @param transactions Array of transactions to process
 * @param categories Array of categories to include in output
 * @returns Array of objects with date and one property per category name
 */
export function groupTransactionsByDateAndCategory(
  transactions: Transaction[],
  categories: Category[]
): DailyCategorySummary[] {
  // Create a map of category ID to category name for quick lookup
  const categoryMap = new Map<number, string>();
  categories.forEach(category => {
    categoryMap.set(category.id, category.name);
  });

  // Group transactions by date (using ISO string for consistent grouping)
  const dateGroups = new Map<string, Map<number, number>>();
  
  transactions.forEach(transaction => {
    if (!transaction.date) return;
    if (!transaction.value) return;
    if (!transaction.categoryId) return;

    const date = typeof transaction.date === "string" ? new Date(transaction.date) : transaction.date;
    const dateKey = date.toISOString().split('T')[0]; // YYYY-MM-DD
    
    if (!dateGroups.has(dateKey)) {
      dateGroups.set(dateKey, new Map<number, number>());
    }
    
    const categoryValues = dateGroups.get(dateKey)!;
    const currentSum = categoryValues.get(transaction.categoryId) || 0;
    categoryValues.set(transaction.categoryId, currentSum + transaction.value);
  });

  // Convert to final result format
  const result: DailyCategorySummary[] = [];
  
  dateGroups.forEach((categoryValues, dateKey) => {
    const dailySummary: DailyCategorySummary = { date: dateKey };
    
    // Initialize all categories to 0 for this date
    categories.forEach(category => {
      dailySummary[category.name] = 0;
    });
    
    // Set actual values for categories that have transactions
    categoryValues.forEach((value, categoryId) => {
      const categoryName = categoryMap.get(categoryId);
      if (categoryName) {
        dailySummary[categoryName] = value;
      }
    });
    
    result.push(dailySummary);
  });

  // Sort by date (optional, but usually helpful)
  return result.sort((a, b) => a.date.localeCompare(b.date));
}