# LAB 4.2 – Sprint 2 thanh toán, vận hành đơn và hậu mãi

**Mục tiêu đề xuất để nhóm xác nhận:** mở luồng so sánh, thanh toán trực tuyến, xử lý đơn, giao hàng, đánh giá và hậu mãi trên nền Sprint 1. Bản chạy mặc định hiện đặt `Release:ActiveSprint=2`. Nhóm dự kiến báo cáo vào **21/10/2026**; ngày bắt đầu/kết thúc, Story Point, phân công và trạng thái nghiệm thu Sprint 2 chưa được nhóm chốt trong backlog được cung cấp.

**Đối chiếu nhanh:** [bảng đã làm và chưa làm của LAB 4.2](README.md#lab-42--sprint-2).

## Báo cáo Sprint 1 trước khi lập Sprint 2

Backlog Sprint 1 ghi 10 User Story, 37 Story Point và phân công theo bốn thành viên. Mã hiện tại hỗ trợ demo các luồng Sprint 1; bộ test backend chạy ngày 14/10/2026 đạt 110/110. [Báo cáo Sprint 1](sprint-1.md) nêu các hạn chế về ngày thực tế, Issue, PR review và nghiệm thu lịch sử. Các hạn chế đó cần được giải trình trong phần mở đầu buổi báo cáo Sprint 2.

## Sprint Backlog

Bảng dưới đây giữ nguyên 12 User Story Sprint 2 mà nhóm gửi. `AC` là bản tóm tắt để nhóm rà soát, chưa phải Acceptance Criteria đã được Product Owner duyệt. **Không coi `Tổng Sprint 2 = 0` trong sheet là 0 điểm đã hoàn thành**: các ô Story Point thực tế đang trống. Trạng thái `Có mã ở bản Sprint 2` được kiểm tra từ route/service/test hiện tại, không thay thế quyết định `Done` của nhóm.

| ID | Ưu tiên đề xuất | User Story và Acceptance Criteria cần xác nhận | SP | Phụ trách | Trạng thái kỹ thuật hiện tại |
| --- | --- | --- | --- | --- | --- |
| US-04 | Cao | So sánh giá và thông số của hai điện thoại cùng danh mục; dữ liệu đúng catalog. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |
| US-15 | Cao | Thanh toán VNPAY; xác minh chữ ký/số tiền và xử lý callback lặp an toàn. | Chưa chốt | Chưa chốt | Có mã; cần sandbox để demo ngoài |
| US-13 | Cao | Hủy đơn đủ điều kiện, hoàn kho một lần; mua lại đưa hàng hợp lệ vào giỏ. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |
| US-05 | Cao | Điều chỉnh tồn kho có lịch sử; không ghi đè giao dịch bán đồng thời. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |
| US-10 | Cao | Admin cập nhật trạng thái đơn, hoàn tiền, hóa đơn và CSV; giữ đúng vòng đời đơn. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |
| US-19 | Cao | Khách theo dõi tiến trình giao hàng và xem thông báo của mình. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |
| US-21 | Trung bình | Khách đã mua gửi đánh giá/ảnh hợp lệ; chờ admin duyệt. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |
| US-23 | Trung bình | Gửi yêu cầu đổi trả/bảo hành trong giới hạn số lượng đã mua; admin xem từ đơn. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |
| US-22 | Trung bình | Dashboard hiển thị số liệu hợp lệ; admin nhận thông báo đơn mới. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |
| US-17 | Trung bình | Tạo, đồng bộ, theo dõi, hủy vận đơn GHN; callback không làm lùi trạng thái. | Chưa chốt | Chưa chốt | Có mã; cần sandbox để demo ngoài |
| US-24 | Trung bình | Admin duyệt, từ chối hoặc xóa đánh giá theo quyền. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |
| US-01 | Cao | Áp voucher hợp lệ; từ chối mã hết lượt/không đủ điều kiện, tránh dùng vượt lượt. | Chưa chốt | Chưa chốt | Có mã ở Sprint 2 |

**Phạm vi báo cáo nên chọn sau khi nhóm chốt:** không nhận mặc định cả 12 story là đã nghiệm thu. Trước 21/10, chọn số story phù hợp năng lực nhóm, ước lượng và phân công; những story chưa được kiểm tra đủ Acceptance Criteria giữ trạng thái `In progress` hoặc `To do`. Có thể dùng US-04, US-15, US-13 và US-01 làm bốn luồng minh họa đầu tiên trong phiếu, rồi mở rộng khi nhóm xác nhận.

Bốn luồng minh họa đã có Issue chờ nhóm chốt: [US-04 #7](https://github.com/Anhtuan2005/cnpm/issues/7), [US-15 #8](https://github.com/Anhtuan2005/cnpm/issues/8), [US-13 #9](https://github.com/Anhtuan2005/cnpm/issues/9), [US-01 #10](https://github.com/Anhtuan2005/cnpm/issues/10). Tám story còn lại trong bảng chưa có Issue riêng; không coi bốn Issue mới là bằng chứng công việc đã thực hiện trong quá khứ.

## Tích hợp với Sprint 1

- `SprintScopeTests` kiểm tra route Sprint 1 vẫn mở khi Sprint 2 hoạt động, route Sprint 2 được mở và Sprint 3 trở lên vẫn bị khóa.
- `OrderPaymentTests` và `SafetyRegressionTests` kiểm tra checkout/giỏ/kho cũ khi thêm voucher, VNPAY, hủy đơn và mua ngay.
- [CI](../../.github/workflows/portfolio-tests.yml) được cấu hình chạy test backend với SQL Server 2022 trên push/PR; kết quả local dưới đây không được gọi là kết quả CI của PR mới.

## Kiểm thử và lỗi

| TC | Luồng | Dữ liệu / trường hợp | Kỳ vọng | Kết quả và bằng chứng hiện tại |
| --- | --- | --- | --- | --- |
| S2-01 | So sánh | Hai điện thoại cùng danh mục | Hiện giá và thông số để đối chiếu | Pass trong browser smoke 22/22; `storefront.spec.mjs` |
| S2-02 | Thanh toán VNPAY | Callback hợp lệ và callback lặp | Chỉ ghi thanh toán một lần | Pass trong bộ 110 test; `OrderPaymentTests.Concurrent_payment_callbacks_apply_once_including_payment_after_cancellation` |
| S2-03 | Thanh toán VNPAY | Callback sau hủy | Không hồi sinh đơn; ghi xử lý hoàn tiền theo trạng thái | Pass; cùng bài test ở S2-02 và `OrderLifecycleTests.Expiry_cancellation_and_late_payment_race_release_stock_once_and_require_refund` |
| S2-04 | Hủy đơn | Hai yêu cầu hủy cùng lúc | Chỉ hoàn kho một lần | Pass; `OrderPaymentTests.Concurrent_cancellation_restores_inventory_once` |
| S2-05 | Voucher | Khách cuối cùng dùng mã còn một lượt | Chỉ một khách dùng thành công | Pass; `OrderPaymentTests.Last_voucher_use_is_atomic_across_customers` |
| S2-06 | Voucher sai điều kiện | Giá trị ngoài giới hạn hoặc dữ liệu admin sai | Từ chối, không ghi sai | Pass; `AdminAuditTests.Voucher_preview_rejects_amounts_outside_database_limits` và `PortfolioQualityTests.Voucher_rejects_invalid_admin_input` |
| S2-07 | Điều chỉnh kho | Admin điều chỉnh khi checkout đồng thời | Số lượng và log nhất quán, lỗi rollback | Pass; `CatalogConsistencyTests.Stock_adjustment_and_checkout_both_apply_and_log_failure_rolls_back_adjustment` |
| S2-08 | GHN | Callback trùng hoặc lệch thứ tự | Trạng thái không lùi, email không gửi lặp | Pass; `OrderLifecycleTests.GHN_duplicate_and_out_of_order_callbacks_do_not_regress_delivery_or_repeat_email` |
| S2-09 | Yêu cầu hậu mãi | Hai yêu cầu vượt số lượng đã mua | Từ chối phần vượt | Pass; `SafetyRegressionTests.Concurrent_return_requests_cannot_exceed_the_purchased_quantity` |
| S2-10 | Thông báo hậu mãi | Khách gửi yêu cầu | Admin mở được đơn tương ứng | Pass; `SafetyRegressionTests.Support_request_notification_opens_an_order_available_in_sprint_two` |
| S2-11 | Báo cáo/admin | Đơn hủy, chờ hoàn, đã hoàn tiền | Không tính vào doanh thu | Pass; `CatalogConsistencyTests.Dashboard_and_report_exclude_cancelled_pending_refund_and_refunded_orders` |
| S2-12 | CSV | Chuỗi chứa dấu nháy hoặc công thức bảng tính | Escape đúng, chặn công thức | Pass; `PortfolioQualityTests.Csv_formatter_escapes_quotes_uses_invariant_numbers_and_blocks_formulas` |
| S2-13 | Đánh giá kèm ảnh | Ảnh lỗi/không hợp lệ | Hiện validation thay vì lỗi server | Pass với ảnh PNG hỏng ở `UploadValidationTests`; luồng duyệt thủ công cần demo lại |
| S2-14 | Thanh toán/giao hàng thật | Tài khoản sandbox VNPAY/GHN | Tạo giao dịch/vận đơn ngoài hệ thống | **Chưa chạy** vì repository không chứa thông tin sandbox |

**Kết quả hồi quy local 14/10/2026:** `dotnet test test/test-cases/EcommerceApp.Tests/EcommerceApp.Tests.csproj --configuration Debug --no-restore --verbosity minimal` → **110 pass, 0 fail, 0 skipped**, khoảng 11 giây chạy test (chưa tính build). `npm run test:smoke` từ `src/frontend/` trên Microsoft Edge và database demo riêng → **22 pass, 0 fail** trong 1,4 phút. [Bằng chứng và cách chạy](../user-guide/backend-tests.md). Không ghi giao dịch sandbox hoặc demo thủ công là `Pass` khi chưa chạy.

**Bug Report:** lần chạy backend và browser không phát hiện lỗi mới. Chưa có GitHub Bug Issue được xác minh để điền mã BUG-01/BUG-02; nếu demo thủ công hoặc sandbox phát hiện lỗi, tạo Issue, ghi bước tái hiện, mức độ, commit sửa và kết quả retest.

## Quản lý phiên bản và minh chứng

| Nội dung | Trạng thái có bằng chứng |
| --- | --- |
| Mã Sprint 2 | Có trong repository; các route được mở khi `Release:ActiveSprint=2` theo `Program.cs` và `SprintFeatures.cs`. |
| Branch/commit theo từng story | Có bốn Issue nháp nhưng chưa có chuỗi Issue → branch → commit → PR chứng minh cho từng story. |
| PR và review | [PR #6](https://github.com/Anhtuan2005/cnpm/pull/6) chứa hồ sơ Lab 4, đã merge vào `main` ngày 14/10/2026 nhưng API chưa ghi nhận review. [PR #1](https://github.com/Anhtuan2005/cnpm/pull/1), [#4](https://github.com/Anhtuan2005/cnpm/pull/4), [#5](https://github.com/Anhtuan2005/cnpm/pull/5) cũng đã merge nhưng API chưa ghi nhận review; [#2](https://github.com/Anhtuan2005/cnpm/pull/2), [#3](https://github.com/Anhtuan2005/cnpm/pull/3) còn mở tại lần kiểm tra trước. |
| GitHub Issues/Project | [#7](https://github.com/Anhtuan2005/cnpm/issues/7)–[#10](https://github.com/Anhtuan2005/cnpm/issues/10) mới được tạo cho bốn luồng minh họa; chưa có Project board hoặc Issue cho tám story còn lại. |

## Sprint Review và Retrospective để nhóm xác nhận

- **Demo đề xuất:** so sánh sản phẩm → checkout với voucher và chọn COD/VNPAY → hủy/mua lại → theo dõi giao hàng/đánh giá/yêu cầu hậu mãi → admin xem đơn, dashboard, tồn kho và duyệt review. VNPAY/GHN chỉ demo giao dịch thật khi có sandbox.
- **Điều làm tốt có bằng chứng:** mã Sprint 1–2 cùng chạy trong bản mặc định; bộ test SQL Server 110/110 và browser smoke 22/22 pass, gồm callback lặp, tranh chấp tồn kho và hồi quy giỏ/checkout.
- **Khó khăn cần nhóm xác nhận:** bảng Sprint 2 chưa có người, điểm hoặc ngày; Issue/PR review theo story chưa có; tích hợp ngoài cần sandbox.
- **Cải tiến đề xuất:** chốt scope/owner/SP trước buổi báo cáo, liên kết PR với Issue, nhờ người khác review và lưu test report của đúng commit được demo.

## Nhật ký minh chứng

| Mốc có bằng chứng | Công việc | Vướng mắc | Minh chứng |
| --- | --- | --- | --- |
| 07/10/2026 | Repo được sắp xếp lại, README và kịch bản demo cập nhật | Lịch sử gốc vẫn là commit tổng hợp | [4991e3b](https://github.com/Anhtuan2005/cnpm/commit/4991e3b), [PR #4](https://github.com/Anhtuan2005/cnpm/pull/4) |
| 14/10/2026 | Chạy lại 110 test backend, 22 smoke test và lập hồ sơ Sprint 2 | Chưa có xác nhận phân công/SP hoặc demo thủ công | Báo cáo này; [mã test](../../test/test-cases/EcommerceApp.Tests/) |

**Kết luận tạm thời:** bản hiện tại có các luồng Sprint 2; backend regression và browser smoke pass. Không thể kết luận Sprint Goal đã được nhóm nghiệm thu cho đến khi phân công/Story Point, Acceptance Criteria, demo thủ công và minh chứng review được chốt.
