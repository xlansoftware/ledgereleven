import { test, expect } from '@playwright/test';

import { APP_URL, generateRandomUsername } from '../helpers/tools';
import { createUser } from '../helpers/auth';

test.use({
  viewport: { width: 640, height: 480 },
});

test('Merge books', async ({ page }) => {
    const uniqueEmail = generateRandomUsername();
    const password = 'MySecurePassword123!';

    await page.goto(APP_URL);
    await createUser(page, uniqueEmail, password);

    await expect(page.url()).toBe(`${APP_URL}app`);
  
    // Create record Groceries 42.0
    await page.getByRole('textbox', { name: "Amount" }).fill('42.0');
    await page.getByRole('button', { name: "Category Groceries" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();

    // Go to the Settings tab
    await page.getByRole('button', { name: "Settings Screen" }).click();

    // Create a new book
    await page.getByRole('button', { name: "Edit Books..." }).click();

    await page.getByRole('textbox', { name: "New book name" }).fill("Vacation 2025");
    await page.getByRole('button', { name: "Create" }).click();

    await expect(page.getByTestId('Space: Vacation 2025')).toBeVisible();

    // Switch to the new book
    await page.getByTestId('Space: Vacation 2025').click();

    // Create few records in the new book

    // Go to the Add tab
    await page.getByRole('button', { name: "Add Screen" }).click();
    await page.getByRole('button', { name: "Category Entertainment" }).click();

    // Add two Entertainment records
    const textboxAmount = page.getByRole('textbox', { name: "Amount" });
    const buttonAdd = page.getByRole('button', { name: "Add", exact: true });
    
    await textboxAmount.fill('11.0');
    await buttonAdd.click();
    await page.waitForTimeout(1000);

    await textboxAmount.fill('108.0');
    await buttonAdd.click();
    await page.waitForTimeout(1000);

    // Close the Vacation book

    // Got to the Settings tab -> Spaces
    await page.getByRole('button', { name: "Settings Screen" }).click();
    await page.getByRole('button', { name: "Edit Books..." }).click();

    await page.getByTestId('Space: Vacation 2025').getByRole('button', { name: "Actions" }).click();
    const dialogActions = page.getByRole('dialog', { name: "Actions" });
    await expect(dialogActions).toBeVisible();

    await dialogActions.getByRole('button', { name: "Close and Merge" }).click();
    const dialogMerge = page.getByRole('dialog', { name: "Merge Vacation 2025" });
    await expect(dialogMerge).toBeVisible();
    // "Target Book:" should be Ledger because it is the only one available
    await expect(dialogMerge.getByRole('combobox', { name: "Target Book:" })).toHaveText("Ledger");
    await dialogMerge.getByRole('button', { name: "Merge" }).click();

    // Switch back to the original book
    await page.getByTestId('Space: Ledger').click();

    // Assert the history shows the record for the merged book
    await page.getByRole('button', { name: "History Screen" }).click();
    await expect(page.getByTestId('Item: Vacation 2025, 119.00')).toBeVisible();

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