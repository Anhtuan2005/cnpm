import { test, expect } from './playwright.mjs';
import { signInDemo } from './auth.mjs';

test('confirmation sends one cancellation only after explicit approval', async ({ page }) => {
  await signInDemo(page, 'User');
  // Demo data may have no pending orders. Inject a form while keeping the real page,
  // dialog, and bundled script; intercept the POST so no order is changed.
  await page.route('**/Order/History', async route => {
    const response = await route.fetch();
    const body = (await response.text()).replace('</main>', `
      <form data-order-cancel-form data-order-code="#DH1234" action="/Order/Cancel" method="post">
        <button class="btn-ghost danger order-cancel-trigger" type="submit">Huỷ đơn</button>
      </form>
    </main>`);
    await route.fulfill({ response, body });
  });
  let postCount = 0;
  await page.route('**/Order/Cancel', async route => {
    postCount++;
    expect(route.request().method()).toBe('POST');
    await route.fulfill({ status: 200, contentType: 'text/html; charset=utf-8', body: '<h1>Đã gửi yêu cầu huỷ</h1>' });
  });
  await page.goto('/Order/History');
  await page.locator('[data-order-cancel-form][data-order-code="#DH1234"] button').click();
  await expect(page.locator('[data-order-cancel-dialog]')).toBeVisible();
  expect(postCount).toBe(0);
  await page.locator('[data-order-cancel-confirm]').click();
  await expect(page.getByRole('heading', { name: 'Đã gửi yêu cầu huỷ' })).toBeVisible();
  expect(postCount).toBe(1);
});
