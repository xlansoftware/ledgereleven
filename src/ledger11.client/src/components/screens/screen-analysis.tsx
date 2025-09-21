import { useEffect, useState } from "react";
import Filter from "../history/Filter";
import { type FilterRequest, type Transaction } from "@/lib/types";
import { useBookStore } from "@/lib/store-book";
import { fetchWithAuth } from "@/api";
import { DataCard } from "../analysis/DataCard";
import {
  aggregateAllCategories,
  analyzeCategoryData,
  groupTransactionsByDateAndCategory,
  type TimeframeAnalysis,
} from "../analysis/dataGroup";
import CategoryTable from "../analysis/CategoryTable";

async function loadTransactions(
  filter?: FilterRequest
): Promise<{ transactions: Transaction[]; totalCount: number }> {
  const params = new URLSearchParams();
  params.append("limit", "-1");

  if (filter) {
    Object.entries(filter).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        if (Array.isArray(value)) {
          value.forEach((v) => params.append(key, v.toString()));
        } else {
          params.append(key, value.toString());
        }
      }
    });
  }

  const response = await fetchWithAuth(`/api/filter?${params.toString()}`);

  if (!response.ok) throw new Error("Failed to load transactions");

  const result: { transactions: Transaction[]; totalCount: number } =
    await response.json();

  return result;
}

// Helper function to format currency
const formatCurrency = (value: number): string => {
  return new Intl.NumberFormat("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
    useGrouping: true, // This adds thousand separators (commas)
  }).format(value);
};

export default function ScreenAnalysis() {
  const { categories } = useBookStore();
  const [filter, setFilter] = useState<FilterRequest | undefined>();
  const [total, setTotal] = useState<{
    total: number;
    dailyAverage: number;
    weeklyAverage: number;
    monthlyAverage: number;
  }>();

  const [filterArgument, setFilterArgument] = useState<{
    categories: number[];
    users: string[];
  }>({ categories: [], users: [] });

  const [timeframe, setTimeframe] = useState<TimeframeAnalysis>();

  // const [perDay, setPerDay] = useState<Stat>();

  const initFilter = async () => {
    const response = await fetchWithAuth("/api/filter/arguments");
    if (response.ok) {
      const data = await response.json();
      setFilterArgument({
        categories: data.categories,
        users: data.users,
      });
    }
  };

  const applyFilter = async () => {
    const result = await loadTransactions(filter);
    const grouped = groupTransactionsByDateAndCategory(
      result.transactions,
      categories
    );
    // setData(grouped);
    const tf = analyzeCategoryData(grouped);
    setTimeframe(tf);

    const total = {
      total: result.transactions.reduce((pv, c) => pv + (c.value || 0), 0),
      dailyAverage: aggregateAllCategories(tf.daily).average,
      weeklyAverage: aggregateAllCategories(tf.weekly).average,
      monthlyAverage: aggregateAllCategories(tf.monthly).average,
    };
    setTotal(total);
  };

  useEffect(() => {
    initFilter();
  }, []);

  return (
    <div className="flex items-center justify-start items-start h-full">
      <div className="max-w-80">
        <Filter
          filter={filter || {}}
          categories={categories.filter(
            (c) => filterArgument.categories.indexOf(c.id) !== -1
          )}
          users={filterArgument.users}
          onApply={async (filter) => {
            setFilter(filter);
            applyFilter();
          }}
        />
      </div>

      <div className="flex flex-1 flex-col gap-4">
        <div className="grid auto-rows-min gap-4 grid-cols-1 sm:grid-cols-2 md:grid-cols-4">
          {total && (
            <>
              <DataCard title="Total">{formatCurrency(total.total)}</DataCard>
              <DataCard title="Daily Avg">
                {formatCurrency(total.dailyAverage)}
              </DataCard>
              <DataCard title="Weekly Avg">
                {formatCurrency(total.weeklyAverage)}
              </DataCard>
              <DataCard title="Monthly Avg">
                {formatCurrency(total.monthlyAverage)}
              </DataCard>
            </>
          )}
        </div>
        <div className="bg-muted/50 min-h-[200vh] flex-1 rounded-xl md:min-h-min">
          {!timeframe && "No data"}
          {timeframe && (
            <CategoryTable analysis={timeframe} categories={categories} />
          )}
        </div>
      </div>
    </div>
  );
}
