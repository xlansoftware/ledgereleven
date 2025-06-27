import { Page, expect } from '@playwright/test'
import { APP_URL } from './tools'

export async function logout(page: Page) {
    await page.goto(`${APP_URL}Identity/Account/Logout`);
    await expect(page).toHaveURL(`${APP_URL}Identity/Account/Logout`);  

    // Click Logout link
    await page.getByRole('button', { name: /Click here to Logout/i }).click()
}

export async function login(page: Page, email: string, password: string) {
    // Go to home page
    await page.goto(APP_URL)

    // Click Login link
    await page.getByRole('link', { name: /Start managing your expenses/i }).click()

    // Fill email and password
    await page.getByRole('textbox', { name: /email/i }).fill(email)
    await page.getByRole('textbox', { name: /password/i }).fill(password)

    // Click login button
    await page.getByRole('button', { name: 'Log in' }).click()

    // Wait for final URL after all redirects
    console.log(`1: ${page.url()}`);
    await page.waitForURL(`${APP_URL}app`);

    // Wait for redirect back to app
    console.log(`2: ${page.url()}`);
    await expect(page).toHaveURL(`${APP_URL}app`);
}