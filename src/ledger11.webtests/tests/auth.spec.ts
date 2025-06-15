import { test, expect } from '@playwright/test';

import { APP_URL, assertTestMode, AUTH_URL, createTestUser } from '../helpers/tools';
import { login } from '../helpers/auth';

test('has title', async ({ page }) => {
  console.log('AUTH_URL:', AUTH_URL);
  console.log('APP_URL:', APP_URL);

  await page.goto(AUTH_URL);
  await expect(page).toHaveTitle(/Ledger Eleven/);

  await page.goto(APP_URL);
  await expect(page).toHaveTitle(/Ledger Eleven/);

});

test('Register flow works correctly', async ({ page }) => {
    const uniqueEmail = `testuser+${Date.now()}@example.com`
    const password = 'MySecurePassword123!'

    // the auth server must be in "test mode" to read emails
    await assertTestMode();

    // 1. Navigate to the app
    await page.goto(APP_URL)

    // 2. Expect "Not logged in" to be present
    // await expect(page.locator('text=Start Free Today')).toBeVisible()

    // 3. Click the "Login" link
    await page.getByRole('link', { name: /Start managing your expenses/i }).click()

    // 4. Expect redirect to login page on http://localhost:5001/
    // console.log(page.url());
    await expect(page.url().startsWith(AUTH_URL)).toBe(true);

    // 5. Click the "Register as a new user" link
    await page.getByRole('link', { name: /register as a new user/i }).click()

    // 6. Fill in Email field
    await page.getByRole('textbox', { name: /email/i }).fill(uniqueEmail)

    // 7. Fill in Password field
    await page.getByRole('textbox', { name: 'Password', exact: true }).fill(password)

    // 8. Fill in Confirm Password field
    await page.getByRole('textbox', { name: 'Confirm Password'}).fill(password)

    // 9. Click "Register" button
    await page.getByRole('button', { name: /register/i }).click()

    // 10. Assert the page shows confirmation
    await expect(page.url().startsWith(APP_URL)).toBe(true);

    // or on-bording page?
})

test('Login with created user', async ({ page }) => {
    const uniqueEmail = `testuser+${Date.now()}@example.com`
    const password = 'MySecurePassword123!'

    // console.log({ uniqueEmail, password });

    await createTestUser(uniqueEmail, password);
    await login(page, uniqueEmail, password)
})