import { Page, expect } from '@playwright/test'
import { APP_URL, AUTH_URL } from './tools'

export async function login(page: Page, email: string, password: string) {
    // Go to home page
    await page.goto(APP_URL)

    // Click Login link
    await page.getByRole('link', { name: /Start managing your expenses/i }).click()

    // Wait for redirect to login page
    // console.log(page.url());
    await expect(page.url().startsWith(AUTH_URL)).toBe(true);

    // Fill email and password
    await page.getByRole('textbox', { name: /email/i }).fill(email)
    await page.getByRole('textbox', { name: /password/i }).fill(password)

    // Click login button
    await page.getByRole('button', { name: 'Log in' }).click()

    // Wait for final URL after all redirects
    // console.log(page.url());
    await page.waitForURL(`${APP_URL}app`);

    // Wait for redirect back to app
    // console.log(page.url());
    await expect(page).toHaveURL(`${APP_URL}app`);
}