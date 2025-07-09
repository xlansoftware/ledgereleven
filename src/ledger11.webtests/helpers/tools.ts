import { request, expect, Page } from "playwright/test";

export const APP_URL = process.env.APP_URL || "http://localhost:5139/";

export async function dumpAllTestIds(page: Page) {
    const items = page.locator('[data-testid]');
    const count = await items.count();

    for (let i = 0; i < count; i++) {
        const item = items.nth(i);
        console.log(await item.getAttribute('data-testid'));
    }
}

export function generateRandomUsername() {
  const timestamp = Date.now(); // current time in ms
  const randomNum = Math.floor(Math.random() * 100000); // random number from 0 to 99999
  return `testuser+${timestamp}${randomNum}@example.com`;
}
