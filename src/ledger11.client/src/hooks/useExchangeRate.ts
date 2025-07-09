import { fetchWithAuth } from "@/api";
import { useEffect, useRef, useState } from "react";

// API response type definition
interface ExchangeRateResponse {
  from: string;
  to: string;
  rate: number;
  source: "cache" | "live";
}

/**
 * Custom hook that fetches and returns the exchange rate between two currencies.
 * Automatically re-fetches when currencies change.
 * Remembers and returns the last known valid rate while fetching or on error.
 */
export function useExchangeRate(
  fromCurrency: string,
  toCurrency: string
): number | undefined {
  const [rate, setRate] = useState<number | undefined>(undefined);

  // Store last successful rate as fallback
  const lastKnownRateRef = useRef<number | undefined>(undefined);

  useEffect(() => {
    if (!fromCurrency || !toCurrency) return;

    const fetchRate = async () => {
      try {
        const params = new URLSearchParams({ fromCurrency, toCurrency });
        const response = await fetchWithAuth(
          `/api/currency/exchange-rate?${params.toString()}`
        );

        if (!response.ok) {
          console.warn(`Exchange rate fetch failed: ${response.status}`);
          return;
        }

        const data: ExchangeRateResponse = await response.json();
        setRate(data.rate);
        lastKnownRateRef.current = data.rate;
      } catch (error) {
        console.error("Error fetching exchange rate:", error);
        // Use last known rate as fallback
        setRate(lastKnownRateRef.current);
      }
    };

    fetchRate();
  }, [fromCurrency, toCurrency]);

  return rate;
}
