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
        TextBox txtLog, txtMessage, txtName, txtRecipient;
        Button btnSend, btnConnect, btnAttach, btnJoin;
        ComboBox cboMode;
        GroupBox grpMessaging;

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
            this.Size = new Size(600, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorTranslator.FromHtml("#F0F2F5");
            this.Font = new Font("Segoe UI", 10);
            this.MinimumSize = new Size(550, 450);

            // =========================================================
            // ✅ HEADER: CHỨA TÊN NGƯỜI DÙNG VÀ NÚT KẾT NỐI
            // =========================================================
            Panel headerPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblName = new Label() { Text = "Tên của bạn:", Location = new Point(15, 23), AutoSize = true };

            txtName = new TextBox()
            {
                Size = new Size(150, 28),
                Location = new Point(120, 18),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            btnConnect = new Button()
            {
                Text = "Kết nối",
                Size = new Size(100, 35),
                BackColor = ColorTranslator.FromHtml("#007BFF"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(450, 12),
                Cursor = Cursors.Hand
            };
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Click += BtnConnect_Click;

            headerPanel.Controls.AddRange(new Control[] { lblName, txtName, btnConnect });
            
            // =========================================================
            // ✅ KHUNG CHAT: HIỂN THỊ LỊCH SỬ TIN NHẮN
            // =========================================================
            txtLog = new TextBox()
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5f), // Font lớn hơn chút
                Padding = new Padding(10)
            };

            // =========================================================
            // ✅ FOOTER: GROUPBOX CHỨA TOÀN BỘ CHỨC NĂNG GỬI
            // =========================================================
            grpMessaging = new GroupBox
            {
                Text = "Gửi tin nhắn",
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = Color.White,
                Padding = new Padding(10),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // --- Dòng 1: Chế độ, Người nhận, Nút Join ---
            Label lblMode = new Label() { Text = "Chế độ:", Location = new Point(15, 30), AutoSize = true };
            cboMode = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(120, 28),
                Location = new Point(80, 25)
            };
            cboMode.Items.AddRange(new string[] { "Public", "Private", "Group" });
            cboMode.SelectedIndex = 0;
            cboMode.SelectedIndexChanged += CboMode_SelectedIndexChanged;

            txtRecipient = new TextBox() { Size = new Size(160, 28), Location = new Point(220, 25), Visible = false };
            btnJoin = new Button()
            {
                Text = "Tham gia",
                Size = new Size(90, 28),
                BackColor = ColorTranslator.FromHtml("#ffc107"),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(390, 25),
                Visible = false
            };
            btnJoin.Click += BtnJoin_Click;

            // --- Dòng 2: Ô nhập tin nhắn và nút gửi ---
            txtMessage = new TextBox() { PlaceholderText = "Aa...", Size = new Size(370, 28), Location = new Point(15, 80) };
            btnSend = new Button()
            {
                Text = "Gửi",
                Size = new Size(80, 28),
                BackColor = ColorTranslator.FromHtml("#007BFF"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(395, 80)
            };
            btnSend.Click += BtnSend_Click;

            btnAttach = new Button()
            {
                Text = "📎",
                Size = new Size(40, 28),
                BackColor = ColorTranslator.FromHtml("#28a745"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(485, 80)
            };
            btnAttach.Click += BtnAttach_Click;

            grpMessaging.Controls.AddRange(new Control[] { lblMode, cboMode, txtRecipient, btnJoin, txtMessage, btnSend, btnAttach });

            // =========================================================
            // ✅ THÊM CONTROL VÀO FORM
            // =========================================================
            this.Controls.AddRange(new Control[] { txtLog, grpMessaging, headerPanel });

            // Khởi tạo trạng thái giao diện
            SetUIConnectedState(false); 
        }

        // ===================================================================
        // ✅ THAY ĐỔI TRẠNG THÁI GIAO DIỆN KHI KẾT NỐI / MẤT KẾT NỐI
        // ===================================================================
        private void SetUIConnectedState(bool connected)
        {
            isConnected = connected;

            // Header controls
            txtName.ReadOnly = connected;
            btnConnect.Enabled = !connected;
            btnConnect.Text = connected ? "Đã kết nối" : "Kết nối";
            btnConnect.BackColor = connected ? Color.LightGray : ColorTranslator.FromHtml("#007BFF");

            // Messaging controls
            grpMessaging.Enabled = connected;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => {
                    if (!connected) AppendChat("🔌 Đã ngắt kết nối. Vui lòng kết nối lại.");
                }));
            }
            else
            {
                 if (!connected) AppendChat("🔌 Vui lòng nhập tên và nhấn 'Kết nối' để bắt đầu.");
            }
        }
        
        // ===================================================================
        // ✅ SỰ KIỆN THAY ĐỔI CHẾ ĐỘ GỬI
        // ===================================================================
        private void CboMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            string mode = cboMode.SelectedItem.ToString();
            bool isGroup = mode == "Group";
            bool isPrivate = mode == "Private";

            txtRecipient.Visible = isGroup || isPrivate;
            btnJoin.Visible = isGroup;
            
            if(isPrivate) txtRecipient.PlaceholderText = "Tên người nhận";
            if(isGroup) txtRecipient.PlaceholderText = "Tên nhóm";
        }
        
        // =====================================================
        // ✅ SỰ KIỆN NHẤN NÚT CONNECT
        // =====================================================
        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Vui lòng nhập tên."); return; }

            try
            {
                client = new TcpClient("127.0.0.1", 5000);
                stream = client.GetStream();

                SendMessage($"NAME:{txtName.Text}");

                receiveThread = new Thread(ReceiveMessages) { IsBackground = true };
                receiveThread.Start();

                SetUIConnectedState(true);
                AppendChat("✅ Kết nối thành công đến server!");
            }
            catch
            {
                MessageBox.Show("Không thể kết nối đến server. Hãy đảm bảo server đang chạy.");
            }
        }

        // =====================================================
        // ✅ THAM GIA NHÓM
        // =====================================================
        private void BtnJoin_Click(object sender, EventArgs e)
        {
            string groupName = txtRecipient.Text.Trim();
            if (string.IsNullOrEmpty(groupName)) { MessageBox.Show("Vui lòng nhập tên nhóm."); return; }
            SendMessage($"JOIN:{groupName}");
        }

        // =====================================================
        // ✅ NHẤN GỬI TIN NHẮN
        // =====================================================
        private void BtnSend_Click(object sender, EventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            string mode = cboMode.SelectedItem.ToString();
            string finalMsg = text;
            string recipient = txtRecipient.Text.Trim();

            if (mode == "Private")
            {
                if (string.IsNullOrEmpty(recipient)) { MessageBox.Show("Vui lòng nhập tên người nhận."); return; }
                finalMsg = $"@{recipient}:{text}";
                AppendChat($"[Tôi → {recipient}]: {text}"); 
            }
            else if (mode == "Group")
            {
                if (string.IsNullOrEmpty(recipient)) { MessageBox.Show("Vui lòng nhập tên nhóm."); return; }
                finalMsg = $"ROOM:{recipient}:{text}";
                AppendChat($"[Tôi gửi vào nhóm {recipient}]: {text}");
            }
            else
            {
                 AppendChat($"[Tôi]: {text}");
            }

            SendMessage(finalMsg);
            txtMessage.Clear();
        }

        // =====================================================
        // ✅ GỬI FILE
        // =====================================================
        private void BtnAttach_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            
            string targetName = "ALL";
            string mode = cboMode.SelectedItem.ToString();

            if (mode == "Private")
            {
                targetName = txtRecipient.Text.Trim();
                if (string.IsNullOrEmpty(targetName)) { MessageBox.Show("Vui lòng nhập tên người nhận file."); return; }
            }
            else if (mode == "Group")
            {
                MessageBox.Show("Chức năng gửi file vào nhóm chưa được hỗ trợ trong phiên bản này.");
                return;
            }

            string filePath = ofd.FileName;
            string fileName = System.IO.Path.GetFileName(filePath);
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

            string header = $"FILE|{txtName.Text}|{targetName}|{fileName}|{fileBytes.Length}";
            
            try
            {
                SendWithLengthPrefix(stream, Encoding.UTF8.GetBytes(header));
                stream.Write(fileBytes, 0, fileBytes.Length);
                AppendChat($"📎 Bạn đã gửi file '{fileName}' đến {targetName}.");
            }
            catch
            {
                AppendChat("❌ Gửi file thất bại.");
            }
        }

        // =====================================================
        // ✅ GỬI DATA QUA SOCKET (DÙNG LPP)
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
        // ✅ ĐỌC DATA TỪ SOCKET (DÙNG LPP)
        // =====================================================
        int ReadAll(NetworkStream st, byte[] buffer, int offset, int size)
        {
            int totalRead = 0;
            while (totalRead < size)
            {
                int read = st.Read(buffer, offset + totalRead, size - totalRead);
                if (read == 0) return 0; 
                totalRead += read;
            }
            return totalRead;
        }
        void SendWithLengthPrefix(NetworkStream st, byte[] data)
        {
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            st.Write(lengthBytes, 0, 4);
            st.Write(data, 0, data.Length);
        }

        // =====================================================
        // ✅ LUỒNG NHẬN TIN TỪ SERVER
        // =====================================================
        private void ReceiveMessages()
        {
            byte[] lengthBuffer = new byte[4];
            try
            {
                while (ReadAll(stream, lengthBuffer, 0, 4) > 0)
                {
                    int messageSize = BitConverter.ToInt32(lengthBuffer, 0);
                    if (messageSize <= 0) continue;

                    byte[] messageBuffer = new byte[messageSize];
                    if (ReadAll(stream, messageBuffer, 0, messageSize) == 0) break;

                    string msg = Encoding.UTF8.GetString(messageBuffer);

                    if (msg.StartsWith("FILE|"))
                    {
                        string[] parts = msg.Split('|');
                        string sender = parts[1], fileName = parts[3];
                        int fileSize = int.Parse(parts[4]);
                        byte[] fileBuffer = new byte[fileSize];
                        if (ReadAll(stream, fileBuffer, 0, fileSize) > 0)
                        {
                            string savePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
                            System.IO.File.WriteAllBytes(savePath, fileBuffer);
                            AppendChat($"📥 Nhận file '{fileName}' từ {sender}. Đã lưu tại Documents.");
                        }
                        continue;
                    }

                    if (msg.Contains("Name already in use"))
                    {
                        MessageBox.Show("Tên này đã được sử dụng!");
                        this.Invoke(new Action(() => SetUIConnectedState(false)));
                        stream?.Close();
                        client?.Close();
                        break;
                    }
                    
                    AppendChat(msg);
                }
            }
            catch
            {
                // Lỗi xảy ra, ngắt kết nối
            }
            finally
            {
                SetUIConnectedState(false);
                stream?.Close();
                client?.Close();
            }
        }

        // =====================================================
        // ✅ THÊM TIN NHẮN VÀO HỘP CHAT (THREAD-SAFE)
        // =====================================================
        private void AppendChat(string msg)
        {
            if (txtLog.InvokeRequired) {
                txtLog.Invoke(new Action(() => AppendChat(msg)));
                return;
            }
            txtLog.AppendText(msg + Environment.NewLine);
        }
    }
}