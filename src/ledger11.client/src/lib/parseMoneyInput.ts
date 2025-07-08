type MoneyResult = {
  value: number;
  currency?: string;
};

export function parseMoneyInput(input: string): MoneyResult | null {
  if (!input) return null;

  const currencySymbols: Record<string, string> = {
    '$': 'USD',
    '€': 'EUR',
    '£': 'GBP',
    '¥': 'JPY',
  };

  let sanitized = input.trim();

  // Extract currency using RegExp (symbols or codes)
  const currencyRegex = /\b([A-Z]{3})\b|([$€£¥])/i;
  const currencyMatch = sanitized.match(currencyRegex);

  let currency: string | undefined;

  if (currencyMatch) {
    const [, code, symbol] = currencyMatch;
    currency = code?.toUpperCase() || currencySymbols[symbol] || undefined;
    sanitized = sanitized.replace(currencyMatch[0], ''); // remove the currency part from expression
  }

  // Clean up remaining string (e.g., remove extra whitespace)
  const mathExpression = sanitized.replace(/[^-()\d/*+.]/g, '').trim();

  if (!mathExpression) return null;

  let value: number;

  try {
    // Use Function constructor for safe eval of math expressions
    // Only if the string is known to contain only math-safe characters
    // eslint-disable-next-line no-new-func
    value = Function(`"use strict"; return (${mathExpression})`)();
    if (typeof value !== 'number' || isNaN(value)) return null;
  } catch {
    return null;
  }

  return { value, currency };
}
