import { test, expect } from '@playwright/test';
import { signInDemo } from './auth.mjs';

test('Sprint 2 storefront exposes Sprint 2 routes and keeps later routes unavailable', async ({ page }) => {
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  for (const path of ['/', '/Product']) {
    const response = await page.goto(path);
    expect(response.status()).toBe(200);
    await expect(page.locator('main')).toBeVisible();
    await expect(page.locator('body')).not.toContainText('InvalidOperationException');
  }
  await page.goto('/');
  await expect(page.locator('.brand-category-tile')).toHaveCount(8);
  const bannerImages = await page.locator('.slide-bg img').evaluateAll(images => images.map(image => new URL(image.src).pathname));
  expect(new Set(bannerImages).size).toBe(bannerImages.length);
  for (const path of ['/Product/Compare', '/Info/About']) {
    expect((await page.goto(path)).status(), `${path} must be available in Sprint 2`).toBe(200);
  }
  for (const path of ['/BuildPc', '/AiChat', '/sitemap.xml', '/robots.txt']) {
    expect((await page.goto(path)).status(), `${path} must stay unavailable after Sprint 2`).toBe(404);
  }
  await page.goto('/');
  await expect(page.locator('body')).not.toContainText('Banking');
  await page.goto('/Order/History');
  await expect(page).toHaveURL(/\/Account\/Login/i);
  expect(errors).toEqual([]);
});

test('Sprint 2 storefront supports product comparison without Sprint 3 catalog extras', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Sản phẩm nổi bật' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Sản phẩm mới nhất' })).toHaveCount(0);

  await page.goto('/Product');
  await expect(page.locator('.catalog-layout-toggle')).toHaveCount(0);
  await expect(page.locator('[data-compare-product]')).not.toHaveCount(0);

  const comparableIds = await page.locator('[data-compare-product]').evaluateAll(buttons => {
    const groups = new Map();
    for (const button of buttons) {
      const category = button.dataset.compareCategoryId;
      if (!groups.has(category)) groups.set(category, []);
      groups.get(category).push(button.dataset.compareProduct);
    }
    return [...groups.values()].find(ids => ids.length >= 2)?.slice(0, 2) || [];
  });
  expect(comparableIds).toHaveLength(2);
  for (const id of comparableIds) {
    await page.locator(`[data-compare-product="${id}"]`).click();
  }
  await expect(page.locator('[data-compare-count]')).toHaveText('2');
  await page.locator('[data-compare-open]').click();
  await expect(page).toHaveURL(/\/Product\/Compare\?/i);
  await expect(page.locator('.compare-product')).toHaveCount(2);

  await page.goto('/Product');
  await page.locator('.product-card .card-body').first().click();
  await expect(page.locator('[data-copy-link]')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Sản phẩm liên quan' })).toHaveCount(0);
  await expect(page.locator('.pdp-order-lines > div', { hasText: 'Vận chuyển' })).toContainText('Tính theo khu vực');
});

test('admin storefront is read-only without customer-only controls', async ({ page }) => {
  await signInDemo(page, 'Admin');
  await page.goto('/Product');
  await expect(page.getByText('Chỉ xem')).toHaveCount(0);
  await expect(page.locator('.product-card .btn-wishlist')).toHaveCount(0);
  await expect(page.locator('.product-card .btn-card-cart')).toHaveCount(0);
  await expect(page.locator('.nav-actions a[href*="Wishlist"]')).toHaveCount(0);
  await expect(page.locator('.nav-actions a[href*="Cart"]')).toHaveCount(0);

  await page.locator('.product-card .card-body').first().click();
  await expect(page.locator('.pdp-icon-actions')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Thêm vào giỏ' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Mua ngay' })).toHaveCount(0);
  await expect(page.getByText('Chế độ xem admin')).toHaveCount(0);
});

test('customer can access the required order history without extra filters', async ({ page }) => {
  await signInDemo(page, 'User');
  await page.goto('/Order/History');
  await expect(page.getByRole('heading', { name: 'Đơn hàng của tôi' })).toBeVisible();
  await expect(page.locator('.order-tabs')).toHaveCount(0);
  await expect(page.getByText('Trạng thái tiền')).toHaveCount(0);
});

test('mobile catalog fits the viewport', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/Product');
  await expect(page.locator('main')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
});
