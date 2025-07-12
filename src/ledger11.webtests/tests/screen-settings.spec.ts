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