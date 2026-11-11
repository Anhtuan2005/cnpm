import { test, expect } from './playwright.mjs';
import { signInDemo } from './auth.mjs';

test('US-30 suggests discounted compatible accessories and restores retail price without the phone', async ({ page }) => {
  await page.goto('/');
  test.skip(Number(await page.locator('body').getAttribute('data-active-sprint')) < 3, 'Cross sell opens in Sprint 3');
  await signInDemo(page);
  await page.goto('/Cart');
  const existing = page.locator('.cart-item');
  while (await existing.count()) {
    const count = await existing.count();
    await existing.first().locator('.cart-remove').click();
    await expect(existing).toHaveCount(count - 1);
  }
  await page.goto('/Product?search=iPhone');
  const phone = page.locator('.product-card').filter({ hasText: 'iPhone 15 Pro' });
  const phoneId = await phone.locator('[data-cart-product]').getAttribute('data-cart-product');
  await phone.hover();
  await phone.locator('[data-cart-product]').click();
  const modal = page.locator('#cartOfferModal');
  await expect(modal).toBeVisible();
  const offer = modal.locator('.cart-offer-item').filter({ hasText: 'Ốp lưng trong suốt iPhone 15 Pro' });
  await expect(offer).toBeVisible();
  await expect(modal).not.toContainText('Google Pixel');
  await expect(modal).not.toContainText('Ốp lưng Samsung');
  await offer.locator('[data-cart-offer-add]').click();
  await expect(offer).toBeHidden();
  await page.goto('/Cart');
  const added = page.locator('.cart-item').filter({ hasText: 'Ốp lưng trong suốt iPhone 15 Pro' });
  await expect(added.locator('[data-cart-offer-note]')).toBeVisible();
  await expect(page.locator('[data-cart-cross-sell-summary]:visible')).toHaveCount(2);
  await page.screenshot({ path: '../../artifacts/flash-sale/us30-cart.png', fullPage: true, animations: 'disabled' });
  await page.locator(`.cart-item[data-product-id="${phoneId}"]`).locator('.cart-remove').click();
  await expect(added.locator('[data-cart-offer-note]')).toBeHidden();
  await expect(page.locator('[data-cart-cross-sell-summary]:visible')).toHaveCount(0);
  await page.goto('/Product');
  await page.locator('.nav-menu a', { hasText: 'Phụ kiện' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Phụ kiện điện thoại' })).toBeVisible();
  await expect(page.locator('.product-card')).toHaveCount(9);
  await expect(page.locator('[data-compare-product]')).toHaveCount(0);
  await page.screenshot({ path: '../../artifacts/accessories/catalog.png', fullPage: true, animations: 'disabled' });
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
  await page.screenshot({ path: '../../artifacts/accessories/catalog-mobile.png', fullPage: true, animations: 'disabled' });
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.locator('.product-card').filter({ hasText: 'Ốp lưng trong suốt iPhone 15 Pro' }).locator('.card-body').click();
  await expect(page.getByRole('heading', { level: 1, name: 'Ốp lưng trong suốt iPhone 15 Pro' })).toBeVisible();
  await expect(page.locator('[data-compare-product]')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Mua ngay', exact: true })).toBeVisible();
});

test('delivered phone orders suggest compatible accessories with retail prices and product photos', async ({ page }) => {
  await page.goto('/');
  test.skip(Number(await page.locator('body').getAttribute('data-active-sprint')) < 3, 'Accessory suggestions open in Sprint 3');
  await signInDemo(page);
  await page.goto('/Order/History');
  const delivered = page.locator('.order-card').filter({
    has: page.locator('.order-status.completed')
  }).filter({ hasText: 'iPhone 15 Pro' });
  await delivered.getByRole('link', { name: 'Xem chi tiết', exact: true }).first().click();

  const section = page.getByRole('region', { name: 'Phụ kiện cho điện thoại của bạn' });
  await expect(section).toBeVisible();
  const caseLink = section.getByRole('link', { name: /Ốp lưng trong suốt iPhone 15 Pro/ });
  await expect(caseLink).toContainText(/99[,.]000/);
  await expect(section.getByRole('link', { name: /Kính cường lực iPhone 15 Pro/ })).toBeVisible();
  await expect(section.getByRole('link', { name: /Củ sạc GaN USB-C PD\/PPS 100W/ })).toBeVisible();
  await expect(section.getByRole('link', { name: /Cáp USB-C sang USB-C/ })).toBeVisible();
  await expect(section).not.toContainText(/Galaxy S24|Lightning/);
  await expect(caseLink.locator('img')).toHaveAttribute('src', '/images/accessories/case-iphone-15-pro.webp');
  await caseLink.scrollIntoViewIfNeeded();
  await expect.poll(() => caseLink.locator('img').evaluate(image => image.naturalWidth)).toBeGreaterThan(0);

  await page.setViewportSize({ width: 390, height: 844 });
  expect(await section.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
  await section.screenshot({ path: '../../artifacts/after-purchase-accessories/order-accessories-mobile.png', animations: 'disabled' });
  await caseLink.click();
  await expect(page.getByRole('heading', { level: 1, name: 'Ốp lưng trong suốt iPhone 15 Pro' })).toBeVisible();
});
