# Bảng trạng thái User Story

Bảng này trình bày theo kiểu GitHub Projects để thầy xem nhanh việc đã có mã, việc còn kiểm tra và việc chưa phát hành. Trạng thái được đối chiếu với repository và kết quả kiểm thử ngày **14/10/2026**. **“Có mã/test” không có nghĩa là User Story đã được nhóm nghiệm thu hoặc hoàn thành đúng hạn Sprint.** Các điểm và người phụ trách Sprint 1 lấy từ backlog nhóm cung cấp; Sprint 2–3 chưa có các giá trị này.

| Trạng thái | Ý nghĩa |
| --- | --- |
| ✅ Có mã/test | Chức năng ở bản mặc định và có minh chứng kiểm thử hiện tại. |
| 🟡 Có mã; cần xác nhận | Có mã trong phạm vi Sprint 2, nhưng còn bước nghiệm thu, phân công hoặc demo. |
| 🟠 Cần sandbox | Có mã/test nội bộ, chưa thử giao dịch với dịch vụ ngoài. |
| ⬜ Chưa phát hành | Chức năng thuộc Sprint sau, chưa mở trong bản mặc định. |

## Sprint 1 — nền tảng mua sắm và đặt hàng

| ID | User Story | Status | SP | Phụ trách theo backlog | Còn cần xác nhận |
| --- | --- | --- | ---: | --- | --- |
| US-07 | Trang chủ, danh mục, sản phẩm nổi bật | ✅ Có mã/test | 2 | Nguyễn Trương Anh Tuấn | Nghiệm thu tại 27/09. |
| US-06 | Tìm kiếm, lọc, sắp xếp, phân trang | ✅ Có mã/test | 4 | Nguyễn Trương Anh Tuấn | Nghiệm thu tại 27/09. |
| US-11 | Chi tiết, hình ảnh, thông số, giá, tồn kho | ✅ Có mã/test | 3 | Nguyễn Trương Anh Tuấn | Nghiệm thu tại 27/09. |
| US-02 | Đăng ký, đăng nhập, hồ sơ | ✅ Có mã/test | 4 | Trần Tấn Phát | Nghiệm thu tại 27/09. |
| US-09 | Danh sách sản phẩm yêu thích | ✅ Có mã/test | 2 | Trần Tấn Phát | Nghiệm thu tại 27/09. |
| US-14 | Giỏ hàng, mua ngay, ghép giỏ | ✅ Có mã/test | 5 | Trương Đức Phú | Nghiệm thu tại 27/09. |
| US-16 | Quản lý catalog và banner | ✅ Có mã/test | 5 | Bùi Phạm Phong Phú | Nghiệm thu tại 27/09. |
| US-12 | Checkout, địa chỉ và phí vận chuyển | ✅ Có mã/test | 5 | Trương Đức Phú | Nghiệm thu tại 27/09. |
| US-08 | Đặt hàng COD | ✅ Có mã/test | 3 | Bùi Phạm Phong Phú | Nghiệm thu tại 27/09. |
| US-03 | Xác nhận, lịch sử, chi tiết đơn | ✅ Có mã/test | 4 | Trần Tấn Phát | Nghiệm thu tại 27/09. |

**Minh chứng chung:** [Backlog và test Sprint 1](sprint-1.md), [báo cáo chạy test](test-run-2026-10-14.md). Tổng Sprint 1 theo bảng nhóm gửi: **37 SP**. Chín ngày bắt đầu thực tế ghi năm sai trong sheet gốc cần nhóm sửa.

## Sprint 2 — thanh toán, vận hành đơn và hậu mãi

| ID | User Story | Status | Issue | Còn cần làm |
| --- | --- | --- | --- | --- |
| US-04 | So sánh giá và thông số | 🟡 Có mã; cần xác nhận | [#7](https://github.com/Anhtuan2005/cnpm/issues/7) | Chốt AC, SP, owner, demo. |
| US-15 | Thanh toán trực tuyến VNPAY | 🟠 Cần sandbox | [#8](https://github.com/Anhtuan2005/cnpm/issues/8) | Chốt AC, SP, owner; thử giao dịch sandbox. |
| US-13 | Hủy đơn và mua lại | 🟡 Có mã; cần xác nhận | [#9](https://github.com/Anhtuan2005/cnpm/issues/9) | Chốt AC, SP, owner, demo. |
| US-05 | Tồn kho và lịch sử nhập xuất | 🟡 Có mã; cần xác nhận | Chưa có | Chốt AC, SP, owner; tạo Issue nếu nhận làm. |
| US-10 | Quản lý đơn, hoàn tiền, hóa đơn, CSV | 🟡 Có mã; cần xác nhận | Chưa có | Chốt AC, SP, owner; tạo Issue nếu nhận làm. |
| US-19 | Theo dõi giao hàng và thông báo | 🟡 Có mã; cần xác nhận | Chưa có | Chốt AC, SP, owner; tạo Issue nếu nhận làm. |
| US-21 | Đánh giá sản phẩm kèm hình ảnh | 🟡 Có mã; cần xác nhận | Chưa có | Chốt AC, SP, owner; tạo Issue nếu nhận làm. |
| US-23 | Yêu cầu đổi trả hoặc bảo hành | 🟡 Có mã; cần xác nhận | Chưa có | Chốt AC, SP, owner; tạo Issue nếu nhận làm. |
| US-22 | Dashboard và thông báo đơn mới | 🟡 Có mã; cần xác nhận | Chưa có | Chốt AC, SP, owner; tạo Issue nếu nhận làm. |
| US-17 | Vận đơn GHN | 🟠 Cần sandbox | Chưa có | Chốt AC, SP, owner; thử GHN sandbox. |
| US-24 | Duyệt, từ chối, xóa đánh giá | 🟡 Có mã; cần xác nhận | Chưa có | Chốt AC, SP, owner; tạo Issue nếu nhận làm. |
| US-01 | Kiểm tra và áp dụng voucher | 🟡 Có mã; cần xác nhận | [#10](https://github.com/Anhtuan2005/cnpm/issues/10) | Chốt AC, SP, owner, demo. |

**Minh chứng chung:** [Backlog và test Sprint 2](sprint-2.md), [báo cáo chạy test](test-run-2026-10-14.md). Chưa gọi 12 story là `Done` vì Sprint 2 chưa được nhóm chốt và nghiệm thu.

## Sprint 3 — dự kiến làm tiếp

| ID | User Story | Status | Bước tiếp theo |
| --- | --- | --- | --- |
| US-18 | Xử lý yêu cầu đổi trả và bảo hành | ⬜ Chưa phát hành | Chốt phạm vi, AC, điểm và người phụ trách. |
| US-20 | Quản lý người dùng, vai trò, khóa tài khoản | ⬜ Chưa phát hành | Chốt phạm vi, AC, điểm và người phụ trách. |
| US-26 | Quản lý voucher, khuyến mãi, phân khúc | ⬜ Chưa phát hành | Chốt phạm vi, AC, điểm và người phụ trách. |
| US-29 | Báo cáo doanh thu, sản phẩm, danh mục | ⬜ Chưa phát hành | Chốt phạm vi, AC, điểm và người phụ trách. |
| US-28 | Gợi ý sản phẩm cá nhân hóa | ⬜ Chưa phát hành | Chốt phạm vi, AC, điểm và người phụ trách. |
| US-30 | Sản phẩm mua kèm, nhắc giỏ bỏ quên | ⬜ Chưa phát hành | Chốt phạm vi, AC, điểm và người phụ trách. |
| US-25 | Chatbot tư vấn sản phẩm | ⬜ Chưa phát hành | Chốt phạm vi, AC, điểm và người phụ trách. |
| US-27 | Bảo mật, log, health check, kiểm thử tự động | ⬜ Chưa phát hành | Xác định tiêu chí bổ sung cho Sprint 3; một số biện pháp kỹ thuật đã có trong mã hiện tại. |

**Lưu ý:** “Chưa phát hành” nói về bản chạy mặc định, không khẳng định repository hoàn toàn chưa có mã cho chức năng đó. Các route Sprint 3 hiện bị khóa theo cấu hình phát hành.

## Công việc quản lý cần hoàn tất trước báo cáo

| Việc | Status | Minh chứng hoặc bước tiếp theo |
| --- | --- | --- |
| Phiếu LAB 4.1 và 4.2, báo cáo Sprint | ✅ Đã chuẩn bị | [Hồ sơ Lab 4](README.md) và hai file Word. |
| Test backend và browser | ✅ Đã chạy | [110/110 backend, 22/22 browser](test-run-2026-10-14.md) ngày 14/10. |
| Sửa ngày thực tế sai trong backlog Sprint 1 | ⬜ Chưa xác nhận | Đối chiếu sheet gốc và biên bản nhóm. |
| Chốt SP, owner, ngày, AC, trạng thái Sprint 2 | ⬜ Chưa chốt | Nhóm phân công và cập nhật backlog. |
| Review PR bởi thành viên khác | ⬜ Chưa có minh chứng | [PR #6](https://github.com/Anhtuan2005/cnpm/pull/6) đã merge nhưng API không ghi nhận review. |
| Demo thủ công và VNPAY/GHN sandbox | ⬜ Chưa có minh chứng | Nhóm chạy và lưu ảnh/video, URL hoặc biên bản. |
| Sprint Review và Retrospective | ⬜ Chưa có biên bản | Ghi ngày, người tham gia, kết quả và hành động cải tiến. |
