type MoneyResult = {
  value: number;
  currency?: string;
};

export function parseMoneyInput(input: string): MoneyResult | null {
  if (!input) return null;

  const currencySymbols: Record<string, string> = {
    $: "USD",
    "€": "EUR",
    "£": "GBP",
    "¥": "JPY",
  };

  let sanitized = input.trim();
  let currency: string | undefined;

  // Regex for currency at start or end (e.g. USD5, 20+5USD, USD 5, 20 + 5 USD)
  const startOrEndCurrencyRegex =
    /^(?:([A-Z]{3})|([$€£¥]))\s*|(?:\s*([A-Z]{3})|([$€£¥]))$/i;
  const match = sanitized.match(startOrEndCurrencyRegex);

  if (match) {
    const code = match[1] || match[3];
    const symbol = match[2] || match[4];
    currency = code?.toUpperCase() || currencySymbols[symbol] || undefined;

    // Remove matched currency from start or end
    sanitized = sanitized.replace(startOrEndCurrencyRegex, "").trim();
  }

  // Extract only math-safe characters
  const mathExpression = sanitized.replace(/[^-()\d/*+.]/g, "").trim();
  if (!mathExpression) return null;

  let value: number;
  try {
    // Evaluate math expression safely
    value = Function(`"use strict"; return (${mathExpression})`)();
    if (typeof value !== "number" || isNaN(value)) return null;
  } catch {
    return null;
  }

  return { value, currency };
}
