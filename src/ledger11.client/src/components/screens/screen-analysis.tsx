import { useEffect, useState } from "react";
import Filter from "../history/Filter";
import { type FilterRequest, type Transaction } from "@/lib/types";
import { useBookStore } from "@/lib/store-book";
import { fetchWithAuth } from "@/api";
import { SectionCards } from "../analysis/SectionCards";

// interface Stat {
//   avg: number;
//   total: number;
// }

async function loadTransactions(
  filter?: FilterRequest
): Promise<{ transactions: Transaction[]; totalCount: number }> {
  const params = new URLSearchParams();

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

export default function ScreenAnalysis() {
  const { categories } = useBookStore();

  const [filter, setFilter] = useState<FilterRequest | undefined>();

  const [filterArgument, setFilterArgument] = useState<{
    categories: number[];
    users: string[];
  }>({ categories: [], users: [] });

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

  useEffect(() => {
    initFilter();
  }, []);

  return (
    <div className="container flex items-center justify-center max-w-80">
      <Filter
        filter={filter || {}}
        categories={categories.filter(
          (c) => filterArgument.categories.indexOf(c.id) !== -1
        )}
        users={filterArgument.users}
        onApply={async (filter) => {
          setFilter(filter);
          await loadTransactions(filter);
        }}
      />

      <div className="flex flex-1 flex-col">
        <div className="@container/main flex flex-1 flex-col gap-2">
          <div className="flex flex-col gap-4 py-4 md:gap-6 md:py-6">
            <SectionCards />
            <div className="px-4 lg:px-6">
              {/* <ChartAreaInteractive /> */}
            </div>
            {/* <DataTable data={data} /> */}
          </div>
        </div>
      </div>
    </div>
  );
}
