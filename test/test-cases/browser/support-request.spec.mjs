import { test, expect } from '@playwright/test';
import { signInDemo } from './auth.mjs';

test('Sprint 2 support request reaches the admin order without a Sprint 3 route', async ({ page }) => {
  await signInDemo(page, 'User');
  const adminContext = await page.context().browser().newContext();
  const adminPage = await adminContext.newPage();
  try {
    await signInDemo(adminPage, 'Admin');
    const adminHubConnected = adminPage.waitForEvent('websocket', socket =>
      socket.url().includes('/hubs/admin-notifications'));
    await adminPage.goto('/Admin/Product');
    await adminHubConnected;
    await expect(adminPage.locator('#admin-notification-badge')).toBeAttached();
    const initialNotificationCount = Number(await adminPage.locator('#admin-notification-badge').textContent()) || 0;

    await page.goto('/Order/History');
    const createLink = page.getByRole('link', { name: 'Yêu cầu hỗ trợ' }).first();
    await expect(createLink).toBeVisible();
    const createUrl = await createLink.getAttribute('href');
    const orderId = new URL(createUrl, 'http://localhost').searchParams.get('orderId');
    expect(orderId).toBeTruthy();

    await createLink.click();
    await page.locator('input[name="Type"][value="Đổi trả"]').check();
    await page.locator('#Reason').selectOption({ label: 'Sản phẩm lỗi khi nhận hàng' });
    await page.locator('#Description').fill('Máy khởi động nhưng màn hình không hiển thị hình ảnh. ' + 'M'.repeat(140));
    await page.getByRole('button', { name: 'Gửi yêu cầu' }).click();
    await expect(page).toHaveURL(/\/ReturnWarranty\/Details\/\d+/);
    const requestId = page.url().match(/\/Details\/(\d+)/)?.[1];
    expect(requestId).toBeTruthy();
    const successToast = page.locator('#toast-stack .toast-success');
    await expect(successToast).toContainText(`Đã gửi yêu cầu đổi trả #${requestId}.`);
    const description = page.locator('.support-note-block .order-info-list dd').nth(2);
    await expect(description).toBeVisible();
    const descriptionWidth = await description.evaluate(element => ({
      clientWidth: element.clientWidth,
      scrollWidth: element.scrollWidth
    }));
    expect(descriptionWidth.scrollWidth).toBeLessThanOrEqual(descriptionWidth.clientWidth + 1);

    await expect(adminPage.locator('#admin-notification-badge')).toHaveText(String(initialNotificationCount + 1));
    await expect(adminPage.locator('.adm-toast').last()).toContainText('Yêu cầu đổi trả mới');
    await expect(successToast).toBeHidden({ timeout: 6000 });

    await adminPage.goto('/Admin/Notification');
    await adminPage.locator('.admin-notification-row', { hasText: 'Yêu cầu đổi trả mới' })
      .filter({ hasText: `Đơn #DH${orderId.padStart(4, '0')}` }).first().click();
    await expect(adminPage).toHaveURL(new RegExp(`/Admin/Order/${orderId}#support-request-${requestId}$`));
    await expect(adminPage.locator(`#support-request-${requestId}`)).toContainText('Máy khởi động nhưng màn hình không hiển thị hình ảnh.');
  } finally {
    await adminContext.close();
  }
});
