# Báo cáo Buổi 7

**Nhóm 3 · Techvora · Hệ thống bán điện thoại online · Lớp 261122402437 · Ngày 28/10/2026.**

Sprint 1–2 đã tích hợp trên bản nền `73aa335`. Đợt kiểm tra Buổi 7 đạt **132/132 backend và 26/26 browser**, sửa lỗi chạy browser #38 và bổ sung CI cho luồng liên chức năng. Sprint 3 được chuẩn bị theo ba luồng hậu mãi, voucher và báo cáo; các chức năng này chưa được mở trong bản mặc định.

## Tài liệu và minh chứng

- [Phiếu Buổi 7 đã điền](Buoi_7_Techvora.docx).
- [Tổng kết Sprint 2](../lab4/sprint-2.md).
- [Test Report và bước chạy lại](../../test/TestReport.md), [kết quả từng test](../../test/evidence/buoi7-2026-10-28.json).
- [Sprint 3 Goal và Backlog](sprint-3-plan.md), [milestone Sprint 3](https://github.com/Anhtuan2005/cnpm/milestone/3).
- [PR #39](https://github.com/Anhtuan2005/cnpm/pull/39), [bug #38](https://github.com/Anhtuan2005/cnpm/issues/38), [commit test và bản sửa 04f4a18](https://github.com/Anhtuan2005/cnpm/commit/04f4a18).
- [CI thành công tại commit 435deda](https://github.com/Anhtuan2005/cnpm/actions/runs/40641956825): 132 backend và 26 Chromium pass trên checkout sạch Ubuntu 24.04.

## Sprint 2 Retrospective

| Nội dung | Kết quả và hành động |
| --- | --- |
| Điều làm tốt | Dùng chung giỏ, đơn, kho và tài khoản giữa hai sprint; đã có kiểm tra tranh chấp kho, callback lặp, voucher lượt cuối, hủy/hoàn kho và phân quyền. Luồng đánh giá công khai, shop phản hồi và xác nhận nhận hàng/hoàn tiền được bổ sung trong các commit 25fbbf9, 73aa335. |
| Vấn đề còn tồn tại | Báo cáo cũ ghi 110/115 test và chưa phản ánh 12 issue Sprint 2 đã đóng; US-24 trên GitHub còn tên duyệt đánh giá cũ. Test browser nằm ngoài package frontend nên resolve sai dependency. Chưa có kết quả giao dịch VNPAY/GHN sandbox trong buổi này. |
| Cải tiến cụ thể | Đồng bộ báo cáo và issue theo hành vi hiện tại; mỗi PR lưu kết quả test đúng commit. Sửa runner #38, chạy backend/asset/browser trong CI. Sprint 3 nhận từng luồng nối vào đơn/giỏ hiện có, có owner và AC; cần người khác review trước merge. |

Đây là tổng kết kỹ thuật và hành động đề xuất từ việc rà soát mã/test ngày 28/10; các nhận xét về quy trình và phân công chưa thay thế biên bản thảo luận/nghiệm thu của nhóm.

## Phiên bản tích hợp

`main` tại thời điểm bắt đầu: `73aa33508d224619f639bdb7df15d409f52210ac`. Nhánh công việc: `codex/sprint12-integration-sprint3`. Không ghép các PR tài liệu cũ chưa review vào bản demo. Mã chạy giữ `Release:ActiveSprint=2`; database test tách biệt dữ liệu làm việc.

| Luồng | Sprint | Kết quả | Minh chứng |
| --- | --- | --- | --- |
| Giỏ khách → ghép giỏ → voucher/COD → đơn/kho | 1 + 2 | Đạt ở SQL Server; tổng tiền 1.920.000 và kho 5→3 | IT-01, RG-07 |
| Đơn → giao → xác nhận → đánh giá và shop phản hồi | 1 + 2 | Đạt; dữ liệu lưu đúng, phản hồi gắn đúng review | IT-01, IT-03 |
| Đơn đã giao → yêu cầu bảo hành → thông báo → admin mở đúng đơn | 1 + 2 | Đạt ở backend và browser hai vai trò | IT-01, IT-02 |
| VNPAY/callback, hủy đơn, voucher cạnh tranh | 1 + 2 | Kiểm thử tự động đạt; dịch vụ ngoài dùng test double | IT-04, RG-01, RG-02 |

## Bug Tracking

| Bug | Mức độ | Người xử lý | Trạng thái |
| --- | --- | --- | --- |
| [#38 Browser test resolve sai Playwright](https://github.com/Anhtuan2005/cnpm/issues/38) | Medium | Nguyễn Trương Anh Tuấn (@Anhtuan2005) | Đã sửa ở 04f4a18; retest local 26/26; chờ review/merge PR #39 |

Issue có bước tái hiện, Expected/Actual, nguyên nhân, người xử lý và liên kết PR. Không tạo thêm lỗi nghiệp vụ khi chưa có bằng chứng tái hiện. Việc pin Ubuntu 24.04 là điều chỉnh môi trường CI mới bổ sung, có log thất bại và commit 435deda.

## Chuẩn bị Sprint 3

Mục tiêu: nối **hậu mãi → phản hồi khách**, **voucher quản trị → checkout** và **đơn/hoàn tiền → báo cáo**, giữ các chức năng Sprint 1–2 pass trên CI. Bốn mục cốt lõi: US-27 (#37), US-18 (#30), US-26 (#32), US-29 (#33). Bốn mục dự phòng: US-20/28/30/25. [Kế hoạch](sprint-3-plan.md) ghi owner đề xuất, phụ thuộc, AC, trình tự tích hợp và Definition of Done.

GitHub đã có đủ tám issue Sprint 3; cập nhật issue và milestone thay vì tạo bản sao. Quyền hiện có không bao gồm `read:project`, nên lần này không sửa các trường riêng của Project board. Trạng thái issue, nội dung và assignee được cập nhật trực tiếp trên repository.
