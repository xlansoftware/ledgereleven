"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";
import { FilterRequest, Transaction } from "./types";
import { fetchWithAuth } from "@/api";

interface TransactionStoreState {
  transactions: Transaction[];
  totalCount?: number;

  // Actions
  addTransaction: (transaction: Omit<Transaction, "id">) => Promise<number>;
  updateTransaction: (id: number, data: Partial<Transaction>) => Promise<void>;
  removeTransaction: (id: number) => Promise<void>;
  loadTransactions: (
    clear: boolean,
    start?: number,
    limit?: number,
    filter?: FilterRequest
  ) => Promise<void>;
}

export const useTransactionStore = create<TransactionStoreState>()(
  persist(
    (set, get) => ({
      transactions: [],

      addTransaction: async (transaction) => {
        const response = await fetchWithAuth("/api/transaction", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(transaction),
        });

        if (!response.ok) throw new Error("Failed to add transaction");

        const created: Transaction = await response.json();

        set((state) => ({
          transactions: [...state.transactions, created],
        }));

        return created.id!;
      },

      updateTransaction: async (id, data) => {
        const response = await fetchWithAuth(`/api/transaction/${id}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(data),
        });

        if (!response.ok) throw new Error("Failed to update transaction");

        set((state) => ({
          transactions: state.transactions.map((t) =>
            t.id === id ? { ...t, ...data } : t
          ),
        }));
      },

      removeTransaction: async (id) => {
        const response = await fetchWithAuth(`/api/transaction/${id}`, {
          method: "DELETE",
        });

        if (!response.ok) throw new Error("Failed to delete transaction");

        set((state) => ({
          transactions: state.transactions.filter((t) => t.id !== id),
        }));
      },

      loadTransactions: async (
        clear: boolean,
        start?: number,
        limit?: number,
        filter?: FilterRequest
      ) => {
        const params = new URLSearchParams();

        if (start !== undefined) {
          params.append("start", start.toString());
        }

        if (limit !== undefined) {
          params.append("limit", limit.toString());
        }

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

        const response = await fetchWithAuth(
          `/api/filter?${params.toString()}`
        );

        if (!response.ok) throw new Error("Failed to load transactions");

        const {
          transactions,
          totalCount,
        }: { transactions: Transaction[]; totalCount: number } =
          await response.json();

        if (clear) {
          set({ transactions, totalCount });
          return;
        }

        const existingIds = new Set(transactions.map((t) => t.id));

        const existing = get().transactions;
        // Merge, giving priority to new data (e.g., if updated)
        const merged = [
          ...transactions,
          ...existing.filter((t) => !existingIds.has(t.id)),
        ];

        merged.sort((a, b) => {
          const dateA = new Date(a.date!).getTime();
          const dateB = new Date(b.date!).getTime();
          return dateB - dateA; // Sort in descending order
        });

        set(() => ({
          transactions: merged,
          totalCount,
        }));
      },
    }),
    {
      name: "transaction-store",
      skipHydration: true,
    }
  )
);
