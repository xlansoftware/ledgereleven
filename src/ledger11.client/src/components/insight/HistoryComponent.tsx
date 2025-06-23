import { useEffect, useState } from "react";
import { fetchWithAuth } from "@/api";
import { useCategoryStore } from "@/lib/store-category";
import { Category } from "@/lib/types";
import BarChartComponent from "@/components/insight/BarChartComponent";

export interface HistoryRecord {
  date: string;
  value: number;
  byCategory: Record<string, number>;
}

export interface HistoryResult {
  monthly: HistoryRecord[];
  weekly: HistoryRecord[];
  dayly: HistoryRecord[];
}

export default function HistoryComponent() {
  const { categories, loadCategories } = useCategoryStore();
  const colors = categories.reduce((acc, category) => {
    acc[category.name] = category;
    return acc;
  }, {} as Record<string, Category>);
  const [data, setData] = useState<HistoryResult | null>(null);

  useEffect(() => {
    if (categories.length === 0) {
      loadCategories().catch((error) => {
        console.error("Error loading categories:", error);
      });
    }
    const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    fetchWithAuth(`/api/insight/history/${encodeURIComponent(timeZone)}`).then(
      (response) => {
        if (response.ok) {
          response.json().then((data) => {
            setData(data);
          });
        } else {
          console.error("Error fetching insights:", response.statusText);
        }
      }
    );
  }, [categories.length, loadCategories]);

  const { dayly, weekly, monthly } = data ?? {};

  return (
    <div className="relative">
      {dayly && (
        <div className="m-4">
          <BarChartComponent
            data={dayly.slice(-7)}
            title="Daily"
            categories={colors}
          />
        </div>
      )}
      {weekly && (
        <div className="m-4">
          <BarChartComponent
            data={weekly.slice(-7)}
            title="Weekly"
            categories={colors}
          />
          <div>Last 7 weeks</div>
        </div>
      )}
      {monthly && (
        <div className="m-4">
          <BarChartComponent
            data={monthly.slice(-12)}
            title="Monthly"
            categories={colors}
          />
        </div>
      )}
    </div>
  );
}
