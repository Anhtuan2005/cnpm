import { test, expect } from './playwright.mjs';
import { signInDemo } from './auth.mjs';

test('admin can edit an accessory without entering phone specifications', async ({ page }) => {
  await page.goto('/');
  test.skip(Number(await page.locator('body').getAttribute('data-active-sprint')) < 3, 'Accessories open in Sprint 3');
  await signInDemo(page, 'Admin');
  await page.goto('/Admin/Product');
  const row = page.locator('tr').filter({ hasText: 'Ốp lưng trong suốt iPhone 15 Pro' });
  await row.locator('a[href*="/Admin/Product/Edit/"]').click();
  await expect(page.locator('#CategoryId option:checked')).toHaveText('Phụ kiện điện thoại');
  await expect(page.locator('[data-phone-specifications]')).toBeHidden();
  const original = await page.locator('#Description').inputValue();
  const editUrl = page.url();
  await page.locator('#Description').fill(original + ' Phụ kiện đã được kiểm tra trong demo.');
  await page.getByRole('button', { name: 'Lưu sản phẩm', exact: true }).click();
  await expect(page).toHaveURL(/\/Admin\/Product$/);
  await page.goto(editUrl);
  await expect(page.locator('#Description')).toHaveValue(original + ' Phụ kiện đã được kiểm tra trong demo.');
  await page.locator('#Description').fill(original);
  await page.getByRole('button', { name: 'Lưu sản phẩm', exact: true }).click();
  await expect(page).toHaveURL(/\/Admin\/Product$/);
});
