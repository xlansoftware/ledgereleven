import { InsightComponent } from "../widgets/InsightComponent";
import { useCategoryStore } from "@/lib/store-category";
import { useEffect, useState } from "react";
import { fetchWithAuth } from "@/api";
import { Category } from "@/lib/types";

type PerMonthData = {
  title: string;
  expense: Record<string, number>;
  income: Record<string, number>;
};

export default function PerMonthComponent() {
  const { categories, loadCategories } = useCategoryStore();
  const [monthlyData, setMonthlyData] = useState<PerMonthData[]>([]);
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
        const response = await fetchWithAuth("api/insight/per-month");
        if (response.ok) {
          const data = await response.json();
          setMonthlyData(data);
        }
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [loadCategories]);

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