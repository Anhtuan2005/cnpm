# LAB 4.1 – Sprint 1 nền tảng mua sắm và đặt hàng

**Sprint Goal từ phạm vi backlog:** khách tìm được điện thoại, quản lý tài khoản/giỏ hàng, đặt đơn COD và xem lại đơn; admin quản lý catalog. Kế hoạch trong backlog nhóm cung cấp: 16/09–27/09/2026. Ngày hoàn thành thực tế cần xác nhận lại trong file gốc.

**Đối chiếu nhanh:** [bảng đã làm và chưa làm của LAB 4.1](README.md#lab-41--sprint-1).

## Sprint Backlog

`AC` là tiêu chí chấp nhận tóm tắt từ luồng hiện có và kịch bản demo. `Có mã/test hiện tại` chỉ phản ánh repository ngày 14/10/2026; không xác nhận User Story đã Done tại ngày kết thúc Sprint.

| ID | Ưu tiên | User Story và Acceptance Criteria tóm tắt | SP | Phụ trách theo backlog | Trạng thái có thể chứng minh |
| --- | --- | --- | ---: | --- | --- |
| US-07 | Cao | Trang chủ hiển thị danh mục, banner, sản phẩm nổi bật hợp lệ trên desktop/mobile. | 2 | Nguyễn Trương Anh Tuấn | Có mã/test hiện tại |
| US-06 | Cao | Tìm kiếm, lọc, sắp xếp, phân trang; giữ điều kiện khi chuyển trang. | 4 | Nguyễn Trương Anh Tuấn | Có mã/test hiện tại |
| US-11 | Cao | Chi tiết có ảnh, thông số, giá, tồn kho; chặn thao tác mua khi hết hàng. | 3 | Nguyễn Trương Anh Tuấn | Có mã/test hiện tại |
| US-02 | Cao | Đăng ký, đăng nhập/đăng xuất, hồ sơ; validation và quyền truy cập đúng. | 4 | Trần Tấn Phát | Có mã/test hiện tại |
| US-09 | Trung bình | Khách thêm/bỏ yêu thích và chỉ thấy danh sách của mình. | 2 | Trần Tấn Phát | Có mã/test hiện tại |
| US-14 | Cao | Giỏ khách, giỏ tài khoản, mua ngay, ghép giỏ; không vượt tồn kho. | 5 | Trương Đức Phú | Có mã/test hiện tại |
| US-16 | Cao | Admin quản lý danh mục, sản phẩm, ảnh, thông số và banner; dữ liệu hợp lệ. | 5 | Bùi Phạm Phong Phú | Có mã/test hiện tại |
| US-12 | Cao | Checkout nhận địa chỉ đủ cấp, tính lại phí giao và tổng tiền tại server. | 5 | Trương Đức Phú | Có mã/test hiện tại |
| US-08 | Cao | Đặt COD kiểm tra tồn kho và tạo đơn/trừ kho trong giao dịch nguyên tử. | 3 | Bùi Phạm Phong Phú | Có mã/test hiện tại |
| US-03 | Cao | Khách xem xác nhận, lịch sử và chi tiết đơn của chính mình. | 4 | Trần Tấn Phát | Có mã/test hiện tại |
| **Tổng** | | | **37** | | |

**Nguồn phân công và điểm:** bảng Sprint Backlog nhóm gửi ngày 14/10/2026; [kịch bản Planning Poker](../daily-scrum-sprint-1.md) có cùng 10 User Story và tổng 37 điểm, nhưng văn bản đó ghi rõ phân công ban đầu là đề xuất.

## Mã nguồn, branch và PR

Các luồng Sprint 1 nằm tại `src/backend/Controllers/` và `src/backend/Services/`. [Commit 307db31](https://github.com/Anhtuan2005/cnpm/commit/307db31) là mốc đưa mã vào repository; [commit 4991e3b](https://github.com/Anhtuan2005/cnpm/commit/4991e3b) sắp xếp lại cấu trúc và bổ sung kiểm tra phạm vi Sprint. Có [PR #1](https://github.com/Anhtuan2005/cnpm/pull/1) cho dữ liệu/test đăng ký, đã merge ngày 07/10/2026; đây là việc bổ sung sau mốc Sprint 1, không phải bằng chứng PR của tất cả story Sprint 1. Chưa có GitHub Issue riêng, PR review của thành viên khác hoặc bằng chứng branch cho từng story.

## Kiểm thử và lỗi

| TC | Luồng | Dữ liệu / trường hợp | Kỳ vọng | Kết quả và bằng chứng hiện tại |
| --- | --- | --- | --- | --- |
| S1-01 | Đăng ký hợp lệ | Mẫu hợp lệ trong `registration_test_data.json` | Model hợp lệ | Pass trong bộ 110 test; `RegistrationValidationDataTests` |
| S1-02 | Đăng ký sai/biên | Email, số điện thoại hoặc mật khẩu sai | Báo đúng trường lỗi | Pass trong bộ 110 test; `RegistrationValidationDataTests` |
| S1-03 | Giỏ hàng/mua ngay | Mua ngay sản phẩm hợp lệ khi giỏ có sẵn | Đơn đúng sản phẩm, giỏ cũ giữ nguyên | Pass; `SafetyRegressionTests.Buy_now_creates_the_requested_order_without_changing_existing_cart_quantity` |
| S1-04 | Giỏ hàng sai số lượng | Số lượng 0, âm hoặc tràn | Từ chối, không đổi giỏ | Pass; `SafetyRegressionTests.Cart_rejects_overflow_and_nonpositive_additions_without_changing_existing_quantity` |
| S1-05 | Phí giao hàng | Địa chỉ thuộc các vùng giao | Phí đúng theo vùng | Pass; `PortfolioQualityTests.Shipping_fee_is_calculated_by_delivery_zone_for_phone_orders` |
| S1-06 | Checkout sai số lượng | Giỏ chứa số lượng không hợp lệ | Không tạo đơn/ghi dữ liệu | Pass; `SafetyRegressionTests.Checkout_rejects_invalid_quantity_from_cart_provider_before_writing_anything` |
| S1-07 | Đặt COD | Giỏ và tồn kho hợp lệ | Đơn, kho, giỏ commit cùng nhau | Pass; `OrderPaymentTests.Checkout_with_retry_enabled_commits_order_stock_and_cart_together` |
| S1-08 | Tranh món cuối | Hai khách mua cùng đơn vị cuối | Chỉ một đơn thành công | Pass; `OrderPaymentTests.Two_customers_competing_for_last_item_create_exactly_one_order` |
| S1-09 | Quản trị catalog | Hai thao tác tạo danh mục nhanh | Slug không trùng | Pass; `AdminAuditTests.Repeated_quick_category_creation_produces_unique_slugs` |
| S1-10 | Sửa catalog cũ | Form sản phẩm cũ sau khi đã bán/chỉnh sửa | Không ghi đè tồn kho/dữ liệu mới | Pass; `CatalogConsistencyTests.Stale_product_form_cannot_restore_sold_stock_or_overwrite_another_edit` |

Các ca trên là ví dụ kiểm thử tự động. Bộ test đầy đủ ngày 14/10/2026: **110/110 backend** và **22/22 smoke test trình duyệt** pass. [Kịch bản demo](../user-guide/demo.md) đi qua trang chủ, tìm kiếm, chi tiết, giỏ, COD, lịch sử và admin; lần demo thủ công của nhóm chưa được ghi nhận trong hồ sơ này.

**Bug Report:** không có lỗi đang mở được tái hiện trong lần chạy backend và browser trên. Không tạo `BUG-01`/`BUG-02` giả; nếu demo thủ công phát hiện lỗi, tạo GitHub Issue, ghi mức độ, người sửa, commit và kết quả retest rồi cập nhật bảng này.

## Sprint Review và Retrospective

- **Review có thể trình bày:** demo 10 luồng ở backlog theo [kịch bản](../user-guide/demo.md), chỉ rõ tiêu chí AC và kết quả test hiện tại. Không dùng kết quả 14/10 để khẳng định toàn bộ 10 story đã được nghiệm thu vào 27/09.
- **Điều làm tốt để nhóm xác nhận:** các luồng checkout, tồn kho và giới hạn quyền đã có test tự động.
- **Khó khăn để nhóm xác nhận:** lịch sử commit Sprint 1 gộp nhiều tính năng nên khó truy xuất đóng góp và ngày hoàn thành từng story; năm ở `Act Start Date` bị sai.
- **Cải tiến đề xuất cho Sprint 2:** một Issue cho mỗi story, branch/PR liên kết Issue, review của thành viên khác và lưu kết quả test trước merge.

## Nhật ký minh chứng

| Mốc có bằng chứng | Đã hoàn thành | Vướng mắc | Minh chứng |
| --- | --- | --- | --- |
| 27/09/2026 | Có commit tổng hợp mang tên `Sprint 1` | Không tách được thời điểm hoàn thành từng story | [307db31](https://github.com/Anhtuan2005/cnpm/commit/307db31) |
| 07/10/2026 | Bổ sung test dữ liệu đăng ký và sắp xếp repo | Sau thời hạn kế hoạch Sprint 1 | [ae26805](https://github.com/Anhtuan2005/cnpm/commit/ae26805), [4991e3b](https://github.com/Anhtuan2005/cnpm/commit/4991e3b) |
| 14/10/2026 | Chạy lại 110 test backend và 22 smoke test trình duyệt, 0 fail | Chưa có bản ghi buổi họp thực tế | [Bằng chứng test](../user-guide/backend-tests.md) và báo cáo này |

**Kết luận:** mã hiện tại cho phép demo Sprint 1; 110 test backend và 22 smoke test trình duyệt pass. Trạng thái nghiệm thu lịch sử, biên bản hai buổi, Issue/PR review theo từng story và số liệu `Act Start Date` còn cần nhóm xác nhận.
