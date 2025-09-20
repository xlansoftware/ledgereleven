import { create } from "zustand";
import { fetchWithAuth } from "@/api";
import { type CategorySlice, createCategorySlice } from "./store-category";
import { type TransactionSlice, createTransactionSlice } from "./store-transaction";
import { type SettingsSlice, createSettingsSlice } from "./store-settings";

// 2. Define the combined state
export interface BookState
  extends CategorySlice,
    TransactionSlice,
    SettingsSlice {
  isLoading: boolean;
  error: string | null;
  currentSpaceId: string | null;
  openBook: (spaceId: string) => Promise<void>;
}

// 3. Create the unified store
export const useBookStore = create<BookState>()((...a) => ({
  isLoading: false,
  error: null,
  currentSpaceId: null,
  ...createCategorySlice(...a),
  ...createTransactionSlice(...a),
  ...createSettingsSlice(...a),

  // --- Core Action ---
  openBook: async (spaceId: string) => {
    const set = a[0];
    set({ isLoading: true, error: null, currentSpaceId: spaceId });
    try {
      const response = await fetchWithAuth(`/api/book/open/${spaceId}`);
      if (!response.ok) {
        throw new Error("Failed to open book.");
      }
      const data = await response.json();

      // Set the entire state at once
      set({
        categories: data.categories,
        transactions: data.transactions.items,
        totalCount: data.transactions.totalCount,
        settings: data.settings,
        isLoading: false,
      });
    } catch (e: unknown) {
      set({ isLoading: false, error: (e as Error).message });
    }
  },
}));