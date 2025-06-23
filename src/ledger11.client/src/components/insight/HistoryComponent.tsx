import { useEffect, useState } from "react";
import { fetchWithAuth } from "@/api";
import { useCategoryStore } from "@/lib/store-category";
import { Category } from "@/lib/types";
import BarChartComponent from "@/components/insight/BarChartComponent";
import HorizontalBarChartComponent from "./HorizontalBarChartComponent";
// import { formatCurrency } from "@/lib/utils";

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

function ChartTitle({ title }: { title: string }) {
  return (
    <h3 className="text-lg font-semibold text-gray-900 dark:text-white w-full text-center">
      {title}
    </h3>
  );
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

  const cleanValues = (data: HistoryRecord[]) => {
    data.forEach((item) => {
      item.value = Math.round(item.value);
    })
    return data;
  };

  return (
    <div className="relative">
      {dayly && (
        <div className="m-4">
          <ChartTitle title="Last 7 days" />
          <BarChartComponent
            data={cleanValues(dayly.slice(-7))}
            title="Daily"
            categories={colors}
          />
        </div>
      )}
      {weekly && (
        <div className="m-4">
          <ChartTitle title="Last 7 weeks" />
          <HorizontalBarChartComponent
            data={cleanValues(weekly.slice(-7).reverse())}
            title="Weekly"
            categories={colors}
          />
        </div>
      )}
      {monthly && (
        <div className="m-4">
          <ChartTitle title="Last 12 months" />
          <HorizontalBarChartComponent
            data={cleanValues(monthly.slice(-12).reverse())}
            title="Monthly"
            categories={colors}
          />
        </div>
      )}
    </div>
  );
}
