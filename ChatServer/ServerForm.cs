using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ChatServer
{
    public class Form1 : Form
    {
        // ====== Các control trên giao diện ======
        TextBox txtLog;
        Button btnStart, btnStop, btnViewHistory;
        FlowLayoutPanel pnlClients;

        // ====== Các biến mạng ======
        TcpListener listener;                     
        List<TcpClient> clients = new List<TcpClient>();     
        Dictionary<TcpClient, string> clientNames = new Dictionary<TcpClient, string>(); 

        bool isRunning = false;                  

        public Form1()
        {
            // ============================ FORM CHÍNH ============================
            this.Text = "Zalo Chat Server";
            this.Size = new Size(800, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorTranslator.FromHtml("#F5F7FA");
            this.Font = new Font("Segoe UI", 10);

            // ============================ HEADER ============================
            Panel header = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ColorTranslator.FromHtml("#0091FF")
            };
            Label lblTitle = new Label()
            {
                Text = "💻 Zalo Chat Server",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);
            this.Controls.Add(header);

            // ============================ PANEL NÚT START/STOP ============================
            Panel buttonPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.WhiteSmoke
            };

            // Nút tạo bằng hàm CreateButton
            btnStart = CreateButton("▶ Start Server", ColorTranslator.FromHtml("#28a745"));
            btnStop = CreateButton("■ Stop Server", ColorTranslator.FromHtml("#dc3545"));
            btnViewHistory = CreateButton("🕓 View History", ColorTranslator.FromHtml("#007bff"));

            btnStop.Enabled = false;     // Chưa chạy server → tắt Stop

            // Vị trí nút
            btnStart.Location = new Point(30, 15);
            btnStop.Location = new Point(170, 15);
            btnViewHistory.Location = new Point(310, 15);

            // Sự kiện click
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnViewHistory.Click += BtnViewHistory_Click;

            buttonPanel.Controls.AddRange(new Control[] { btnStart, btnStop, btnViewHistory });
            this.Controls.Add(buttonPanel);

            // ============================ LOG SERVER ============================
            GroupBox grpLog = new GroupBox()
            {
                Text = "Server Logs",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(20, 140),
                Size = new Size(500, 320),
                BackColor = Color.White
            };

            txtLog = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.None
            };

            grpLog.Controls.Add(txtLog);
            this.Controls.Add(grpLog);

            // ============================ DANH SÁCH CLIENT ============================
            GroupBox grpClients = new GroupBox()
            {
                Text = "Connected Users",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(540, 140),
                Size = new Size(220, 320),
                BackColor = Color.White
            };
            pnlClients = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            grpClients.Controls.Add(pnlClients);
            this.Controls.Add(grpClients);
        }

        // ===================================================================
        // TẠO MỘT BUTTON CÓ STYLE ĐẸP – TÁCH RIÊNG GIÚP CODE GỌN GÀNG
        // ===================================================================
        private Button CreateButton(string text, Color color)
        {
            return new Button()
            {
                Text = text,
                Size = new Size(120, 35),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        // ===================================================================
        // BẤM START SERVER
        // ===================================================================
        private void BtnStart_Click(object sender, EventArgs e)
        {
            Thread serverThread = new Thread(StartServer); // Server chạy trên luồng riêng
            serverThread.IsBackground = true;
            serverThread.Start();

            AppendLog("✅ Server started on port 5000...");
            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        // ===================================================================
        // BẤM STOP SERVER
        // ===================================================================
        private void BtnStop_Click(object sender, EventArgs e)
        {
            isRunning = false;      // Ngừng vòng lặp
            listener?.Stop();       // Tắt TCP listener

            // Đóng toàn bộ client
            lock (clients)
            {
                foreach (var c in clients) c.Close();
                clients.Clear();
                clientNames.Clear();
            }

            pnlClients.Controls.Clear();
            AppendLog("🛑 Server stopped.");

            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        // ===================================================================
        // XEM LỊCH SỬ CHAT (đọc file history.txt)
        // ===================================================================
        private void BtnViewHistory_Click(object sender, EventArgs e)
        {
            string path = "history.txt";

            if (!System.IO.File.Exists(path))
            {
                MessageBox.Show("No chat history found.", "History");
                return;
            }

            string history = System.IO.File.ReadAllText(path, Encoding.UTF8);

            // Hiển thị lịch sử trong cửa sổ mới
            Form f = new Form()
            {
                Text = "Chat History",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };
            TextBox txt = new TextBox()
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                Text = history
            };
            f.Controls.Add(txt);
            f.ShowDialog();
        }

        // ===================================================================
        // BẮT ĐẦU SERVER – LẮNG NGHE CLIENT MỚI
        // ===================================================================
        void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            isRunning = true;

            while (isRunning)
            {
                try
                {
                    // Chấp nhận client mới
                    TcpClient client = listener.AcceptTcpClient();
                    lock (clients) clients.Add(client);

                    AppendLog("🔌 A new client connected.");

                    // Mỗi client chạy trên 1 thread riêng
                    Thread t = new Thread(HandleClient);
                    t.IsBackground = true;
                    t.Start(client);
                }
                catch { break; }
            }
        }

        // ===================================================================
        // ✅ HÀM HỖ TRỢ ĐỌC ĐẦY ĐỦ SỐ BYTE YÊU CẦU (LPP)
        // ===================================================================
        int ReadAll(NetworkStream stream, byte[] buffer, int offset, int size)
        {
            int totalRead = 0;
            while (totalRead < size)
            {
                // Thử đọc phần còn lại
                int read = stream.Read(buffer, offset + totalRead, size - totalRead);
                if (read == 0) return 0; // Kết nối bị đóng
                totalRead += read;
            }
            return totalRead;
        }

        // ===================================================================
        // ✅ HÀM HỖ TRỢ GỬI DỮ LIỆU CÓ TIỀN TỐ ĐỘ DÀI (LPP)
        // ===================================================================
        void SendWithLengthPrefix(NetworkStream s, byte[] data)
        {
            if (data == null || data.Length == 0) return;
            // 1. Gửi 4 byte độ dài
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            s.Write(lengthBytes, 0, 4); 
            // 2. Gửi dữ liệu
            s.Write(data, 0, data.Length); 
        }

        // ===================================================================
        // XỬ LÝ MỘT CLIENT: ĐỌC TIN BẰNG LPP
        // ===================================================================
        void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            byte[] lengthBuffer = new byte[4]; // Buffer 4 bytes cho Length Prefix

            try
            {
                // Vòng lặp chính đọc Length Prefix (4 bytes)
                while (ReadAll(stream, lengthBuffer, 0, 4) > 0)
                {
                    // Chuyển 4 bytes thành kích thước gói tin
                    int messageSize = BitConverter.ToInt32(lengthBuffer, 0);

                    // Kiểm tra độ lớn hợp lệ (ví dụ: không quá 100MB cho buffer)
                    if (messageSize <= 0 || messageSize > 100 * 1024 * 1024) continue; 

                    // Đọc toàn bộ gói tin/header theo kích thước đã xác định
                    byte[] messageBuffer = new byte[messageSize];
                    if (ReadAll(stream, messageBuffer, 0, messageSize) == 0) break; // Lỗi đọc nội dung

                    string message = Encoding.UTF8.GetString(messageBuffer);

                    // ===================================================================
                    // NHẬN FILE (FILE|sender|target|filename|size)
                    // ===================================================================
                    if (message.StartsWith("FILE|"))
                    {
                        string[] parts = message.Split('|');

                        string senderName = parts[1];
                        string target = parts[2];
                        string fileName = parts[3];
                        int fileSize = int.Parse(parts[4]);

                        // Nhận dữ liệu file theo size gửi từ client (Đã dùng ReadAll)
                        byte[] fileBuffer = new byte[fileSize];
                        if (ReadAll(stream, fileBuffer, 0, fileSize) == 0) break;

                        AppendLog($"📎 {senderName} gửi file '{fileName}' đến {target}");

                        // Gửi file đến tất cả
                        if (target.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                        {
                            BroadcastFile(message, fileBuffer, client);
                        }
                        else
                        {
                            // Gửi riêng
                            TcpClient targetClient =
                                clientNames.FirstOrDefault(x => x.Value.Equals(target, StringComparison.OrdinalIgnoreCase)).Key;

                            if (targetClient != null)
                                SendFileToClient(message, fileBuffer, targetClient);
                            else
                                SendToClient($"⚠️ User '{target}' không tồn tại.", client);
                        }
                        continue;
                    }

                    // ===================================================================
                    // CLIENT GỬI TÊN (NAME:xxx) → CHECK TRÙNG
                    // ===================================================================
                    if (message.StartsWith("NAME:"))
                    {
                        string name = message.Substring(5).Trim();

                        lock (clients)
                        {
                            if (clientNames.Any(x => x.Value.Equals(name, StringComparison.OrdinalIgnoreCase)))
                            {
                                SendToClient("⚠️ Name already in use!", client);
                                continue;
                            }

                            clientNames[client] = name;
                        }

                        AppendLog($"👤 {name} connected.");
                        Broadcast($"{name} joined the chat.", client);
                        UpdateClientList();
                        continue;
                    }

                    // Nếu không phải file + không phải tên → xử lý tin nhắn
                    ProcessMessage(message, client);
                }
            }
            catch
            {
                // Nếu client bị mất kết nối (lỗi đọc)
                if (clientNames.ContainsKey(client))
                {
                    string name = clientNames[client];
                    AppendLog($"❌ {name} disconnected.");
                    Broadcast($"{name} left the chat.", client);

                    lock (clients)
                    {
                        clients.Remove(client);
                        clientNames.Remove(client);
                    }

                    UpdateClientList();
                }
            }
        }

        // ===================================================================
        // XỬ LÝ TIN NHẮN BÌNH THƯỜNG + PRIVATE CHAT
        // ===================================================================
        void ProcessMessage(string message, TcpClient sender)
        {
            string senderName = clientNames.ContainsKey(sender) ? clientNames[sender] : "Unknown";

            // ===== PRIVATE CHAT (@user:message) =====
            if (message.StartsWith("@"))
            {
                int colonIdx = message.IndexOf(':');

                if (colonIdx > 1)
                {
                    string target = message.Substring(1, colonIdx - 1);
                    string content = message.Substring(colonIdx + 1).Trim();

                    TcpClient targetClient =
                        clientNames.FirstOrDefault(x => x.Value.Equals(target, StringComparison.OrdinalIgnoreCase)).Key;

                    if (targetClient != null)
                    {
                        string msg = $"[Private] {senderName} → {target}: {content}";
                        SendToClient(msg, targetClient);
                        SendToClient(msg, sender);
                        AppendLog(msg);
                    }
                    else
                    {
                        SendToClient($"⚠️ User '{target}' not found.", sender);
                    }
                    return;
                }
            }

            // ===== NGƯỜI GỬI → TIN NHẮN PUBLIC =====
            string normalMsg = $"{senderName}: {message}";
            AppendLog(normalMsg);
            Broadcast(normalMsg, sender);
        }

        // ===================================================================
        // GỬI TIN CHO TẤT CẢ TRỪ NGƯỜI GỬI (Đã dùng LPP)
        // ===================================================================
        void Broadcast(string message, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            lock (clients)
            {
                foreach (var c in clients)
                {
                    if (c != sender)
                    {
                        try { SendWithLengthPrefix(c.GetStream(), data); } 
                        catch 
                        { 
                            // Gửi thất bại → Đóng kết nối để luồng HandleClient tự dọn dẹp.
                            c.Close(); 
                        }
                    }
                }
            }
        }

        // ===================================================================
        // GỬI FILE CHO TẤT CẢ (Đã dùng LPP cho HEADER và loại bỏ Thread.Sleep)
        // ===================================================================
        void BroadcastFile(string header, byte[] fileBytes, TcpClient sender)
        {
            byte[] headerBytes = Encoding.UTF8.GetBytes(header);

            lock (clients)
            {
                foreach (var c in clients)
                {
                    if (c != sender)
                    {
                        try
                        {
                            SendWithLengthPrefix(c.GetStream(), headerBytes); // Gửi HEADER bằng LPP
                            c.GetStream().Write(fileBytes, 0, fileBytes.Length); // Gửi DATA
                        }
                        catch 
                        { 
                            // Gửi thất bại → Đóng kết nối để luồng HandleClient tự dọn dọn.
                            c.Close(); 
                        }
                    }
                }
            }
        }

        // ===================================================================
        // GỬI FILE RIÊNG CHO 1 CLIENT (Đã dùng LPP cho HEADER và loại bỏ Thread.Sleep)
        // ===================================================================
        void SendFileToClient(string header, byte[] fileBytes, TcpClient client)
        {
            try
            {
                byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                SendWithLengthPrefix(client.GetStream(), headerBytes); // Gửi HEADER bằng LPP
                client.GetStream().Write(fileBytes, 0, fileBytes.Length); // Gửi DATA
            }
            catch 
            { 
                // Gửi thất bại → Đóng kết nối để luồng HandleClient tự dọn dọn.
                client.Close();
            }
        }

        // ===================================================================
        // GỬI TIN NHẮN RIÊNG CHO 1 CLIENT (Đã dùng LPP)
        // ===================================================================
        void SendToClient(string message, TcpClient client)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                SendWithLengthPrefix(client.GetStream(), data);
            }
            catch 
            { 
                // Gửi thất bại → Đóng kết nối để luồng HandleClient tự dọn dọn.
                client.Close(); 
            }
        }

        // ===================================================================
        // GHI LOG + LƯU VÀO FILE history.txt
        // ===================================================================
        void AppendLog(string msg)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendLog(msg)));
                return;
            }

            txtLog.AppendText(msg + Environment.NewLine);
            txtLog.ScrollToCaret();
            SaveToHistory(msg);
        }

        // Lưu log vào history.txt
        void SaveToHistory(string msg)
        {
            string path = "history.txt";
            string line = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {msg}";

            try { System.IO.File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8); }
            catch { }
        }

        // ===================================================================
        // CẬP NHẬT DANH SÁCH CLIENT TRÊN GIAO DIỆN
        // ===================================================================
        void UpdateClientList()
        {
            if (pnlClients.InvokeRequired)
            {
                pnlClients.Invoke(new Action(UpdateClientList));
                return;
            }

            pnlClients.Controls.Clear();

            lock (clients)
            {
                foreach (var name in clientNames.Values)
                {
                    // Card hiển thị tên client
                    Panel card = new Panel()
                    {
                        Width = 180,
                        Height = 50,
                        BackColor = ColorTranslator.FromHtml("#E6F3FF"),
                        Margin = new Padding(5),
                        Padding = new Padding(8),
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    Label lbl = new Label()
                    {
                        Text = name,
                        Location = new Point(10, 15),
                        AutoSize = true,
                        Font = new Font("Segoe UI", 10, FontStyle.Bold),
                        ForeColor = ColorTranslator.FromHtml("#007BFF")
                    };

                    card.Controls.Add(lbl);
                    pnlClients.Controls.Add(card);
                }
            }
        }
    }
}