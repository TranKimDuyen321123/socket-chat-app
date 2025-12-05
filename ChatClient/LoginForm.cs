using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ChatClient
{
    public class LoginForm : Form
    {
        // =====================================================
        // 🎨 SETUP GIAO DIỆN & BIẾN
        // =====================================================
        private Panel pnlLeft, pnlRight;
        private Panel pnlLogin, pnlRegister;
        
        // Controls cho Login
        private TextBox txtLoginUser, txtLoginPass;
        private Button btnLoginAction, btnGoToRegister;
        
        // Controls cho Register
        private TextBox txtRegUser, txtRegPass, txtRegConfirm;
        private Button btnRegAction, btnGoToLogin;

        // Kết quả trả về
        public TcpClient LoggedInClient { get; private set; }
        public string LoggedInUser { get; private set; }
        private static readonly HttpClient _httpClient = new HttpClient();

        // Hỗ trợ kéo thả cửa sổ không viền
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hwnd, int wmsg, int wparam, int lparam);

        public LoginForm()
        {
            // Thiết lập Form chính
            this.FormBorderStyle = FormBorderStyle.None; // Bỏ viền Windows cũ
            this.Size = new Size(750, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            InitializeUI();
        }

        private void InitializeUI()
        {
            // --- 1. PNL LEFT (Gradient & Branding) ---
            // Thêm pnlLeft TRƯỚC để Dock.Left chiếm chỗ trước
            pnlLeft = new Panel() { Dock = DockStyle.Left, Width = 300 };
            pnlLeft.Paint += (s, e) => 
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(pnlLeft.ClientRectangle, 
                       ColorTranslator.FromHtml("#4e54c8"), 
                       ColorTranslator.FromHtml("#8f94fb"), 
                       90F))
                {
                    e.Graphics.FillRectangle(brush, pnlLeft.ClientRectangle);
                }
                e.Graphics.DrawString("Welcome to\nZalo Chat", 
                    new Font("Segoe UI", 24, FontStyle.Bold), 
                    Brushes.White, new Point(40, 150));
                e.Graphics.DrawString("Connect with friends\neasily and quickly.", 
                    new Font("Segoe UI", 11), 
                    Brushes.WhiteSmoke, new Point(42, 250));
            };
            pnlLeft.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            // --- 2. PNL RIGHT (Container chứa Form) ---
            // Thêm pnlRight SAU để Dock.Fill lấp đầy phần còn lại
            pnlRight = new Panel() { Dock = DockStyle.Fill, Padding = new Padding(40) };
            pnlRight.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            Label btnClose = new Label()
            {
                Text = "✕", Font = new Font("Arial", 14), ForeColor = Color.Gray,
                Location = new Point(410, 10), AutoSize = true, Cursor = Cursors.Hand, // Căn sát góc phải (450px width)
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => Application.Exit();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;
            pnlRight.Controls.Add(btnClose);

            InitLoginPanel();
            InitRegisterPanel();

            // Add Panels vào pnlRight
            pnlRight.Controls.Add(pnlLogin);
            pnlRight.Controls.Add(pnlRegister);

            // Add Panels chính vào Form
            this.Controls.Add(pnlLeft);
            this.Controls.Add(pnlRight);
            
            // Đảm bảo thứ tự hiển thị đúng
            pnlLeft.SendToBack(); // Đẩy pnlLeft xuống dưới cùng của Z-order để nó được Dock đầu tiên
            pnlRight.BringToFront(); // pnlRight nằm trên để hiển thị nội dung

            // Mặc định hiển thị Login
            ToggleMode(true);
        }

        // =====================================================
        // 🔐 GIAO DIỆN LOGIN
        // =====================================================
        private void InitLoginPanel()
        {
            pnlLogin = new Panel() { Dock = DockStyle.Fill, BackColor = Color.White };

            // Căn giữa theo chiều dọc: (450 - ~300) / 2 = ~75px start Y (đẩy xuống title ở y=50)
            Label lblTitle = CreateTitle("Sign In", 50);
            
            // X = 75 để căn giữa (450 - 300)/2
            Panel boxUser = CreateInputBox("Username", 110, false, out txtLoginUser);
            Panel boxPass = CreateInputBox("Password", 170, true, out txtLoginPass);

            btnLoginAction = CreateButton("LOGIN", 240, "#4e54c8");
            btnLoginAction.Click += async (s, e) => await DoProcess("LOGIN");

            Label lblOr = new Label() { Text = "or", Location = new Point(215, 300), AutoSize = true, ForeColor = Color.Gray };
            
            btnGoToRegister = CreateLinkButton("Create new account", 330);
            btnGoToRegister.Click += (s, e) => ToggleMode(false); 

            pnlLogin.Controls.AddRange(new Control[] { lblTitle, boxUser, boxPass, btnLoginAction, lblOr, btnGoToRegister });
        }

        // =====================================================
        // 📝 GIAO DIỆN REGISTER
        // =====================================================
        private void InitRegisterPanel()
        {
            pnlRegister = new Panel() { Dock = DockStyle.Fill, BackColor = Color.White, Visible = false };

            Label lblTitle = CreateTitle("Create Account", 40);

            Panel boxUser = CreateInputBox("Username", 100, false, out txtRegUser);
            Panel boxPass = CreateInputBox("Password", 160, true, out txtRegPass);
            Panel boxConfirm = CreateInputBox("Confirm Pass", 220, true, out txtRegConfirm);

            btnRegAction = CreateButton("REGISTER", 290, "#42b72a");
            btnRegAction.Click += async (s, e) => await DoProcess("REGISTER");

            btnGoToLogin = CreateLinkButton("← Back to Login", 340);
            btnGoToLogin.Click += (s, e) => ToggleMode(true);

            pnlRegister.Controls.AddRange(new Control[] { lblTitle, boxUser, boxPass, boxConfirm, btnRegAction, btnGoToLogin });
        }

        // =====================================================
        // 🛠 HELPER TẠO CONTROL ĐẸP
        // =====================================================
        private Label CreateTitle(string text, int y)
        {
            return new Label()
            {
                Text = text, Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#333333"),
                Location = new Point(0, y), Size = new Size(450, 45), TextAlign = ContentAlignment.MiddleCenter // Width 450 để căn giữa panel
            };
        }

        private Panel CreateInputBox(string placeholder, int y, bool isPass, out TextBox txt)
        {
            // X = 75 để căn giữa (PanelRight Width 450 - Box Width 300) / 2 = 75
            Panel p = new Panel() { Location = new Point(75, y), Size = new Size(300, 45) };
            
            Panel line = new Panel() { Dock = DockStyle.Bottom, Height = 2, BackColor = Color.LightGray };
            
            txt = new TextBox()
            {
                PlaceholderText = placeholder,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                Dock = DockStyle.Top,
                Height = 30,
                UseSystemPasswordChar = isPass
            };
            
            txt.Enter += (s, e) => line.BackColor = ColorTranslator.FromHtml("#4e54c8");
            txt.Leave += (s, e) => line.BackColor = Color.LightGray;

            p.Controls.Add(line);
            p.Controls.Add(txt);
            return p;
        }

        private Button CreateButton(string text, int y, string colorHex)
        {
            Button btn = new Button()
            {
                Text = text, Location = new Point(75, y), Size = new Size(300, 45), // X = 75
                BackColor = ColorTranslator.FromHtml(colorHex), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateLinkButton(string text, int y)
        {
            Button btn = new Button()
            {
                Text = text, Location = new Point(75, y), Size = new Size(300, 30), // X = 75
                BackColor = Color.White, ForeColor = ColorTranslator.FromHtml("#4e54c8"),
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.White;
            btn.FlatAppearance.MouseDownBackColor = Color.White;
            return btn;
        }

        private void ToggleMode(bool showLogin)
        {
            // Clear inputs khi chuyển form
            txtLoginUser.Clear(); txtLoginPass.Clear();
            txtRegUser.Clear(); txtRegPass.Clear(); txtRegConfirm.Clear();

            if (showLogin)
            {
                pnlRegister.Visible = false;
                pnlLogin.Visible = true;
                pnlLogin.BringToFront();
            }
            else
            {
                pnlLogin.Visible = false;
                pnlRegister.Visible = true;
                pnlRegister.BringToFront();
            }
        }

        // =====================================================
        // 🚀 XỬ LÝ LOGIC MẠNG (HTTP API)
        // =====================================================
        private async Task DoProcess(string action)
        {
            string u, p;
            if (action == "LOGIN")
            {
                u = txtLoginUser.Text.Trim();
                p = txtLoginPass.Text.Trim();
            }
            else
            {
                u = txtRegUser.Text.Trim();
                p = txtRegPass.Text.Trim();
                string cf = txtRegConfirm.Text.Trim();
                if (p != cf) { MessageBox.Show("Mật khẩu xác nhận không khớp!"); return; }
            }

            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin.");
                return;
            }

            try
            {
                var payload = new { Username = u, Password = p };
                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                string endpoint = action == "LOGIN" ? "http://localhost:5000/api/auth/login" : "http://localhost:5000/api/auth/register";

                HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);
                string responseString = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                     if (action == "LOGIN")
                     {
                        LoggedInUser = u;
                        // ⚠️ Lưu ý: Logic kết nối Socket đã bị loại bỏ ở đây vì ta chuyển sang HTTP Login.
                        // Bạn sẽ cần kết nối SignalR ở Form chính (Form1) hoặc tái cấu trúc lại luồng kết nối.
                        // Để tương thích nhanh với code cũ (đòi TcpClient), ta tạm thời bỏ qua TcpClient ở đây 
                        // và sẽ sửa Form1 để dùng SignalR sau.
                        // Tuy nhiên, vì Form1 constructor đang nhận TcpClient, ta có thể fake hoặc null tạm thời,
                        // hoặc tốt nhất là chuyển Form1 sang SignalR HubConnection.
                        
                        // Hack tạm: Vẫn kết nối TCP Client tới ChatHub nếu muốn giữ code cũ?
                        // KHÔNG, kiến trúc đã thay đổi sang Web API + SignalR (hoặc Socket).
                        // Nhưng ChatServer hiện tại đang chạy API Controller. 
                        // Nếu muốn chat, ta phải sửa cả ClientForm sang SignalR Client.

                        // Để đơn giản cho người dùng (đang chuyển đổi), tôi sẽ trả về OK
                        // và ClientForm sẽ cần được cập nhật để dùng SignalR thay vì TcpClient.
                        
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                     }
                     else
                     {
                        MessageBox.Show("✅ Đăng ký thành công! Vui lòng đăng nhập.");
                        ToggleMode(true);
                     }
                }
                else
                {
                     // Parse error message from JSON if possible
                     MessageBox.Show("❌ " + response.ReasonPhrase + "\n" + responseString);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }
    }
}
