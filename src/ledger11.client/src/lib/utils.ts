import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";
import { format, Locale } from "date-fns";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function count(n: number | undefined, single: string, plural: string) {
  if (n === undefined || n === null) return "";
  return n === 1 ? `1 ${single}` : `${n} ${plural}`;
}

/**
 * Inverts a given color (hex, RGB, or HSL) and returns the inverted color as hex
 * @param color - The color to invert (e.g., "#FFFFFF", "rgb(255, 0, 0)", "hsl(0, 100%, 50%)")
 * @returns The inverted color in hex format (e.g., "#000000")
 */
export function invertColor(color: string): string {
  // Helper function to convert hex to RGB
  const hexToRgb = (hex: string): [number, number, number] => {
    const shorthandRegex = /^#?([a-f\d])([a-f\d])([a-f\d])$/i;
    hex = hex.replace(shorthandRegex, (_, r, g, b) => r + r + g + g + b + b);

    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    if (!result) throw new Error(`Invalid HEX color: ${hex}`);

    return [
      parseInt(result[1], 16),
      parseInt(result[2], 16),
      parseInt(result[3], 16),
    ];
  };

  // Helper function to convert RGB to hex
  const rgbToHex = (r: number, g: number, b: number): string => {
    return (
      "#" +
      [r, g, b]
        .map((x) => {
          const hex = x.toString(16);
          return hex.length === 1 ? "0" + hex : hex;
        })
        .join("")
    );
  };

  // Handle hex colors
  if (/^#?[0-9a-f]{3,6}$/i.test(color)) {
    const [r, g, b] = hexToRgb(color);
    return rgbToHex(255 - r, 255 - g, 255 - b);
  }

  // Handle rgb() colors
  const rgbMatch = color.match(/^rgb\((\d+),\s*(\d+),\s*(\d+)\)$/i);
  if (rgbMatch) {
    const r = parseInt(rgbMatch[1], 10);
    const g = parseInt(rgbMatch[2], 10);
    const b = parseInt(rgbMatch[3], 10);
    return rgbToHex(255 - r, 255 - g, 255 - b);
  }

  // Handle hsl() colors - convert to RGB first, then invert
  const hslMatch = color.match(/^hsl\((\d+),\s*(\d+)%,\s*(\d+)%\)$/i);
  if (hslMatch) {
    const h = parseInt(hslMatch[1], 10) / 360;
    const s = parseInt(hslMatch[2], 10) / 100;
    const l = parseInt(hslMatch[3], 10) / 100;

    // Convert HSL to RGB
    let r, g, b;
    if (s === 0) {
      r = g = b = l;
    } else {
      const hue2rgb = (p: number, q: number, t: number) => {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1 / 6) return p + (q - p) * 6 * t;
        if (t < 1 / 2) return q;
        if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
        return p;
      };

      const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
      const p = 2 * l - q;

      r = hue2rgb(p, q, h + 1 / 3);
      g = hue2rgb(p, q, h);
      b = hue2rgb(p, q, h - 1 / 3);
    }

    const rgb = [Math.round(r * 255), Math.round(g * 255), Math.round(b * 255)];
    return rgbToHex(255 - rgb[0], 255 - rgb[1], 255 - rgb[2]);
  }

  throw new Error(`Invalid color format: ${color}`);
}

export function formatCurrency(
  amount: number,
  fractionDigits: number = 2
): string {
  // Define the formatting options
  const options: Intl.NumberFormatOptions = {
    // style: "currency",
    // currency: "USD",
    minimumFractionDigits: fractionDigits, // Set minimum decimal places
    maximumFractionDigits: fractionDigits, // Set maximum decimal places
  };

  // Create a formatter and format the amount
  return new Intl.NumberFormat("en-US", options).format(amount);
}

export function formatDate(dateUtc: Date): string {
  const now = new Date();
  const date = new Date(dateUtc); // This creates a local time from UTC timestamp

  const isSameDay = (d1: Date, d2: Date) =>
    d1.getFullYear() === d2.getFullYear() &&
    d1.getMonth() === d2.getMonth() &&
    d1.getDate() === d2.getDate();

  const timeFormatter = new Intl.DateTimeFormat(undefined, {
    hour: "2-digit",
    minute: "2-digit",
  });

  const weekdayFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: "long",
  });

  const monthDayFormatter = new Intl.DateTimeFormat(undefined, {
    month: "long",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });

  const fullDateFormatter = new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "long",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });

  const yesterday = new Date(now);
  yesterday.setDate(now.getDate() - 1);

  const startOfWeek = new Date(now);
  startOfWeek.setDate(now.getDate() - ((now.getDay() + 6) % 7)); // last Monday

  const isToday = isSameDay(now, date);
  const isYesterday = isSameDay(yesterday, date);
  const isThisWeek = date >= startOfWeek;
  const isThisYear = now.getFullYear() === date.getFullYear();

  const timeStr = timeFormatter.format(date);

  if (isToday) {
    return `Today ${timeStr}`;
  } else if (isYesterday) {
    return `Yesterday ${timeStr}`;
  } else if (isThisWeek) {
    return `${weekdayFormatter.format(date)} ${timeStr}`;
  } else if (isThisYear) {
    return monthDayFormatter.format(date);
  } else {
    return fullDateFormatter.format(date);
  }
}

export function downloadUrl(url: string) {
  const a = document.createElement("a");
  a.setAttribute("hidden", "");
  a.setAttribute("href", url);
  // a.setAttribute("download", filename);
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
}


export function formatDateWithoutCurrentYear(
  date: Date | string | undefined,
  locale?: Locale
): string {
  if (!date) return "";
  
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  const currentYear = new Date().getFullYear();
  const dateYear = dateObj.getFullYear();
  
  return format(
    dateObj, 
    dateYear === currentYear ? "MMM d" : "MMM d, yyyy",
    { locale }
  );
}