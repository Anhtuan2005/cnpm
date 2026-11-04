# Cài đặt và chạy Techvora 1 0 0

Bản này dùng cho demo và nghiệm thu kỹ thuật Sprint 1–3. Tải gói `Techvora-1.0.0.zip` và `SHA256SUMS.txt` tại [Release v1.0.0](https://github.com/Anhtuan2005/cnpm/releases/tag/v1.0.0). Các dịch vụ VNPAY, GHN, Gemini và SMTP là tùy chọn, cần thông tin xác thực riêng để gọi thật.

## Chạy gói đã build trên Windows

Chuẩn bị **ASP.NET Core Runtime 8**, **SQL Server** đang chạy và PowerShell. Tài khoản kết nối SQL cần quyền tạo database thử. Không cần SDK hoặc Node.js khi chạy gói đã build. Kết nối Internet cần cho lần tải/cài prerequisite; khi đã có đủ phần mềm, có thể trình bày các luồng dùng dữ liệu local theo [phương án dự phòng](demo-script.md).

```powershell
Get-FileHash .\Techvora-1.0.0.zip -Algorithm SHA256
# So sánh với SHA256SUMS.txt vừa tải.
Expand-Archive .\Techvora-1.0.0.zip -DestinationPath .\Techvora-1.0.0
cd .\Techvora-1.0.0
powershell -ExecutionPolicy Bypass -File .\Start-Demo.ps1
```

Script tạo tên database demo mới trên `localhost` bằng Windows Authentication, migrate và seed dữ liệu. Mở [http://localhost:5009](http://localhost:5009). Nếu dùng SQL instance khác:

```powershell
.\Start-Demo.ps1 -ConnectionString 'Server=localhost\SQLEXPRESS;Database=TechvoraReleaseDemo;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False' -Port 5009 -Sprint 3
```

Chỉ đưa database demo riêng vào lệnh này: script bật `Database:MigrateOnStartup` và `Demo:Enabled`. Dùng lại cùng connection string sẽ giữ dữ liệu đã demo; dùng database mới để chạy lại smoke test đầy đủ. Ctrl+C dừng ứng dụng; database và file upload mới vẫn còn để xem lại.

| Tài khoản thử | Mật khẩu thử | Vai trò |
| --- | --- | --- |
| admin@shop.vn | Admin@123 | Quản trị |
| khachhang1@shop.vn | User@123 | Khách hàng có đơn mẫu |
| khachhang2@shop.vn | User@123 | Khách hàng thứ hai |

Script dùng môi trường `Demo`, email tắt và chatbot từ catalog local; không nạp User Secrets của môi trường Development. Chọn `-Sprint 2` để kiểm tra hồi quy phạm vi cũ. Gói không chứa database, API key, khóa Data Protection, log hoặc upload từ máy build. Các file đó được tạo mới khi chạy.

## Chạy từ mã nguồn

Chuẩn bị **.NET SDK 8 hoặc SDK hỗ trợ net8.0**, SQL Server và Git. Node.js chỉ cần khi sửa/build CSS/JS hoặc chạy browser test; asset đã build được lưu trong repository.

```powershell
git clone --branch v1.0.0 https://github.com/Anhtuan2005/cnpm.git
cd cnpm
dotnet restore EcommerceApp.sln
$env:ASPNETCORE_ENVIRONMENT = 'Demo'
$env:DOTNET_ENVIRONMENT = 'Demo'
$env:ConnectionStrings__DefaultConnection = 'Server=localhost;Database=TechvoraSourceDemo;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False'
$env:Database__MigrateOnStartup = 'true'
$env:Demo__Enabled = 'true'
$env:Email__Enabled = 'false'
$env:Release__ActiveSprint = '3'
dotnet run --project src/backend/EcommerceApp.csproj --no-launch-profile --urls http://127.0.0.1:5009
```

Để build lại gói từ đúng tag:

```powershell
.\scripts\release\Build-Package.ps1 -Version 1.0.0 -OutputDirectory artifacts\release-rebuild
.\scripts\release\Test-Package.ps1 -Package artifacts\release-rebuild\Techvora-1.0.0.zip
```

Mỗi lần dùng thư mục output mới; script từ chối ghi đè gói có sẵn. `manifest.json` lưu source commit, trạng thái có thay đổi khi build và SHA256 từng file; `SHA256SUMS.txt` ghi hash cả ZIP. Build để phát hành từ checkout sạch. ZIP có thể khác hash giữa các lần build do timestamp và metadata; kiểm tra hash của đúng asset đã tải.

## Linux và container

Gói framework-dependent có native assets SkiaSharp cho Windows, macOS và Linux; CI xác nhận trên Ubuntu 24.04. Với SQL Server từ xa, đặt các biến môi trường như phần trên, chuyển vào `app/` và chạy `dotnet EcommerceApp.dll --urls http://127.0.0.1:5009`. Không bật demo ở Production.

Repository giữ `Dockerfile` và `compose.demo.yml` như phương án bổ sung. Docker chưa được chạy trên máy thực hiện Lab 6; CI kiểm tra gói publish trên Linux, không thay cho kiểm tra image Docker. Nếu máy có Docker:

```powershell
$env:DEMO_SQL_PASSWORD = '<mật khẩu mạnh cho SQL demo>'
docker compose -f compose.demo.yml up --build
```

Mở `http://localhost:8080`. Dùng `docker compose -f compose.demo.yml down` để dừng; không dùng tùy chọn xóa volume khi còn cần dữ liệu demo.

## Kiểm tra sau cài đặt

1. `/health/live` và `/health/ready` trả 200 ở Sprint 3; ready xác nhận kết nối database.
2. Trang chủ/catalog có dữ liệu; CSS/JS và ảnh tĩnh hiển thị.
3. Khách đăng nhập, mở mua ngay, xem phụ kiện và tổng tiền; admin mở báo cáo, hậu mãi và voucher.
4. Kiểm tra `/BuildPc` vẫn trả 404 vì nằm ngoài bản phát hành Sprint 3.

Nếu không kết nối SQL, kiểm tra tên instance, dịch vụ SQL, quyền tạo database và connection string. Nếu thiếu native library, dùng đầy đủ thư mục `runtimes/` từ ZIP; không chỉ chép riêng DLL ứng dụng. Nếu cổng 5009 đã có ứng dụng khác, đặt `-Port 5010`. Danh sách huyện/xã dùng API ngoài; khi Internet lỗi, theo phương án dự phòng ở kịch bản demo.
