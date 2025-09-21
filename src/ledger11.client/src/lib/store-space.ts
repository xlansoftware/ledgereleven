"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";
import { type Space } from "./types";
import { fetchWithAuth } from "@/api";

interface SpaceStoreState {
  current?: Space;
  spaces: Space[];
  closedSpaces: Space[];

  addSpace: (category: Omit<Space, "id">) => Promise<string>;
  updateSpace: (id: string, updatedFields: Partial<Space>) => Promise<void>;
  removeSpace: (id: string) => Promise<void>;
  setCurrentSpace: (id: string) => Promise<void>;
  loadSpaces: (includeDetails?: boolean) => Promise<Space | undefined>;
}

export const useSpaceStore = create<SpaceStoreState>()(
  persist(
    (set, get) => ({
      current: undefined,
      spaces: [],
      closedSpaces: [],

      addSpace: async (space) => {
        const response = await fetchWithAuth("/api/space", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(space),
        });

        if (!response.ok) throw new Error("Failed to add space");

        const newSpace: Space = await response.json();

        set((state) => ({
          spaces: [newSpace, ...state.spaces],
        }));

        return newSpace.id!;
      },

      updateSpace: async (id, updatedFields) => {
        const response = await fetchWithAuth(`/api/space/${id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(updatedFields),
        });

        if (!response.ok) throw new Error("Failed to update space");

        const updatedSpace: Space = await response.json();

        set((state) => ({
          current: state.current?.id === id ? updatedSpace : state.current,
          spaces: state.spaces.map((space) =>
            space.id === id ? updatedSpace : space
          ),
        }));
      },

      removeSpace: async (id) => {
        const response = await fetchWithAuth(`/api/space/${id}`, {
          method: "DELETE",
        });

        if (!response.ok) throw new Error("Failed to delete space");

        set((state) => ({
          spaces: state.spaces.filter((space) => space.id !== id),
        }));
      },

      setCurrentSpace: async (id: string) => {
        const response = await fetchWithAuth("/api/space/current", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(id),
        });

        if (!response.ok) throw new Error("Failed to set current space");

        const current = {
          ...get().spaces.find((s) => s.id === id),
        };
        set({ current });
      },

      loadSpaces: async (includeDetails = false) => {
        const response = await fetchWithAuth(`/api/space?includeDetails=${includeDetails}`);

        if (!response.ok) throw new Error("Failed to load categories");

        const result: SpaceStoreState = await response.json();

        const { spaces, closedSpaces } = result.spaces.reduce(
          (acc, space) => {
            if (space.settings && space.settings["Status"] === "Closed") {
              acc.closedSpaces.push(space);
            } else {
              acc.spaces.push(space);
            }
            return acc;
          },
          { spaces: [], closedSpaces: [] } as { spaces: Space[]; closedSpaces: Space[] }
        );

        set({
          current: result.current,
          spaces: spaces,
          closedSpaces: closedSpaces,
        });

        return result.current;
      },
    }),
    {
      name: "ledger11-spaces-storage",
      skipHydration: true,
    }
  )
);
