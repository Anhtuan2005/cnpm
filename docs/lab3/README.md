# Lab 3 – Quản lý cấu hình Techvora

Đây là hồ sơ Lab 3 của dự án bán điện thoại Techvora, dựa trên repository đang sử dụng: https://github.com/Anhtuan2005/cnpm. Ngày thực hành trong phiếu gốc: 07/10/2026.

## Sản phẩm

- [Kế hoạch quản lý cấu hình](configuration-management-plan.md): danh sách CI, cấu trúc lưu trữ, phiên bản, nhánh, commit và quy trình review.
- [Yêu cầu thay đổi CR-01](change-request-01.md): bổ sung dữ liệu và kiểm thử tự động cho quy tắc đăng ký tài khoản.
- [Dữ liệu kiểm thử](../../test/test-data/registration_test_data.json): ba trường hợp hợp lệ và ba trường hợp không hợp lệ hoặc biên.
- [Bài kiểm thử](../../test/test-cases/EcommerceApp.Tests/RegistrationValidationDataTests.cs): đọc dữ liệu JSON và kiểm tra kết quả validation.
- [Phiếu thực hành đã điền](Phieu_thuc_hanh_Lab3_Techvora.docx): cập nhật theo mẫu Word của Lab 3 và ghi rõ các minh chứng còn thiếu.

## Minh chứng và việc cần xác nhận

Branch `codex/lab3-scm-registration-tests` chứa thay đổi của Lab 3; [PR #1](https://github.com/Anhtuan2005/cnpm/pull/1) đã merge vào `main` ngày 07/10/2026. Danh sách review của PR hiện trống. Chưa xác định được lớp của Nhóm 3 từ tài liệu được cung cấp.

Theo sheet `Team` trong `T4_B3_Nhom3.xlsx`, Nhóm 3 gồm Nguyễn Trương Anh Tuấn (Dev / Product Owner), Trần Tấn Phát (Dev / Scrum Master), Trương Đức Phú (Dev / Scrum Master) và Bùi Phạm Phong Phú (Dev / Leader). File này ghi tài khoản `Dev1`–`Dev4`, chưa ghi tài khoản GitHub hoặc minh chứng branch/commit của từng người.

Minh chứng bổ sung trên GitHub: Trần Tấn Phát có [commit `49efeda`](https://github.com/Anhtuan2005/cnpm/commit/49efeda158be79a2ee0f19f685d2c92c63dc9ccb) cập nhật `docs/user-guide/demo.md` trực tiếp trên `main`. Bùi Phạm Phong Phú có [commit `77e3741`](https://github.com/Anhtuan2005/cnpm/commit/77e3741db4b2b81f33c90313500f10936a044729) trên branch `test/update-readme`; chưa có PR cho branch này. Trương Đức Phú được phân công US-14 và US-12 trong Sprint backlog, nhưng chưa thấy commit trên các branch của repository.

Chưa xác minh được các tài liệu gốc Tuần 1–2 trong `docs/requirements` và `docs/design`. Nhóm cần bổ sung tài liệu gốc nếu có và xác nhận quyền truy cập của cả bốn thành viên.
