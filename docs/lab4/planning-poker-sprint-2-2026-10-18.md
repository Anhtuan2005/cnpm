# Kịch bản Planning Poker Sprint 2 — 18/10/2026

**Thời lượng đọc và lật thẻ:** khoảng 12–14 phút. **Người tham gia:** Tuấn, Phát, Đức Phú, Phong Phú. Mỗi người chuẩn bị thẻ 1–5.

Đây là **kịch bản đề xuất để nhóm duyệt ngày mai**, gồm cả phiếu mẫu và điểm chốt mẫu; chưa phải biên bản cuộc họp đã diễn ra. Nếu nhóm bỏ phiếu khác, thay điểm trong lời thoại và bảng tổng kết trước khi ghi vào Sprint Backlog. Story Point là độ phức tạp tương đối của **toàn bộ story**, không phải số ngày làm việc hay trạng thái hoàn thành.

## Nhịp trình bày

| Thời gian | Nội dung |
| --- | --- |
| 00:00–01:00 | Mở đầu, cách lật thẻ |
| 01:00–11:30 | 12 story, khoảng 45–55 giây mỗi story |
| 11:30–13:30 | Đọc lại tổng điểm, phạm vi và việc tiếp theo |

**Chỉ dẫn sân khấu:** trước mỗi câu “Phiếu là…”, bốn người úp thẻ, Phát đếm “ba, hai, một” rồi cùng lật. Lời giải thích ở những story có chênh lệch được viết ngắn để giữ thời lượng. Không đọc to các chỉ dẫn và bảng tổng kết trong lúc diễn.

## Lời thoại

### Mở đầu

**Phát:** “Chào mọi người. Hôm nay nhóm mình làm Planning Poker cho 12 User Story Sprint 2. Tuấn, Đức Phú và Phong Phú sẽ lần lượt giới thiệu phạm vi; cả bốn người chọn thẻ kín rồi cùng lật. Nếu điểm khác nhau, người chọn khác giải thích ngắn, sau đó nhóm chốt điểm.”

**Tuấn:** “Sprint 2 tập trung vào so sánh sản phẩm, voucher, VNPAY, vận hành đơn và kho, giao hàng, đánh giá của khách và phản hồi hỗ trợ sau mua, hậu mãi cùng dashboard. Các chức năng của Sprint 3 như AI, gợi ý cá nhân hóa, bán kèm và báo cáo nâng cao không được cộng vào phần ước lượng hôm nay.”

**Phát:** “Mình bắt đầu từ bước khách chọn điện thoại: so sánh sản phẩm.”

### US-04 — So sánh hai sản phẩm

**Tuấn:** “Khách so sánh giá và thông số của đúng hai điện thoại cùng danh mục. Hệ thống phải lấy đúng dữ liệu catalog và từ chối cặp khác danh mục.”

**Phát:** “Ba, hai, một. Phiếu là 2, 2, 3, 2. Đức Phú chọn 3?”

**Đức Phú:** “Mình tính thêm trường hợp thông số thiếu. Nếu dùng dữ liệu chi tiết sản phẩm sẵn có và giới hạn hai máy, mình đồng ý 2.”

**Phát:** “Chốt **2 điểm**. Sau khi chọn máy, mình chuyển sang thanh toán VNPAY.”

### US-15 — Thanh toán VNPAY

**Đức Phú:** “Khách thanh toán qua VNPAY. Server phải xác minh chữ ký, mã đơn, số tiền và trạng thái; callback lặp không được ghi thanh toán hai lần. Phần Sandbox và IPN cần ghi đúng kết quả đã kiểm tra.”

**Phát:** “Ba, hai, một. Cả bốn cùng chọn 5. Chốt **5 điểm** vì có tích hợp ngoài và nhiều tình huống thanh toán. Tiếp theo là trường hợp khách muốn hủy đơn hoặc mua lại.”

### US-13 — Hủy đơn và mua lại

**Tuấn:** “Khách chỉ hủy đơn đủ điều kiện; tồn kho được hoàn đúng một lần. Mua lại chỉ đưa hàng còn hợp lệ vào giỏ.”

**Phát:** “Phiếu là 4, 4, 3, 4. Đức Phú?”

**Đức Phú:** “Mình chọn 3 vì giao diện khá gọn. Nhưng xử lý hủy lặp và hoàn kho đồng thời làm rủi ro cao hơn.”

**Phát:** “Nhóm chốt **4 điểm**. Để các thao tác này không làm sai số lượng hàng, mình xét phần tồn kho.”

### US-05 — Điều chỉnh tồn kho

**Phong Phú:** “Admin thay đổi tồn kho và xem lịch sử điều chỉnh. Khi khách đặt hàng cùng lúc, số tồn và log phải nhất quán, không bị form cũ ghi đè.”

**Phát:** “Phiếu là 4, 3, 4, 4. Mình chọn 3 vì form đơn giản, nhưng xử lý đồng thời và rollback đáng tính thêm. Chốt **4 điểm**. Từ kho, mình chuyển sang quản lý đơn hàng.”

### US-10 — Quản lý đơn, hoàn tiền và hóa đơn

**Phong Phú:** “Admin đổi trạng thái đơn đúng vòng đời, ghi nhận hoàn tiền, xuất hóa đơn và CSV. Phải ngăn chuyển trạng thái sai và loại đơn hủy hoặc chờ hoàn khỏi doanh thu.”

**Phát:** “Cả bốn cùng chọn 5. Chốt **5 điểm** vì story trải qua nhiều bước vận hành. Khi đơn được xử lý, khách cần theo dõi giao hàng.”

### US-19 — Theo dõi giao hàng và thông báo

**Đức Phú:** “Khách xem được tiến trình giao của đơn mình và chỉ thấy thông báo thuộc tài khoản mình. Trạng thái giao hàng phải khớp với trang đơn.”

**Phát:** “Phiếu là 3, 3, 3, 2. Phong Phú?”

**Phong Phú:** “Mình chọn 2 vì chủ yếu đọc dữ liệu. Nhưng quyền truy cập và đồng bộ trạng thái làm story rộng hơn. Đồng ý 3.”

**Phát:** “Chốt **3 điểm**. Sau khi nhận hàng, khách có thể gửi đánh giá.”

### US-21 — Gửi đánh giá kèm ảnh

**Tuấn:** “Khách có đơn đã giao mới gửi được đánh giá kèm ảnh. Nội dung hợp lệ xuất hiện công khai ngay sau khi gửi.”

**Phát:** “Phiếu là 3, 4, 3, 3. Mình chọn 4 vì upload ảnh có rủi ro, nhưng validation và điều kiện đơn đã giao đủ gói trong story 3. Chốt **3 điểm**. Tiếp theo là yêu cầu đổi trả hoặc bảo hành.”

### US-23 — Yêu cầu đổi trả hoặc bảo hành

**Đức Phú:** “Khách gửi yêu cầu cho hàng đã mua, không vượt số lượng mua kể cả khi gửi đồng thời. Khách xem lại yêu cầu; admin mở được từ đơn liên quan. Màn hình xử lý hậu mãi nâng cao thuộc Sprint 3.”

**Phát:** “Phiếu là 3, 3, 4, 3. Đức Phú?”

**Đức Phú:** “Mình lo phần kiểm soát số lượng khi gửi cùng lúc. Nếu dùng kiểm tra giao dịch trong service hiện có, mình đồng ý 3.”

**Phát:** “Chốt **3 điểm**. Bây giờ mình chuyển sang góc nhìn quản trị: dashboard và thông báo.”

### US-22 — Dashboard và thông báo đơn mới

**Phong Phú:** “Dashboard admin hiển thị số liệu đơn hợp lệ và thông báo đơn mới. Không tính đơn hủy hoặc chờ hoàn tiền thành doanh thu; báo cáo nâng cao để Sprint 3.”

**Phát:** “Cả bốn cùng chọn 3. Chốt **3 điểm**. Tiếp theo là phần tích hợp vận chuyển với GHN.”

### US-17 — Tích hợp GHN

**Đức Phú:** “Admin tạo, đồng bộ, theo dõi và hủy vận đơn GHN. Callback trùng hoặc đến sai thứ tự không được làm lùi trạng thái. Phần gọi GHN thật cần Sandbox, token và Shop ID.”

**Phát:** “Cả bốn cùng chọn 5. Chốt **5 điểm** vì có tích hợp ngoài và vòng đời vận đơn. Mình chuyển sang phản hồi hỗ trợ sau khi khách gửi đánh giá.”

### US-24 — Cửa hàng phản hồi đánh giá

**Tuấn:** “Nhân viên cửa hàng trả lời công khai đánh giá của khách. Khách xem phản hồi ngay dưới nội dung đánh giá trên trang sản phẩm; US-21 vẫn là quyền khách gửi đánh giá và ảnh.”

**Phát:** “Story này thay cho phần admin duyệt/từ chối/xóa đã ghi trong kịch bản cũ, nên điểm 2 trước đây không còn áp dụng. Nhóm cần bỏ phiếu lại cho phạm vi phản hồi của cửa hàng. Story cuối là voucher trong bước checkout.”

### US-01 — Áp voucher

**Đức Phú:** “Checkout áp voucher còn hiệu lực và đủ điều kiện. Mã hết lượt hoặc không hợp lệ phải bị từ chối; khi tranh lượt cuối chỉ một đơn được dùng. Quản trị tạo voucher thuộc Sprint 3.”

**Phát:** “Phiếu là 4, 4, 3, 4. Đức Phú?”

**Đức Phú:** “Mình chọn 3 vì phần nhập mã nhỏ, nhưng kiểm tra điều kiện và lượt dùng đồng thời phức tạp hơn. Đồng ý 4.”

**Phát:** “Chốt **4 điểm**. Mình tổng kết điểm của Sprint 2.”

### Kết thúc

**Phát:** “Bảng điểm cũ có 11 story với tổng đề xuất **41 Story Point**; US-24 cần được ước lượng lại sau khi đổi phạm vi. Điểm phản ánh độ phức tạp tương đối, không phải số ngày và không chứng minh story đã hoàn tất.”

**Tuấn:** “Trước buổi báo cáo, mình sẽ đối chiếu từng story với tiêu chí chấp nhận và kết quả test. Với VNPAY và GHN, nhóm ghi rõ phần đã thử trên Sandbox và phần còn phụ thuộc IPN hoặc cấu hình dịch vụ. Sprint 3 vẫn nằm ngoài demo.”

**Phong Phú:** “Mình sẽ rà soát các luồng quản trị đơn, kho, phản hồi đánh giá và dashboard theo bảng điểm vừa chốt.”

**Đức Phú:** “Mình sẽ kiểm tra lại thanh toán, voucher, hủy đơn, giao hàng và các trường hợp gửi yêu cầu lặp.”

**Phát:** “Mình ghi kết quả nhóm xác nhận vào Sprint Backlog, phân công người phụ trách từng story và giữ riêng các blocker chưa xử lý. Buổi Planning Poker kết thúc.”

## Bảng điểm đề xuất

| Story | Tuấn | Phát | Đức Phú | Phong Phú | Chốt |
| --- | ---: | ---: | ---: | ---: | ---: |
| US-04 | 2 | 2 | 3 | 2 | **2** |
| US-15 | 5 | 5 | 5 | 5 | **5** |
| US-13 | 4 | 4 | 3 | 4 | **4** |
| US-05 | 4 | 3 | 4 | 4 | **4** |
| US-10 | 5 | 5 | 5 | 5 | **5** |
| US-19 | 3 | 3 | 3 | 2 | **3** |
| US-21 | 3 | 4 | 3 | 3 | **3** |
| US-23 | 3 | 3 | 4 | 3 | **3** |
| US-22 | 3 | 3 | 3 | 3 | **3** |
| US-17 | 5 | 5 | 5 | 5 | **5** |
| US-24 | Cần bỏ phiếu lại | Cần bỏ phiếu lại | Cần bỏ phiếu lại | Cần bỏ phiếu lại | **Chưa chốt** |
| US-01 | 4 | 4 | 3 | 4 | **4** |
| **Tổng đã có đề xuất** | | | | | **41 + US-24 chưa chốt** |

Nếu đây là buổi bỏ phiếu thực tế, cả bốn người chọn thẻ theo đánh giá của mình và cập nhật kết quả thật. Không dùng bảng đề xuất này làm bằng chứng đã họp trước ngày 18/10/2026.
