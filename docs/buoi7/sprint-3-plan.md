# Kế hoạch Sprint 3

Ngày lập: **28/10/2026**. Nền tích hợp là Sprint 1–2 ở `73aa335`, bổ sung kiểm thử và CI trong [PR #39](https://github.com/Anhtuan2005/cnpm/pull/39). Backlog hiện có gồm tám issue #30–#37; giữ các ID này để không tạo trùng công việc.

## Sprint Goal đề xuất

Hoàn thiện ba luồng quản trị nối với dữ liệu mua sắm hiện có: **yêu cầu hậu mãi → xử lý → khách nhận phản hồi; voucher quản trị → checkout → ghi nhận sử dụng; đơn và hoàn tiền → báo cáo doanh thu**. Mỗi phần phải có kiểm thử tích hợp, phân quyền và hồi quy Sprint 1–2 trên CI trước khi mở cho người dùng.

Chọn **US-27, US-18, US-26 và US-29** làm phạm vi cốt lõi. Các mục còn lại là dự phòng; không mặc định nhận cả tám story trong cùng sprint. Người phụ trách dưới đây là **phân công đề xuất để bắt đầu chuẩn bị**, đã gán trên GitHub; Story Point, năng lực và lịch sprint cần được nhóm chốt, không suy ra từ trạng thái issue.

## Backlog và người phụ trách

| Issue | Nội dung | Ưu tiên | Lựa chọn | Phụ trách đề xuất |
| --- | --- | --- | --- | --- |
| [US-27 #37](https://github.com/Anhtuan2005/cnpm/issues/37) | Ổn định tích hợp và kiểm thử tự động | High | Cốt lõi | Bùi Phạm Phong Phú (@572bpphu) |
| [US-18 #30](https://github.com/Anhtuan2005/cnpm/issues/30) | Yêu cầu hậu mãi đến xử lý và phản hồi cho khách | High | Cốt lõi | Trương Đức Phú (@PhuTruong259) |
| [US-26 #32](https://github.com/Anhtuan2005/cnpm/issues/32) | Voucher quản trị đến checkout và ghi nhận sử dụng | High | Cốt lõi | Nguyễn Trương Anh Tuấn (@Anhtuan2005) |
| [US-29 #33](https://github.com/Anhtuan2005/cnpm/issues/33) | Đơn hàng và hoàn tiền đến báo cáo doanh thu | High | Cốt lõi | Trần Tấn Phát (@Capta1nPhat) |
| [US-20 #31](https://github.com/Anhtuan2005/cnpm/issues/31) | Quản lý người dùng và hiệu lực quyền truy cập | Medium | Dự phòng | Trần Tấn Phát (@Capta1nPhat) |
| [US-28 #34](https://github.com/Anhtuan2005/cnpm/issues/34) | Gợi ý sản phẩm từ tương tác mua sắm | Low | Dự phòng | Nguyễn Trương Anh Tuấn (@Anhtuan2005) |
| [US-30 #35](https://github.com/Anhtuan2005/cnpm/issues/35) | Mua kèm và phục hồi giỏ hàng | Low | Dự phòng | Trương Đức Phú (@PhuTruong259) |
| [US-25 #36](https://github.com/Anhtuan2005/cnpm/issues/36) | Chatbot tư vấn dẫn đến sản phẩm có thể mua | Low | Dự phòng | Bùi Phạm Phong Phú (@572bpphu) |

## Thứ tự tích hợp

1. **Nền kiểm thử US-27:** sửa runner #38, chạy test backend/asset/browser trên push và PR. Chuẩn bị cơ chế mở từng phần Sprint 3, cùng test kiểm tra route, menu, background job và quay lại Sprint 2.
2. **Hậu mãi US-18:** dùng đơn đã giao và yêu cầu US-23 của Sprint 2; nối thông báo đến màn hình admin, xử lý trạng thái rồi trả thông tin về tài khoản khách.
3. **Voucher US-26:** dùng màn hình quản trị tạo dữ liệu mới, áp ngay ở checkout Sprint 1–2; kiểm tra giới hạn lượt và dữ liệu đơn trước khi mở rộng phân khúc/khuyến mãi.
4. **Báo cáo US-29:** dùng các đơn vừa tạo, giao, hủy và hoàn để đối chiếu doanh thu/CSV. Chạy lại toàn bộ luồng mua sắm, hậu mãi và quyền truy cập.
5. **Dự phòng:** chỉ kéo thêm US-20/28/30/25 sau khi bốn mục cốt lõi đạt và nhóm còn năng lực. US-30 tách phần mua kèm trước, nhắc giỏ sau; AI cần môi trường API thử riêng.

## Acceptance Criteria theo từng phần

### US-27 và issue #37

**Luồng:** Cổng kiểm tra chung cho mọi phần tích hợp; bằng chứng nền ở test/TestReport.md.

**Phụ thuộc:** Bắt đầu ngay; sửa #38 trước khi dùng browser làm cổng kiểm tra.

- [ ] CI chạy backend SQL Server, kiểm tra asset và browser smoke trên mỗi push/PR; lưu TRX, JSON và trace khi lỗi.
- [ ] Luồng giỏ → voucher → đơn → giao → đánh giá → hỗ trợ pass; hồi quy đăng nhập, giỏ, checkout, hủy và phân quyền pass.
- [ ] Chuẩn bị cơ chế mở từng nhóm chức năng Sprint 3 có kiểm thử route/menu/background job và đường quay lại Sprint 2; chưa tăng giới hạn sprint toàn cục khi các nhóm còn chưa đạt.
- [ ] Kiểm chứng health/live, health/ready và log không lộ dữ liệu nhạy cảm trước khi phát hành Sprint 3.

### US-18 và issue #30

**Luồng:** Đơn Sprint 1 → giao hàng Sprint 2 → yêu cầu hỗ trợ Sprint 2 → xử lý admin Sprint 3 → khách theo dõi.

**Phụ thuộc:** Nền Sprint 2 US-23 #26, US-19 #24, US-10 #23; cơ chế mở phạm vi từ #37.

- [ ] Khách gửi yêu cầu từ đơn đã giao; admin mở đúng yêu cầu qua thông báo và trang đơn.
- [ ] Admin chuyển trạng thái hợp lệ, ghi nội dung xử lý; khách thấy trạng thái và nhận thông báo tương ứng.
- [ ] Chặn khách khác đọc yêu cầu; tổng số lượng đang xử lý không vượt số lượng đã mua; thao tác lặp không phát sinh xử lý trùng.
- [ ] Luồng hoàn tiền giữ bằng chứng chuyển tiền thủ công và xác nhận của khách riêng; không tự đánh dấu tiền đã về chỉ từ thao tác admin.

### US-26 và issue #32

**Luồng:** Admin tạo voucher → khách chọn hàng → checkout Sprint 1–2 → đơn và voucher usage → dữ liệu đầu vào báo cáo #33.

**Phụ thuộc:** Checkout US-12 #19, voucher US-01 #10; cơ chế mở phạm vi từ #37.

- [ ] Admin tạo hoặc sửa voucher với ngày, giá trị, giới hạn lượt và điều kiện hợp lệ; khách không truy cập được màn hình quản trị.
- [ ] Voucher vừa tạo áp dụng được ở checkout; tổng tiền và điều kiện được tính lại tại server.
- [ ] Mã hết hạn, chưa bắt đầu, hết lượt, không đủ giá trị hoặc khách đã dùng bị từ chối; hai khách tranh lượt cuối chỉ một người thành công.
- [ ] Đơn lưu voucher và lượt sử dụng nhất quán. Khuyến mãi nâng cao và phân khúc khách hàng tách thành phần tiếp theo, chỉ chọn sau khi luồng cơ bản đạt.

### US-29 và issue #33

**Luồng:** Đơn COD/VNPAY Sprint 2 → trạng thái thanh toán/hoàn tiền → tổng hợp doanh thu Sprint 3 → đối chiếu CSV.

**Phụ thuộc:** Đơn/thu tiền/hoàn tiền Sprint 2 #23; dữ liệu voucher từ #32; cơ chế mở phạm vi từ #37.

- [ ] Admin lọc khoảng ngày, sản phẩm và danh mục; từ chối khoảng ngày không hợp lệ.
- [ ] Đối chiếu tổng với bộ dữ liệu đơn đã thanh toán, hủy, chờ hoàn và đã hoàn; không tính doanh thu sai trạng thái.
- [ ] Khoản COD được ghi nhận đã thanh toán và mốc đối soát tiền về shop hiển thị theo ý nghĩa riêng, không đánh đồng.
- [ ] Xuất CSV giữ số tiền đúng, chống công thức trong chuỗi; khách thường không truy cập được báo cáo.

### US-20 và issue #31

**Luồng:** Đăng nhập Sprint 1 → quản trị tài khoản Sprint 3 → hiệu lực phiên trên checkout và trang admin.

**Phụ thuộc:** Tài khoản Sprint 1 #15; ưu tiên sau bốn mục cốt lõi.

- [ ] Admin thay đổi vai trò hoặc khóa tài khoản; khách bị từ chối truy cập.
- [ ] Tài khoản bị khóa hoặc thu hồi quyền mất quyền ở request tiếp theo; phiên đang mở không giữ đặc quyền cũ.
- [ ] Không khóa hoặc hạ quyền admin cuối cùng; có kiểm thử tài khoản tự thay đổi quyền và hai thao tác cạnh tranh.

### US-28 và issue #34

**Luồng:** Xem/tìm sản phẩm → ghi tương tác → gợi ý → giỏ và checkout hiện có.

**Phụ thuộc:** Danh mục Sprint 1 #13/#14; nền truy vết và phạm vi #37.

- [ ] Gợi ý chỉ gồm điện thoại đang hoạt động, đúng giá/tồn kho; người dùng mới có kết quả dự phòng.
- [ ] Tương tác của người này không làm lộ lịch sử cá nhân của người khác; kiểm tra quyền và giới hạn thu thập.
- [ ] Gợi ý → chi tiết → giỏ → checkout hoạt động; khi dịch vụ gợi ý lỗi, mua sắm cơ bản vẫn dùng được.

### US-30 và issue #35

**Luồng:** Giỏ → gợi ý mua kèm → checkout; giỏ chưa mua → nhắc → khôi phục → checkout.

**Phụ thuộc:** Giỏ Sprint 1 #17; khuyến mãi cơ bản #32; kiểm soát background job #37.

- [ ] Phần A: gợi ý mua kèm đúng danh mục và tồn kho; giá được kiểm tra lại tại checkout.
- [ ] Phần B chỉ bắt đầu sau A: giỏ bỏ quên đủ điều kiện nhận tối đa một nhắc cho cùng mốc; người dùng đã mua không tiếp tục nhận nhắc cũ.
- [ ] Job tắt ở Sprint 2; bật lại không gửi trùng; mở nhắc dẫn về đúng giỏ của chủ sở hữu.

### US-25 và issue #36

**Luồng:** Tư vấn nhu cầu → liên kết sản phẩm → giỏ → checkout hiện có.

**Phụ thuộc:** Catalog và checkout ổn định; chỉ chọn khi có môi trường API thử nghiệm và đủ thời gian.

- [ ] Câu trả lời dẫn đến sản phẩm tồn tại trong catalog, không bịa giá hoặc tồn kho; giá cuối lấy từ trang sản phẩm.
- [ ] API lỗi hoặc hết thời gian có thông báo và đường về tìm kiếm; khách vẫn mua hàng được.
- [ ] Có rate limit, không đưa khóa API vào repository hoặc trình duyệt; kiểm thử provider bằng stub và tách riêng bằng chứng thử thật.

## Quy tắc tích hợp liên tục và phát hành

- Mỗi PR nối một phần chức năng với luồng hiện có, kèm dữ liệu thử và test tại ranh giới hai module; không chỉ demo một màn hình riêng lẻ.
- Liên kết issue trong PR; một thành viên khác review; CI backend SQL Server, đồng bộ asset và browser phải xanh trước khi merge.
- Lưu kết quả của đúng commit, ghi lỗi có bước tái hiện và kiểm thử lại. Issue lỗi chỉ đóng sau khi xác nhận bản sửa; tránh dùng số test của mốc cũ cho thay đổi mới.
- Hiện `Program.cs` và `SprintFeatureService` đều giới hạn tối đa Sprint 2. Đổi riêng `Release:ActiveSprint` sang 3 chưa mở tính năng. Không tăng cả hai giới hạn để vô tình mở đồng loạt AI, SEO, quản trị, khuyến mãi và background job chưa kiểm tra.
- Trước phát hành: kiểm tra migration trên database thử, xác định phạm vi được mở và phương án quay về Sprint 2. Các chức năng Sprint 3 chưa được phát hành trong PR Buổi 7.
- Definition of Done: AC đạt; test hợp lệ/sai/biên/quyền đạt; luồng xuyên suốt đạt; CI xanh; review của thành viên khác; hướng dẫn demo và báo cáo đã cập nhật.

## Kịch bản demo khi hoàn thành Sprint 3

Admin tạo voucher → khách đăng nhập, chọn điện thoại, áp mã và đặt COD → admin xác nhận/giao → khách xác nhận nhận hàng và gửi yêu cầu bảo hành → admin xử lý → khách thấy trạng thái/thông báo → admin đối chiếu đơn, voucher và doanh thu. Thêm nhánh hủy/hoàn để chứng minh báo cáo loại đúng doanh thu không hợp lệ. VNPAY/GHN thật được demo riêng khi sandbox sẵn sàng.
