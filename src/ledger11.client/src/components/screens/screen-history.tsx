import { useBookStore } from "@/lib/store-book";
import { useEffect, useState } from "react";
import TransactionRow from "../history/TransactionRow";
import { Button } from "../ui/button";
import DonutSkeleton from "../DonutSkeleton";
import { SearchIcon, X } from "lucide-react";
import Filter from "../history/Filter";
import { type FilterRequest } from "@/lib/types";
import { Badge } from "../ui/badge";
import { count } from "@/lib/utils";
import { fetchWithAuth } from "@/api";

const PAGE_SIZE = 10;

function isSameDay(date1: Date, date2: Date) {
  if (!date1 || !date2) return false;
  return (
    date1.getFullYear() === date2.getFullYear() &&
    date1.getMonth() === date2.getMonth() &&
    date1.getDate() === date2.getDate()
  );
}

function getTransactionGroup(
  transactionDate: string | Date | undefined,
  today: Date
) {
  if (!transactionDate) return "More";
  if (typeof transactionDate === "string")
    transactionDate = new Date(transactionDate);
  if (isSameDay(transactionDate, today)) {
    return "Today";
  }

  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);
  if (isSameDay(transactionDate, yesterday)) {
    return "Yesterday";
  }

  const sevenDaysAgo = new Date(today);
  sevenDaysAgo.setDate(today.getDate() - 7);
  if (transactionDate > sevenDaysAgo) {
    return "This week";
  }

  return "This month";
}

export default function HistoryScreen() {
  const [loading, setLoading] = useState(false);
  const {
    transactions,
    totalCount,
    loadTransactions,
    categories,
    isLoading: initialLoading,
  } = useBookStore();
  const [showFilter, setShowFilter] = useState(false);
  const [filter, setFilter] = useState<FilterRequest | undefined>();
  const today = new Date();

  useEffect(() => {
    // auto refresh on visit
    loadTransactions(true).then(() => {
      console.log(`refresh complete`);
    });
  }, [loadTransactions])


  const [filterArgument, setFilterArgument] = useState<{
    categories: number[];
    users: string[];
  }>({ categories: [], users: [] });

  const handleLoadMore = async () => {
    setLoading(true);
    await loadTransactions(false, transactions.length, PAGE_SIZE, filter);
    setLoading(false);
  };

  const handleClearFilter = async () => {
    setFilter(undefined);
    setShowFilter(false);
    await loadTransactions(true, 0, PAGE_SIZE * 2, undefined);
  };

  const handleOpenFilter = async () => {
    const response = await fetchWithAuth("/api/filter/arguments");
    if (response.ok) {
      const data = await response.json();
      setFilterArgument({
        categories: data.categories,
        users: data.users,
      });
    }
    setShowFilter(true);
  };

  const groupedTransactions = transactions.reduce(
    (groups: { [key: string]: typeof transactions }, transaction) => {
      const group = getTransactionGroup(transaction.date, today);
      if (!groups[group]) groups[group] = [];
      groups[group].push(transaction);
      return groups;
    },
    {}
  );

  const groupOrder = ["Today", "Yesterday", "This week", "This month", "More"];

  if (initialLoading && transactions.length === 0) {
    return (
      <div className="flex items-center justify-center pt-16">
        <DonutSkeleton size={220} thickness={32} />
      </div>
    );
  }

  return (
    <>
      {showFilter && (
        <Filter
          filter={filter || {}}
          categories={categories.filter(
            (c) => filterArgument.categories.indexOf(c.id) !== -1
          )}
          users={filterArgument.users}
          onClose={() => setShowFilter(false)}
          onApply={async (filter) => {
            setShowFilter(false);
            setFilter(filter);
            await loadTransactions(true, 0, PAGE_SIZE * 2, filter);
          }}
        />
      )}
      {!showFilter && (
        <div className="flex items-center justify-end w-full pl-1">
          {filter && (
            <Badge
              variant={"outline"}
              className="mr-2 text-muted-foreground cursor-pointer"
              onClick={handleClearFilter}
            >
              Filter returned {count(totalCount, "record", "records")}
              <button
                onClick={handleClearFilter}
                className="ml-1 rounded-full outline-none focus:ring-2 focus:ring-ring"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
          <div className="flex-1"></div>
          <Button variant="link" onClick={() => handleOpenFilter()}>
            <SearchIcon />
            Filter
          </Button>
        </div>
      )}

      {!initialLoading && transactions.length === 0 && (
        <div className="flex items-center justify-center pt-16">
          No records.
        </div>
      )}
      {groupOrder.map((groupName) => {
        const transactionsInGroup = groupedTransactions[groupName] || [];
        if (transactionsInGroup.length === 0) return null;
        return (
          <div key={groupName}>
            <div className="text-sm font-medium text-muted-foreground bg-muted p-2">
              {groupName}
            </div>
            {transactionsInGroup.map((transaction) => (
              <TransactionRow key={transaction.id} transaction={transaction} />
            ))}
          </div>
        );
      })}
      {transactions.length > 0 &&
        totalCount &&
        transactions.length < totalCount && (
          <Button
            disabled={loading}
            className="w-full mt-4"
            variant={"secondary"}
            onClick={handleLoadMore}
          >
            {loading ? "Loading..." : "Load more"}
          </Button>
        )}
    </>
  );
}

