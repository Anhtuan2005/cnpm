# Sprint 3: quản trị, tăng trưởng và tư vấn

Phạm vi lấy từ `T4_B3_Nhom3.xlsx`, sheet `Sprint backlog`, A33:B40: toàn bộ tám story Sprint 3. Bản triển khai ngày 29/10/2026 kế thừa các controller/service đã có, mở chúng ở Sprint 3 và bổ sung các điểm nối cùng kiểm thử. Kế hoạch Buổi 7 là tài liệu lịch sử, từng đề xuất chỉ chọn bốn story cốt lõi.

## Chức năng và cách mở

| Story | Luồng sử dụng |
| --- | --- |
| US-18 | Admin → Đổi trả/Bảo hành: lọc yêu cầu, xem ảnh, chuyển trạng thái hợp lệ, ghi chú và gửi thông báo cho khách. Từ thông báo đơn hàng có thể mở thẳng nút xử lý yêu cầu. Yêu cầu đã đóng giữ nguyên dữ liệu; xử lý bảo hành không tự đánh dấu hoàn tiền. |
| US-20 | Khách thấy số lượt còn lại sau mỗi lần sai. Lần sai thứ 5 tự khoá tạm 30 phút. Bảng Người dùng cập nhật thời điểm hết khoá. Admin chọn Xem xét, ghi lý do và chọn thời hạn khoá thủ công từ 24 giờ đến 90 ngày hoặc không thời hạn. Cấp Admin bắt đầu từ danh sách Người dùng. Cấp/gỡ quyền cần mật khẩu của admin đang thao tác và lý do. Thay đổi thu hồi phiên cũ của tài khoản được xử lý. |
| US-26 | Admin → Voucher: tạo/sửa/bật/tắt/xóa mã chưa dùng, chọn nhóm áp dụng. Nhóm khách gồm khách mới, VIP (từ 3 đơn hợp lệ hoặc 20 triệu đồng), lâu không mua (60 ngày). Giá khuyến mãi nằm ở sửa sản phẩm; checkout tính lại giá và điều kiện voucher tại server. |
| US-29 | Admin → Báo cáo: lọc 7/30 ngày, 3 tháng/1 năm hoặc tùy chọn; xem doanh thu theo ngày, danh mục, top sản phẩm và tải CSV cùng bộ lọc. |
| US-28 | Trang chủ và chi tiết sản phẩm: gợi ý theo tương tác, giỏ, yêu thích và lịch sử mua; chỉ điện thoại còn hàng. Người mới có danh mục dự phòng. Không dùng lịch sử cá nhân của tài khoản khác trên cùng session. |
| US-30 | Thêm vào giỏ hoặc Mua ngay đều gợi ý phụ kiện phù hợp có ưu đãi. Mua ngay cho chọn/bỏ qua phụ kiện trước checkout, giữ nguyên giỏ cũ; server tính lại giá khi đặt đơn. Giỏ thành viên bỏ quên từ 24 giờ nhận một thông báo và mã cá nhân giảm 30.000 đồng cho đơn từ 300.000 đồng, hiệu lực 48 giờ. Có nhắc khi đăng nhập và email nếu SMTP được bật. |
| US-25 | Nút tư vấn: chat tiếng Việt, gợi ý hãng/ngân sách, bấm vào sản phẩm thật. Không có khóa hoặc provider lỗi thì dùng tìm kiếm trong danh mục, không hiển thị lỗi cấu hình cho khách. |
| US-27 | Phân quyền Admin/User, CSRF, giới hạn tốc độ và kích thước tin nhắn; health/live, health/ready; log request không chứa query string; kiểm thử SQL Server và trình duyệt cho Sprint 2 và 3. |

## Chạy và cấu hình

Làm theo phần chạy local trong [README](../../README.md). `appsettings.json` đã đặt `Release:ActiveSprint=3`; nếu máy có biến môi trường hoặc User Secrets ghi đè thành 2, đặt lại thành 3 rồi khởi động lại ứng dụng.

```powershell
$env:Release__ActiveSprint = '3'
dotnet run --project src/backend/EcommerceApp.csproj --launch-profile http
```

Chạy `dotnet tool run dotnet-ef database update --project src/backend/EcommerceApp.csproj` để cập nhật database, bao gồm migration `AddAccountAccessReview` cho lịch sử và phân biệt khoá thủ công. Bật `Demo:Enabled=true` trên database riêng để có tài khoản demo, đơn mẫu, giá khuyến mãi và cặp điện thoại mua kèm.

AI tùy chọn: cấu hình `Gemini:ApiKey` bằng User Secrets hoặc biến môi trường, giữ model được tài khoản Gemini hỗ trợ trong `Gemini:Model`. Khóa không gửi ra trình duyệt. Luồng tư vấn danh mục hoạt động mà không cần dịch vụ ngoài. Ngày 29/10/2026 đã lưu key local bằng User Secrets và xác nhận gọi thật `gemini-3.5-flash`: cả API Google và `/AiChat/Ask` trả HTTP 200, câu trả lời tư vấn kèm liên kết sản phẩm thật, không dùng chế độ danh mục dự phòng. Key nằm ngoài repository; các file `.env.*`, `secrets.json` và cấu hình local được bỏ qua bởi Git.

Email mặc định tắt. Để gửi thật, cấu hình `Email:Enabled`, `Email:Host`, tài khoản SMTP và `Email:FrontendUrl` là URL ứng dụng. Job quét lần đầu sau một phút rồi mỗi giờ; mỗi lượt tối đa 50 giỏ. Email lỗi được ghi log, thông báo trong ứng dụng và voucher vẫn giữ lại; hiện không tự gửi lại email lỗi. Không ghi `EmailSentAt` khi SMTP đang tắt. Khi đổi nội dung giỏ, mốc nhắc thay đổi; nhắc cũ không xuất hiện nếu đã có đơn sau mốc đó hoặc mã đã hết lượt.

Quay về Sprint 2: đặt `Release:ActiveSprint=2`, khởi động lại. Route Sprint 3 trả 404, menu tương ứng biến mất và job nhắc giỏ không được đăng ký; dữ liệu đã tạo vẫn được giữ. Giới hạn bản phát hành tối đa là 3 nên cấu hình lớn hơn cũng không mở Build PC của Sprint 4.

## Quy ước báo cáo

- Dùng ngày đặt hàng theo UTC, bao gồm hết ngày kết thúc; tối đa 366 ngày.
- Ghi nhận đơn đã trả tiền hoặc đã giao, loại đơn hủy và trạng thái chờ hoàn/đã hoàn tiền theo quy tắc chung với dashboard.
- Tiền hàng − voucher + phí giao hàng = doanh thu. Danh mục và top sản phẩm thể hiện tiền hàng trước voucher, chưa gồm vận chuyển.
- Không suy ra tăng trưởng 100% khi kỳ trước bằng 0; hiển thị chưa có kỳ gốc.
- Giữ doanh thu của sản phẩm đã xóa mềm; không gộp hai ID sản phẩm chỉ vì trùng tên. CSV dùng UTF-8 BOM, chặn nội dung có thể bị Excel diễn giải thành công thức.

## Demo

1. Đăng nhập admin, tạo voucher cho khách mới. Mở Nhóm khách để xem điều kiện và danh sách thành viên.
2. Dùng tài khoản khách mới, thêm điện thoại vào giỏ. Thử iPhone 15 Pro để xem gợi ý ốp lưng đúng mẫu, củ sạc và tai nghe có ưu đãi. Áp voucher và đặt COD. [Hướng dẫn US-30](us-30.md) có đầy đủ bước thử mua phụ kiện kèm điện thoại và nhắc giỏ không cần chờ 24 giờ.
3. Admin xác nhận, giao hàng; khách mở đơn đã giao và gửi yêu cầu bảo hành.
4. Admin mở thông báo → đơn hàng → xử lý yêu cầu. Chuyển Đang xử lý → Đã duyệt → Hoàn tất, nhập ghi chú. Khách xem trạng thái và thông báo tương ứng.
5. Mở Báo cáo, chọn kỳ và xuất CSV. So sánh tiền hàng, voucher, vận chuyển và tổng đơn; kiểm tra đơn hủy/hoàn bị loại.
6. Thử chatbot “Samsung dưới 10 triệu” và mở sản phẩm được gợi ý. Sau năm lần đăng nhập sai trên tài khoản thử, mở **Người dùng** để kiểm tra thời điểm hết khoá và chọn **Xem xét** để đọc lịch sử. Với tài khoản User đang hoạt động, thử **Cấp Admin** với mật khẩu xác nhận sai, sau đó dùng mật khẩu của admin đang thao tác để xác nhận, rồi gỡ quyền sau khi thử.
7. Kiểm thử nhắc giỏ bằng dữ liệu fixture trong test, không cần chờ 24 giờ hay gửi email thật.

## Kiểm thử

Kết quả local ngày 29/10/2026: **143/143 backend**, **28/28 browser Sprint 2**, **28/28 browser Sprint 3**, build và asset pass. Sau chỉnh giao diện mobile, kiểm thử riêng báo cáo/CSV/voucher pass thêm một lần. Xem [bằng chứng từng test](../../test/evidence/sprint3-2026-10-29.json).

```powershell
dotnet test test/test-cases/EcommerceApp.Tests/EcommerceApp.Tests.csproj
npm run check:assets --prefix src/frontend
# Chạy trên server demo riêng đã được khởi động
$env:TECHVORA_SMOKE_URL = 'http://localhost:5009'
npm run test:smoke --prefix src/frontend
```

Mã test mới: [SprintThreeIntegrationTests.cs](../../test/test-cases/EcommerceApp.Tests/SprintThreeIntegrationTests.cs), [sprint-three.spec.mjs](../../test/test-cases/browser/sprint-three.spec.mjs). Test backend tạo/xóa database riêng; test browser cần database demo riêng và server mới để tránh trạng thái đơn hoặc rate limit của lượt chạy trước.

CI đã cấu hình matrix Sprint 2/3, lưu TRX và browser JSON/trace riêng từng sprint. Kết quả local không thay thế một lần chạy GitHub Actions sau khi push. Bộ kiểm thử tự động dùng sender giả, provider stub và chế độ tư vấn danh mục; kết nối Gemini thật được xác nhận riêng bằng hai request nhỏ sau khi thêm key local. SMTP thật chưa được xác nhận. VNPAY/GHN giữ nguyên yêu cầu sandbox riêng của Sprint 2.
