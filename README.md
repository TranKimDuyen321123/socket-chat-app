🚀 Hướng Dẫn Vận Hành Zalo Chat (Docker & Local)
=======================================================

Dự án này gồm 2 phần:
1.  **ChatServer & Database**: Nên chạy bằng **Docker** để tự động thiết lập môi trường và CSDL SQL Server.
2.  **ChatClient**: Là ứng dụng Windows Desktop (WinForms), phải chạy trực tiếp trên máy tính (**không chạy trong Docker**).

---

## CÁCH 1: Chạy Bằng Docker (Khuyên Dùng)
Cách này giúp bạn không cần cài đặt SQL Server thủ công.

### Bước 1: Khởi động Server & Database
Mở Terminal tại thư mục gốc của dự án và chạy lệnh:

```powershell
docker-compose up --build
```

*   Lệnh này sẽ tải SQL Server, tạo Database và khởi chạy Chat Server.
*   Chờ đến khi thấy thông báo **"Application started. Press Ctrl+C to shut down."** hoặc **Server đang lắng nghe tại port 5000**.

### Bước 2: Chạy Client (Ứng dụng Chat)
Vì Client là ứng dụng giao diện Windows, bạn cần mở một Terminal **mới** (giữ Terminal Docker đang chạy) và gõ:

```powershell
dotnet run --project ChatClient
```

*   Bạn có thể mở nhiều cửa sổ Terminal và chạy lệnh này nhiều lần để tạo nhiều người dùng chat với nhau.

---

## CÁCH 2: Chạy Thủ Công (Local - Không dùng Docker)
Dùng cách này nếu bạn không cài Docker và đã có sẵn SQL Server cài trên máy.

### Bước 1: Cấu hình Database
*   Mở file `ChatServer/appsettings.json` (nếu chưa có thì tạo mới hoặc sửa trong `Program.cs`).
*   Đảm bảo `ConnectionStrings` trỏ đúng tới SQL Server trên máy bạn.

### Bước 2: Chạy Server
```powershell
dotnet run --project ChatServer
```

### Bước 3: Chạy Client
```powershell
dotnet run --project ChatClient
```

---

## 🛠 Các Lệnh Thường Dùng

| Tác vụ | Lệnh (PowerShell / CMD) |
| :--- | :--- |
| **Build Code** | `dotnet build` |
| **Chạy Docker** | `docker-compose up --build` |
| **Tắt Docker** | `docker-compose down` |
| **Chạy Client** | `dotnet run --project ChatClient` |

## ⚠️ Lưu Ý Quan Trọng
*   **ChatClient** là ứng dụng **Windows Forms**, nên nó **không thể chạy bên trong Docker Linux Container**. Đó là lý do bạn chỉ chạy Server bằng Docker, còn Client thì chạy lệnh `dotnet run` ở ngoài.
*   Server chạy qua Docker sẽ map port `5000` ra máy chủ (localhost), nên Client kết nối tới `127.0.0.1:5000` vẫn hoạt động bình thường.
