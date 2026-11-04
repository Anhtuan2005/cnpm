# Tổng kết dự án Techvora qua ba Sprint

Nhóm 3 xây dựng cửa hàng điện thoại và phụ kiện trên ASP.NET Core 8 MVC, EF Core, SQL Server, Razor và JavaScript. Bản 1.0.0 dùng cho nghiệm thu kỹ thuật, với chuỗi chọn hàng, checkout, thanh toán/COD, vận hành đơn, hậu mãi và các chức năng quản trị/tăng trưởng Sprint 3.

## Sprint Goal và phạm vi

| Sprint | Mục tiêu | Phạm vi backlog | Kết quả và cách đọc |
| --- | --- | --- | --- |
| 1 | Tìm điện thoại, tài khoản, giỏ, COD và xem đơn; admin quản lý catalog | 10 story, 37 SP theo backlog được cung cấp | 10 issue đã đóng trên GitHub; chức năng nền được kiểm tra lại trong các suite hiện tại. Ngày nghiệm thu từng story cần theo hồ sơ nhóm |
| 2 | Mở so sánh, voucher/VNPAY, kho/đơn, giao hàng, đánh giá và hậu mãi | 12 story; SP và ngày thực tế chưa chốt trong hồ sơ gốc | 12 issue đã đóng; thay đổi cuối sprint có phản hồi review công khai, xác nhận nhận hàng/hoàn tiền và lịch sử đối soát |
| 3 | Quản trị hậu mãi, voucher/nhóm khách, báo cáo, người dùng; gợi ý, mua kèm/nhắc giỏ, chatbot và nền kiểm thử | 8 story trong backlog; Buổi 7 từng đề xuất ưu tiên 4, bản triển khai local sau đó mở cả 8 | 8 story có mã và kiểm tra kỹ thuật ở bản phát hành. Trạng thái issue và chấp thuận PO/nhóm được theo dõi riêng; không quy đổi số test thành số story đã nghiệm thu |

Nguồn: [Sprint 1](../lab4/sprint-1.md), [Sprint 2](../lab4/sprint-2.md), [kế hoạch Buổi 7](../buoi7/sprint-3-plan.md), [hướng dẫn Sprint 3](../user-guide/sprint-3.md), milestone/issue trên GitHub. Kế hoạch và kết quả lịch sử được giữ theo thời điểm, không sửa số liệu cũ thành ngày hoàn thành giả.

## Hoàn thiện ở Lab 6

Mã Sprint 3 đã có trên máy được lưu thành commit `42fcead` để repository chứa đủ phần triển khai và test. Đợt chốt phát hành phát hiện hai lỗi: audit package báo advisory và publish lẫn 10 ảnh upload local. Các lỗi được ghi tại #40/#41, sửa trong `b705b18`, kèm kiểm thử ảnh và kiểm tra gói sau giải nén.

Thư viện ảnh được chuyển sang SkiaSharp 3.119.4 có native assets Linux; WebP giữ xoay EXIF và giới hạn resize, loại metadata gốc, chặn ảnh hỏng và ảnh vượt 40 triệu pixel. Pkcs được pin 8.0.2. Không thêm migration nghiệp vụ mới trong Lab 6. Gói không mang theo database, upload, log, khóa hoặc settings riêng; database demo được tạo/migrate khi người nhận chủ động chạy script.

Lần browser Sprint 2 local còn phát hiện timeout do ảnh CDN gần 50 giây (#43). Commit `de77cb7` tách tài nguyên ảnh/font/icon ngoài mạng khỏi kiểm thử nghiệp vụ, có cờ opt-in để kiểm tra live asset. Lần chạy lỗi được giữ, kiểm thử lại so sánh đạt và các bộ liên quan được chạy lại; không đổi kết quả lỗi ban đầu thành Pass.

## Kiến trúc và tích hợp

[Kiến trúc cập nhật](../design/architecture.md) mô tả MVC → service → EF Core → SQL Server; các luồng dùng chung catalog, giỏ, đơn và kho. Sprint 3 nối voucher quản trị vào checkout, nối yêu cầu hỗ trợ vào xử lý admin và thông báo khách, nối dữ liệu đơn/hoàn tiền vào báo cáo. Job nhắc giỏ, gợi ý và chatbot có kiểm soát phạm vi Sprint; Build PC vẫn nằm ngoài bản phát hành.

Transaction và khóa database bảo vệ tồn kho, lượt voucher và vòng đời đơn trước thao tác lặp/cạnh tranh. Xác thực cookie được kiểm lại khi vai trò hoặc khóa tài khoản thay đổi. Chi tiết nguồn kiểm thử, kết quả retest và giới hạn nằm trong [Test Report Lab 6](TestReport.md).

## Nghiệm thu và giới hạn

[Bảng nghiệm thu](acceptance.md) chọn năm luồng đại diện, đối chiếu User Story và AC với test/ảnh/đường dẫn. Đây là nghiệm thu kỹ thuật có bằng chứng thực thi; phần nhóm trực tiếp demo và ký xác nhận cần thực hiện tại buổi Review. Không ghi đã tổ chức cuộc họp hoặc có reviewer khác khi chưa có minh chứng.

Gemini có một lần kiểm chứng thật trong hồ sơ ngày 29/10; chưa kiểm tra lại tải/quota trong Lab 6. VNPAY/GHN và SMTP cần cấu hình thử riêng; test mô phỏng không thay thế đối chiếu giao dịch/vận đơn thật. API địa chỉ và ảnh/font ngoài mạng ảnh hưởng demo offline; [kịch bản dự phòng](demo-script.md) sử dụng catalog local, COD/đơn mẫu và minh chứng đã lưu. Docker chưa được chạy tại máy này. Bản này chưa được xác nhận triển khai production.

## Review và Retrospective Sprint 3

**Đạt về kỹ thuật trong phạm vi đã kiểm tra:** các nhóm chức năng Sprint 3 dùng được trên bản demo, các điểm nối có integration test và browser test, cấu hình Sprint 2 vẫn được kiểm tra. Việc nhóm chốt hoàn thành Goal cần dựa trên AC và demo thực tế, không chỉ trạng thái issue.

**Điểm làm tốt:** dùng chung dữ liệu nghiệp vụ giữa các sprint, có kiểm thử tranh chấp, quyền truy cập và nhắc giỏ lặp; đóng gói có manifest và kiểm tra trên bản giải nén. Cấu hình CI được mở rộng sang hai sprint.

**Vấn đề và bài học:** mã local chưa commit khiến GitHub không phản ánh Sprint 3; báo cáo nhiều mốc dễ lỗi thời. Dependency có thể phát sinh advisory dù nghiệp vụ vẫn pass; `.gitignore` không bảo vệ publish. Cần chốt mã và kết quả theo commit, kiểm tra cả output phát hành và thường xuyên chạy audit.

## Việc trước bảo vệ

| Công việc | Phụ trách đề xuất | Mốc kiểm tra |
| --- | --- | --- |
| Nhóm demo năm luồng, xác nhận AC và số story nghiệm thu | Cả nhóm; Tuấn tổng hợp | Trước buổi bảo vệ, ngày cụ thể do nhóm chốt |
| Kiểm chứng VNPAY/GHN sandbox với callback công khai và đối chiếu giao dịch | Tuấn và Trương Đức Phú | Trước khi trình bày là tích hợp ngoài hoàn tất |
| Kiểm chứng SMTP và hành vi lỗi/quota Gemini | Bùi Phạm Phong Phú | Trước khi cam kết email/provider hoạt động thật |
| Chạy gói trên máy thành viên thứ hai; nếu dùng Docker thì thử image/volume | Trần Tấn Phát | Trước buổi demo trực tiếp |
| Xác nhận bảng đóng góp, SP/ngày thực tế và review PR của nhóm | Cả nhóm | Khi hoàn thiện hồ sơ cuối kỳ |

Đây là danh sách theo dõi và phân công đề xuất, chưa phải lịch họp hoặc cam kết hạn đã được cả nhóm phê duyệt. [Bảng đóng góp](contributions.md), [hướng dẫn cài đặt](install-guide.md), [kịch bản demo](demo-script.md), [Release Notes](release-notes.md) là các phần bàn giao kèm theo.
