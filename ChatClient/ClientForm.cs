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

        // =====================================================
        // ✅ SỰ KIỆN NHẤN NÚT CONNECT → GỬI TÊN LÊN SERVER
        // =====================================================
        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (isConnected) { MessageBox.Show("Bạn đã kết nối rồi!"); return; }
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Vui lòng nhập tên."); return; }

            try
            {
                // 1) Tạo kết nối TCP đến server
                client = new TcpClient("127.0.0.1", 5000);
                stream = client.GetStream();

                // 2) Gửi tên lên server
                SendMessage($"NAME:{txtName.Text}");

                // 3) Khởi chạy luồng nhận tin nhắn
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
        // ✅ NHẤN "FILE" → GỬI FILE
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
                string filePath = ofd.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                // HEADER gửi trước để server biết dung lượng file
                string header = $"FILE|{txtName.Text}|ALL|{fileName}|{fileBytes.Length}";
                byte[] headerBytes = Encoding.UTF8.GetBytes(header);

                try
                {
                    // Gửi HEADER
                    stream.Write(headerBytes, 0, headerBytes.Length);

                    Thread.Sleep(50); // tránh dính gói

                    // Gửi BYTE FILE
                    stream.Write(fileBytes, 0, fileBytes.Length);

                    AppendChat($"📎 Bạn đã gửi file '{fileName}'");
                }
                catch
                {
                    AppendChat("❌ Gửi file thất bại.");
                }
            }
        }

        // =====================================================
        // ✅ GỬI CHUỖI DATA QUA SOCKET
        // =====================================================
        private void SendMessage(string msg)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(msg);
                stream.Write(data, 0, data.Length);
            }
            catch
            {
                AppendChat("❌ Gửi tin nhắn thất bại.");
            }
        }

        // =====================================================
        // ✅ LUỒNG NHẬN TIN TỪ SERVER (GỬI FILE + CHAT)
        // =====================================================
        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) continue;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // ==========================================
                    // ✅ 1) NHẬN FILE
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
                            int totalRead = 0;

                            // Đọc đến khi đủ fileSize
                            while (totalRead < fileSize)
                            {
                                int read = stream.Read(fileBuffer, totalRead, fileSize - totalRead);
                                if (read == 0) break;
                                totalRead += read;
                            }

                            // Lưu file vào Documents
                            string savePath = System.IO.Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                fileName
                            );

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

                        // Ngắt kết nối
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
