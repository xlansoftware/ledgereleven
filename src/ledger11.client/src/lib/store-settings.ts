"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";
import { fetchWithAuth } from "@/api";

interface SettingsState {
  settings: Record<string, string | undefined>;
  loadSettings: () => Promise<void>;
  getSetting: (key: string, defaultValue?: string) => string | undefined;
  setSetting: (key: string, value: string) => Promise<void>;

  // Accessor to the Language setting
  setLanguage: (language: string) => Promise<void>;
  getLanguage: () => string;
}

export const useSettingsStore = create<SettingsState>()(
  persist(
    (set, get) => ({
      settings: {},

      loadSettings: async () => {
        try {
          const response = await fetchWithAuth("/api/settings");
          if (!response.ok) {
            console.error("Failed to load settings from server.");
            return;
          }
          const serverSettings = await response.json();
          set((state) => ({
            settings: { ...state.settings, ...serverSettings },
          }));
        } catch (error) {
          console.error("Error loading settings:", error);
        }
      },

      getSetting: (key: string, defaultValue?: string) => {
        return get().settings[key] ?? defaultValue;
      },

      setSetting: async (key: string, value: string) => {
        // Optimistic update
        set((state) => ({
          settings: { ...state.settings, [key]: value },
        }));

        try {
          const response = await fetchWithAuth(`/api/settings/${key}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ value }), // ASP.NET Core default is camelCase
          });

          if (!response.ok) {
            console.error(`Failed to save setting ${key} to the server.`);
          }
        } catch (error) {
          console.error(`Error saving setting ${key}:`, error);
        }
      },

      setLanguage: async (language: string) => {
        await get().setSetting("Language", language);
      },
      getLanguage: () => {
        return get().getSetting("Language", "English") ?? "English";
      },

    }),
    {
      name: "ledger11-global-settings",
    }
  )
);
