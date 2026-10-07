# Change Request CR-01 – Dữ liệu kiểm thử đăng ký tài khoản

Ngày lập: 07/10/2026 · Trạng thái: đã triển khai trên branch và mở [PR #1](https://github.com/Anhtuan2005/cnpm/pull/1), chờ review và merge.

| Nội dung | Kết quả |
| --- | --- |
| Mã thay đổi | CR-01 |
| Mô tả | Bổ sung dữ liệu test có đầu vào hợp lệ, không hợp lệ và giá trị biên cho biểu mẫu đăng ký; thêm bài kiểm thử tự động đọc chính dữ liệu này để kiểm tra quy tắc validation hiện hành. |
| Người đề xuất | Codex trong quá trình thực hiện bài Lab 3 theo yêu cầu của chủ repository; nhóm cần xác nhận người đề xuất chính thức. |
| Mức ưu tiên | Medium |
| CI bị ảnh hưởng | CI-06 (test và test data), CI-07 (hồ sơ thay đổi), CI-05 (cấu hình test project). |
| Tiến độ | Thấp: dữ liệu và bài test độc lập, không cần sửa schema hoặc luồng mua hàng. |
| Chi phí | Không phát sinh dịch vụ ngoài; chỉ cần thời gian review và chạy test. |
| Chất lượng | Tăng khả năng phát hiện hồi quy ở các ranh giới email, số điện thoại và mật khẩu; bài test chỉ kiểm tra model validation, chưa kiểm thử đăng ký với database thật. |
| Quyết định | Chọn triển khai để mở PR; quyết định chấp thuận và merge chính thức thuộc về reviewer/nhóm. |
| Branch | `codex/lab3-scm-registration-tests` |

## Tiêu chí chấp nhận

1. Có ít nhất ba mẫu hợp lệ và ba mẫu không hợp lệ hoặc biên trong `evidence/lab3/registration_test_data.json`.
2. Bài test đọc file dữ liệu, kiểm tra `RegisterViewModel` bằng `Validator.TryValidateObject` và xác nhận trường có lỗi.
3. Test chạy độc lập, không cần SQL Server hoặc tài khoản ngoài.
4. PR có review của người khác tác giả trước khi merge.

## Minh chứng

- Dữ liệu: [`registration_test_data.json`](../../evidence/lab3/registration_test_data.json)
- Mã test: [`RegistrationValidationDataTests.cs`](../../tests/EcommerceApp.Tests/RegistrationValidationDataTests.cs)
- Kết quả local: `dotnet test tests/EcommerceApp.Tests/EcommerceApp.Tests.csproj --filter FullyQualifiedName~RegistrationValidationDataTests --no-restore --verbosity minimal` đạt **6/6** ngày 07/10/2026.
- Commit bộ dữ liệu và bài test: `ae26805` (`test: cover registration validation with lab data`). PR: [#1](https://github.com/Anhtuan2005/cnpm/pull/1). Chưa có review hoặc merge tại thời điểm lập hồ sơ.
