import { Space } from "./types";

export function getCurrencySetting(space: Space | undefined): string {
    return space?.settings?.Currency || "USD"; // Default to USD
}

export function getTintSetting(space: Space | undefined): string {
    return space?.settings?.Tint || "#FFFFFF"; // Default to white
}