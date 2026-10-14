# Lab 4 – Phát triển và kiểm thử Sprint 1–2

Hồ sơ này dùng cho hai phiếu LAB 4.1 và LAB 4.2 của dự án Techvora. Bản chạy mặc định của repository mở đến Sprint 2. Báo cáo Sprint 2 dự kiến vào **thứ Tư, 21/10/2026** theo thông tin nhóm cung cấp ngày 14/10/2026.

| Nội dung | Minh chứng |
| --- | --- |
| Phiếu LAB 4.1 đã điền | [LAB4.1-Techvora.docx](LAB4.1-Techvora.docx) |
| Phiếu LAB 4.2 đã điền | [LAB4.2-Techvora.docx](LAB4.2-Techvora.docx) |
| Backlog, Acceptance Criteria và phân công | [Sprint 1](sprint-1.md#sprint-backlog) · [Sprint 2](sprint-2.md#sprint-backlog) |
| Test report, lỗi và kiểm thử hồi quy | [Sprint 1](sprint-1.md#kiểm-thử-và-lỗi) · [Sprint 2](sprint-2.md#kiểm-thử-và-lỗi) |
| Kết quả chạy test mới | [110 backend và 22 browser smoke](test-run-2026-10-14.md) |
| Demo và chạy local | [README dự án](../../README.md#demo-local) · [Kịch bản demo](../user-guide/demo.md) |
| Kiến trúc và kết quả test cũ | [Kiến trúc](../design/architecture.md) · [Bằng chứng test](../user-guide/backend-tests.md) |

## Nguồn và giới hạn minh chứng

- Bảng Sprint Backlog do nhóm cung cấp ghi **10 User Story Sprint 1, 37 Story Point** cùng tên người phụ trách. Sprint 2 có **12 User Story** nhưng các cột điểm, người phụ trách và ngày còn trống; vì vậy phiếu LAB 4.2 không ghi các giá trị này như quyết định đã chốt.
- Cột `Act Start Date` của chín dòng Sprint 1 ghi năm **2027–2035**, sau cả ngày kết thúc 29/09/2026. Chỉ dòng US-07 ghi 24/09/2026. Nhóm cần kiểm tra file backlog gốc trước khi báo cáo; hồ sơ này không tự sửa các ngày đó.
- Tại thời điểm kiểm tra, GitHub repository [Anhtuan2005/cnpm](https://github.com/Anhtuan2005/cnpm) chưa có Issue riêng cho User Story. [PR #1](https://github.com/Anhtuan2005/cnpm/pull/1), [#4](https://github.com/Anhtuan2005/cnpm/pull/4) và [#5](https://github.com/Anhtuan2005/cnpm/pull/5) đã merge, nhưng API GitHub không ghi nhận lượt review trên các PR này. Không dùng những PR đó để chứng minh đã có reviewer.
- Lịch sử Git có commit tổng hợp [307db31](https://github.com/Anhtuan2005/cnpm/commit/307db31) mang tên `Sprint 1` ngày 27/09/2026. Commit này chứa cả mã tính năng sau Sprint 1, nên không đủ để xác định từng User Story được hoàn thành đúng ngày nào hoặc do ai viết. Phân công trong backlog là phân công nhóm cung cấp, không phải kết luận từ `git blame`.
- Lệnh `dotnet test test/test-cases/EcommerceApp.Tests/EcommerceApp.Tests.csproj --configuration Debug --no-restore --verbosity minimal` chạy trên máy local ngày **14/10/2026**: **110 pass, 0 fail, 0 skipped**. Cùng ngày, `npm run test:smoke` đạt **22/22** trên Microsoft Edge với database demo riêng. Kết quả này chứng minh trạng thái mã hiện tại; chưa phải bằng chứng test tại thời điểm kết thúc từng Sprint.
- Hồ sơ Lab 4 được chuẩn bị trên branch local `codex/readme-showcase`. Chưa có PR hoặc lượt review của thành viên khác cho hồ sơ này; bản trên GitHub chỉ thay đổi sau khi branch được đẩy lên và merge.

## Việc nhóm cần hoàn tất trên GitHub trước buổi báo cáo

1. Chốt Story Point, người phụ trách, thời gian và trạng thái cho Sprint 2; xác nhận lại các năm sai trong Sprint 1.
2. Tạo GitHub Issues cho User Story/công việc còn cần theo dõi, gắn Acceptance Criteria và liên kết với PR. Không tạo Issue hồi tố với ngày hoặc người thực hiện giả.
3. Nhờ thành viên khác tác giả review PR còn mở [#2](https://github.com/Anhtuan2005/cnpm/pull/2) và [#3](https://github.com/Anhtuan2005/cnpm/pull/3), hoặc PR mới của Lab 4, rồi chỉ merge sau khi kiểm tra.
4. Chạy demo thủ công theo [hướng dẫn](../user-guide/demo.md); lưu ảnh/video hoặc URL bản chạy nếu nhóm thực hiện được. Smoke test tự động 22/22 đã pass ngày 14/10/2026.
5. Ghi nhật ký hai buổi họp thực tế và retrospective của nhóm. Phần nhận xét trong hồ sơ là **đề xuất thảo luận**, chưa phải biên bản họp đã diễn ra.
