import { create } from "zustand";
import { persist } from "zustand/middleware";

interface SettingsState {
  preferredReceiptLanguage: string;
  setPreferredReceiptLanguage: (language: string) => void;
  getPreferredReceiptLanguage: () => string;
}

const useSettingsStore = create<SettingsState>()(
  persist(
    (set, get) => ({
      preferredReceiptLanguage: "eng", // Default language is English
      setPreferredReceiptLanguage: (language: string) =>
        set({ preferredReceiptLanguage: language }),
      getPreferredReceiptLanguage: () => get().preferredReceiptLanguage,
    }),
    {
      name: "expense-tracker-settings",
    }
  )
);

export default useSettingsStore;
