# Lab 6 hoàn thiện đóng gói và nghiệm thu

**Nhóm 3 · Techvora · Lớp 261122402437 · Ngày 04/11/2026.** Phiên bản 1.0.0 chốt mã Sprint 3 đang có trên máy, sửa lỗi chốt phát hành, kiểm tra gói sau giải nén và chuẩn bị hồ sơ bàn giao.

| Sản phẩm | Vị trí |
| --- | --- |
| Phiếu Lab 6 đã điền | [LAB6-Techvora.docx](LAB6-Techvora.docx) |
| Repository và bản phát hành | [main](https://github.com/Anhtuan2005/cnpm/tree/main) · [Release v1.0.0](https://github.com/Anhtuan2005/cnpm/releases/tag/v1.0.0) · [PR #42](https://github.com/Anhtuan2005/cnpm/pull/42) |
| Hướng dẫn cài và chạy gói | [install-guide.md](install-guide.md) |
| Kết quả kiểm thử và lỗi | [TestReport.md](TestReport.md) · [minh chứng từng test](../../test/evidence/lab6-2026-11-04.json) |
| Nghiệm thu năm luồng chính | [acceptance.md](acceptance.md) |
| Nháp báo cáo cuối kỳ và Review/Retrospective | [final-report.md](final-report.md) |
| Demo và phân công trình bày | [demo-script.md](demo-script.md) · [contributions.md](contributions.md) |
| Chức năng và giới hạn phiên bản | [release-notes.md](release-notes.md) |

Mã Sprint 3 có sẵn được ghi nhận ở `42fcead`; bản sửa chính ở `b705b18`, sửa fixture CDN ở `de77cb7`. Ba bug thực tế #40/#41/#43 có bước tái hiện và retest. Lab 6 kế thừa bộ test đã có, bổ sung hai trường hợp chuyển ảnh để bảo vệ thay đổi dependency, không thiết kế lại toàn bộ test case Lab 5.

Kết quả kỹ thuật: 157 backend đạt; browser trên gói Sprint 3 đạt 33, cấu hình Sprint 2 đạt 29 và bỏ qua 4 trường hợp chỉ dành cho Sprint 3. CI dùng gói giải nén và SQL Server 2022. Xem Test Report để phân biệt chạy local, CI, giả lập provider và kiểm chứng ngoài.

Chưa có biên bản demo/PO ký xác nhận hoặc review độc lập của thành viên khác; các bảng phân công trình bày và việc trước bảo vệ là đề xuất. Các giới hạn ngoài hệ thống và thử trên máy thứ hai được giữ trong danh sách việc còn lại, không đánh dấu hoàn thành thay nhóm.
