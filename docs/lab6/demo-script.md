# Kịch bản demo và nghiệm thu kỹ thuật

Thời lượng đề xuất 12–15 phút, dùng Release v1.0.0 và database demo riêng. Chuẩn bị sẵn hai cửa sổ trình duyệt theo vai trò khách/admin, kiểm tra health/ready trước khi trình bày. Người trình bày dưới đây là đề xuất theo phân công backlog, cần nhóm xác nhận tại buổi bảo vệ.

| Thứ tự | Người đề xuất | Luồng và thao tác | Tiêu chí quan sát |
| --- | --- | --- | --- |
| 1 · 3 phút | Nguyễn Trương Anh Tuấn | Khách mở catalog/Flash Sale, chọn iPhone 15 Pro → Mua ngay → chọn ốp phù hợp hoặc bỏ qua → nhập địa chỉ, voucher → đặt COD → xem xác nhận/lịch sử | Ưu đãi đúng phụ kiện, server tính giá; đơn tạo thành công; giỏ cũ giữ nguyên; không mua vượt kho |
| 2 · 3 phút | Trương Đức Phú | Khách mở đơn đã giao mẫu → gửi bảo hành → admin mở thông báo, vào đơn và nút xử lý → chuyển trạng thái hợp lệ, ghi chú → khách xem lại yêu cầu/thông báo | Yêu cầu gắn đúng đơn, đúng chủ; trạng thái và ghi chú được lưu; không yêu cầu vượt số lượng mua |
| 3 · 2 phút | Trần Tấn Phát | Admin mở Báo cáo → chọn kỳ 7/30 ngày → xuất CSV; đối chiếu với đơn trả tiền, hủy và hoàn theo dữ liệu thử | Bộ lọc giữ nguyên, CSV tải được, quy tắc doanh thu loại đúng đơn hủy/chờ hoàn/đã hoàn |
| 4 · 2 phút | Nguyễn Trương Anh Tuấn và Trần Tấn Phát | Admin tạo voucher có giới hạn/nhóm khách; khách đủ điều kiện áp ở checkout; thử khóa tài khoản khách khác đang có phiên đăng nhập | Từ chối mã sai điều kiện; khóa có hiệu lực ở request tiếp theo; admin không tự khóa hoặc loại admin cuối cùng |
| 5 · 2 phút | Bùi Phạm Phong Phú | Khách hỏi chatbot “Samsung dưới 10 triệu” → mở sản phẩm thật; mở minh chứng nhắc giỏ; xem CI và manifest gói | Chatbot danh mục local hoạt động khi không có provider; một nhắc/phiên bản giỏ; đúng commit được kiểm thử và đóng gói |

Không chờ job nhắc giỏ 24 giờ trên sân khấu. Dùng kết quả `Abandoned_cart_scan_creates_one_private_offer_and_notification_per_cart_version_without_fake_email_delivery` để giải thích điều kiện, voucher cá nhân và chống nhắc trùng. Nếu muốn thao tác thực, chuẩn bị fixture trên database demo riêng theo [US-30](../user-guide/us-30.md).

## Khi không có Internet

- Chuẩn bị ZIP, Runtime/SQL đã cài, database đã seed, ảnh minh chứng và bộ test trước buổi báo cáo. Không phụ thuộc tải NuGet/npm khi đang trình bày.
- Dùng COD và đơn đã giao mẫu cho hậu mãi; không gọi VNPAY/GHN. Chatbot dùng catalog local, email tắt. Ảnh upload mới và asset CSS/JS phục vụ từ máy local.
- Danh sách huyện/xã dùng API ngoài, một số ảnh/font có URL ngoài. Nếu API địa chỉ không tải được, trình bày mua ngay đến bước kiểm tra giá, rồi mở đơn mẫu và minh chứng test đặt COD với API địa chỉ giả lập. Không gọi đó là đặt đơn offline hoàn chỉnh.
- Nếu SQL/app không chạy, mở ảnh minh chứng cùng báo cáo test và nêu rõ lỗi môi trường; không đánh dấu thao tác live đã thành công.

## Câu hỏi dự kiến

**Vì sao cần Integration và Regression?** Integration xác nhận voucher, giỏ, đơn, kho và hậu mãi làm việc trên cùng dữ liệu. Regression xác nhận thay đổi Sprint 3 vẫn giữ hành vi checkout/giỏ/hủy của Sprint 1–2; CI chạy cả hai cấu hình.

**Số liệu doanh thu là gì?** Là tổng đơn theo quy tắc nghiệp vụ, gồm tiền hàng sau voucher và phí giao; không phải sổ tiền ngân hàng theo ngày đối soát. COD được ghi nhận thanh toán và việc shop nhận tiền đối soát là hai mốc riêng.

**Đã thử tích hợp ngoài chưa?** Có minh chứng Gemini ngày 29/10 cho một lần gọi thật; Lab 6 kiểm tra chatbot local/provider stub. VNPAY/GHN và SMTP thật chưa có kết quả nghiệm thu mới; Release Notes ghi giới hạn này.

**Đóng góp cá nhân chứng minh thế nào?** Mở issue/PR/commit được liệt kê trong [bảng đóng góp](contributions.md), phân biệt tác giả Git, assignee và vai trò trình bày. Không suy ra tỷ lệ đóng góp từ một commit tổng hợp.
