import { test, expect } from './playwright.mjs';
import { signInDemo } from './auth.mjs';

test('the fifth failed login locks the account for 30 minutes and updates the open admin list', async ({ page, browser, baseURL }) => {
  test.setTimeout(45000);
  await page.goto('/');
  test.skip(Number(await page.locator('body').getAttribute('data-active-sprint')) < 3, 'User management opens in Sprint 3');
  await signInDemo(page, 'Admin');
  await page.goto('/Admin/User');
  const email = 'khachhang2@shop.vn';
  const row = page.locator('[data-admin-user-id]').filter({ hasText: email });
  await expect(row).toContainText('Hoạt động');

  const customerContext = await browser.newContext({ baseURL });
  try {
    await customerContext.route('**/*', route => new URL(route.request().url()).origin === new URL(baseURL).origin
      ? route.continue() : route.abort());
    const customer = await customerContext.newPage();
    await customer.goto('/Account/Login');
    const notice = customer.locator('.auth-login-alert[role="alert"]');
    await expect(notice).toHaveCount(0);
    await customer.locator('#Email').fill(email);
    for (let attempt = 1; attempt <= 5; attempt++) {
      await customer.locator('#Password').fill('IncorrectPassword123');
      await customer.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
      await expect(notice).toContainText(attempt < 5
        ? `Bạn còn ${5 - attempt} lần thử đăng nhập.`
        : 'Vui lòng thử lại sau 30 phút.');
    }
    await expect(notice).toHaveClass(/auth-login-alert--warning/);
    await expect(notice.getByText('Tạm khoá đăng nhập', { exact: true })).toBeVisible();

    await page.bringToFront();
    await expect(row).toContainText('Tạm khoá đăng nhập đến', { timeout: 15000 });
    await expect(row.getByRole('link', { name: 'Cấp Admin' })).toBeHidden();
    await expect(page.getByRole('heading', { name: 'Cảnh báo đăng nhập' })).toHaveCount(0);
    const inbox = await page.request.get('/Admin/Notification');
    expect(await inbox.text()).not.toContain('Cảnh báo đăng nhập bất thường');
    await row.getByRole('link', { name: 'Xem xét', exact: true }).click();
    await expect(page.getByText('Khoá tạm do đăng nhập sai', { exact: true })).toBeVisible();
    await page.getByLabel('Lý do xử lý').fill('Kết thúc kiểm thử khoá tạm.');
    await page.getByRole('button', { name: 'Xác nhận mở khoá' }).click();
    await expect(page.getByText('Hoạt động', { exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Cấp Admin', exact: true })).toHaveCount(0);

    await page.getByLabel('Lý do xử lý').fill('Kiểm thử khoá thủ công một tuần.');
    await page.getByLabel('Thời hạn khoá').selectOption('7');
    await page.getByRole('button', { name: 'Xác nhận khoá tài khoản' }).click();
    await expect(page.getByText(/Quản trị viên đã khoá đến/)).toBeVisible();
    await expect(page.getByText(/Khoá đến \d/)).toBeVisible();
    await customer.locator('#Password').fill('User@123');
    await customer.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(notice).not.toHaveClass(/auth-login-alert--warning/);
    await expect(notice.getByText('Tài khoản bị khoá', { exact: true })).toBeVisible();
    await expect(notice).toContainText('đã bị quản trị viên khoá đến');
    await page.getByLabel('Lý do xử lý').fill('Kết thúc kiểm thử khoá thủ công.');
    await page.getByRole('button', { name: 'Xác nhận mở khoá' }).click();
    await page.getByRole('link', { name: 'Danh sách người dùng', exact: true }).click();
    await row.getByRole('link', { name: 'Cấp Admin', exact: true }).click();
    await expect(page).toHaveURL(/\/Admin\/User\/ConfirmRole\//);
    await expect(page.getByLabel('Mật khẩu hiện tại của bạn')).toBeVisible();
  } finally {
    await customerContext.close();
  }
});
