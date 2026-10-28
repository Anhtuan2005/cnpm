# Tổng kết Sprint 2

**Cập nhật ngày 28/10/2026 phục vụ Buổi 7.** Sprint 2 mở luồng so sánh, voucher/VNPAY, quản lý kho/đơn, giao hàng, đánh giá và hậu mãi trên nền tài khoản, catalog, giỏ và checkout Sprint 1. Bản mặc định đang ở `Release:ActiveSprint=2`.

GitHub có **12/12 issue Sprint 2 đã đóng** tại thời điểm rà soát. Kết quả kiểm tra mới đạt **132/132 backend và 26/26 browser**, gồm các thay đổi đánh giá/phản hồi và xác nhận giao/hoàn tiền. Trạng thái issue và kết quả kỹ thuật không tự xác định Story Point, người thực hiện lịch sử hay ngày nghiệm thu của nhóm; các dữ liệu này vẫn cần đối chiếu backlog gốc.

## Sprint Backlog

| Story và issue | Chức năng đã có trong bản Sprint 2 | Hành vi và minh chứng rà soát |
| --- | --- | --- |
| [US-04 #7](https://github.com/Anhtuan2005/cnpm/issues/7) | So sánh điện thoại | So sánh giá, thông số và nhận xét theo catalog; browser so sánh đạt. |
| [US-15 #8](https://github.com/Anhtuan2005/cnpm/issues/8) | Thanh toán VNPAY | Kiểm tra chữ ký, số tiền, callback lặp và callback sau hủy; cần thử giao dịch sandbox ngoài hệ thống. |
| [US-13 #9](https://github.com/Anhtuan2005/cnpm/issues/9) | Hủy và mua lại | Hủy đúng vòng đời, hoàn kho một lần; mua lại đưa sản phẩm hợp lệ vào giỏ. |
| [US-01 #10](https://github.com/Anhtuan2005/cnpm/issues/10) | Voucher checkout | Server tính điều kiện và tổng tiền; tranh lượt cuối chỉ một khách dùng được. |
| [US-05 #22](https://github.com/Anhtuan2005/cnpm/issues/22) | Tồn kho | Điều chỉnh có log, giữ nhất quán với checkout đồng thời; tồn đầu, xuất và hoàn được phân loại. |
| [US-10 #23](https://github.com/Anhtuan2005/cnpm/issues/23) | Vận hành đơn và hoàn tiền | Chuyển trạng thái hợp lệ, hóa đơn/CSV; bằng chứng hoàn thủ công và đối soát COD tách khỏi xác nhận khách. |
| [US-19 #24](https://github.com/Anhtuan2005/cnpm/issues/24) | Giao hàng và thông báo | Khách xem tiến trình, xác nhận nhận hàng hoặc báo sự cố; admin phản hồi và khách xác nhận lại. |
| [US-21 #25](https://github.com/Anhtuan2005/cnpm/issues/25) | Đánh giá sau mua | Đơn đã giao cho phép gửi review/ảnh hợp lệ; đánh giá công khai ngay, không chờ duyệt. |
| [US-23 #26](https://github.com/Anhtuan2005/cnpm/issues/26) | Yêu cầu đổi trả và bảo hành | Giới hạn theo số lượng đã mua; khách theo dõi yêu cầu; admin mở đúng đơn từ thông báo Sprint 2. |
| [US-22 #27](https://github.com/Anhtuan2005/cnpm/issues/27) | Dashboard và thông báo admin | Số liệu vận hành, thông báo đơn/hậu mãi qua SignalR; đối chiếu quyền và trạng thái đơn. |
| [US-17 #28](https://github.com/Anhtuan2005/cnpm/issues/28) | Tích hợp GHN | Tạo/đồng bộ/hủy vận đơn; callback trùng/lệch thứ tự không làm lùi trạng thái. Sandbox ngoài chưa chạy buổi này. |
| [US-24 #29](https://github.com/Anhtuan2005/cnpm/issues/29) | Shop phản hồi đánh giá công khai | Admin trả lời review; câu trả lời lưu cùng đánh giá và hiển thị trên trang sản phẩm. Thay phạm vi duyệt/từ chối/xóa cũ. |

## Các thay đổi sau báo cáo cũ

- [25fbbf9](https://github.com/Anhtuan2005/cnpm/commit/25fbbf9): đánh giá công khai ngay, shop trả lời công khai; mở rộng so sánh/thông số điện thoại, lịch sử tồn kho, ghi nhận COD và hoàn tiền có bằng chứng.
- [73aa335](https://github.com/Anhtuan2005/cnpm/commit/73aa335): khách xác nhận nhận hàng/tiền hoàn hoặc báo sự cố; admin lưu chứng từ giao, đối soát COD và phản hồi để khách xác nhận lại; có lịch sử mốc xác nhận trên đơn.
- [PR #39](https://github.com/Anhtuan2005/cnpm/pull/39): sửa runner browser, thêm luồng tích hợp xuyên suốt, đưa backend/asset/browser vào CI; cập nhật báo cáo và chuẩn bị Sprint 3.

Xác nhận khách, trạng thái giao của GHN và ghi nhận chuyển tiền của shop có ý nghĩa riêng. Phần mềm không xác minh tự động tiền đã về ngân hàng; hoàn tiền VNPAY hiện được ghi nhận thủ công sau khi chuyển tiền bên ngoài.

## Tích hợp với Sprint 1

Luồng đã kiểm tra trên SQL Server: **giỏ khách → ghép giỏ tài khoản → voucher/COD → đơn và tồn kho → giao → khách xác nhận → đánh giá/shop phản hồi → bảo hành → thông báo đúng đơn**. Test xác nhận tổng 1.920.000 đồng từ hai máy giá 1.000.000, giảm 100.000 và phí 20.000; tồn kho 5→3, giữ hàng chưa chọn trong giỏ, ghi một lượt voucher và chặn người khác đọc đơn/yêu cầu.

Browser kiểm tra thêm luồng yêu cầu hỗ trợ tới admin và phản hồi sự cố giao/hoàn tiền giữa hai vai trò. Phạm vi Sprint 1 vẫn mở trong Sprint 2; chức năng Sprint 3 chưa được mở bằng thay đổi cấu hình đơn lẻ.

## Kiểm thử và lỗi

| Mốc | Kết quả | Cách hiểu |
| --- | --- | --- |
| 14/10/2026 | 110 backend, 22 browser | Bằng chứng lịch sử tại lần chạy đó |
| 28/10/2026 trước bổ sung | 131 backend; browser lỗi khởi chạy | Lỗi runner #38 do spec và config resolve hai instance Playwright |
| 28/10/2026 sau bổ sung | 132 backend, 26 browser, asset đạt | Bằng chứng local cho mã test/bản sửa 04f4a18 |

[Test Report](../../test/TestReport.md) chứa input, expected, actual, Pass/Fail, lệnh chạy lại và file kết quả từng test. [Bug #38](https://github.com/Anhtuan2005/cnpm/issues/38) đã sửa, retest local đạt, chờ review/merge PR #39. Callback VNPAY/GHN dùng test double; giao dịch/vận đơn sandbox thật **chưa chạy trong buổi này**.

## Sprint Review và Retrospective

**Làm tốt:** chức năng hai sprint cùng dùng một mô hình giỏ/đơn/kho; có test concurrency và trạng thái tiền/giao; bộ kiểm thử đã bao phủ thêm các thay đổi cuối Sprint 2.

**Tồn tại:** báo cáo cũ và tên issue US-24 chưa theo kịp mã; test browser chưa được CI kiểm tra sau đổi cấu trúc; kiểm chứng dịch vụ ngoài còn thiếu. Ngày bắt đầu/kết thúc thực tế, Story Point và phân công Sprint 2 chưa được xác nhận từ backlog gốc.

**Hành động:** mỗi PR phải lưu kết quả của đúng commit, liên kết issue và có người khác review. CI chạy thêm browser và asset. Sprint 3 nhận các phần gắn với luồng đang chạy, có AC, phụ thuộc và owner đề xuất rõ ràng. Các nhận xét quy trình là đề xuất cho nhóm thảo luận, không ghi thành biên bản họp đã diễn ra.

## Chuẩn bị Sprint 3

[Kế hoạch Sprint 3](../buoi7/sprint-3-plan.md) ưu tiên US-27, US-18, US-26, US-29: CI và mở phạm vi có kiểm soát; xử lý hậu mãi; quản trị voucher nối checkout; báo cáo nối đơn/hoàn tiền. Các tính năng gợi ý, mua kèm/nhắc giỏ, AI và quản lý người dùng nằm ở nhóm dự phòng. Phiếu đã điền và tổng hợp bằng chứng ở [hồ sơ Buổi 7](../buoi7/README.md).

## Lịch sử hồ sơ

Hồ sơ Lab 4 và hai phiếu Word giữ giá trị theo thời điểm lập. [Bản báo cáo Sprint 2 trước cập nhật](https://github.com/Anhtuan2005/cnpm/blob/73aa335/docs/lab4/sprint-2.md) lưu số liệu cũ để đối chiếu; PR #6 đã merge. Buổi 7 không suy đoán lại ngày hoàn thành hoặc người viết từng story từ commit tổng hợp.
