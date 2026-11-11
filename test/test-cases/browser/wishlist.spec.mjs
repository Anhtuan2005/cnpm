import { test, expect } from './playwright.mjs';
import { signInDemo } from './auth.mjs';

test('wishlist hearts keep the saved state after reload and across storefront pages', async ({ page }) => {
  await signInDemo(page);
  const catalog = '/Product?search=Samsung+Galaxy+A55';
  const productCards = page.locator('.product-card').filter({
    has: page.getByRole('heading', { name: 'Samsung Galaxy A55 5G', exact: true })
  });
  const productCard = productCards.first();
  const cardHeart = productCard.locator('button.btn-wishlist');
  const badge = page.locator('#wishlist-count');

  const countBefore = Number(await badge.textContent());
  const detailUrl = await productCard.locator('a.card-body').getAttribute('href');
  await expect(cardHeart).not.toHaveClass(/\bactive\b/);
  await productCard.hover();
  await cardHeart.click();
  await expect(cardHeart).toHaveClass(/\bactive\b/);
  await expect(badge).toHaveText(String(countBefore + 1));
  for (const heart of await productCards.locator('button.btn-wishlist').all()) {
    await expect(heart).toHaveAttribute('aria-pressed', 'true');
  }

  await page.reload();
  await expect(badge).toHaveText(String(countBefore + 1));
  await expect(cardHeart).toHaveClass(/\bactive\b/);
  await expect(cardHeart).toHaveAttribute('aria-pressed', 'true');

  await page.goto(catalog);
  await expect(cardHeart).toHaveClass(/\bactive\b/);
  await page.goto('/Wishlist');
  await expect(cardHeart).toHaveClass(/\bactive\b/);
  await page.goto(detailUrl);
  const detailHeart = page.locator('button.btn-wishlist').first();
  await expect(detailHeart).toHaveClass(/\bactive\b/);
  await detailHeart.click();
  await expect(detailHeart).not.toHaveClass(/\bactive\b/);
  await expect(detailHeart).toHaveAttribute('aria-pressed', 'false');
  await expect(badge).toHaveText(String(countBefore));

  await page.goto(catalog);
  await expect(cardHeart).not.toHaveClass(/\bactive\b/);
  await page.goto('/Wishlist');
  await expect(productCards).toHaveCount(0);
});

test('saving a phone refreshes the phone and compatible accessory recommendations', async ({ page }) => {
  await page.goto('/Account/Login');
  await page.locator('#Email').fill('khachhang2@shop.vn');
  await page.locator('#Password').fill('User@123');
  await page.locator('form.auth-form button[type="submit"]').click();
  await page.waitForURL(url => url.pathname === '/');
  test.skip(Number(await page.locator('body').getAttribute('data-active-sprint')) < 3, 'Recommendations open in Sprint 3');

  const section = page.getByRole('heading', { name: 'Dành riêng cho bạn', exact: true })
    .locator('xpath=ancestor::section[1]');
  const names = section.locator('.card-body h3');
  const sony = page.locator('.product-card').filter({
    has: page.getByRole('heading', { name: 'Sony Xperia 1 V', exact: true })
  }).first();
  const heart = sony.locator('button.btn-wishlist');
  const matching = section.locator('.product-card').filter({ hasText: 'Sony Xperia 5 V' });
  const accessories = page.getByRole('region', { name: 'Phụ kiện hợp với Sony Xperia 1 V' });
  const before = await names.allTextContents();
  await expect(accessories).toHaveCount(0);
  await heart.focus();
  await heart.press('Enter');
  await expect(heart).toHaveAttribute('aria-pressed', 'true');
  await expect.poll(() => names.allTextContents(), { timeout: 3500 }).not.toEqual(before);
  await expect(names.nth(1)).toHaveText('Sony Xperia 5 V');
  await expect(section.getByRole('status')).toContainText('Sony Xperia 1 V');
  await expect(accessories.getByRole('link', { name: /Cáp USB-C sang USB-C 1m/ })).toBeVisible();
  await expect(accessories.getByRole('link', { name: /Củ sạc USB-C PD 20W/ })).toBeVisible();
  await expect(accessories).not.toContainText(/Lightning|Ốp lưng Samsung|100W/);
  const viewAccessories = page.getByRole('link', { name: 'Xem phụ kiện', exact: true });
  await expect(viewAccessories).toBeVisible();
  await viewAccessories.click();
  await expect(page).toHaveURL(/#home-accessory-heading$/);
  await expect(accessories.getByRole('heading')).toBeInViewport();

  await matching.getByRole('button', { name: 'Thêm Sony Xperia 5 V vào so sánh' }).click();
  await expect(matching.locator('[data-compare-product]')).toHaveAttribute('aria-pressed', 'true');
  await matching.locator('button.btn-wishlist').focus();
  await matching.locator('button.btn-wishlist').press('Enter');
  await expect(matching.locator('button.btn-wishlist')).toHaveAttribute('aria-pressed', 'true');
  await expect(section.getByRole('status')).toContainText('Sony Xperia 5 V');
  await expect(matching.locator('[data-compare-product]')).toHaveAttribute('aria-pressed', 'true');
  const latestAccessories = page.getByRole('region', { name: 'Phụ kiện hợp với Sony Xperia 5 V' });
  const cable = latestAccessories.getByRole('link', { name: /Cáp USB-C sang USB-C 1m/ });
  await expect(cable.locator('img')).toHaveAttribute('src', '/images/accessories/cable-usb-c.webp');
  await page.reload();
  await expect(cable).toBeVisible();
  await cable.click();
  await expect(page.getByRole('heading', { level: 1, name: 'Cáp USB-C sang USB-C 1m', exact: true })).toBeVisible();
});
