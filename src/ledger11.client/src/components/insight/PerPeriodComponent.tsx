import { InsightComponent } from "../widgets/InsightComponent";
import { useCategoryStore } from "@/lib/store-category";
import { useEffect, useState, useCallback } from "react";
import { fetchWithAuth } from "@/api";
import { Category } from "@/lib/types";
import { Button } from "@/components/ui/button";

type PerPeriodData = {
  title: string;
  expense: Record<string, number>;
  income: Record<string, number>;
};

const BATCH_SIZE = 5;

export default function PerPeriodComponent({ period }: { period: 'day' | 'week' | 'month' }) {
  const { categories, loadCategories } = useCategoryStore();
  const [data, setData] = useState<PerPeriodData[]>([]);
  const [start, setStart] = useState(0);
  const [loading, setLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);

  const colors = categories.reduce((acc, category) => {
    acc[category.name] = category;
    return acc;
  }, {} as Record<string, Category>);

  const fetchData = useCallback(async (fetchStart: number) => {
    setLoading(true);
    try {
      const response = await fetchWithAuth(`api/insight/per-period/${period}?start=${fetchStart}&count=${BATCH_SIZE}`);
      if (response.ok) {
        const newData = await response.json();
        if (fetchStart === 0) {
          setData(newData);
        } else {
          setData(prevData => [...prevData, ...newData]);
        }
        setStart(fetchStart + BATCH_SIZE);
        if (newData.length < BATCH_SIZE) {
          setHasMore(false);
        }
      }
    } finally {
      setLoading(false);
    }
  }, [period]);

  useEffect(() => {
    loadCategories();
  }, [loadCategories]);

  useEffect(() => {
    setData([]);
    setStart(0);
    setHasMore(true);
    fetchData(0);
  }, [period, fetchData]);

  const handleLoadMore = () => {
    fetchData(start);
  };

  if (loading && data.length === 0) {
    return <div>Loading...</div>;
  }

  return (
    <div className="relative space-y-4">
      {data.map(({ title, expense, income }) => (
        <div key={title} className="mb-4">
          <InsightComponent
            data={expense}
            altData={income}
            title={title}
            categories={colors}
          />
        </div>
      ))}
      {hasMore && (
        <div className="flex justify-center">
            <Button onClick={handleLoadMore} disabled={loading} variant={"secondary"}>
                {loading ? 'Loading...' : 'Load more...'}
            </Button>
        </div>
      )}
    </div>
  );
}