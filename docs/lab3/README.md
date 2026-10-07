# Lab 3 – Quản lý cấu hình Techvora

Đây là hồ sơ Lab 3 của dự án bán điện thoại Techvora, dựa trên repository đang sử dụng: https://github.com/Anhtuan2005/cnpm. Ngày thực hành trong phiếu gốc: 07/10/2026.

## Sản phẩm

- [Kế hoạch quản lý cấu hình](configuration-management-plan.md): danh sách CI, cấu trúc lưu trữ, phiên bản, nhánh, commit và quy trình review.
- [Yêu cầu thay đổi CR-01](change-request-01.md): bổ sung dữ liệu và kiểm thử tự động cho quy tắc đăng ký tài khoản.
- [Dữ liệu kiểm thử](../../evidence/lab3/registration_test_data.json): ba trường hợp hợp lệ và ba trường hợp không hợp lệ hoặc biên.
- [Bài kiểm thử](../../tests/EcommerceApp.Tests/RegistrationValidationDataTests.cs): đọc dữ liệu JSON và kiểm tra kết quả validation.
- [Phiếu thực hành đã điền](Phieu_thuc_hanh_Lab3_Techvora.docx): cập nhật theo mẫu Word của Lab 3 và ghi rõ các minh chứng còn thiếu.

## Minh chứng và việc cần xác nhận

Branch `codex/lab3-scm-registration-tests` chứa thay đổi của Lab 3; [PR #1](https://github.com/Anhtuan2005/cnpm/pull/1) đang mở để nhóm review. Review, kết quả merge và đóng góp của từng thành viên chỉ được ghi nhận sau khi các thao tác đó thực sự có trên GitHub. Chưa xác định được lớp và danh sách thành viên Nhóm 3 từ tài liệu được cung cấp.

Repository này chưa chứa tài liệu Tuần 1–2 ở dạng riêng để chuyển vào `docs/requirements` hoặc `docs/design`. Danh sách collaborator hiện chỉ có `Anhtuan2005` (admin) tại thời điểm kiểm tra. Nhóm cần bổ sung các tài liệu gốc nếu có và mời đúng các thành viên khác vào repository.
