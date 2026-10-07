# Change Request CR-01 – Dữ liệu kiểm thử đăng ký tài khoản

Ngày lập: 07/10/2026 · Trạng thái: đã triển khai và merge qua [PR #1](https://github.com/Anhtuan2005/cnpm/pull/1); chưa có review được ghi nhận trên PR.

| Nội dung | Kết quả |
| --- | --- |
| Mã thay đổi | CR-01 |
| Mô tả | Bổ sung dữ liệu test có đầu vào hợp lệ, không hợp lệ và giá trị biên cho biểu mẫu đăng ký; thêm bài kiểm thử tự động đọc chính dữ liệu này để kiểm tra quy tắc validation hiện hành. |
| Người đề xuất | Chưa xác nhận người đề xuất chính thức trong nhóm. |
| Mức ưu tiên | Medium |
| CI bị ảnh hưởng | CI-06 (test và test data), CI-07 (hồ sơ thay đổi), CI-05 (cấu hình test project). |
| Tiến độ | Thấp: dữ liệu và bài test độc lập, không cần sửa schema hoặc luồng mua hàng. |
| Chi phí | Không phát sinh dịch vụ ngoài; chỉ cần thời gian review và chạy test. |
| Chất lượng | Tăng khả năng phát hiện hồi quy ở các ranh giới email, số điện thoại và mật khẩu; bài test chỉ kiểm tra model validation, chưa kiểm thử đăng ký với database thật. |
| Quyết định | Approve cho việc triển khai trên branch theo yêu cầu của chủ repository. PR #1 đã merge, nhưng tiêu chí review bởi người khác tác giả chưa được đáp ứng. |
| Branch | `codex/lab3-scm-registration-tests` |

## Tiêu chí chấp nhận

1. Có ít nhất ba mẫu hợp lệ và ba mẫu không hợp lệ hoặc biên trong `test/test-data/registration_test_data.json`.
2. Bài test đọc file dữ liệu, kiểm tra `RegisterViewModel` bằng `Validator.TryValidateObject` và xác nhận trường có lỗi.
3. Test chạy độc lập, không cần SQL Server hoặc tài khoản ngoài.
4. PR có review của người khác tác giả trước khi merge.

## Minh chứng

- Dữ liệu: [`registration_test_data.json`](../../test/test-data/registration_test_data.json)
- Mã test: [`RegistrationValidationDataTests.cs`](../../test/test-cases/EcommerceApp.Tests/RegistrationValidationDataTests.cs)
- Kết quả local: `dotnet test test/test-cases/EcommerceApp.Tests/EcommerceApp.Tests.csproj --filter FullyQualifiedName~RegistrationValidationDataTests --no-restore --verbosity minimal` đạt **6/6** ngày 07/10/2026.
- Commit bộ dữ liệu và bài test: `ae26805` (`test: cover registration validation with lab data`). PR: [#1](https://github.com/Anhtuan2005/cnpm/pull/1), đã merge ngày 07/10/2026; chưa có review được ghi nhận.
