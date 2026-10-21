import { test, expect } from '@playwright/test';
import { fileURLToPath } from 'node:url';
import { signInDemo } from './auth.mjs';

test('customer delivery report is visible to admin and can be confirmed after a response', async ({ page }) => {
  await signInDemo(page, 'User');
  await page.goto('/Order/History');
  const detailUrls = await page.locator('a.order-code').evaluateAll(links => links.map(link => link.getAttribute('href')));
  let orderId;
  for (const url of detailUrls) {
    await page.goto(url);
    const awaitingDeliveryReceipt = await page.getByRole('button', { name: 'Tôi đã nhận hàng' }).isVisible().catch(() => false);
    const hasRefundPanel = await page.getByRole('heading', { name: 'Tiền hoàn đã về?' }).isVisible().catch(() => false);
    if (awaitingDeliveryReceipt && !hasRefundPanel) {
      orderId = page.url().match(/\/Detail\/(\d+)/)?.[1];
      break;
    }
  }
  expect(orderId, 'Demo needs a delivered order awaiting customer confirmation').toBeTruthy();
  if (process.env.TECHVORA_CAPTURE_ASSURANCE) {
    await page.screenshot({ path: fileURLToPath(new URL('../../../artifacts/assurance-customer.png', import.meta.url)), fullPage: true });
  }
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.locator('.assurance-panel').first()).toBeVisible();
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  if (process.env.TECHVORA_CAPTURE_ASSURANCE) {
    await page.screenshot({ path: fileURLToPath(new URL('../../../artifacts/assurance-customer-mobile.png', import.meta.url)), fullPage: true });
  }
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.getByText('Tôi chưa nhận hàng hoặc hàng có vấn đề').click();
  await page.locator('#delivery-issue').fill('Chưa nhận được kiện hàng, vui lòng kiểm tra chứng từ.');
  await page.getByRole('button', { name: 'Gửi báo cáo giao hàng' }).click();
  await expect(page.getByText('Đang chờ shop kiểm tra:')).toBeVisible();

  await signInDemo(page, 'Admin');
  await page.goto(`/Admin/Order/${orderId}`);
  await expect(page.locator('.admin-assurance-issue')).toContainText('Chưa nhận được kiện hàng, vui lòng kiểm tra chứng từ.');
  if (process.env.TECHVORA_CAPTURE_ASSURANCE) {
    await page.screenshot({ path: fileURLToPath(new URL('../../../artifacts/assurance-admin.png', import.meta.url)), fullPage: true });
  }
  await page.locator('#delivery-resolution').fill('Đã đối chiếu vận đơn, mời khách xác nhận lại.');
  await page.getByRole('button', { name: 'Gửi phản hồi và mở xác nhận lại' }).first().click();
  await expect(page.getByText('Khách báo có vấn đề')).toHaveCount(0);
  await page.locator('#cod-settlement-amount').fill('100000');
  await page.locator('#cod-settlement-reference').fill(`GHN-DEMO-${orderId}`);
  await page.getByRole('button', { name: 'Ghi nhận đã nhận COD' }).click();
  await expect(page.locator('.admin-assurance-panel')).toContainText('100.000 ₫');

  await signInDemo(page, 'User');
  await page.goto(`/Order/Detail/${orderId}`);
  await expect(page.locator('.assurance-response')).toContainText('Đã đối chiếu vận đơn, mời khách xác nhận lại.');
  await page.getByRole('button', { name: 'Tôi đã nhận hàng' }).click();
  await expect(page.locator('.assurance-panel').first()).toContainText('Khách đã xác nhận');
});

test('refund receipt report alerts the open admin page and can be resolved for customer confirmation', async ({ page }) => {
  await signInDemo(page, 'User');
  await page.goto('/Order/History');
  const detailUrls = await page.locator('a.order-code').evaluateAll(links => links.map(link => link.getAttribute('href')));
  let orderId;
  for (const url of detailUrls) {
    await page.goto(url);
    const awaitingDeliveryReceipt = await page.getByRole('button', { name: 'Tôi đã nhận hàng' }).isVisible().catch(() => false);
    const hasRefundPanel = await page.getByRole('heading', { name: 'Tiền hoàn đã về?' }).isVisible().catch(() => false);
    if (awaitingDeliveryReceipt && !hasRefundPanel) {
      orderId = page.url().match(/\/Detail\/(\d+)/)?.[1];
      break;
    }
  }
  expect(orderId, 'Demo needs a paid, delivered order to record a manual refund').toBeTruthy();

  const adminContext = await page.context().browser().newContext();
  const adminPage = await adminContext.newPage();
  try {
    await signInDemo(adminPage, 'Admin');
    const firstHubConnection = adminPage.waitForEvent('websocket', socket =>
      socket.url().includes('/hubs/admin-notifications'));
    await adminPage.goto(`/Admin/Order/${orderId}`);
    await firstHubConnection;
    const initialNotificationCount = Number(await adminPage.locator('#admin-notification-badge').textContent()) || 0;

    await adminPage.locator('details.admin-refund > summary').click();
    await adminPage.locator('#refund-method').selectOption({ label: 'Chuyển khoản ngân hàng' });
    await adminPage.locator('#refund-reference').fill(`DEMO-REFUND-${orderId}`);
    await adminPage.locator('#refund-note').fill('Đã chuyển hoàn thủ công trong dữ liệu demo.');
    await adminPage.locator('input[name="confirmed"]').check();
    const hubAfterRefund = adminPage.waitForEvent('websocket', socket =>
      socket.url().includes('/hubs/admin-notifications'));
    await adminPage.getByRole('button', { name: 'Ghi nhận đã chuyển tiền hoàn' }).click();
    await hubAfterRefund;
    await expect(adminPage.locator('.admin-assurance-history')).toContainText('Admin ghi nhận đã chuyển tiền hoàn');

    await page.goto(`/Order/Detail/${orderId}`);
    await expect(page.getByRole('button', { name: 'Tôi đã nhận tiền hoàn' })).toBeVisible();
    await page.locator('.assurance-report summary', { hasText: 'Tôi chưa nhận được tiền' }).click();
    await page.locator('#refund-issue').fill('Vẫn chưa thấy khoản hoàn vào tài khoản ngân hàng.');
    await page.getByRole('button', { name: 'Báo chưa nhận tiền' }).click();
    await expect(page.locator('.assurance-alert')).toContainText('Vẫn chưa thấy khoản hoàn vào tài khoản ngân hàng.');

    await expect(adminPage.locator('#admin-notification-badge')).toHaveText(String(initialNotificationCount + 1));
    const liveNotification = adminPage.locator('.adm-toast').last();
    await expect(liveNotification).toContainText('Khách báo chưa nhận tiền hoàn');
    await expect(liveNotification).toHaveAttribute('href', `/Admin/Order/${orderId}`);
    await liveNotification.click();
    await expect(adminPage.locator('.admin-assurance-issue')).toContainText('Vẫn chưa thấy khoản hoàn vào tài khoản ngân hàng.');

    const response = 'Đã đối soát mã giao dịch với ngân hàng, vui lòng kiểm tra lại tài khoản.';
    await adminPage.locator('#refund-resolution').fill(response);
    await adminPage.getByRole('button', { name: 'Gửi phản hồi và mở xác nhận lại' }).click();
    await expect(adminPage.locator('#admin-toast-stack .adm-toast.success')).toContainText('Khách có thể xác nhận lại khoản hoàn tiền.');

    await page.goto(`/Order/Detail/${orderId}`);
    await expect(page.locator('.assurance-response')).toContainText(response);
    await page.getByRole('button', { name: 'Tôi đã nhận tiền hoàn' }).click();
    await expect(page.locator('.assurance-panel').last()).toContainText('Khách đã xác nhận');
  } finally {
    await adminContext.close();
  }
});
