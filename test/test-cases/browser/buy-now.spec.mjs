import { test, expect } from './playwright.mjs';
import { signInDemo as login } from './auth.mjs';

test('buy now can skip accessory offers and checkout without changing the cart count', async ({ page }) => {
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
  const enabled = Number(await page.locator('body').getAttribute('data-active-sprint')) >= 3;
  if (enabled) {
    const offersResponse = page.waitForResponse(response => response.url().includes('/Product/BuyNowOffers?'));
    await page.locator('#btnBuyNow').click();
    const data = await (await offersResponse).json();
    if (data.crossSellSuggestions.length) {
      await expect(page.locator('#cartOfferModal')).toBeVisible();
      await page.locator('#cartOfferList [data-cart-offer-add]').first().click();
      await page.locator('[data-buy-now-skip]').click();
    }
  } else {
    expect((await page.request.get(`/Product/BuyNowOffers?productId=${productId}&quantity=1`)).status()).toBe(404);
    await page.locator('#btnBuyNow').click();
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

test('buy now selects discounted accessories, preserves them after validation, and leaves the cart untouched', async ({ page }) => {
  await login(page);
  // Keep checkout independent of the external address API's availability.
  const province = { name: 'Thành phố Hồ Chí Minh', code: 79 };
  const district = { name: 'Quận 1', code: 760, province_code: 79 };
  const responses = {
    '/api/v1/p/': [province],
    '/api/v1/p/79': { ...province, districts: [district] },
    '/api/v1/d/760': { ...district, wards: [{ name: 'Phường Bến Nghé', code: 26734, district_code: 760 }] }
  };
  await page.route('https://provinces.open-api.vn/**', route => {
    const data = responses[new URL(route.request().url()).pathname];
    return data ? route.fulfill({ json: data }) : route.abort();
  });
  await page.goto('/Product?search=iPhone');
  test.skip(Number(await page.locator('body').getAttribute('data-active-sprint')) < 3, 'Accessory offers open in Sprint 3');
  const errors = [];
  page.on('pageerror', error => {
    // Edge may abort a document view transition when Playwright disables
    // animations for screenshots; this is a browser transition, not app JS.
    if (!error.message.startsWith('Transition was aborted')) errors.push(error.message);
  });
  const cartWrites = [];
  page.on('request', request => { if (request.url().includes('/Cart/Add')) cartWrites.push(request.url()); });
  const cartCount = (await page.locator('#cart-count').textContent()).trim();
  const phoneId = await page.locator('.product-card').filter({ hasText: 'iPhone 15 Pro' })
    .locator('[data-cart-product]').getAttribute('data-cart-product');
  await page.goto(`/Product/Detail/${phoneId}`);
  await page.locator('#qtyInput').fill('2');
  await page.locator('#btnBuyNow').click();
  const modal = page.locator('#cartOfferModal');
  await expect(modal).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(modal).toBeHidden();
  await expect(page.locator('#btnBuyNow')).toBeFocused();
  await page.locator('#btnBuyNow').click();
  await expect(modal).toBeVisible();
  await expect(modal.locator('[data-cart-offer-actions]')).toBeHidden();
  const caseOffer = modal.locator('.cart-offer-item').filter({ hasText: 'Ốp lưng trong suốt iPhone 15 Pro' });
  await expect(caseOffer).toContainText('79.200');
  await expect(modal).not.toContainText('Ốp lưng Samsung');
  const button = caseOffer.locator('[data-cart-offer-add]');
  const accessoryId = await button.getAttribute('data-cart-offer-add');
  await button.click();
  await expect(button).toHaveAttribute('aria-pressed', 'true');
  await button.click();
  await expect(button).toHaveAttribute('aria-pressed', 'false');
  await button.click();
  await page.screenshot({ path: '../../artifacts/buy-now/offers-desktop.png', animations: 'disabled' });
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await modal.evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
  expect(await modal.locator('[data-buy-now-checkout]').evaluate(button => {
    const box = button.getBoundingClientRect();
    return box.top >= 0 && box.bottom <= innerHeight;
  })).toBe(true);
  await page.screenshot({ path: '../../artifacts/buy-now/offers-mobile.png', animations: 'disabled' });
  await modal.locator('[data-buy-now-checkout]').click();
  await page.waitForURL(url => url.pathname === '/Order/Checkout');
  await expect(page.locator('input[name=BuyNowAccessoryIds]')).toHaveValue(accessoryId);
  await expect(page.locator('#BuyNowQuantity')).toHaveValue('2');
  await expect(page.locator('.summary-item')).toHaveCount(2);
  const caseSummary = page.locator('.summary-item').filter({ hasText: 'Ốp lưng trong suốt iPhone 15 Pro' });
  await expect(caseSummary.locator('b')).toContainText('79.200');
  await expect(page.locator('#cart-count')).toHaveText(cartCount);
  // A real invalid POST must rebuild the same direct quote and retain its hidden selection.
  const validationResponse = page.waitForResponse(response => response.url().endsWith('/Order/Checkout') && response.request().method() === 'POST');
  await page.locator('#RecipientName').fill('');
  await page.locator('#checkoutForm').evaluate(form => HTMLFormElement.prototype.submit.call(form));
  expect((await validationResponse).status()).toBe(200);
  await page.waitForLoadState('load');
  await expect(page.locator('[data-valmsg-for=RecipientName]')).toContainText('Vui lòng nhập');
  await expect(page.locator('input[name=BuyNowAccessoryIds]')).toHaveValue(accessoryId);
  await expect(page.locator('.summary-item')).toHaveCount(2);
  await expect(caseSummary.locator('b')).toContainText('79.200');
  const shippingResponse = page.waitForResponse(response => response.url().includes('/Order/ShippingFee?'));
  await page.locator('#Province').selectOption({ label: 'Thành phố Hồ Chí Minh' });
  const shipping = await shippingResponse;
  expect(new URL(shipping.url()).searchParams.getAll('buyNowAccessoryIds')).toEqual([accessoryId]);
  expect((await shipping.json()).subtotal).toBe(Number(await page.locator('#checkoutTotal').getAttribute('data-subtotal')));
  await page.locator('#RecipientName').fill('Khách mua ngay');
  await page.locator('#RecipientPhone').fill('0901234567');
  await expect(page.locator('#District')).toBeEnabled();
  await page.locator('#District').selectOption({ label: 'Quận 1' });
  await expect(page.locator('#Ward')).toBeEnabled();
  await page.locator('#Ward').selectOption({ label: 'Phường Bến Nghé' });
  await page.locator('#Street').fill('123 Nguyễn Huệ');
  await expect(page.locator('#shippingFeeText')).toContainText('20.000');
  const finalTotal = await page.locator('#checkoutTotal').textContent();
  await page.screenshot({ path: '../../artifacts/buy-now/checkout-mobile.png', fullPage: true, animations: 'disabled' });
  await expect(page.locator('[data-valmsg-for=Street]')).toBeEmpty();
  await page.locator('#checkoutSubmit').click();
  await page.waitForURL(/\/Order\/Confirmation\/\d+/);
  await expect(page.locator('.order-confirmation-summary')).toContainText(finalTotal.trim());
  await expect(page.locator('#cart-count')).toHaveText(cartCount);
  await page.goto('/Cart');
  await expect(page.locator('#cart-count')).toHaveText(cartCount);
  expect(cartWrites).toEqual([]);
  expect(errors).toEqual([]);
});

test('buy now continues directly when offers cannot be loaded', async ({ page }) => {
  await login(page);
  await page.goto('/Product');
  const productId = await page.locator('[data-cart-product]:not([disabled])').first().getAttribute('data-cart-product');
  await page.goto(`/Product/Detail/${productId}`);
  const count = await page.locator('#cart-count').textContent();
  await page.route('**/Product/BuyNowOffers?**', route => route.fulfill({ status: 503, contentType: 'application/json', body: '{}' }));
  await page.locator('#btnBuyNow').click();
  await page.waitForURL(url => url.pathname === '/Order/Checkout');
  await expect(page.locator('.summary-item')).toHaveCount(1);
  await expect(page.locator('#cart-count')).toHaveText(count.trim());
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
