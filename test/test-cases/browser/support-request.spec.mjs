import { test, expect } from '@playwright/test';
import { signInDemo } from './auth.mjs';

test('Sprint 2 support request reaches the admin order without a Sprint 3 route', async ({ page }) => {
  await signInDemo(page, 'User');
  await page.goto('/Order/History');
  const createLink = page.getByRole('link', { name: 'Yêu cầu hỗ trợ' }).first();
  await expect(createLink).toBeVisible();
  const createUrl = await createLink.getAttribute('href');
  const orderId = new URL(createUrl, 'http://localhost').searchParams.get('orderId');
  expect(orderId).toBeTruthy();

  await createLink.click();
  await page.locator('input[name="Type"][value="Đổi trả"]').check();
  await page.locator('#Reason').selectOption({ label: 'Sản phẩm lỗi khi nhận hàng' });
  await page.locator('#Description').fill('Máy khởi động nhưng màn hình không hiển thị hình ảnh.');
  await page.getByRole('button', { name: 'Gửi yêu cầu' }).click();
  await expect(page).toHaveURL(/\/ReturnWarranty\/Details\/\d+/);
  const requestId = page.url().match(/\/Details\/(\d+)/)?.[1];
  expect(requestId).toBeTruthy();

  await signInDemo(page, 'Admin');
  await page.goto('/Admin/Notification');
  await page.locator('.admin-notification-row', { hasText: 'Yêu cầu đổi trả mới' })
    .filter({ hasText: `Đơn #DH${orderId.padStart(4, '0')}` }).first().click();
  await expect(page).toHaveURL(new RegExp(`/Admin/Order/${orderId}#support-request-${requestId}$`));
  await expect(page.locator(`#support-request-${requestId}`)).toContainText('Máy khởi động nhưng màn hình không hiển thị hình ảnh.');
});
