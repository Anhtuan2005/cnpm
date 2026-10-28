# Báo cáo kiểm thử tích hợp Sprint 1 và Sprint 2

**Ngày chạy: 28/10/2026. Kết quả: 132/132 backend, 26/26 browser, 0 fail, 0 skipped; kiểm tra asset đạt.** Bản ứng dụng nền `73aa335`; mã test và bản sửa runner tại [04f4a18](https://github.com/Anhtuan2005/cnpm/commit/04f4a180d1f01d4d6148d9a03721ec92f0ac16d1). CI được bổ sung trong [PR #39](https://github.com/Anhtuan2005/cnpm/pull/39); kết quả local và kết quả CI được phân biệt rõ.

Môi trường local: Windows, ứng dụng/test target .NET 8, SQL Server local, Microsoft Edge, `Release:ActiveSprint=2`. Backend tạo database `TechvoraTests_*` riêng và tự xóa; browser dùng database demo `TechvoraBuoi7_20261028`. Mỗi lần chạy lại toàn bộ browser cần database demo mới vì các test thay đổi trạng thái nhận hàng, hoàn tiền và yêu cầu hỗ trợ.

## Kết quả thực thi

Các dòng dưới là trường hợp tiêu biểu từ bộ test đã chạy, không phải kết quả demo thủ công. Danh sách đủ tên test, kết quả và thời lượng được lưu trong [minh chứng JSON](evidence/buoi7-2026-10-28.json), đã loại đường dẫn máy và cấu hình không cần công khai.

| ID và loại | Bước thực hiện và input | Expected Result | Actual Result | Kết quả |
| --- | --- | --- | --- | --- |
| IT-01 Integration | Giỏ khách 1 máy + tài khoản có 1 máy cùng loại và 1 máy khác; ghép giỏ; mua 2 máy giá 1.000.000; voucher 100.000; phí TP.HCM; giao, đánh giá, phản hồi và bảo hành | Tổng 1.920.000; kho 5→3; giữ máy chưa chọn trong giỏ; một lượt voucher; review/reply và hai thông báo gắn đúng đơn | Các giá trị và bản ghi được xác nhận từ DbContext mới; người khác không đọc được đơn/yêu cầu | Pass |
| IT-02 Integration | Khách gửi yêu cầu từ đơn đã giao; admin đang mở trang nhận thông báo và bấm liên kết | Badge tăng, có thông báo realtime, mở đúng `/Admin/Order/{id}#support-request-{id}` ở Sprint 2 | Browser xác nhận badge, URL, nội dung yêu cầu và thông báo thành công | Pass |
| IT-03 Integration | Khách báo chưa nhận hàng; admin phản hồi; khách xác nhận lại | Sự cố hiện cho admin; phản hồi đến khách; ghi nhận xác nhận mới | Browser chạy xuyên hai vai trò; các nội dung và trạng thái đúng | Pass |
| IT-04 Integration | Hai callback thanh toán hợp lệ đồng thời, gồm cả callback đến sau hủy | Ghi nhận một lần; không hồi sinh đơn hủy; đặt trạng thái cần hoàn nếu nhận tiền muộn | Các assert về paid/status/refund và số lần gửi email đạt | Pass với callback giả lập |
| RG-01 Regression | Hai yêu cầu hủy cùng lúc trên đơn còn được hủy | Chỉ một lần hủy thành công, hoàn tồn kho đúng một lần | `Concurrent_cancellation_restores_inventory_once` đạt | Pass |
| RG-02 Regression | Hai khách dùng voucher chỉ còn 1 lượt | Một đơn áp dụng thành công, UsedCount=1, một VoucherUsage | `Last_voucher_use_is_atomic_across_customers` đạt | Pass |
| RG-03 Regression | Giỏ thêm 0, âm hoặc giá trị gây tràn | Từ chối input, giỏ cũ không đổi | Test an toàn số lượng đạt; browser kiểm tra dữ liệu sai không xóa hàng | Pass |
| RG-04 Regression | Mua ngay khi đã có sản phẩm khác trong giỏ | Checkout đúng sản phẩm; số lượng giỏ trước đó không đổi | Backend và browser `buy now` đều đạt | Pass |
| RG-05 Regression | Địa chỉ Bến Tre, Châu Thành, Tân Thạch; để trống số nhà rồi gửi đến server | Báo lỗi Street và giữ lại tỉnh/huyện/xã | Browser xác nhận validation và các lựa chọn sau render | Pass với API địa chỉ giả lập |
| RG-06 Regression | Truy cập trang Sprint 1–2, rồi AI/BuildPC/sitemap/robots trong bản Sprint 2 | Trang trong phạm vi hoạt động; trang chưa mở trả 404; đơn yêu cầu đăng nhập | Browser và `SprintScopeTests` đạt | Pass |
| RG-07 Regression | Checkout gửi DiscountAmount=999999 và ShippingFee=1 cùng voucher hợp lệ | Server bỏ số tiền client, tính giảm 100.000 và phí 20.000 | IT-01 xác nhận các số tiền đã lưu đúng | Pass |
| RT-38 Retest công cụ | Chạy browser từ package frontend sau sửa đường dẫn resolve Playwright | Config/spec cùng runner; discover và chạy toàn bộ test | Discover 26 test; 26/26 chạy đạt trên Edge | Pass |
| EXT-01 Kiểm thử ngoài | Tạo giao dịch VNPAY Sandbox và vận đơn GHN thật, callback qua URL công khai | Đối chiếu ID giao dịch/vận đơn và trạng thái thực | Chưa thực hiện trong buổi kiểm thử này | Chưa chạy |

## Truy vết test

| ID | File và test |
| --- | --- |
| IT-01, RG-07 | [SprintIntegrationTests.cs](test-cases/EcommerceApp.Tests/SprintIntegrationTests.cs): `Sprint_one_cart_flows_into_sprint_two_voucher_delivery_review_and_support` |
| IT-02 | [support-request.spec.mjs](test-cases/browser/support-request.spec.mjs) |
| IT-03 | [order-assurance.spec.mjs](test-cases/browser/order-assurance.spec.mjs) |
| IT-04, RG-01, RG-02 | [OrderPaymentTests.cs](test-cases/EcommerceApp.Tests/OrderPaymentTests.cs): `Concurrent_payment_callbacks_apply_once_including_payment_after_cancellation`, `Concurrent_cancellation_restores_inventory_once`, `Last_voucher_use_is_atomic_across_customers` |
| RG-03, RG-04 | [SafetyRegressionTests.cs](test-cases/EcommerceApp.Tests/SafetyRegressionTests.cs), [cart-resilience.spec.mjs](test-cases/browser/cart-resilience.spec.mjs), [buy-now.spec.mjs](test-cases/browser/buy-now.spec.mjs) |
| RG-05 | [checkout-address.spec.mjs](test-cases/browser/checkout-address.spec.mjs) |
| RG-06 | [SprintScopeTests.cs](test-cases/EcommerceApp.Tests/SprintScopeTests.cs), [storefront.spec.mjs](test-cases/browser/storefront.spec.mjs) |
| RT-38 | [playwright.mjs](test-cases/browser/playwright.mjs), [issue #38](https://github.com/Anhtuan2005/cnpm/issues/38) |

## Lệnh chạy lại

Tại gốc repository:

```powershell
dotnet test test/test-cases/EcommerceApp.Tests/EcommerceApp.Tests.csproj --configuration Release --logger "trx;LogFileName=sprint12.trx" --results-directory artifacts/buoi7/retest
npm --prefix src/frontend ci
npm --prefix src/frontend run check:assets
```

Tạo ứng dụng demo ở terminal riêng; dùng tên database mới cho mỗi lần chạy browser, không dùng database nghiệp vụ:

```powershell
$demoDatabase = 'TechvoraBuoi7_' + [Guid]::NewGuid().ToString('N')
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=$demoDatabase;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
$env:Database__MigrateOnStartup = 'true'
$env:Demo__Enabled = 'true'
$env:Email__Enabled = 'false'
$env:Release__ActiveSprint = '2'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/backend/EcommerceApp.csproj --configuration Release --no-launch-profile --urls http://127.0.0.1:5017
```

Trong terminal kiểm thử:

```powershell
$env:TECHVORA_SMOKE_URL = 'http://127.0.0.1:5017'
$env:PLAYWRIGHT_CHANNEL = 'msedge'
$env:PLAYWRIGHT_JSON_OUTPUT_FILE = '../../artifacts/buoi7/browser-results.json'
npm --prefix src/frontend run test:smoke -- --reporter=list,json
```

Backend gốc trước bổ sung test đạt 131/131; sau bổ sung đạt 132/132. Bộ browser ban đầu không discover được do lỗi #38; sau sửa đạt 26/26. Không coi lỗi khởi chạy công cụ là lỗi checkout.

## Giới hạn của kết quả

IT-01 dùng SQL Server thật và các service nghiệp vụ thật; email, hub transport và dịch vụ ngoài được thay thế. Browser IT-02/IT-03 kiểm tra HTTP, giao diện và SignalR thật trên demo. Một số test kiểm tra biểu mẫu có chặn request để kiểm soát input/lỗi; API địa chỉ trong RG-05 được giả lập. Không gán Pass cho giao dịch VNPAY/GHN thật hoặc đồng bộ tiền về tài khoản ngân hàng khi chưa thử sandbox.

## CI và quản lý lỗi

[Workflow](../.github/workflows/portfolio-tests.yml) chạy trên Ubuntu 24.04 để tương thích Playwright 1.58.2 đã khóa; có SQL Server 2022, backend test, kiểm tra asset và Chromium. Mỗi lần chạy lưu artifact `sprint12-test-results` gồm TRX, kết quả browser và log/trace nếu lỗi. Lần CI đầu trên `ubuntu-latest` không cài được browser do runner chuyển sang Ubuntu 26.04; đã sửa pin runner trong commit `435deda`.

[PR #39](https://github.com/Anhtuan2005/cnpm/pull/39) liên kết bản sửa và các lần CI. Issue #38 giữ mở khi PR chưa merge; local retest đã đạt. Các hạn chế sandbox được ghi là việc kiểm chứng còn lại, không tạo bug giả.

**CI đã xác minh:** [run 40641956825](https://github.com/Anhtuan2005/cnpm/actions/runs/40641956825) của commit `435deda9ae472ccba8cacf1c62cf53ec1b2032b7` thành công trên Ubuntu 24.04: **132 backend pass, 26 Chromium pass**, kiểm tra asset đạt. Đây là lần chạy trên checkout sạch của GitHub, độc lập với kết quả Edge local.
