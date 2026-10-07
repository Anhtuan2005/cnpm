import { test, expect } from '@playwright/test';
import { signInDemo } from './auth.mjs';

let storageState;
test.beforeAll(async ({ browser, baseURL }) => {
  const context = await browser.newContext({ baseURL });
  const page = await context.newPage();
  await signInDemo(page, 'Admin');
  storageState = await context.storageState();
  await context.close();
});

test.beforeEach(async ({ context }) => {
  await context.addCookies(storageState.cookies);
});

test('Sprint 2 order administration is available', async ({ page }) => {
  expect((await page.goto('/Admin/Order')).status()).toBe(200);
  await expect(page.locator('main')).toBeVisible();
  await expect(page.locator('body')).not.toContainText('InvalidOperationException');
  expect((await page.goto('/Admin/Dashboard')).status()).toBe(200);
  await expect(page.locator('a[href="/Admin/Report"]')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Top sản phẩm bán chạy' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Sản phẩm được xem nhiều' })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Cần xem lại giá/ưu đãi' })).toHaveCount(0);
});

test('banner upload without a URL reaches the server and shows a file validation error', async ({ page }) => {
  await page.goto('/Admin/Banner/Create');
  await page.locator('#Title').fill('Upload validation test');
  await page.locator('#banner-image-file').setInputFiles({ name: 'invalid.gif', mimeType: 'image/gif', buffer: Buffer.from('invalid') });
  await Promise.all([
    page.waitForResponse(response => response.request().method() === 'POST' && response.url().includes('/Admin/Banner/Create')),
    page.getByRole('button', { name: 'Lưu banner' }).click()
  ]);
  await expect(page.locator('[data-valmsg-for=imageFile]')).toContainText('Chỉ hỗ trợ');
  await expect(page.locator('[data-valmsg-for=ImageUrl]')).toBeEmpty();
});

test('product upload without image URLs shows upload validation rather than blocking the form', async ({ page }) => {
  await page.goto('/Admin/Product/Create');
  await page.locator('#Name').fill('Upload validation test');
  await page.locator('#Description').fill('Test input with an intentionally invalid file.');
  await page.locator('#Price').fill('100000');
  await page.locator('#Stock').fill('1');
  await page.locator('#CategoryId').selectOption({ index: 0 });
  await page.locator('#product-image-files').setInputFiles({ name: 'invalid.gif', mimeType: 'image/gif', buffer: Buffer.from('invalid') });
  await Promise.all([
    page.waitForResponse(response => response.request().method() === 'POST' && response.url().includes('/Admin/Product/Create')),
    page.getByRole('button', { name: 'Lưu sản phẩm' }).click()
  ]);
  await expect(page.locator('[data-valmsg-for=imageFiles]')).toContainText('Chỉ hỗ trợ');
  await expect(page.locator('[data-valmsg-for=ImageUrls]')).toBeEmpty();
});
