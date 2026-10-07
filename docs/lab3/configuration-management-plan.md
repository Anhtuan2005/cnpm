# Kế hoạch quản lý cấu hình Techvora

Phiên bản kế hoạch: `v1.0.0` · Ngày lập: 07/10/2026 · Phạm vi: repository `Anhtuan2005/cnpm`.

## Configuration Items

| Mã | CI | Vị trí hiện tại | Vai trò phụ trách | Kiểm soát phiên bản |
| --- | --- | --- | --- | --- |
| CI-01 | Yêu cầu và mô tả sản phẩm | `README.md`; tài liệu mới tại `docs/requirements/` khi có | BA / chủ sản phẩm | Git; sửa qua PR; gắn tag phát hành |
| CI-02 | Thiết kế giao diện và ảnh minh chứng | `Views/`, `wwwroot/`, `docs/screenshots/`; thiết kế mới tại `docs/design/` | UI/UX và frontend | Git; review ảnh và giao diện khi đổi |
| CI-03 | Mã nguồn backend | `Controllers/`, `Services/`, `Models/` | Backend | Branch riêng, commit và PR |
| CI-04 | Lược đồ và dữ liệu demo | `Data/AppDbContext.cs`, `Data/Migrations/`, `Data/SeedData.cs` | Backend / database | Migration bất biến sau khi đã chia sẻ; PR kèm kiểm thử |
| CI-05 | Cấu hình ứng dụng và build | `appsettings.json`, `EcommerceApp.csproj`, `.github/workflows/` | Backend / DevOps | Git; secret lưu ngoài repository; review thay đổi cấu hình |
| CI-06 | Ca kiểm thử và dữ liệu kiểm thử | `tests/EcommerceApp.Tests/`, `evidence/lab3/` | Tester / developer | Git; dữ liệu có kết quả mong đợi; chạy test trong PR |
| CI-07 | Tài liệu hướng dẫn và hồ sơ thay đổi | `README.md`, `docs/lab3/`, `evidence/` | BA / người thực hiện thay đổi | Git; cập nhật cùng PR liên quan |

Tên trong cột phụ trách là **vai trò dự kiến**, chưa thay thế danh sách thành viên thật của Nhóm 3.

## Cấu trúc lưu trữ

Giữ nguyên cấu trúc ASP.NET Core MVC hiện có: `Controllers/`, `Services/`, `Models/`, `Views/`, `Data/`, `wwwroot/`. Test tự động đặt tại `tests/EcommerceApp.Tests/`; dữ liệu và kết quả có thể tái sử dụng đặt tại `evidence/`. Hồ sơ quản lý cấu hình đặt tại `docs/lab3/`. Nếu có tài liệu Tuần 1–2, đặt yêu cầu vào `docs/requirements/` và thiết kế vào `docs/design/`; không tạo bản sao giả khi chưa có tài liệu gốc.

## Quy tắc file và phiên bản

- Giữ tên lớp C# và file trùng nhau, dùng PascalCase; tài liệu và dữ liệu mới dùng `kebab-case` hoặc `snake_case` nhất quán trong từng thư mục.
- Tên file nêu rõ nội dung, không dùng `final`, `final2` hoặc tên ngày tháng như cách đánh phiên bản duy nhất.
- Phát hành theo `vMAJOR.MINOR.PATCH`: MAJOR khi đổi không tương thích, MINOR khi thêm chức năng tương thích, PATCH khi sửa lỗi hoặc tài liệu. Tag chỉ được tạo sau khi phiên bản đã được kiểm thử và merge.
- Bản kế hoạch này là `v1.0.0`; sửa nhỏ kế hoạch tăng PATCH, thay đổi quy trình tăng MINOR.

## Branch, commit và review

1. Tạo branch từ `main` cho từng phần việc. Đặt tên theo mục đích thay đổi, ví dụ `feature/<muc-tieu>`, `fix/<loi>`, `docs/<noi-dung>`.
2. Mỗi commit chứa một thay đổi có mục đích rõ, theo mẫu `type: mô tả cụ thể` (`feat`, `fix`, `test`, `docs`, `chore`). Ví dụ: `test: cover registration validation boundaries`. Tránh `update`, `fix`, `final`, `abc` đứng một mình.
3. Mở PR với mục tiêu, CI bị ảnh hưởng, cách kiểm thử, ảnh minh chứng khi thay đổi giao diện và liên kết Change Request nếu có.
4. Người khác tác giả đọc diff, kiểm tra tác động và kết quả test trước khi approve. Sau review và checks đạt, người có quyền merge vào `main`. Chưa thiết lập hoặc xác nhận được branch protection tự động trên repository; đây là quy trình nhóm cần thực hiện.
5. Sau merge, cập nhật tài liệu liên quan và chỉ xóa branch khi đã giữ được liên kết PR/commit phục vụ truy vết.

## Kiểm soát thay đổi

Mỗi thay đổi đáng kể có mã `CR-NN`, mô tả, người đề xuất, mức ưu tiên, CI bị ảnh hưởng và đánh giá tiến độ/chi phí/chất lượng. Nhóm ghi rõ quyết định Approve/Reject/Defer trước khi triển khai. Với CR được duyệt, dùng branch riêng, test, PR, review và merge; ghi liên kết chứng cứ vào hồ sơ CR. [CR-01](change-request-01.md) là thay đổi Lab 3 đầu tiên.

## Kiểm tra trước khi nộp

Xác nhận danh sách thành viên và quyền truy cập GitHub; bổ sung tài liệu Tuần 1–2 nếu có; xác nhận mỗi thành viên có branch và commit riêng; kiểm tra PR đã có review thật và chỉ đánh dấu merge khi thực sự hoàn tất. Không dùng tài khoản hoặc commit giả để thay thế minh chứng đóng góp.
