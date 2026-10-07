import { test, expect } from '@playwright/test';
import { signInDemo as login } from './auth.mjs';

test('buy now opens a direct checkout without changing the cart count', async ({ page }) => {
  await login(page);
  await page.goto('/Cart');
  const cartProductIds = await page.locator('[name=selectedProductIds]').evaluateAll(inputs => inputs.map(input => input.value));
  const cartCount = await page.locator('#cart-count').textContent();

  await page.goto('/Product');
  const productId = await page.locator('[data-cart-product]').evaluateAll((buttons, existingIds) => {
    const button = buttons.find(item => !existingIds.includes(item.dataset.cartProduct) && !item.disabled);
    return button?.dataset.cartProduct;
  }, cartProductIds);
  expect(productId).toBeTruthy();

  await page.goto(`/Product/Detail/${productId}`);
  let releaseCheckout;
  let markCheckoutStarted;
  const checkoutStarted = new Promise(resolve => { markCheckoutStarted = resolve; });
  const checkoutReleased = new Promise(resolve => { releaseCheckout = resolve; });
  await page.route('**/Order/Checkout?**', async route => {
    markCheckoutStarted();
    await checkoutReleased;
    await route.continue();
  });
  const buttonState = await page.locator('#btnBuyNow').evaluate(button => {
    button.click();
    return { disabled: button.disabled, text: button.textContent.trim() };
  });
  await checkoutStarted;
  try {
    expect(buttonState).toEqual({ disabled: false, text: 'Mua ngay' });
  } finally {
    releaseCheckout();
  }
  await page.waitForURL(url => url.pathname === '/Order/Checkout');

  await expect(page.locator('#cart-count')).toHaveText(cartCount.trim());
  await expect(page.locator('.summary-item')).toHaveCount(1);
  await expect(page.locator('.summary-item small')).toContainText('Số lượng: 1');

  await page.goBack();
  await expect(page).toHaveURL(new RegExp(`/Product/Detail/${productId}$`));
  await expect(page.locator('#btnBuyNow')).toBeEnabled();
  await expect(page.locator('#btnBuyNow')).toHaveText('Mua ngay');
});

test('Sprint 2 mobile checkout shows voucher and both payment options', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await login(page);
  await page.goto('/Product');
  const productId = await page.locator('[data-cart-product]:not([disabled])').first().getAttribute('data-cart-product');
  expect(productId).toBeTruthy();
  await page.goto(`/Order/Checkout?buyNowProductId=${productId}&buyNowQuantity=1`);

  await page.evaluate(() => document.fonts.ready);
  await expect(page.locator('#voucherInput')).toBeVisible();
  await expect(page.locator('input[value=VNPAY]')).toHaveCount(1);
  await expect(page.locator('input[value=COD]')).toBeChecked();
  await expect.poll(() => page.locator('.payment-choice').evaluate(box => {
    return [...box.querySelectorAll('.payment-option')].every(option => {
      const bounds = option.getBoundingClientRect();
      return bounds.left >= 0 && bounds.right <= window.innerWidth;
    });
  }), { message: 'Both payment options must fit the mobile viewport' }).toBe(true);
});
