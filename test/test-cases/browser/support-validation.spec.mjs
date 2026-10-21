import { test, expect } from '@playwright/test';

test('invalid support request points to each field and clears errors after correction', async ({ page }) => {
  await page.goto('/');
  await page.setContent(`
    <link rel="stylesheet" href="/css/site.css">
    <form class="support-form" action="/ReturnWarranty/Create" method="post" data-support-form>
      <div class="support-type-grid"><input type="radio" name="Type" value="Đổi trả" checked></div>
      <span data-valmsg-for="Type"></span>
      <div class="support-item-list">
        <div class="support-item-row">
          <input type="checkbox" name="Items[0].Selected" checked>
          <input type="number" name="Items[0].Quantity" value="1" min="1" max="1">
          <small data-valmsg-for="Items[0].Quantity"></small>
        </div>
      </div>
      <small data-support-item-error hidden></small>
      <input name="ContactName" value="Anh Tuấn Nguyễn Trương">
      <span data-valmsg-for="ContactName"></span>
      <input name="ContactPhone" value="0348021904">
      <span data-valmsg-for="ContactPhone"></span>
      <select id="Reason" name="Reason" data-val="true" data-val-required="Vui lòng chọn lý do">
        <option value="">Chọn lý do</option>
        <option value="Sản phẩm lỗi khi nhận hàng">Sản phẩm lỗi khi nhận hàng</option>
      </select>
      <span data-valmsg-for="Reason" data-valmsg-replace="true"></span>
      <textarea id="Description" name="Description" data-val="true"
        data-val-required="Vui lòng mô tả tình trạng sản phẩm"
        data-val-minlength="Mô tả cần ít nhất 20 ký tự" data-val-minlength-min="20"></textarea>
      <span data-valmsg-for="Description" data-valmsg-replace="true"></span>
      <input name="Images" type="file" multiple>
      <small data-support-image-error hidden></small>
      <div class="support-submit-bar">
        <button type="submit">Gửi yêu cầu</button>
      </div>
    </form>
    <script src="/js/site.js"></script>
  `);
  await page.locator('#Description').fill('Đơn hàng bị lỗi');
  await page.getByRole('button', { name: 'Gửi yêu cầu' }).click();

  await expect(page.locator('[data-valmsg-for="Reason"]')).toHaveText('Vui lòng chọn lý do.');
  await expect(page.locator('[data-valmsg-for="Description"]')).toHaveText('Mô tả chưa đủ 20 ký tự (hiện có 15).');
  await expect(page.locator('#Reason')).toHaveClass(/support-invalid/);
  await expect(page.locator('#Description')).toHaveClass(/support-invalid/);
  await expect(page.locator('#Reason')).toBeFocused();
  await expect(page.locator('.support-submit-bar')).not.toContainText('Vui lòng chọn lý do');

  await page.locator('#Reason').selectOption({ label: 'Sản phẩm lỗi khi nhận hàng' });
  await page.locator('#Description').fill('Sản phẩm khởi động nhưng màn hình không hiển thị.');
  await expect(page.locator('[data-valmsg-for="Reason"]')).toBeHidden();
  await expect(page.locator('[data-valmsg-for="Description"]')).toBeHidden();
  await expect(page.locator('#Reason')).not.toHaveClass(/support-invalid/);

  await page.locator('[name="ContactName"]').fill('');
  await page.locator('[name="ContactPhone"]').fill('');
  await page.locator('#Description').fill('');
  await page.locator('[name="Items[0].Selected"]').uncheck();
  await page.getByRole('button', { name: 'Gửi yêu cầu' }).click();
  await expect(page.locator('[data-support-item-error]')).toContainText('Vui lòng chọn ít nhất một sản phẩm');
  await expect(page.locator('[data-valmsg-for="ContactName"]')).toContainText('Vui lòng nhập người liên hệ');
  await expect(page.locator('[data-valmsg-for="ContactPhone"]')).toContainText('Vui lòng nhập số điện thoại');
  await expect(page.locator('[data-valmsg-for="Description"]')).toContainText('Vui lòng mô tả tình trạng sản phẩm');

  await page.locator('[name="Items[0].Selected"]').check();
  await page.locator('[name="Items[0].Quantity"]').fill('2');
  await page.getByRole('button', { name: 'Gửi yêu cầu' }).click();
  await expect(page.locator('[data-valmsg-for="Items[0].Quantity"]')).toContainText('Số lượng phải từ 1 đến 1');
  await page.locator('[name="Items[0].Quantity"]').fill('1');

  await page.locator('[name="Images"]').setInputFiles({ name: 'ghi-chu.txt', mimeType: 'text/plain', buffer: Buffer.from('not an image') });
  await page.getByRole('button', { name: 'Gửi yêu cầu' }).click();
  await expect(page.locator('[data-support-image-error]')).toContainText('Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP');
  await page.locator('[name="Images"]').setInputFiles([]);

  await page.locator('[name="ContactName"]').fill('Anh Tuấn Nguyễn Trương');
  await page.locator('[name="ContactPhone"]').fill('0348021904');
  await page.locator('#Description').fill('Sản phẩm khởi động nhưng màn hình không hiển thị.');
  await expect(page.locator('[data-support-image-error]')).toBeHidden();
  await page.route('**/ReturnWarranty/Create', route => route.fulfill({ status: 200, body: 'accepted' }));
  const request = page.waitForRequest(request => request.url().endsWith('/ReturnWarranty/Create') && request.method() === 'POST');
  await page.getByRole('button', { name: 'Gửi yêu cầu' }).click();
  await request;
});
