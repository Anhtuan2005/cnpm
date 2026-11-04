# Kiểm thử lại và báo cáo lỗi Lab 6

Ngày 04/11/2026. Bộ test Lab 5 được giữ lại; Lab 6 chạy lại phần chịu tác động của sửa dependency, xử lý ảnh, đóng gói và fixture trình duyệt. Không thay test Pass/Fail bằng đánh giá chủ quan. [Kết quả từng test và hash đầu vào](../../test/evidence/lab6-2026-11-04.json) lưu bản tổng hợp không chứa cấu hình hoặc thông tin xác thực.

## Kết quả và phạm vi

| Lần chạy | Actual Result | Cách hiểu |
| --- | --- | --- |
| Backend trước sửa | 155 pass, 0 fail, 0 skip | Nghiệp vụ hiện có pass nhưng NuGet audit vẫn phát hiện advisory |
| Audit trước sửa | ImageSharp 3.1.12 có 6 advisory, Pkcs 8.0.1 có 2 | Lỗi chốt phát hành #40; không đồng nghĩa đã khai thác được qua ứng dụng |
| Upload retest sau thay thư viện | 3/3 đạt: ảnh PNG, JPEG xoay EXIF và PNG hỏng | Bổ sung 2 trường hợp hợp lệ, giữ test ảnh hỏng cũ |
| Backend cuối | **157 pass, 0 fail, 0 skip** | Sau sửa fixture dữ liệu test và dependency; không tính hai test mới bị lỗi setup ở lần chạy thử là Pass |
| Browser Sprint 3 local trước tách CDN | 33 pass, 0 fail, 0 skip | Chạy trực tiếp gói publish/giải nén |
| Browser Sprint 2 local trước tách CDN | **28 pass, 1 fail, 4 skip** | Test so sánh timeout do ảnh CDN 49,9 giây; giữ kết quả thất bại này để truy vết #43 |
| Retest riêng #43 | 1/1 đạt; thân test 2,4 giây | Sau khi thay tài nguyên ngoài bằng fixture |
| Browser Sprint 2 local cuối | **29 pass, 0 fail, 4 skip** | 4 skip chỉ cho quản trị phụ kiện, mua ngay kèm phụ kiện, cross-sell và Flash Sale ở Sprint 3 |
| Browser Sprint 3 local cuối | **33 pass, 0 fail, 0 skip** | Chạy lại toàn bộ sau sửa fixture trên database demo mới |
| Asset và audit cuối | Asset đồng bộ; NuGet audit không liệt kê advisory | Không kết luận toàn bộ ứng dụng không có lỗ hổng |
| Publish/giải nén | Manifest từng file đạt; không có upload, local config hoặc runtime data | Có database demo mới khi chủ động chạy script |
| CI tại b705b18 | Hai job đạt: mỗi job 157 backend; browser S2 29/4skip, S3 33/0skip | [Run 41769846813](https://github.com/Anhtuan2005/cnpm/actions/runs/41769846813), checkout sạch Ubuntu 24.04, SQL Server 2022, Chromium, gói giải nén |

Môi trường local: Windows, target net8.0, SQL Server local, Edge. Gói ứng viên local đầu tiên có manifest ghi working tree còn thay đổi; không dùng nhãn commit cũ trong manifest đó để khẳng định là bản phát hành bất biến. CI xây gói từ checkout sạch; gói tải ở Release cuối được build lại từ đúng Tag và có manifest/SHA256 riêng.

## Bug Report và kiểm thử lại

| Bug | Ưu tiên và người xử lý | Expected → Actual trước sửa | Bản sửa và retest |
| --- | --- | --- | --- |
| [#40 Dependency advisory](https://github.com/Anhtuan2005/cnpm/issues/40) | High · Nguyễn Trương Anh Tuấn | Audit không còn package thuộc khoảng đã công bố lỗi → 8 advisory trên 2 package | `b705b18`: SkiaSharp 3.119.4 thay ImageSharp, Pkcs 8.0.2; audit sạch và 157 test backend đạt |
| [#41 Upload lọt vào publish](https://github.com/Anhtuan2005/cnpm/issues/41) | Medium · Nguyễn Trương Anh Tuấn | Gói không kèm dữ liệu máy build → publish ban đầu chứa 10 upload local | `b705b18`: CopyToPublishDirectory loại upload/settings/runtime; build ZIP, xác minh manifest và chạy từ bản giải nén |
| [#43 Timeout tài nguyên CDN](https://github.com/Anhtuan2005/cnpm/issues/43) | Medium · Nguyễn Trương Anh Tuấn | Test nghiệp vụ không phụ thuộc độ trễ ảnh/icon ngoài → page.goto quá 30 giây | `de77cb7`: fixture ảnh/font/stylesheet/script ngoài; test so sánh retest đạt; hai suite được chạy lại |

[PR #42](https://github.com/Anhtuan2005/cnpm/pull/42) liên kết ba lỗi và commit. Trạng thái đóng hiện tại xem trực tiếp ở issue; nhánh sửa chỉ đóng lỗi sau retest. #38 Playwright đã được sửa và đóng ở Buổi 7; không tạo lại lỗi lịch sử.

## Các kiểm tra chịu ảnh hưởng

- `UploadValidationTests`: file hợp lệ được chuyển WebP đúng kích thước, JPEG orientation 6 được xoay đúng; URL thuộc upload folder, không xóa nhầm folder; PNG hỏng trả lỗi validation. Thư viện được kiểm tra trên cả Windows và Linux CI.
- `PasswordResetTests`, `UserAccessTests`, `OrderPaymentTests`, `SprintThreeIntegrationTests`: chạy lại vì dependency/build và gói thay đổi; dùng SQL Server thật, sender/provider stub ở các điểm tích hợp ngoài.
- Browser chạy cả cấu hình Sprint 2/3 vì phạm vi route/menu/job và phụ kiện khác nhau; có tạo COD thực trên database demo, kiểm tra validation, giỏ, hậu mãi, báo cáo và quyền truy cập.
- Build/manifest kiểm tra đúng output publish, thay vì suy ra file an toàn từ `.gitignore`. Script không xóa dữ liệu nguồn hoặc database đang làm việc.

## Chạy lại

```powershell
dotnet test test/test-cases/EcommerceApp.Tests/EcommerceApp.Tests.csproj -c Release --logger trx --results-directory artifacts/lab6/retest
dotnet list src/backend/EcommerceApp.csproj package --vulnerable --include-transitive
npm --prefix src/frontend ci
npm --prefix src/frontend run check:assets
.\scripts\release\Build-Package.ps1 -OutputDirectory artifacts\lab6-rebuild
.\scripts\release\Test-Package.ps1 -Package artifacts\lab6-rebuild\Techvora-1.0.0.zip -Destination artifacts\lab6-recheck
.\artifacts\lab6-recheck\Start-Demo.ps1 -Port 5019 -Sprint 3
```

Trong terminal khác:

```powershell
$env:TECHVORA_SMOKE_URL = 'http://127.0.0.1:5019'
$env:PLAYWRIGHT_JSON_OUTPUT_FILE = '../../artifacts/lab6/retest-browser.json'
npm --prefix src/frontend run test:smoke -- --reporter=list,json
```

Dùng database/server demo mới cho mỗi suite vì test tạo/hủy đơn, chuyển trạng thái hỗ trợ và dùng rate limit đăng nhập. Chạy thêm với `-Sprint 2`; bốn test chỉ thuộc Sprint 3 skip có chủ đích. Kết quả browser mặc định dùng fixture cho tài nguyên ngoài; đặt `TECHVORA_SMOKE_LIVE_ASSETS=true` để kiểm tra ảnh/font/icon thật riêng. Test địa chỉ giả lập API trong spec; không suy ra API địa chỉ thật sẵn sàng.

## Giới hạn xác nhận

Không kiểm thử giao dịch VNPAY/GHN hoặc SMTP thật tại Lab 6. Gemini thật có bằng chứng riêng ngày 29/10; không dùng số liệu đó làm kết quả provider của ngày 04/11. Chưa có thử nghiệm tải, kiểm toán bảo mật đầy đủ, thử máy thành viên thứ hai hoặc Docker. Các test mock/stub và ảnh minh chứng không thay thế demo/PO ký nghiệm thu.

Nguồn dependency: [ImageSharp 3.2 yêu cầu license lúc build](https://docs-v3.sixlabors.com/articles/imagesharp/index.html), [SkiaSharp 3.119.4](https://mono.github.io/SkiaSharp/docs/releases/3.119.4.html), [Pkcs advisory](https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx). Bản sửa dùng package có thể build trên checkout sạch, không vô hiệu hóa kiểm tra bản quyền.
