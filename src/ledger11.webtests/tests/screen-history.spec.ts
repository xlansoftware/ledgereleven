import { test, expect } from '@playwright/test';

import { APP_URL, generateRandomUsername } from '../helpers/tools';
import { createUser } from '../helpers/auth';

test.use({
  viewport: { width: 640, height: 480 },
});

test('Edit item', async ({ page }) => {
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

    // Open edit form
    await page.getByRole('button', { name: "Actions" }).click();
    await page.getByRole('button', { name: "Edit" }).click();

    // Change amount to 11.0
    const editDialog = page.getByRole('dialog', { name: 'Edit Transaction' });
    expect(editDialog).toBeVisible();

    await editDialog.getByRole('spinbutton', { name: "Value" }).fill('11.0');
    await editDialog.getByRole('textbox', { name: "Notes" }).fill('Edited');
    await editDialog.getByText("Groceries").click();

    const chooseCategoryDialog = page.getByRole('dialog', { name: "Choose Category" });
    await expect(chooseCategoryDialog).toBeVisible();

    await chooseCategoryDialog.getByText("Education").click();
    await expect(editDialog.getByText("Education")).toBeVisible();

    await editDialog.getByRole('button', { name: "Save Changes" }).click();

    // Assert the page does NOT shows the item
    await expect(page.getByTestId('Item: Groceries, 42.00')).not.toBeVisible();
    // Assert the page shows the updated item
    const editedItem = page.getByTestId('Item: Edited, 11.00');
    await expect(editedItem).toBeVisible();
    await expect(editedItem.getByText("Education")).toBeVisible();

})

test('Edit item currency', async ({ page }) => {
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

    // Open edit form
    await page.getByRole('button', { name: "Actions" }).click();
    await page.getByRole('button', { name: "Edit" }).click();

    // Change currency and exchange rate
    const editDialog = page.getByRole('dialog', { name: 'Edit Transaction' });
    expect(editDialog).toBeVisible();

    await editDialog.getByRole('textbox', { name: "Currency" }).fill('EUR');
    await editDialog.getByRole('spinbutton', { name: "Exchange rate" }).fill('1.2');
    await editDialog.getByRole('button', { name: "Save Changes" }).click();

    // Assert the page does NOT shows the item
    await expect(page.getByTestId('Item: Groceries, 42.00')).not.toBeVisible();
    
    // Assert the page shows the updated item
    const editedItem = page.getByTestId('Item: Groceries, 50.40');
    await expect(editedItem).toBeVisible();
    await expect(editedItem.getByText("42.00 x 1.20 EUR")).toBeVisible();

})

test('Remove item', async ({ page }) => {
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

    // Delete the item
    await page.getByRole('button', { name: "Actions" }).click();
    await page.getByRole('button', { name: "Delete item" }).click();
    await page.getByRole('button', { name: "Confirm" }).click();

    // Assert the page does NOT shows the item
    await expect(page.getByTestId('Item: Groceries, 42.00')).not.toBeVisible();
})

test('Filter by category', async ({ page }) => {
    const uniqueEmail = generateRandomUsername();
    const password = 'MySecurePassword123!';

    await page.goto(APP_URL);
    await createUser(page, uniqueEmail, password);

    await expect(page.url()).toBe(`${APP_URL}app`);
  
    // Create record Groceries 42.0
    await page.getByRole('textbox', { name: "Amount" }).fill('42.0');
    await page.getByRole('button', { name: "Category Groceries" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();

    // Create record Education 11.0
    await page.getByRole('textbox', { name: "Amount" }).fill('11.0');
    await page.getByRole('button', { name: "Category Education" }).click();
    await page.getByRole('button', { name: "Add", exact: true }).click();

    // Go to the History tab
    await page.getByRole('button', { name: "History Screen" }).click();

    // Assert the page shows the items
    await expect(page.getByTestId('Item: Groceries, 42.00')).toBeVisible();
    await expect(page.getByTestId('Item: Education, 11.00')).toBeVisible();

    // Filter by catrgory
    await page.getByRole('button', { name: "Filter" }).click();
    await page.getByRole('switch', { name: "Any category" }).click();
    await page.getByRole('combobox', { name: "Select categories..." }).click();
    await page.getByRole('option', { name: "Education" }).click();
    await page.getByRole('button', { name: "Apply Filters" }).click();

    // Assert the page shows only Education item
    await expect(page.getByTestId('Item: Groceries, 42.00')).not.toBeVisible();
    await expect(page.getByTestId('Item: Education, 11.00')).toBeVisible();

    // Clear filters
    await page.getByText('1 record').click();

    // Assert the page shows only two items again
    await expect(page.getByTestId('Item: Groceries, 42.00')).toBeVisible();
    await expect(page.getByTestId('Item: Education, 11.00')).toBeVisible();

})
