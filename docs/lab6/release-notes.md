# Techvora 1 0 0

Phiên bản demo và nghiệm thu kỹ thuật Sprint 1–3, ngày 04/11/2026. [Repository](https://github.com/Anhtuan2005/cnpm) · [PR Lab 6 #42](https://github.com/Anhtuan2005/cnpm/pull/42) · [Release v1.0.0](https://github.com/Anhtuan2005/cnpm/releases/tag/v1.0.0).

## Chức năng chính

- Catalog điện thoại, phụ kiện, so sánh và Flash Sale; tài khoản, yêu thích, giỏ khách/ghép giỏ, mua ngay.
- Voucher, ưu đãi mua kèm phù hợp, COD và luồng VNPAY; vòng đời đơn, kho, hủy/mua lại, đánh giá và phản hồi công khai.
- Theo dõi giao/hoàn tiền, ghi nhận đối soát COD và bằng chứng chuyển tiền thủ công; yêu cầu hậu mãi, xử lý admin và thông báo khách.
- Báo cáo/CSV, quản lý người dùng, voucher/nhóm khách, gợi ý cá nhân, nhắc giỏ bỏ quên và chatbot có chế độ catalog local.
- Health check, CI matrix Sprint 2/3 và hướng dẫn demo/cài đặt. Build PC/Sprint 4 chưa mở.

## Lỗi đã sửa khi chốt Lab 6

- **#40 — Dependency audit:** loại ImageSharp 3.1.12; dùng SkiaSharp 3.119.4 và native Linux assets, giữ chuyển WebP/xoay EXIF/resize; pin Pkcs 8.0.2. Audit NuGet sau sửa không liệt kê advisory tại ngày kiểm tra.
- **#41 — Gói chứa upload local:** loại file upload/runtime/local settings khỏi publish và Docker context. Script đóng gói kiểm tra file cấm, manifest từng file và SHA256 ZIP.
- **#43 — Browser timeout CDN:** lần local Sprint 2 có ảnh ngoài tải gần 50 giây; fixture tách ảnh/font/icon ngoài khỏi kiểm thử nghiệp vụ. Test so sánh chạy lại đạt 2,4 giây; các lần lỗi được giữ trong Test Report.
- Xóa file kết quả Playwright tạm khỏi Git; dùng cấu hình kết nối mẫu portable, không gắn tên máy phát triển. Lỗi Playwright #38 đã được xử lý ở PR #39 trước đó.

## Kiểm chứng

Local: **157/157 backend**, **33/33 browser Sprint 3**. Cấu hình Sprint 2 có **29 pass, 4 skip theo phạm vi**, không bỏ qua lỗi. Kiểm tra asset đạt. [CI code b705b18](https://github.com/Anhtuan2005/cnpm/actions/runs/41769846813) cũng chạy thành công cả hai sprint trên gói giải nén ở Ubuntu 24.04; mỗi job chạy 157 backend, browser và kiểm tra manifest.

[Hồ sơ Lab 6](README.md), [Test Report](TestReport.md), [bảng nghiệm thu](acceptance.md) và [tổng kết ba Sprint](final-report.md) đi kèm. Gói là framework-dependent: cần ASP.NET Core Runtime 8 và SQL Server. Có `Start-Demo.ps1`, `manifest.json` và `SHA256SUMS.txt`; không kèm database/API key/upload từ máy build.

## Giới hạn còn lại

- VNPAY/GHN và SMTP cần tài khoản, endpoint và thử nghiệm ngoài riêng; không coi test callback/stub là giao dịch thật.
- Gemini có minh chứng gọi thật ngày 29/10 trong repository; đợt Lab 6 dùng chế độ catalog local/stub, chưa xác nhận quota hoặc tải thực tế.
- API huyện/xã, một số ảnh/font phụ thuộc Internet. Khi mất mạng dùng đơn mẫu và minh chứng theo kịch bản; không cam kết đặt đơn offline hoàn chỉnh.
- Hoàn tiền và đối soát được ghi nhận sau thao tác ngoài hệ thống; phần mềm không xác minh tiền thực sự về ngân hàng.
- Docker và máy thành viên thứ hai chưa được kiểm chứng trong đợt này. Release không phải xác nhận triển khai production hay biên bản PO/nhóm đã ký.
