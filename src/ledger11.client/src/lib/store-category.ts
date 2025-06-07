"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";
import { Category } from "./types";
import { fetchWithAuth } from "@/api";

interface CategoryStoreState {
  categories: Category[];
  color: Record<string, string>;

  addCategory: (category: Omit<Category, "id">) => Promise<number>;
  updateCategory: (id: number, category: Partial<Category>) => Promise<void>;
  removeCategory: (id: number, replaceWithId?: number) => Promise<Category[]>;
  loadCategories: () => Promise<Category[]>;
  categoryById: (id?: number) => Category | undefined;
}

export const useCategoryStore = create<CategoryStoreState>()(
  persist(
    (set, get) => ({
      categories: [],
      color: {},

      addCategory: async (category) => {
        const response = await fetchWithAuth("/api/category", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(category),
        });

        if (!response.ok) throw new Error("Failed to add category");

        const newCategory: Category = await response.json();

        set((state) => ({
          categories: [newCategory, ...state.categories],
          color: { ...state.color, [newCategory.id]: newCategory.color },
        }));

        return newCategory.id;
      },

      updateCategory: async (id, updatedFields) => {
        const response = await fetchWithAuth(`/api/category/${id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(updatedFields),
        });

        if (!response.ok) throw new Error("Failed to update category");

        const updatedCategory: Category = await response.json();

        set((state) => ({
          categories: state.categories.map((cat) =>
            cat.id === id ? updatedCategory : cat
          ),
          color: {
            ...state.color,
            [updatedCategory.id]: updatedCategory.color,
          },
        }));
      },

      removeCategory: async (id, replaceWithId) => {
        
        const params = new URLSearchParams();

        if (replaceWithId !== undefined) {
          params.append("replaceWithId", replaceWithId.toString());
        }

        const response = await fetchWithAuth(`/api/category/${id}?${params.toString()}`, {
          method: "DELETE",
        });

        if (!response.ok) throw new Error("Failed to delete category");

        const categories = get().categories.filter((cat) => cat.id !== id);
        set((state) => ({
          categories: categories,
          color: Object.fromEntries(
            Object.entries(state.color).filter(([key]) => key !== id.toString())
          ),
        }));
        return categories;
      },

      loadCategories: async () => {
        const response = await fetchWithAuth("/api/category");

        if (!response.ok) throw new Error("Failed to load categories");

        const categories: Category[] = await response.json();

        set({
          categories,
          color: Object.fromEntries(
            categories.map((cat) => [cat.id.toString(), cat.color || ""])
          ),
        });

        return categories;
      },

      categoryById: (id?: number) => {
        if (!id) return;
        const result = get().categories.find((c) => c.id === id);
        return result;
      },
    }),
    {
      name: "expense-tracker-category-storage",
      skipHydration: true,
    }
  )
);
