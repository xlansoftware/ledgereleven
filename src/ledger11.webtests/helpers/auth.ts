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
    // console.log(`1: ${page.url()}`);
    await page.waitForURL(`${APP_URL}app`);

    // Wait for redirect back to app
    // console.log(`2: ${page.url()}`);
    await expect(page).toHaveURL(`${APP_URL}app`);
}

export async function createUser(page: Page, email: string, password: string) {
    // 1. Navigate to the app
    await page.goto(APP_URL)

    // 2. Click the "Login" link
    await page.getByRole('link', { name: /Start managing your expenses/i }).click()

    // 3. Click the "Register as a new user" link
    await page.getByRole('link', { name: /register as a new user/i }).click()

    // 4. Fill in Email field
    await page.getByRole('textbox', { name: /email/i }).fill(email)

    // 5. Fill in Password field
    await page.getByRole('textbox', { name: 'Password', exact: true }).fill(password)

    // 6. Fill in Confirm Password field
    await page.getByRole('textbox', { name: 'Confirm Password'}).fill(password)

    // 7. Click "Register" button
    await page.getByRole('button', { name: /register/i }).click()

    // 8. Assert the page shows confirmation
    await expect(page.url().startsWith(APP_URL)).toBe(true);

}

