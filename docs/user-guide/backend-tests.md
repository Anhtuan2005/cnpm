# Backend test evidence

Sau khi sắp xếp lại repo ngày 07/10/2026, lệnh `dotnet test test/test-cases/EcommerceApp.Tests/EcommerceApp.Tests.csproj --configuration Debug --no-restore --verbosity minimal` đạt **110/110** test backend trên máy local. `dotnet build EcommerceApp.sln` và `npm run check:assets` trong `src/frontend/` cũng thành công. Ứng dụng trả HTTP 200 cho `/Home/Privacy` và `/css/site.css`.

Kiểm tra local ngày 03/10/2026 trên Windows, test target `net8.0` và SQL Server local. Đây là kết quả của mã nguồn tại máy ở thời điểm chạy, không thay thế kết quả CI sau khi publish.

```powershell
dotnet test test/test-cases/EcommerceApp.Tests/EcommerceApp.Tests.csproj --no-restore --verbosity quiet
```

```text
Passed!  - Failed:     0, Passed:   104, Skipped:     0, Total:   104, Duration: 15 s - EcommerceApp.Tests.dll (net8.0)
```

Trên bản clone mới, bỏ `--no-restore` để `dotnet test` tự khôi phục dependency. Test tạo database `TechvoraTests_*` riêng trên SQL Server được cấu hình qua `TECHVORA_TEST_SQLSERVER`, chạy migration rồi xóa database sau khi hoàn tất. Nếu không đặt biến này, test dùng SQL Server `localhost` với Windows Authentication.

| Tình huống | Mã test |
| --- | --- |
| Kiểm tra route Sprint 2 và khóa chức năng Sprint 3 trở lên | [`SprintScopeTests.cs`](../../test/test-cases/EcommerceApp.Tests/SprintScopeTests.cs) |
| Yêu cầu hỗ trợ đến trang đơn hàng admin trong Sprint 2 | [`SafetyRegressionTests.cs`](../../test/test-cases/EcommerceApp.Tests/SafetyRegressionTests.cs) |
| Phí vận chuyển được tính theo khu vực giao hàng | [`PortfolioQualityTests.cs`](../../test/test-cases/EcommerceApp.Tests/PortfolioQualityTests.cs) |
| Hai khách tranh món hàng cuối và tạo đơn nguyên tử | [`OrderPaymentTests.cs`](../../test/test-cases/EcommerceApp.Tests/OrderPaymentTests.cs) |
| Phân quyền tài khoản và đặt lại mật khẩu | [`UserAccessTests.cs`](../../test/test-cases/EcommerceApp.Tests/UserAccessTests.cs), [`PasswordResetTests.cs`](../../test/test-cases/EcommerceApp.Tests/PasswordResetTests.cs) |

[Workflow CI](../../.github/workflows/portfolio-tests.yml) chạy lại project test với SQL Server 2022 và lưu file TRX. Mốc **104/104** ở trên là lần chạy local, không phải kết quả CI đã publish.

Smoke test trình duyệt chạy trên Microsoft Edge với ứng dụng Sprint 2 tại `http://localhost:5009`:

Từ `src/frontend/`, chạy `npm run test:smoke` sau khi ứng dụng demo đã khởi động. Lần chạy 22/22 bên dưới là kết quả trước khi sắp xếp lại repo; sau khi di chuyển, Playwright đã liệt kê được đầy đủ 22 ca kiểm thử ở đường dẫn mới.

```text
22 passed (1.0m)
```
