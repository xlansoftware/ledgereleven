import { test, expect } from '@playwright/test';

import { APP_URL, generateRandomUsername } from '../helpers/tools';
import { createUser } from '../helpers/auth';

test.use({
  viewport: { width: 640, height: 480 },
});

test('Create book', async ({ page }) => {
    const uniqueEmail = generateRandomUsername();
    const password = 'MySecurePassword123!';

    await page.goto(APP_URL);
    await createUser(page, uniqueEmail, password);

    await expect(page.url()).toBe(`${APP_URL}app`);
  
    // Create record Groceries 42.0
    await page.getByRole('textbox', { name: "Amount" }).fill('42.0');
    await page.getByRole('button', { name: "Category Groceries" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();

    // Go to the History tab
    await page.getByRole('button', { name: "History Screen" }).click();

    // Assert the page shows the item
    await expect(page.getByTestId('Item: Groceries, 42.00')).toBeVisible();

    // Go to the Settings tab
    await page.getByRole('button', { name: "Settings Screen" }).click();

    // Create a new book
    await page.getByRole('button', { name: "Edit Books..." }).click();

    await page.getByRole('textbox', { name: "New book name" }).fill("Book2");
    await page.getByRole('button', { name: "Create" }).click();

    await expect(page.getByTestId('Space: Book2')).toBeVisible();

    // Switch to the new book
    await page.getByTestId('Space: Book2').click();

    // Assert the history shows no items

    // Go to the History tab
    await page.getByRole('button', { name: "History Screen" }).click();
    await expect(page.getByTestId('Item: Groceries, 42.00')).not.toBeVisible();

    // Add a new item in the new book

    // Go to the Add tab
    await page.getByRole('button', { name: "Add Screen" }).click();
    await page.getByRole('textbox', { name: "Amount" }).fill('11.0');
    await page.getByRole('button', { name: "Category Education" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();

    // Assert the history shows the item
    await page.getByRole('button', { name: "History Screen" }).click();
    await expect(page.getByTestId('Item: Education, 11.00')).toBeVisible();

    // Switch back to the original book

    // Got to the Settings tab
    await page.getByRole('button', { name: "Settings Screen" }).click();

    // Switch back to the original book
    await page.getByRole('combobox', { name: "Current Book:" }).click();
    await page.getByRole('option', { name: "Ledger" }).click();

    // Assert the history shows the item of the original book
    await page.getByRole('button', { name: "History Screen" }).click();
    await expect(page.getByTestId('Item: Groceries, 42.00')).toBeVisible();
    await expect(page.getByTestId('Item: Education, 11.00')).not.toBeVisible();

})

test('Book settings', async ({ page }) => {
    const uniqueEmail = generateRandomUsername();
    const password = 'MySecurePassword123!';

    await page.goto(APP_URL);
    await createUser(page, uniqueEmail, password);

    await expect(page.url()).toBe(`${APP_URL}app`);
  
    // Go to the Settings tab
    await page.getByRole('button', { name: "Settings Screen" }).click();

    await page.getByRole('button', { name: "Edit Books..." }).click();

    // The default book should be visible
    await expect(page.getByTestId('Space: Ledger')).toBeVisible();

    await page.getByRole('button', { name: "Actions" }).click();

    const dialogActions = page.getByRole('dialog', { name: "Actions" });
    await expect(dialogActions).toBeVisible();

    await dialogActions.getByRole('button', { name: "Edit" }).click();
    const dialogEdit = page.getByRole('dialog', { name: "Edit Book" });
    await expect(dialogEdit).toBeVisible();

    await dialogEdit.getByRole('textbox', { name: "Name" }).fill("Ledger 11");
    await dialogEdit.getByRole('textbox', { name: "Currency" }).fill("EUR");
    await dialogEdit.getByRole('button', { name: "Save" }).click();

    await expect(page.getByTestId('Space: Ledger 11')).toBeVisible();

})

test('Change book currency and recompute transactions', async ({ page }) => {
    const uniqueEmail = generateRandomUsername();
    const password = 'MySecurePassword123!';

    await page.goto(APP_URL);
    await createUser(page, uniqueEmail, password);

    await expect(page.url()).toBe(`${APP_URL}app`);

    // --- Create Initial Transactions ---
    // Transaction 1: Implicit USD (e.g., 100)
    await page.getByRole('textbox', { name: "Amount" }).fill('100');
    await page.getByRole('button', { name: "Category Groceries" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();
    await page.waitForTimeout(500); // Give time for toast to disappear

    // Transaction 2: Explicit JPY (e.g., 10000 JPY)
    await page.getByRole('textbox', { name: "Amount" }).fill('10000 JPY');
    await page.getByRole('button', { name: "Category Entertainment" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();
    await page.getByRole('spinbutton', { name: 'Exchange Rate' }).fill('0.007'); // JPY to USD
    await page.getByRole('button', { name: 'Confirm' }).click();
    await page.waitForTimeout(500);

    // Transaction 3: Explicit EUR (e.g., 50 EUR)
    await page.getByRole('textbox', { name: "Amount" }).fill('50 EUR');
    await page.getByRole('button', { name: "Category Education" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();
    await page.getByRole('spinbutton', { name: 'Exchange Rate' }).fill('1.08'); // EUR to USD
    await page.getByRole('button', { name: 'Confirm' }).click();
    await page.waitForTimeout(500);

    // Navigate to History to verify initial state (optional, but good for debugging)
    await page.getByRole('button', { name: "History Screen" }).click();
    await expect(page.getByTestId('Item: Groceries, 100.00')).toBeVisible(); // This will be USD
    await expect(page.getByTestId('Item: Entertainment, 70.00')).toBeVisible(); // 10000 JPY * 0.007 = 70 USD
    await expect(page.getByTestId('Item: Education, 54.00')).toBeVisible(); // 50 EUR * 1.08 = 54 USD


    // --- Go to Settings and Change Currency ---
    await page.getByRole('button', { name: "Settings Screen" }).click();
    await page.getByRole('button', { name: "Change Currency" }).click(); // Open the dialog

    // Assume initial currency is USD, changing to EUR with USD -> EUR rate 0.92
    await page.getByLabel('New Default Currency:').fill('EUR');
    await page.getByLabel('Exchange Rate (USD - EUR):').fill('0.92');

    // Click Save Changes
    await page.getByRole('button', { name: "Save Changes" }).click();

    // Confirm the warning dialog
    await page.getByRole('button', { name: "Confirm" }).click(); // Assuming confirm button text is "Confirm"
    await page.waitForTimeout(1000); // Wait for toast and update

    // --- Verify Updated Transactions in History ---
    await page.getByRole('button', { name: "History Screen" }).click();
    // After currency change to EUR (USD to EUR = 0.92, JPY to EUR = 0.007, EUR to EUR = 1)
    // Transaction 1 (100 USD with new default conversion 0.92): should appear as 92.00 EUR
    await expect(page.getByTestId('Item: Groceries, 92.00')).toBeVisible();

    // Transaction 2 (10000 JPY with JPY to EUR conversion 0.007): should appear as 70.00 EUR
    await expect(page.getByTestId('Item: Entertainment, 70.00')).toBeVisible();

    // Transaction 3 (50 EUR with EUR to EUR conversion 1): should appear as 50.00 EUR
    await expect(page.getByTestId('Item: Education, 50.00')).toBeVisible();
});