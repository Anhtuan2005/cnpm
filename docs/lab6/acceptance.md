# Bảng nghiệm thu các luồng chính

Ngày kiểm tra 04/11/2026, bản ứng dụng có nền Sprint 3 ở `42fcead` và bản sửa đóng gói/dependency `b705b18`. Kiểm tra backend trên SQL Server thật; browser chạy trên DLL đã publish và giải nén. Kết quả dưới đây là **nghiệm thu kỹ thuật**, cần nhóm trực tiếp Review/demo để chốt nghiệm thu nội bộ.

| Mã | User Story và luồng | Acceptance Criteria được đối chiếu | Kết quả | Minh chứng |
| --- | --- | --- | --- | --- |
| AC-01 | US-14/12/08/01/30 · mua ngay và phụ kiện → voucher/COD → đơn/kho | Phụ kiện phải phù hợp và đang có ưu đãi; server tính giá; số lượng/kho nhất quán; giữ giỏ cũ; dữ liệu phụ kiện còn sau lỗi validation | Đạt kỹ thuật | `BuyNowAccessoryTests`, `buy-now.spec.mjs`, `Sprint_three_admin_voucher_cross_sell_checkout_and_support_form_one_persisted_flow`; [ảnh giỏ/ưu đãi](evidence/cross-sell.png) |
| AC-02 | US-23/18/19 · đơn đã giao → bảo hành → xử lý admin → khách theo dõi | Chặn người khác/vượt số lượng; thông báo mở đúng đơn; chuyển trạng thái hợp lệ, ghi chú và thông báo cho khách | Đạt kỹ thuật | `SafetyRegressionTests`, `SprintThreeIntegrationTests`, `support-request.spec.mjs` và `order-assurance.spec.mjs` |
| AC-03 | US-29/10 · đơn/hoàn tiền → báo cáo → CSV | Giữ bộ lọc, giới hạn kỳ; loại hủy/chờ hoàn/đã hoàn; giữ doanh thu sản phẩm xóa mềm; CSV chống công thức; khách không được mở trang admin | Đạt kỹ thuật | `Report_and_csv_reconcile_discount_shipping_refunds_and_date_boundaries`, `sprint-three.spec.mjs`; [ảnh báo cáo](evidence/report.png) |
| AC-04 | US-26/20 · voucher theo nhóm và quyền tài khoản | Mã tạo trong admin dùng đúng nhóm/điều kiện; khóa hoặc thu hồi quyền có hiệu lực ở request mới; bảo vệ actor/admin cuối cùng | Đạt kỹ thuật | `SprintThreeIntegrationTests`, `UserAccessTests`, `AdminAuditTests`; browser kiểm tra tạo/bật/tắt voucher |
| AC-05 | US-25/28/30/27 · tư vấn/gợi ý → sản phẩm/giỏ; nhắc giỏ; phạm vi phát hành | Gợi ý là hàng thật còn kho; không lấy lịch sử tài khoản khác; nhắc một lần mỗi phiên bản giỏ; health hoạt động; Sprint 2 tắt chức năng Sprint 3, Sprint 4 vẫn khóa | Đạt kỹ thuật trong chế độ local/stub | `Recommendations_include_old_catalog_fallback_without_using_another_accounts_session_history`, các test `Abandoned_cart_*`, `Chat_falls_back_*`, `SprintScopeTests`, `sprint-three.spec.mjs`; [ảnh tư vấn](evidence/chat.png) |

[Test Report](TestReport.md) và [kết quả từng test](../../test/evidence/lab6-2026-11-04.json) có lệnh, kết quả và CI. Ảnh do browser test sinh trên gói thử, không dùng làm bằng chứng đã gọi provider thật.

## Ngoài kết luận đạt ở trên

Chưa nghiệm thu VNPAY/GHN sandbox hoặc SMTP thật ở Lab 6; chưa chạy gói trên máy thành viên thứ hai/Docker; chưa có xác nhận PO/nhóm hoặc reviewer độc lập trong phiên làm việc này. Quota/độ ổn định Gemini không được suy ra từ một lần HTTP 200 trước đây. Chi tiết giới hạn và việc còn lại ở [báo cáo tổng kết](final-report.md).
