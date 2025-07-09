import { test, expect } from '@playwright/test';

import { APP_URL } from '../helpers/tools';
import { login, logout, createUser } from '../helpers/auth';

test.use({
  viewport: { width: 640, height: 480 },
});

test('Add 42.0', async ({ page }) => {
    const uniqueEmail = `testuser+${Date.now()}@example.com`;
    const password = 'MySecurePassword123!';

    await page.goto(APP_URL);
    await createUser(page, uniqueEmail, password);

    await expect(page.url()).toBe(`${APP_URL}app`);
  
    //1. Type 42.0 in the "Amount" field
    await page.getByRole('textbox', { name: "Amount" }).fill('42.0');

    //2. Select category
    await page.getByRole('button', { name: "Category Groceries" }).click();

    //3. Click the "Add" button
    await page.getByRole('button', { name: "Add", exact: true }).click();

    //4. Go to the History tab
    await page.getByRole('button', { name: "History Screen" }).click();

    //5. Assert the page shows the item
    await expect(page.getByTestId('Item: Groceries, 42.00')).toBeVisible();

    // Add one item
    await page.getByRole('button', { name: "Add Screen" }).click();
    await page.getByRole('textbox', { name: "Amount" }).fill('11.0');
    await page.getByRole('button', { name: "Category Education" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();
    await page.getByRole('button', { name: "History Screen" }).click();
    await expect(page.getByTestId('Item: Education, 11.00')).toBeVisible();
    await expect(page.getByTestId('Item: Groceries, 42.00')).toBeVisible();


})

test('Add expression 2+2', async ({ page }) => {
    const uniqueEmail = `testuser+${Date.now()}@example.com`;
    const password = 'MySecurePassword123!';

    await page.goto(APP_URL);
    await createUser(page, uniqueEmail, password);

    await expect(page.url()).toBe(`${APP_URL}app`);

    // Add
    await page.getByRole('textbox', { name: "Amount" }).fill('2+2');
    await page.getByRole('button', { name: "Category Education" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();
    await page.getByRole('button', { name: "History Screen" }).click();
    await expect(page.getByTestId('Item: Education, 4.00')).toBeVisible();

})

test('Add with currency 20 BGL', async ({ page }) => {
    const uniqueEmail = `testuser+${Date.now()}@example.com`;
    const password = 'MySecurePassword123!';

    await page.goto(APP_URL);
    await createUser(page, uniqueEmail, password);

    await expect(page.url()).toBe(`${APP_URL}app`);

    // Add
    await page.getByRole('textbox', { name: "Amount" }).fill('20 BGL');
    await page.getByRole('button', { name: "Category Education" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();

    await page.getByRole('spinbutton', { name: 'Exchange Rate' }).fill("2");
    await expect(page.getByRole('spinbutton', { name: 'Result (EUR)' })).toHaveValue('40');
    await page.getByRole('button', { name: 'Confirm' }).click();

    await page.getByRole('button', { name: "History Screen" }).click();
    await expect(page.getByTestId('Item: Education, 40.00')).toBeVisible();

})
