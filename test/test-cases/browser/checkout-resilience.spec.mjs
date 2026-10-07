import { test, expect } from '@playwright/test';
import { signInDemo } from './auth.mjs';

let storageState;
let productId;
test.beforeAll(async ({ browser, baseURL }) => {
  const context = await browser.newContext({ baseURL });
  const page = await context.newPage();
  await signInDemo(page);
  await page.goto('/Product');
  productId = await page.locator('[data-cart-product]:not([disabled])').first().getAttribute('data-cart-product');
  storageState = await context.storageState();
  await context.close();
});

test.beforeEach(async ({ context, page }) => {
  await context.addCookies(storageState.cookies);
  await page.route('https://provinces.open-api.vn/**', route => route.fulfill({ json: [] }));
  await page.goto(`/Order/Checkout?buyNowProductId=${productId}&buyNowQuantity=1`);
  await expect(page.locator('#checkoutForm')).toBeVisible();
});

test('Sprint 2 checkout contains COD, VNPAY and voucher fields', async ({ page }) => {
  await expect(page.locator('input[value=COD]')).toBeChecked();
  await expect(page.locator('input[value=VNPAY]')).toHaveCount(1);
  await expect(page.locator('#VoucherCode, #DiscountAmount, #voucherInput, #applyVoucher')).toHaveCount(4);
});

test('an older shipping response cannot overwrite the latest selected address', async ({ page }) => {
  const requests = [];
  await page.route('**/Order/ShippingFee?**', route => { requests.push(route); });
  await page.evaluate(() => {
    const select = document.querySelector('[data-province-select]');
    select.innerHTML = '<option value="old">Old</option><option value="new">New</option>';
    select.value = 'old';
    updateCheckoutShippingFee();
  });
  await expect.poll(() => requests.length).toBe(1);
  await page.evaluate(() => {
    document.querySelector('[data-province-select]').value = 'new';
    updateCheckoutShippingFee();
  });
  await expect.poll(() => requests.length).toBe(2);
  await requests[1].fulfill({ json: { fee: 20000, formattedFee: '20.000 ₫', message: 'New' } });
  await expect(page.locator('#shippingFeeText')).toHaveText('20.000 ₫');
  await requests[0].fulfill({ json: { fee: 50000, formattedFee: '50.000 ₫', message: 'Old' } });
  await page.waitForTimeout(100);
  await expect(page.locator('#shippingFeeText')).toHaveText('20.000 ₫');
});
