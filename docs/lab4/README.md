# Lab 4 – Phát triển và kiểm thử Sprint 1–2

Hồ sơ này dùng cho hai phiếu LAB 4.1 và LAB 4.2 của dự án Techvora. Bản chạy mặc định của repository mở đến Sprint 2. Báo cáo Sprint 2 dự kiến vào **thứ Tư, 21/10/2026** theo thông tin nhóm cung cấp ngày 14/10/2026.

| Nội dung | Minh chứng |
| --- | --- |
| Phiếu LAB 4.1 đã điền | [LAB4.1-Techvora.docx](LAB4.1-Techvora.docx) |
| Phiếu LAB 4.2 đã điền | [LAB4.2-Techvora.docx](LAB4.2-Techvora.docx) |
| Bảng trạng thái theo từng User Story, kiểu GitHub Projects | [Sprint 1–3 và việc quản lý còn thiếu](progress-board.md) |
| Backlog, Acceptance Criteria và phân công | [Sprint 1](sprint-1.md#sprint-backlog) · [Sprint 2](sprint-2.md#sprint-backlog) |
| Test report, lỗi và kiểm thử hồi quy | [Sprint 1](sprint-1.md#kiểm-thử-và-lỗi) · [Sprint 2](sprint-2.md#kiểm-thử-và-lỗi) |
| Kết quả chạy test mới | [110 backend và 22 browser smoke](test-run-2026-10-14.md) |
| Demo và chạy local | [README dự án](../../README.md#demo-local) · [Kịch bản demo](../user-guide/demo.md) |
| Kiến trúc và kết quả test cũ | [Kiến trúc](../design/architecture.md) · [Bằng chứng test](../user-guide/backend-tests.md) |

## Bảng đối chiếu đã làm và chưa làm

Bảng ghi **bằng chứng có tại ngày 14/10/2026**, không thay cho biên bản nghiệm thu của nhóm. “Có mã/test” chỉ chứng minh trạng thái repository hiện tại; ô trống trong phiếu Word không được hiểu là đã hoàn thành.

### LAB 4.1 — Sprint 1

| Hạng mục | Đã làm và minh chứng | Chưa làm hoặc chưa có minh chứng |
| --- | --- | --- |
| Backlog và phân công | Nhóm cung cấp 10 User Story, 37 SP và tên người phụ trách; [bảng chi tiết](sprint-1.md#sprint-backlog). | Chưa xác nhận trạng thái `Done` tại 27/09; chín ngày bắt đầu thực tế trong sheet ghi năm 2027–2035 cần sửa từ bản gốc. |
| Mã nguồn và chạy thử | Các luồng catalog, tài khoản, giỏ, COD và đơn có mã; [kịch bản chạy/demo](../user-guide/demo.md). | Chưa có biên bản demo hoặc nghiệm thu của nhóm tại cuối Sprint 1. |
| Kiểm thử | [110/110 backend và 22/22 browser smoke](test-run-2026-10-14.md) pass ngày 14/10; có ca hợp lệ và ca biên. | Kết quả chạy sau Sprint không chứng minh test đã pass vào ngày 27/09; chưa có minh chứng demo thủ công. |
| Issue, branch, PR, review | Có [commit tổng hợp 307db31](https://github.com/Anhtuan2005/cnpm/commit/307db31); [PR #1](https://github.com/Anhtuan2005/cnpm/pull/1) bổ sung test sau Sprint. | Chưa có chuỗi Issue → branch → PR → review khác tác giả cho từng story Sprint 1. |
| Báo cáo, lỗi và cải tiến | Có [báo cáo Sprint 1](sprint-1.md), phiếu Word và hướng dẫn demo; lần test hiện tại không ghi nhận bug mới. | Chưa có biên bản Sprint Review/Retrospective hoặc bug thực tế kèm commit sửa và retest; không tạo bug giả để đủ biểu mẫu. |

### LAB 4.2 — Sprint 2

| Hạng mục | Đã làm và minh chứng | Chưa làm hoặc chưa có minh chứng |
| --- | --- | --- |
| Backlog và Issue | Đã ghi 12 story trong [backlog Sprint 2](sprint-2.md#sprint-backlog); tạo Issue [#7](https://github.com/Anhtuan2005/cnpm/issues/7)–[#10](https://github.com/Anhtuan2005/cnpm/issues/10) cho bốn luồng minh họa. | Nhóm chưa chốt SP, người phụ trách, ngày, Acceptance Criteria và `Done`; tám story còn lại chưa có Issue riêng. |
| Mã và tích hợp | Có mã các luồng Sprint 2; bản mặc định mở đến Sprint 2. | Chưa có nghiệm thu từng story; VNPAY/GHN chưa chạy với tài khoản sandbox ngoài hệ thống. |
| Kiểm thử hồi quy | [110/110 backend và 22/22 browser smoke](test-run-2026-10-14.md) pass ngày 14/10, gồm giỏ, checkout, voucher, hủy đơn và callback. | Chưa có giao dịch sandbox thật, biên bản demo thủ công hoặc test report của buổi nghiệm thu Sprint 2. |
| PR và review | [PR #6](https://github.com/Anhtuan2005/cnpm/pull/6) chứa hồ sơ Lab 4 đã merge vào `main` ngày 14/10. | GitHub API không ghi nhận review của người khác trên PR #6; chưa có branch/PR liên kết từng story Sprint 2. |
| Báo cáo và lỗi | Có [báo cáo Sprint 2](sprint-2.md), phiếu Word và kịch bản demo; lần test hiện tại không ghi nhận bug mới. | Chưa có biên bản Sprint Review/Retrospective, bug thực tế kèm sửa/retest hoặc bằng chứng demo của nhóm. |

## Nguồn và giới hạn minh chứng

- Bảng Sprint Backlog do nhóm cung cấp ghi **10 User Story Sprint 1, 37 Story Point** cùng tên người phụ trách. Sprint 2 có **12 User Story** nhưng các cột điểm, người phụ trách và ngày còn trống; vì vậy phiếu LAB 4.2 không ghi các giá trị này như quyết định đã chốt.
- Cột `Act Start Date` của chín dòng Sprint 1 ghi năm **2027–2035**, sau cả ngày kết thúc 29/09/2026. Chỉ dòng US-07 ghi 24/09/2026. Nhóm cần kiểm tra file backlog gốc trước khi báo cáo; hồ sơ này không tự sửa các ngày đó.
- Tại thời điểm kiểm tra ban đầu, GitHub repository [Anhtuan2005/cnpm](https://github.com/Anhtuan2005/cnpm) chưa có Issue riêng cho User Story. Ngày 14/10/2026, đã tạo bốn Issue Sprint 2 dạng chờ xác nhận: [US-04 #7](https://github.com/Anhtuan2005/cnpm/issues/7), [US-15 #8](https://github.com/Anhtuan2005/cnpm/issues/8), [US-13 #9](https://github.com/Anhtuan2005/cnpm/issues/9), [US-01 #10](https://github.com/Anhtuan2005/cnpm/issues/10). [PR #1](https://github.com/Anhtuan2005/cnpm/pull/1), [#4](https://github.com/Anhtuan2005/cnpm/pull/4) và [#5](https://github.com/Anhtuan2005/cnpm/pull/5) đã merge, nhưng API GitHub không ghi nhận lượt review trên các PR này. Không dùng những PR đó để chứng minh đã có reviewer.
- Lịch sử Git có commit tổng hợp [307db31](https://github.com/Anhtuan2005/cnpm/commit/307db31) mang tên `Sprint 1` ngày 27/09/2026. Commit này chứa cả mã tính năng sau Sprint 1, nên không đủ để xác định từng User Story được hoàn thành đúng ngày nào hoặc do ai viết. Phân công trong backlog là phân công nhóm cung cấp, không phải kết luận từ `git blame`.
- Lệnh `dotnet test test/test-cases/EcommerceApp.Tests/EcommerceApp.Tests.csproj --configuration Debug --no-restore --verbosity minimal` chạy trên máy local ngày **14/10/2026**: **110 pass, 0 fail, 0 skipped**. Cùng ngày, `npm run test:smoke` đạt **22/22** trên Microsoft Edge với database demo riêng. Kết quả này chứng minh trạng thái mã hiện tại; chưa phải bằng chứng test tại thời điểm kết thúc từng Sprint.
- Hồ sơ Lab 4 đã vào `main` qua [PR #6](https://github.com/Anhtuan2005/cnpm/pull/6) ngày 14/10/2026. GitHub API không ghi nhận lượt review trên PR này; việc merge không được coi là bằng chứng đã có reviewer.

## Việc nhóm cần hoàn tất trên GitHub trước buổi báo cáo

1. Chốt Story Point, người phụ trách, thời gian và trạng thái cho Sprint 2; xác nhận lại các năm sai trong Sprint 1.
2. Xác nhận bốn Issue Sprint 2 mới và tạo Issue cho các story còn được nhóm chọn thực hiện; gắn Acceptance Criteria và liên kết với PR. Không tạo Issue hồi tố với ngày hoặc người thực hiện giả.
3. Nhờ thành viên khác tác giả review PR tiếp theo; rà lại các PR còn mở [#2](https://github.com/Anhtuan2005/cnpm/pull/2) và [#3](https://github.com/Anhtuan2005/cnpm/pull/3) nếu vẫn còn phù hợp. PR #6 đã merge nhưng không có review API.
4. Chạy demo thủ công theo [hướng dẫn](../user-guide/demo.md); lưu ảnh/video hoặc URL bản chạy nếu nhóm thực hiện được. Smoke test tự động 22/22 đã pass ngày 14/10/2026.
5. Ghi nhật ký hai buổi họp thực tế và retrospective của nhóm. Phần nhận xét trong hồ sơ là **đề xuất thảo luận**, chưa phải biên bản họp đã diễn ra.
