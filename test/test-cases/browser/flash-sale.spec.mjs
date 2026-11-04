import { test, expect } from './playwright.mjs';

test('Flash Sale keeps only discounted phones through sorting, search, brands and paging links', async ({ page }) => {
  await page.goto('/Product');
  test.skip(Number(await page.locator('body').getAttribute('data-active-sprint')) < 3, 'Flash Sale opens in Sprint 3');
  await page.locator('.nav-flash:visible').first().click({ force: true });
  await expect(page.getByRole('heading', { level: 1, name: 'Flash Sale' })).toBeVisible();
  const cards = page.locator('.product-card');
  const assertDeals = async () => {
    await expect(cards.first()).toBeVisible();
    expect(await cards.locator('.badge-sale').count()).toBe(await cards.count());
    await expect(cards.locator('.stock-badge.out')).toHaveCount(0);
  };
  await assertDeals();
  for (const href of await page.locator('.pagination a').evaluateAll(links => links.map(link => link.href)))
    expect(new URL(href).searchParams.get('saleOnly')?.toLowerCase()).toBe('true');
  await Promise.all([
    page.waitForURL(url => url.searchParams.get('sortBy') === 'price_asc'),
    page.locator('#catalogPriceSort').selectOption('price_asc')
  ]);
  expect(new URL(page.url()).searchParams.get('saleOnly')?.toLowerCase()).toBe('true');
  await assertDeals();
  await page.locator('.catalog-sort-options a', { hasText: 'Bán chạy' }).click();
  await assertDeals();
  await expect(page.locator('.catalog-sort-options a.active', { hasText: 'Giảm giá' })).toBeVisible();
  await page.locator('.category-rail-link', { hasText: 'Samsung' }).click();
  await assertDeals();
  await expect(cards).toContainText(['Samsung']);
  await page.locator('.catalog-search input[name=search]').fill('Samsung');
  await page.locator('.catalog-search button[type=submit]').click();
  await assertDeals();
  await page.screenshot({ path: '../../artifacts/flash-sale/catalog.png', fullPage: true, animations: 'disabled' });
  await page.locator('.catalog-sort-options a', { hasText: 'Giảm giá' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Tìm điện thoại phù hợp' })).toBeVisible();
  expect(await cards.count()).toBeGreaterThan(await cards.locator('.badge-sale').count());

  await page.goto('/Product?sortBy=discount_desc');
  await assertDeals(); // Existing Flash Sale URLs remain supported.
  await page.goto('/Product?saleOnly=true&sortBy=price_desc&minPrice=999999999');
  await expect(page.getByText('Không tìm thấy sản phẩm phù hợp')).toBeVisible();
  await expect(cards).toHaveCount(0);
});
