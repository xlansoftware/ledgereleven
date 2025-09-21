'use client';

import { type StateCreator } from 'zustand';
import { fetchWithAuth } from '@/api';
import { type BookState } from './store-book';

export interface SettingsSlice {
  settings: Record<string, string | undefined>;
  getSetting: (key: string, defaultValue?: string) => string | undefined;
  setSetting: (key: string, value: string) => Promise<void>;
}

export const createSettingsSlice: StateCreator<BookState, [], [], SettingsSlice> = (
  set,
  get
) => ({
  settings: {},

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
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ value }), // ASP.NET Core default is camelCase
      });

      if (!response.ok) {
        console.error(`Failed to save setting ${key} to the server.`);
      }
    } catch (error) {
      console.error(`Error saving setting ${key}:`, error);
    }
  },
});