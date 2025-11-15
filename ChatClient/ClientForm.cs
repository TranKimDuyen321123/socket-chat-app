using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ChatClient
{
    public class Form1 : Form
    {
        // =============================
        // ✅ KHAI BÁO CONTROL GIAO DIỆN
        // =============================
        TextBox txtLog, txtMessage, txtName;
        Button btnSend, btnConnect, btnAttach;
        Panel headerPanel, footerPanel;

        // =============================
        // ✅ BIẾN DÙNG CHO KẾT NỐI
        // =============================
        TcpClient client;
        NetworkStream stream;
        Thread receiveThread;
        bool isConnected = false;

        public Form1()
        {
            // =============================
            // ✅ CẤU HÌNH FORM
            // =============================
            this.Text = "Zalo Chat Client";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorTranslator.FromHtml("#F5F7FA");
            this.Font = new Font("Segoe UI", 10);

            // =============================
            // ✅ HEADER (tên, nút connect)
            // =============================
            headerPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = ColorTranslator.FromHtml("#0091FF"),
                Padding = new Padding(20, 10, 20, 10)
            };

            Label lblTitle = new Label()
            {
                Text = "💬 Zalo Chat Client",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 20)
            };

            txtName = new TextBox()
            {
                PlaceholderText = "Nhập tên của bạn...",
                Width = 140,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Location = new Point(300, 20)
            };

            btnConnect = new Button()
            {
                Text = "Kết nối",
                Size = new Size(90, 30),
                BackColor = Color.White,
                ForeColor = ColorTranslator.FromHtml("#0091FF"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(450, 20)
            };
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Click += BtnConnect_Click;

            headerPanel.Controls.AddRange(new Control[] { lblTitle, txtName, btnConnect });

            // =============================
            // ✅ LOG CHAT (hiển thị tin nhắn)
            // =============================
            txtLog = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            // =============================
            // ✅ FOOTER (nhập tin + gửi + gửi file)
            // =============================
            footerPanel = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.WhiteSmoke
            };

            txtMessage = new TextBox()
            {
                PlaceholderText = "Nhập tin nhắn...",
                Width = 310,
                Height = 30,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Location = new Point(20, 20)
            };

            btnSend = new Button()
            {
                Text = "Gửi",
                Size = new Size(70, 30),
                BackColor = ColorTranslator.FromHtml("#0091FF"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(340, 20)
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += BtnSend_Click;

            btnAttach = new Button()
            {
                Text = "📎 File",
                Size = new Size(70, 30),
                BackColor = ColorTranslator.FromHtml("#28a745"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(420, 20)
            };
            btnAttach.FlatAppearance.BorderSize = 0;
            btnAttach.Click += BtnAttach_Click;

            footerPanel.Controls.AddRange(new Control[] { txtMessage, btnSend, btnAttach });

            // =============================
            // ✅ ADD CONTROL VÀO FORM
            // =============================
            this.Controls.AddRange(new Control[] { txtLog, headerPanel, footerPanel });
        }
        
        // ===================================================================
        // ✅ HÀM HỖ TRỢ ĐỌC ĐẦY ĐỦ SỐ BYTE YÊU CẦU (LPP)
        // ===================================================================
        int ReadAll(NetworkStream stream, byte[] buffer, int offset, int size)
        {
            int totalRead = 0;
            while (totalRead < size)
            {
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

        // =====================================================
        // ✅ SỰ KIỆN NHẤN NÚT CONNECT → GỬI TÊN LÊN SERVER
        // =====================================================
        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (isConnected) { MessageBox.Show("Bạn đã kết nối rồi!"); return; }
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Vui lòng nhập tên."); return; }

            try
            {
                client = new TcpClient("127.0.0.1", 5000);
                stream = client.GetStream();

                // Gửi tên lên server bằng LPP
                SendMessage($"NAME:{txtName.Text}");

                receiveThread = new Thread(ReceiveMessages) { IsBackground = true };
                receiveThread.Start();

                AppendChat("✅ Đã kết nối đến server.");
                isConnected = true;

                btnConnect.Enabled = false;
                txtName.ReadOnly = true;
            }
            catch
            {
                MessageBox.Show("Không thể kết nối server.");
            }
        }

        // =====================================================
        // ✅ NHẤN GỬI → GỬI TIN VĂN BẢN
        // =====================================================
        private void BtnSend_Click(object sender, EventArgs e)
        {
            if (stream == null) return;

            string text = txtMessage.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                SendMessage(text);
                txtMessage.Clear();
            }
        }

        // =====================================================
        // ✅ NHẤN "FILE" → GỬI FILE (Đã dùng LPP cho HEADER và loại bỏ Thread.Sleep)
        // =====================================================
        private void BtnAttach_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Bạn chưa kết nối server!");
                return;
            }

            using OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string targetName = PromptForTarget("Nhập tên người nhận (Mặc định là 'ALL' để gửi công khai):", "Gửi File");
                if (targetName == null) return; 
                if (string.IsNullOrWhiteSpace(targetName)) targetName = "ALL";
                
                string filePath = ofd.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                // HEADER gửi trước để server biết dung lượng file: FILE|sender|target|filename|size
                string header = $"FILE|{txtName.Text}|{targetName}|{fileName}|{fileBytes.Length}";
                byte[] headerBytes = Encoding.UTF8.GetBytes(header);

                try
                {
                    // Gửi HEADER BẰNG LPP (Thay thế stream.Write + Thread.Sleep)
                    SendWithLengthPrefix(stream, headerBytes); 

                    // Gửi BYTE FILE (Server sẽ dùng fileSize trong header để đọc)
                    stream.Write(fileBytes, 0, fileBytes.Length);

                    AppendChat($"📎 Bạn đã gửi file '{fileName}' đến {targetName.ToUpper()}.");
                }
                catch
                {
                    AppendChat("❌ Gửi file thất bại.");
                }
            }
        }

        // =====================================================
        // ✅ HÀM HỖ TRỢ HIỂN THỊ HỘP THOẠI NHẬP TÊN NGƯỜI NHẬN
        // =====================================================
        private string PromptForTarget(string prompt, string title)
        {
            Form promptForm = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false
            };
            
            Label label = new Label() { Left = 50, Top = 20, Text = prompt, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 280, Text = "ALL" }; 
            Button confirmation = new Button() { Text = "Gửi", DialogResult = DialogResult.OK, Left = 200, Top = 80 };
            Button cancel = new Button() { Text = "Hủy", DialogResult = DialogResult.Cancel, Left = 280, Top = 80 };

            promptForm.AcceptButton = confirmation;
            promptForm.CancelButton = cancel;

            promptForm.Controls.AddRange(new Control[] { label, textBox, confirmation, cancel });

            return promptForm.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
        }

        // =====================================================
        // ✅ GỬI CHUỖI DATA QUA SOCKET (Đã dùng LPP)
        // =====================================================
        private void SendMessage(string msg)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(msg);
                SendWithLengthPrefix(stream, data);
            }
            catch
            {
                AppendChat("❌ Gửi tin nhắn thất bại.");
            }
        }

        // =====================================================
        // ✅ LUỒNG NHẬN TIN TỪ SERVER (Đã dùng LPP)
        // =====================================================
        private void ReceiveMessages()
        {
            byte[] lengthBuffer = new byte[4]; // Buffer 4 bytes cho Length Prefix

            try
            {
                // Vòng lặp chính đọc Length Prefix (4 bytes)
                while (ReadAll(stream, lengthBuffer, 0, 4) > 0)
                {
                    // Chuyển 4 bytes thành kích thước gói tin
                    int messageSize = BitConverter.ToInt32(lengthBuffer, 0);

                    if (messageSize <= 0) continue; 

                    // Đọc toàn bộ gói tin/header theo kích thước đã xác định
                    byte[] messageBuffer = new byte[messageSize];
                    if (ReadAll(stream, messageBuffer, 0, messageSize) == 0) break; // Lỗi đọc nội dung

                    string msg = Encoding.UTF8.GetString(messageBuffer);

                    // ==========================================
                    // ✅ 1) NHẬN FILE (File Header đã được nhận bằng LPP)
                    // ==========================================
                    if (msg.StartsWith("FILE|"))
                    {
                        string[] parts = msg.Split('|');

                        if (parts.Length == 5)
                        {
                            string senderName = parts[1];
                            string fileName = parts[3];
                            int fileSize = int.Parse(parts[4]);

                            // Tạo buffer để đọc toàn bộ file
                            byte[] fileBuffer = new byte[fileSize];
                            
                            // Đọc file data bằng ReadAll
                            if (ReadAll(stream, fileBuffer, 0, fileSize) == 0) break;

                            // Lưu file vào Documents
                            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                            string savePath = System.IO.Path.Combine(documentsPath, fileName);

                            System.IO.File.WriteAllBytes(savePath, fileBuffer);

                            AppendChat($"📥 Nhận file '{fileName}' từ {senderName}. Lưu tại: {savePath}");
                        }
                        continue;
                    }

                    // ==========================================
                    // ✅ 2) KIỂM TRA TRÙNG TÊN
                    // ==========================================
                    if (msg.Contains("Name already in use"))
                    {
                        MessageBox.Show("Tên này đã được sử dụng. Vui lòng nhập tên khác.",
                                        "Trùng tên", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        stream.Close();
                        client.Close();

                        isConnected = false;
                        btnConnect.Enabled = true;
                        txtName.ReadOnly = false;

                        AppendChat("❌ Disconnected from server. Please try a new name.");
                        break;
                    }

                    // ==========================================
                    // ✅ 3) TIN NHẮN BÌNH THƯỜNG
                    // ==========================================
                    AppendChat(msg);
                }
            }
            catch
            {
                AppendChat("❌ Mất kết nối server.");
            }
        }

        // =====================================================
        // ✅ THÊM TIN NHẮN VÀO HỘP CHAT
        // =====================================================
        private void AppendChat(string msg)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendChat(msg)));
                return;
            }

            txtLog.AppendText(msg + Environment.NewLine);
            txtLog.ScrollToCaret();
        }
    }
}