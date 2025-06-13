import { useEffect, useState } from "react";
import { fetchWithAuth } from "@/api";
import { InsightComponent } from "../widgets/InsightComponent";
import { useCategoryStore } from "@/lib/store-category";
import { Category } from "@/lib/types";
import DonutSkeleton from "../DonutSkeleton";
import { Button } from "../ui/button";
import { PlusCircleIcon } from "lucide-react";
import { IncomeComponent } from "../widgets/IncomeComponent";

export default function Insights() {
  const { categories, loadCategories } = useCategoryStore();
  const colors = categories.reduce((acc, category) => {
    acc[category.name] = category;
    return acc;
  }, {} as Record<string, Category>);
  const [data, setData] = useState<TotalByPeriodByCategory | null>(null);

  useEffect(() => {
    if (categories.length === 0) {
      loadCategories().catch((error) => {
        console.error("Error loading categories:", error);
      });
    }
    const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    fetchWithAuth(`/api/insight/${encodeURIComponent(timeZone)}`).then(
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

  const { expense, income } = data ?? {};

  const noExpenses =
    !expense ||
    Object.keys(expense).reduce(
      (acc, key) => acc + Object.keys(expense[key]).length,
      0
    ) === 0;

  return (
    <div className="relative">
      <Button
        className="fixed top-16 right-4 z-50 w-12 h-12 flex items-center justify-center"
        variant={"ghost"}
      >
        <PlusCircleIcon className="w-16 h-16" />
      </Button>
      {noExpenses && (
        <div className="flex items-center justify-center pt-16">
          No expenses.
        </div>
      )}
      {Object.entries(expense || {}).map(
        ([key, value]) =>
          value && (
            <div key={key} className="mb-4">
              <InsightComponent data={value} title={key} categories={colors}>
                {income && income[key] && (
                  <IncomeComponent
                    data={income[key]}
                    title={key}
                    categories={colors}
                  />
                )}
              </InsightComponent>
            </div>
          )
      )}
      {data === null && (
        <div className="flex items-center justify-center pt-16">
          <DonutSkeleton size={220} thickness={32} />
        </div>
      )}
    </div>
  );
}

interface TotalByPeriodByCategory {
  income: Record<string, Record<string, number>>;
  expense: Record<string, Record<string, number>>;
}
