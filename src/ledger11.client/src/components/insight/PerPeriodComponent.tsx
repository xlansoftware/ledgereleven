import { InsightComponent } from "../widgets/InsightComponent";
import { useCategoryStore } from "@/lib/store-category";
import { useEffect, useState } from "react";
import { fetchWithAuth } from "@/api";
import { Category } from "@/lib/types";

type PerPeriodData = {
  title: string;
  expense: Record<string, number>;
  income: Record<string, number>;
};

export default function PerPeriodComponent({ period }: { period: 'day' | 'week' | 'month' }) {
  const { categories, loadCategories } = useCategoryStore();
  const [monthlyData, setMonthlyData] = useState<PerPeriodData[]>([]);
  const [loading, setLoading] = useState(true);

  const colors = categories.reduce((acc, category) => {
    acc[category.name] = category;
    return acc;
  }, {} as Record<string, Category>);

  useEffect(() => {
    loadCategories();
    const fetchData = async () => {
      try {
        setLoading(true);
        const response = await fetchWithAuth(`api/insight/per-period/${period}`);
        if (response.ok) {
          const data = await response.json();
          setMonthlyData(data);
        }
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [loadCategories, period]);

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="relative space-y-4">
      {monthlyData.map(({ title, expense, income }) => (
        <div key={title} className="mb-4">
          <InsightComponent
            data={expense}
            altData={income}
            title={title}
            categories={colors}
          />
        </div>
      ))}
    </div>
  );
}