import { test, expect } from '@playwright/test';

import { APP_URL, generateRandomUsername } from '../helpers/tools';
import { login, logout } from '../helpers/auth';

test('has title', async ({ page }) => {
  // this test mostly makes sure that the site is accessible...
  await page.goto(APP_URL);
  await expect(page).toHaveTitle(/Ledger Eleven/);
});

test('Register flow works correctly', async ({ page }) => {
    const uniqueEmail = generateRandomUsername()
    const password = 'MySecurePassword123!'

    // 1. Navigate to the app
    await page.goto(APP_URL)

    // 2. Expect "Not logged in" to be present
    // TODO: Add some "Greeting" text
    // await expect(page.locator('text=Start Free Today')).toBeVisible()

    // 3. Click the "Login" link
    await page.getByRole('link', { name: /Start managing your expenses/i }).click()

    // 4. Click the "Register as a new user" link
    await page.getByRole('link', { name: /register as a new user/i }).click()

    // 5. Fill in Email field
    await page.getByRole('textbox', { name: /email/i }).fill(uniqueEmail)

    // 6. Fill in Password field
    await page.getByRole('textbox', { name: 'Password', exact: true }).fill(password)

    // 7. Fill in Confirm Password field
    await page.getByRole('textbox', { name: 'Confirm Password'}).fill(password)

    // 8. Click "Register" button
    await page.getByRole('button', { name: /register/i }).click()

    // 9. Assert the page shows confirmation
    // console.log(`3: ${page.url()}`);
    await expect(page.url().startsWith(APP_URL)).toBe(true);

    // or start a on-bording process?

    // Test the new login
    await logout(page)
    
    await login(page, uniqueEmail, password)
})
