🚀 Hướng Dẫn Vận Hành Ứng Dụng Zalo Chat Socket
Ứng dụng này bao gồm hai dự án: ChatServer và ChatClient.

I. Chuẩn bị (Build)
Bạn cần Build mã nguồn trước khi chạy. Lệnh này được thực hiện trong Terminal của VS Code, tại thư mục gốc chứa file .sln.

Sử dụng lệnh sau để khôi phục các gói NuGet và biên dịch mã nguồn:

```powershell
# Sử dụng đường dẫn tuyệt đối (Full Path) cho lệnh dotnet
& "C:\Program Files\dotnet\dotnet.exe" build
```
(Nếu bạn thấy thông báo "Build succeeded" (Build thành công), bạn có thể chuyển sang Bước II.)

II. Khởi chạy Ứng dụng (Run)
Để chạy Server và Client song song, bạn cần mở các Terminal riêng biệt cho mỗi ứng dụng.

1. Khởi động Server (Terminal Bắt buộc)
   Server phải được khởi động đầu tiên.

   Mở Terminal mới (Terminal 1).

   Chạy Server bằng lệnh:

   ```powershell
   & "C:\Program Files\dotnet\dotnet.exe" run --project ChatServer
   ```
   Cửa sổ 💻 Zalo Chat Server sẽ hiện ra. Nhấn nút ▶ Start Server.

   Giữ cửa sổ này mở.

2. Khởi động Client (Terminal 2, 3,...)
   Sau khi Server hoạt động, bạn có thể khởi chạy Client.

   Mở Terminal mới (Terminal 2).

   Chạy Client bằng lệnh:

   ```powershell
   & "C:\Program Files\dotnet\dotnet.exe" run --project ChatClient
   ```
   Cửa sổ 💬 Zalo Chat Client sẽ hiện ra. Nhập tên và nhấn Kết nối.

   **Khởi động Client Thứ Hai (Terminal 3)**
   Để kiểm tra chức năng chat, bạn cần ít nhất hai Client. Lặp lại bước 2 trong một Terminal thứ ba, sử dụng một tên người dùng khác.

III. Các Tính Năng Mới: Chat Riêng và Chat Nhóm

Ứng dụng hỗ trợ các chế độ chat sau:

1.  **Chat Chung (Public Chat)**
    *   **Cách dùng:** Chọn chế độ "Public" từ danh sách. Đây là chế độ mặc định.
    *   Mọi tin nhắn bạn gửi sẽ được gửi đến tất cả mọi người trong phòng chat.

2.  **Chat Riêng (Private Chat)**
    *   **Cách dùng:**
        1.  Chọn chế độ "Private" từ danh sách.
        2.  Nhập tên chính xác của người bạn muốn gửi tin vào ô nhập liệu bên cạnh.
        3.  Nhập tin nhắn và gửi.
    *   Tin nhắn sẽ được định dạng là `[Tôi → NgườiNhận]: Nội dung` ở phía bạn và chỉ người nhận mới thấy.

3.  **Chat Nhóm (Group Chat)**
    *   **Cách dùng:**
        1.  **Tham gia nhóm:**
            *   Chọn chế độ "Group".
            *   Nhập tên nhóm bạn muốn tham gia vào ô nhập liệu (ví dụ: `dev_team`, `gaming`).
            *   Nhấn nút "Tham gia". Server sẽ xác nhận bạn đã vào nhóm.
            *   Các thành viên khác có thể tham gia cùng nhóm bằng cách làm tương tự.
        2.  **Gửi tin vào nhóm:**
            *   Sau khi đã tham gia, đảm bảo chế độ "Group" và tên nhóm vẫn còn trong ô.
            *   Nhập tin nhắn và gửi.
            *   Tin nhắn sẽ được gửi đến tất cả các thành viên đang online trong nhóm đó.

IV. Gửi File
*   Việc gửi file hiện tại hỗ trợ chế độ **Public** và **Private**.
*   Để gửi riêng cho ai đó, hãy chọn chế độ "Private" và nhập tên người nhận trước khi bấm nút "📎".
*   Để gửi cho tất cả mọi người, chọn chế độ "Public".
