import { test, expect } from './playwright.mjs';
import { signInDemo } from './auth.mjs';

test('Sprint 3 health, menus, chatbot and admin boundaries follow the release configuration', async ({ page }) => {
  await page.goto('/');
  const enabled = Number(await page.locator('body').getAttribute('data-active-sprint')) >= 3;
  for (const path of ['/health/live', '/health/ready']) {
    const response = await page.request.get(path);
    expect(response.status()).toBe(enabled ? 200 : 404);
    if (enabled) expect(await response.text()).not.toMatch(/ConnectionString|Password|Server=/i);
  }
  await expect(page.locator('[data-ai-chatbot]')).toHaveCount(enabled ? 1 : 0);
  for (const path of ['/Admin/ReturnWarranty', '/Admin/User', '/Admin/Voucher', '/Admin/CustomerSegment', '/Admin/Report']) {
    const response = await page.request.get(path, { maxRedirects: 0 });
    expect(response.status()).toBe(enabled ? 302 : 404);
  }
  if (!enabled) {
    expect((await page.request.post('/AiChat/Ask', { data: { message: 'Samsung' } })).status()).toBe(404);
    return;
  }
  await page.locator('[data-ai-chat-toggle]').click();
  await page.locator('[data-ai-chat-input]').fill('Samsung dưới 10 triệu');
  await page.locator('[data-ai-chat-send]').click();
  await expect(page.locator('.ai-product-link').first()).toBeVisible();
  await expect(page.locator('.ai-product-link').first()).toContainText('Samsung');
  expect((await page.request.post('/AiChat/Ask', { data: { message: 'Samsung' } })).status()).toBe(400);
  await page.screenshot({ path: '../../artifacts/sprint3/chat.png' });
  await page.locator('.ai-product-link').first().click();
  await expect(page).toHaveURL(/\/Product\/Detail\/\d+/);
  await signInDemo(page);
  expect((await page.request.get('/Admin/Report', { maxRedirects: 0 })).status()).toBe(302);
});

test('Sprint 3 admin pages, report filters, CSV and voucher form work in the browser', async ({ page }) => {
  await page.goto('/');
  const enabled = Number(await page.locator('body').getAttribute('data-active-sprint')) >= 3;
  await signInDemo(page, 'Admin');
  const paths = ['/Admin/ReturnWarranty', '/Admin/User', '/Admin/Voucher', '/Admin/CustomerSegment', '/Admin/Report'];
  if (!enabled) {
    for (const path of paths) expect((await page.request.get(path)).status()).toBe(404);
    return;
  }
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  for (const path of paths) {
    expect((await page.goto(path)).status(), path).toBe(200);
    await expect(page.locator('main')).toBeVisible();
  }
  await page.locator('select[name=period]').selectOption('7');
  await page.getByRole('button', { name: 'Lọc', exact: true }).click();
  await expect(page).toHaveURL(/period=7/);
  await expect(page.locator('body')).toContainText('Tiền hàng');
  const download = page.waitForEvent('download');
  await page.getByRole('link', { name: 'Xuất CSV' }).click();
  expect((await download).suggestedFilename()).toMatch(/^bao-cao-.*\.csv$/);
  await page.screenshot({ path: '../../artifacts/sprint3/report.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
  expect(await page.locator('.report-revenue').evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
  await page.screenshot({ path: '../../artifacts/sprint3/report-mobile.png', fullPage: true });
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto('/Admin/Voucher/Create');
  const code = `S3BROWSER${Date.now()}`;
  await page.locator('#Code').fill(code);
  await page.locator('#Type').selectOption('1');
  await page.locator('#Value').fill('30000');
  await page.locator('#MinOrderAmount').fill('300000');
  await page.locator('#MaxDiscount').fill('30000');
  await page.locator('#UsageLimit').fill('1');
  await page.getByRole('button', { name: 'Lưu voucher' }).click();
  await expect(page).toHaveURL(/\/Admin\/Voucher$/);
  const row = page.locator('tbody tr', { hasText: code });
  await expect(row).toContainText('Đang bật');
  await row.getByRole('button', { name: 'Bật/tắt' }).click();
  await expect(page.locator('tbody tr', { hasText: code })).toContainText('Đã tắt');
  expect(errors).toEqual([]);
});
