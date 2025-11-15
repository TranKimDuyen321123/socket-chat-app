🚀 Hướng Dẫn Vận Hành Ứng Dụng Zalo Chat Socket
Ứng dụng này bao gồm hai dự án: ChatServer và ChatClient.

I. Chuẩn bị (Build)
Bạn cần Build mã nguồn trước khi chạy. Lệnh này được thực hiện trong Terminal của VS Code, tại thư mục gốc chứa file .sln.

Sử dụng lệnh sau để khôi phục các gói NuGet và biên dịch mã nguồn:

PowerShell

# Sử dụng đường dẫn tuyệt đối (Full Path) cho lệnh dotnet

& "C:\Program Files\dotnet\dotnet.exe" build
(Nếu bạn thấy thông báo "Build succeeded" (Build thành công), bạn có thể chuyển sang Bước II.)

II. Khởi chạy Ứng dụng (Run)
Để chạy Server và Client song song, bạn cần mở các Terminal riêng biệt cho mỗi ứng dụng.

1. Khởi động Server (Terminal Bắt buộc)
   Server phải được khởi động đầu tiên.

Mở Terminal mới (Terminal 1).

Chạy Server bằng lệnh:

Đoạn mã

& "C:\Program Files\dotnet\dotnet.exe" run --project ChatServer
Cửa sổ 💻 Zalo Chat Server sẽ hiện ra. Nhấn nút ▶ Start Server.

Giữ cửa sổ này mở.

2. Khởi động Client (Terminal 2, 3,...)
   Sau khi Server hoạt động, bạn có thể khởi chạy Client.

Mở Terminal mới (Terminal 2).

Chạy Client bằng lệnh:

PowerShell

& "C:\Program Files\dotnet\dotnet.exe" run --project ChatClient
Cửa sổ 💬 Zalo Chat Client sẽ hiện ra. Nhập tên và nhấn Kết nối.

Khởi động Client Thứ Hai (Terminal 3)
Để kiểm tra chức năng chat và gửi file, bạn cần ít nhất hai Client. Lặp lại bước 2 trong một Terminal thứ ba, sử dụng một tên người dùng khác.
