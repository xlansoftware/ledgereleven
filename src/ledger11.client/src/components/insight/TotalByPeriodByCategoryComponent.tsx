import { useEffect, useState } from "react";
import { fetchWithAuth } from "@/api";
import { InsightComponent } from "../widgets/InsightComponent";
import { useBookStore } from "@/lib/store-book";
import { type Category } from "@/lib/types";
import DonutSkeleton from "../DonutSkeleton";
import ViewMoreLink from "./ViewMoreLink";
// import { Button } from "../ui/button";
// import { PlusCircleIcon } from "lucide-react";
// import { IncomeComponent } from "../widgets/IncomeComponent";

interface TotalByPeriodByCategory {
  income: Record<string, Record<string, number>>;
  expense: Record<string, Record<string, number>>;
}

interface TotalByPeriodByCategoryComponentProps {
  setTab: (tab: string) => void;
}

export default function TotalByPeriodByCategoryComponent(
  props: TotalByPeriodByCategoryComponentProps
) {
  const { transactions, categories } = useBookStore();
  const colors = categories.reduce((acc, category) => {
    acc[category.name] = category;
    return acc;
  }, {} as Record<string, Category>);
  const [data, setData] = useState<TotalByPeriodByCategory | null>(null);

  useEffect(() => {
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
    // when transactions change, re-fetch the data
  }, [transactions]);

  const { expense, income } = data ?? {};

  const noExpenses =
    !expense ||
    Object.keys(expense).reduce(
      (acc, key) => acc + Object.keys(expense[key]).length,
      0
    ) === 0;

  return (
    <div className="relative">
      {/* <Button
        className="fixed top-16 right-4 z-50 w-12 h-12 flex items-center justify-center"
        variant={"ghost"}
      >
        <PlusCircleIcon className="w-16 h-16" />
      </Button> */}
      {noExpenses && (
        <div className="flex items-center justify-center pt-16">
          No expenses.
        </div>
      )}
      {Object.entries(expense || {}).map(([key, value]) => {
        const hasData = value && Object.keys(value).length > 0;
        if (!hasData) {
          return null;
        }
        return (
          <div
            key={key}
            className="mb-4 flex flex-col content-center items-center"
          >
            <InsightComponent
              data={value}
              altData={income?.[key]}
              title={key}
              categories={colors}
            />
            {key === "today" && (
              <ViewMoreLink onClick={() => props.setTab("per-day")}>
                view per day ...
              </ViewMoreLink>
            )}
            {key === "thisWeek" && (
              <ViewMoreLink onClick={() => props.setTab("per-week")}>
                view per week ...
              </ViewMoreLink>
            )}
            {key === "thisMonth" && (
              <ViewMoreLink onClick={() => props.setTab("per-month")}>
                view per month ...
              </ViewMoreLink>
            )}
          </div>
        );
      })}
      {data === null && (
        <div className="flex items-center justify-center pt-16">
          <DonutSkeleton size={220} thickness={32} />
        </div>
      )}
    </div>
  );
}
