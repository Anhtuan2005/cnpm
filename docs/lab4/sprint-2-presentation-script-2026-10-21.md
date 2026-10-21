# Kịch bản thuyết trình Sprint Review 2 — Techvora

**Thời lượng:** khoảng 10–12 phút, chưa tính phần hỏi đáp.  
**Phân vai:** Tấn Phát mở đầu và kết luận; Đức Phú trình bày trải nghiệm mua hàng; Anh Tuấn trình bày đơn hàng và hậu mãi; Phong Phú trình bày quản trị và kiến trúc.

## Cần sửa trên slide trước khi trình chiếu

1. Slide 1: đổi `03.10.2026` thành đúng ngày thuyết trình.
2. Slide 3: đổi “Tối đa bốn điện thoại” thành **“Đúng hai điện thoại cùng danh mục”** để khớp backlog và code hiện tại.
3. Slide 9: đổi `103/103 BACKEND TESTS` thành **`115/115`**; đổi `21/21 BROWSER SMOKE` thành **`22/22`**. Kết quả backend 115/115 được chạy lại ngày 21/10/2026; browser smoke 22/22 là lần chạy gần nhất đã lưu ngày 14/10/2026.

## Slide 1 — Sprint Review

**Người nói: Tấn Phát — khoảng 45 giây**

“Em xin chào thầy và các bạn. Nhóm em là nhóm 3, hôm nay nhóm trình bày kết quả Sprint 2 của dự án Techvora, một website thương mại điện tử dành cho các sản phẩm công nghệ.

Nếu Sprint 1 xây dựng nền tảng để khách tìm kiếm, xem chi tiết và mua sản phẩm, thì Sprint 2 hoàn thiện phần quan trọng hơn của vòng đời mua hàng: so sánh trước khi mua, thanh toán, theo dõi đơn, xử lý sau bán và vận hành ở phía quản trị.

Trong phần trình bày, nhóm sẽ đi từ góc nhìn của khách hàng sang góc nhìn của admin, sau đó kết thúc bằng kiến trúc và kết quả kiểm thử.”

**Câu chuyển:** “Trước tiên, em xin tóm tắt phạm vi mà nhóm đã giới hạn cho Sprint 2.”

## Slide 2 — Phạm vi Sprint 2

**Người nói: Tấn Phát — khoảng 1 phút**

“Sprint 2 gồm 12 User Story và được tổ chức thành bốn luồng chính.

Luồng thứ nhất là mua sắm, gồm so sánh sản phẩm và kiểm tra thông tin trước khi mua. Luồng thứ hai là đặt hàng, gồm voucher, phí vận chuyển, COD và VNPAY. Luồng thứ ba là hậu mãi, gồm theo dõi giao hàng, thông báo, đánh giá và yêu cầu đổi trả hoặc bảo hành. Luồng cuối là vận hành, nơi admin quản lý đơn, tồn kho, giao hàng, hoàn tiền và dashboard.

Điểm nhóm đặc biệt chú ý là phạm vi phát hành. Cấu hình `Release.ActiveSprint` đang đặt bằng 2 và trong code cũng giới hạn tối đa ở Sprint 2. Vì vậy các chức năng dự kiến cho Sprint 3 như AI, Build PC, gợi ý cá nhân hóa hoặc báo cáo nâng cao không xuất hiện trong bản demo này.”

**Câu chuyển:** “Sau khi chốt phạm vi, nhóm bắt đầu từ quyết định đầu tiên của khách hàng: chọn sản phẩm nào để mua.”

## Slide 3 — So sánh trước khi mua

**Người nói: Đức Phú — khoảng 1 phút**

“Ở bước so sánh, khách chọn đúng hai điện thoại cùng danh mục. Hệ thống đưa giá, tồn kho và thông số kỹ thuật về cùng một màn hình để khách đối chiếu trực tiếp.

Phần này không chỉ ghép hai thẻ sản phẩm lại với nhau. Backend lấy dữ liệu từ catalog hiện tại, giữ đúng thứ tự khách đã chọn và từ chối sản phẩm không cùng phạm vi so sánh. Nhờ đó kết quả không bị lẫn giữa hai loại sản phẩm không tương đồng.

Giá trị của chức năng này là giảm số lần khách phải chuyển qua lại giữa các trang chi tiết và giúp họ ra quyết định trước khi vào giỏ hàng.”

**Nếu demo:** chọn hai điện thoại cùng danh mục, mở khay so sánh và chỉ nhanh ba vùng giá, tồn kho, thông số.

**Câu chuyển:** “Khi khách đã chọn được sản phẩm phù hợp, luồng tiếp theo là tính đúng số tiền và hoàn tất thanh toán.”

## Slide 4 — Checkout và thanh toán

**Người nói: Đức Phú — khoảng 1 phút 30 giây**

“Tại checkout, tổng tiền được tính lại ở server dựa trên sản phẩm, voucher và phí vận chuyển. Voucher chỉ được áp dụng khi còn hiệu lực, còn lượt và đơn hàng đạt điều kiện. Phí GHN được cập nhật trước khi khách xác nhận nên khách biết số tiền cuối cùng phải trả.

Nhóm hỗ trợ hai phương thức thanh toán. Với COD, đơn được tạo và chuyển sang quy trình xác nhận. Với VNPAY, hệ thống tạo yêu cầu thanh toán trên môi trường Sandbox, sau đó kiểm tra chữ ký, mã đơn, số tiền và trạng thái khi nhận kết quả trả về.

Một trường hợp nhóm xử lý kỹ là callback bị gửi lặp. Cùng một giao dịch chỉ được ghi nhận thanh toán một lần, không trừ tồn kho hoặc đổi trạng thái nhiều lần. Nếu thanh toán đến sau khi đơn đã hủy, hệ thống không tự khôi phục đơn mà chuyển sang luồng cần hoàn tiền.

VNPAY đang dùng môi trường thử nghiệm nên giao dịch demo không phải giao dịch tiền thật. IPN server-to-server khi triển khai thực tế cần một địa chỉ HTTPS công khai.”

**Nếu demo:** áp một voucher, cho thấy tổng tiền thay đổi, sau đó mở lựa chọn COD và VNPAY. Không nhập tài khoản ngân hàng thật trong phần trình bày.

**Câu chuyển:** “Thanh toán tạo ra đơn hàng, nhưng hành động tiếp theo phải phụ thuộc đúng vào trạng thái của đơn.”

## Slide 5 — Vòng đời đơn hàng

**Người nói: Anh Tuấn — khoảng 1 phút 15 giây**

“Đơn hàng được quản lý theo một vòng đời rõ ràng: chờ xác nhận, đã xác nhận, đang giao, đã giao hoặc đã hủy. Mỗi trạng thái quyết định thao tác nào được phép xuất hiện.

Ví dụ, khách chỉ được hủy khi đơn còn đủ điều kiện. Khi hủy thành công, tồn kho được hoàn đúng một lần, kể cả khi người dùng bấm lặp hoặc có hai yêu cầu đến gần như đồng thời. Chức năng mua lại chỉ đưa những sản phẩm còn hợp lệ và còn bán vào giỏ hàng.

Ở phía admin, hệ thống ngăn việc chuyển trạng thái sai thứ tự. Cách làm này giúp giao diện và dữ liệu nghiệp vụ luôn thống nhất, thay vì chỉ đổi nhãn trạng thái trên màn hình.”

**Nếu demo:** mở lịch sử đơn, chỉ nút hủy hoặc mua lại ở một đơn phù hợp, sau đó mở chi tiết tiến trình.

**Câu chuyển:** “Sau khi đơn được tạo, Sprint 2 tiếp tục hỗ trợ khách trong toàn bộ giai đoạn giao hàng và sau khi nhận hàng.”

## Slide 6 — Theo dõi và hậu mãi

**Người nói: Anh Tuấn — khoảng 1 phút 30 giây**

“Phần hậu mãi gồm bốn nhóm chức năng có liên kết với nhau.

Thứ nhất, khách theo dõi tiến trình giao hàng ngay trong chi tiết đơn. Thứ hai, hệ thống thông báo các thay đổi quan trọng và dùng SignalR để cập nhật những sự kiện cần thiết theo thời gian thực.

Thứ ba, sau khi đơn đã giao, khách được gửi một đánh giá cho sản phẩm và có thể đính kèm tối đa bốn ảnh hợp lệ. Admin có thể phản hồi công khai; phản hồi xuất hiện ngay dưới đánh giá để tạo thành một luồng hỗ trợ sau mua rõ ràng.

Thứ tư, khách có thể tạo yêu cầu đổi trả hoặc bảo hành cho sản phẩm đã mua. Hệ thống kiểm tra số lượng yêu cầu không vượt quá số lượng trong đơn, còn admin mở được yêu cầu từ đúng đơn hàng liên quan.

Như vậy, dữ liệu đơn hàng là nguồn chung cho theo dõi, thông báo, đánh giá và hậu mãi; khách không phải nhập lại thông tin mua hàng.”

**Nếu demo:** mở chi tiết đơn đã giao, chỉ tiến trình, khu vực đánh giá và nút tạo yêu cầu hậu mãi.

**Câu chuyển:** “Các thao tác của khách được tập trung lại ở phía admin để cửa hàng có thể xử lý trên một màn hình vận hành.”

## Slide 7 — Màn hình quản trị

**Người nói: Phong Phú — khoảng 1 phút 15 giây**

“Dashboard admin tập trung các thông tin cần xử lý hằng ngày như doanh thu hợp lệ, đơn chờ xác nhận, sản phẩm sắp hết hàng và các cảnh báo vận hành.

Từ khu vực quản trị, admin có thể cập nhật đơn, điều chỉnh tồn kho có lưu lịch sử, tạo hoặc đồng bộ vận đơn GHN, ghi nhận hoàn tiền, xuất hóa đơn và CSV. Số liệu doanh thu loại trừ đơn đã hủy, đang chờ hoàn hoặc đã hoàn tiền để tránh báo cáo sai.

Các trang này được bảo vệ theo quyền. Khách hàng không thể mở route quản trị, còn admin chỉ thấy những hành động phù hợp với trạng thái của đơn. Với tích hợp GHN, code đã xử lý callback trùng và callback đến sai thứ tự; phần gọi dịch vụ thật phụ thuộc cấu hình Sandbox của tài khoản.”

**Nếu demo:** mở dashboard, đơn chờ, lịch sử điều chỉnh kho và chi tiết một đơn có khu vực giao hàng hoặc hoàn tiền.

**Câu chuyển:** “Để các luồng vừa trình bày dùng chung dữ liệu và giữ được tính nhất quán, nhóm tổ chức hệ thống theo kiến trúc sau.”

## Slide 8 — Kiến trúc hệ thống

**Người nói: Phong Phú — khoảng 1 phút 15 giây**

“Hệ thống được xây dựng trên ASP.NET Core 8 và SQL Server.

Razor MVC phụ trách giao diện storefront và admin. Controller nhận request và kiểm tra quyền truy cập. Các service giữ nghiệp vụ như voucher, thanh toán, tồn kho, vòng đời đơn, GHN và hậu mãi. EF Core 8 quản lý dữ liệu, migration và transaction; ASP.NET Identity phụ trách tài khoản và phân quyền.

VNPAY và GHN được tách thành các service tích hợp, còn SignalR hỗ trợ thông báo thời gian thực. Việc tách lớp như vậy giúp phần giao diện không tự quyết định nghiệp vụ, đồng thời cho phép nhóm kiểm thử các tình huống khó như callback lặp, tranh chấp tồn kho và hủy đơn đồng thời.”

**Câu chuyển:** “Cuối cùng, em xin tổng kết trạng thái bàn giao và các bằng chứng kiểm thử của Sprint 2.”

## Slide 9 — Bàn giao Sprint 2

**Người nói: Tấn Phát — khoảng 1 phút 15 giây**

“Đến thời điểm bàn giao, bản chạy mặc định đã mở các route của Sprint 1 và Sprint 2, đồng thời khóa các route từ Sprint 3 trở lên.

Nhóm đã kiểm tra lại bộ backend trên bản build hiện có vào ngày 21 tháng 10: 115 trên 115 test đều pass. Lần browser smoke gần nhất có lưu kết quả là 22 trên 22 test pass. Các ca kiểm thử bao gồm thanh toán lặp, hủy đơn và hoàn kho một lần, voucher còn lượt cuối, điều chỉnh kho đồng thời, callback GHN, quyền truy cập và luồng hậu mãi.

Qua Sprint 2, nhóm đã nối được một vòng đời tương đối hoàn chỉnh: khách so sánh, checkout, thanh toán, theo dõi, đánh giá hoặc gửi yêu cầu sau mua; admin tiếp nhận và vận hành trên cùng một hệ thống.

Phần trình bày của nhóm em đến đây là hết. Nhóm em xin cảm ơn thầy và các bạn đã lắng nghe, và nhóm sẵn sàng trả lời câu hỏi.”

## Thứ tự demo đề xuất nếu còn 3–4 phút

1. Chọn hai điện thoại cùng danh mục và mở trang so sánh.
2. Thêm sản phẩm vào giỏ, nhập voucher và xem phí vận chuyển.
3. Cho thấy COD và VNPAY Sandbox, rồi quay lại lịch sử đơn.
4. Mở một đơn đã giao để chỉ theo dõi, đánh giá và yêu cầu hậu mãi.
5. Đăng nhập admin, mở dashboard, đơn hàng và tồn kho.
6. Thử mở `/BuildPc` hoặc `/AiChat` để chứng minh Sprint 3 đang bị khóa nếu giảng viên hỏi về phạm vi.

## Câu trả lời ngắn cho phần hỏi đáp

### “Vì sao VNPAY không trừ tiền thật?”

“Nhóm đang tích hợp môi trường Sandbox của VNPAY. Sandbox dùng để kiểm tra luồng chuyển trang, chữ ký và trạng thái giao dịch; không phải cổng thanh toán production.”

### “Nếu VNPAY gọi callback hai lần thì sao?”

“Server kiểm tra giao dịch đã được xử lý hay chưa và cập nhật trong transaction, vì vậy cùng một giao dịch chỉ được áp dụng một lần.”

### “Sprint 3 có nằm trong bản demo không?”

“Không. Bản chạy giới hạn `ActiveSprint = 2`; các route Sprint 3 bị trả về 404 và không xuất hiện trong điều hướng.”

### “Vì sao chỉ so sánh hai sản phẩm?”

“Đây là Acceptance Criteria của US-04 và code cũng giới hạn hai ID. Hai sản phẩm giúp bảng thông số dễ đọc và tránh chọn các sản phẩm khác danh mục.”

### “115 test có nghĩa là mọi thứ đã hoàn hảo chưa?”

“Không. 115 test chứng minh các ca tự động hiện có đang pass. Nhóm vẫn tách rõ phần cần tài khoản Sandbox hoặc callback HTTPS công khai để kiểm tra tích hợp ngoài.”
