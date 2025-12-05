using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ChatClient
{
    public class Form1 : Form
    {
        // ============================= THEME CONFIG =============================
        private readonly Color CLR_BG_DARK = Color.FromArgb(32, 34, 37);
        private readonly Color CLR_BG_MAIN = Color.FromArgb(54, 57, 63);
        private readonly Color CLR_BG_INPUT = Color.FromArgb(64, 68, 75);
        private readonly Color CLR_ACCENT = Color.FromArgb(88, 101, 242);
        private readonly Color CLR_BUBBLE_THEIRS = Color.FromArgb(64, 68, 75);
        private readonly Color CLR_TEXT = Color.White;

        // ============================= STATE =============================
        private HubConnection connection;
        private string myName = "";
        private string currentChatTarget = "ALL";
        private string lastSender = "";
        private int currentY = 20; // Vị trí Y hiện tại để vẽ tin nhắn tiếp theo

        // ============================= CONTROLS =============================
        private Panel pnlLogin, pnlMainApp, pnlSidebar, pnlChatHistory;
        private TextBox txtUsername, txtMessage;
        private Label lblChatTitle;

        public Form1(string userName) : this() { }

        public Form1()
        {
            this.Text = "Zalo Desktop Ultimate (Fixed Layout)";
            this.Size = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = CLR_BG_MAIN;
            this.DoubleBuffered = true; // Giảm giật lag

            InitializeLoginUI();
        }

        #region 1. LOGIN UI
        private void InitializeLoginUI()
        {
            pnlLogin = new Panel { Dock = DockStyle.Fill, BackColor = CLR_BG_MAIN };

            var card = new Panel { Size = new Size(400, 300), BackColor = CLR_BG_DARK };
            // Căn giữa màn hình login
            card.Location = new Point((this.Width - card.Width) / 2, (this.Height - card.Height) / 2);
            
            var lblTitle = new Label { Text = "ĐĂNG NHẬP", Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.White };
            
            txtUsername = new TextBox { Width = 300, Font = new Font("Segoe UI", 12), Location = new Point(50, 100) };
            var btnLogin = new Button { Text = "BẮT ĐẦU", Width = 300, Height = 50, Location = new Point(50, 160), BackColor = CLR_ACCENT, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold) };

            btnLogin.Click += (s, e) => PerformLogin();
            txtUsername.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) PerformLogin(); };

            card.Controls.Add(btnLogin);
            card.Controls.Add(txtUsername);
            card.Controls.Add(lblTitle);
            pnlLogin.Controls.Add(card);
            
            // Tự động căn giữa lại khi resize cửa sổ
            pnlLogin.SizeChanged += (s,e) => { card.Location = new Point((pnlLogin.Width - card.Width) / 2, (pnlLogin.Height - card.Height) / 2); };

            this.Controls.Add(pnlLogin);
        }

        private async void PerformLogin()
        {
            string user = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(user)) return;

            myName = user;
            pnlLogin.Dispose(); // Xóa màn hình login
            InitializeMainUI();
            await InitializeSignalR();
        }
        #endregion

        #region 2. MAIN UI
        private void InitializeMainUI()
        {
            pnlMainApp = new Panel { Dock = DockStyle.Fill };

            // --- SIDEBAR ---
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 280, BackColor = CLR_BG_DARK, Padding = new Padding(10) };
            var pnlFriends = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Name = "FriendListPanel" };
            
            // Header Sidebar
            var lblMe = new Label { Text = myName, Dock = DockStyle.Top, Height = 50, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = CLR_TEXT, TextAlign = ContentAlignment.MiddleLeft };
            pnlSidebar.Controls.Add(pnlFriends);
            pnlSidebar.Controls.Add(lblMe);

            // --- CHAT AREA ---
            var pnlChatArea = new Panel { Dock = DockStyle.Fill };
            
            // Header Chat
            lblChatTitle = new Label { Dock = DockStyle.Top, Height = 60, Text = "# Public Chat", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = CLR_TEXT, Padding = new Padding(20, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, BackColor = CLR_BG_MAIN };
            
            // History Panel (QUAN TRỌNG: AutoScroll = true để cuộn)
            pnlChatHistory = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = CLR_BG_MAIN };
            
            // Input Area
            var pnlInput = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(20), BackColor = CLR_BG_MAIN };
            txtMessage = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12), BackColor = CLR_BG_INPUT, ForeColor = Color.White, BorderStyle = BorderStyle.None, Multiline = false };
            txtMessage.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SendMessage(); } };
            
            pnlInput.Controls.Add(txtMessage);

            pnlChatArea.Controls.Add(pnlChatHistory); // Add history first to fill
            pnlChatArea.Controls.Add(pnlInput);       // Bottom
            pnlChatArea.Controls.Add(lblChatTitle);   // Top

            pnlMainApp.Controls.Add(pnlChatArea);
            pnlMainApp.Controls.Add(pnlSidebar);
            this.Controls.Add(pnlMainApp);
        }
        #endregion

        #region 3. SIGNALR & LOGIC
        private async Task InitializeSignalR()
        {
            connection = new HubConnectionBuilder().WithUrl("http://localhost:5000/chatHub").WithAutomaticReconnect().Build();

            connection.On<JsonElement>("ReceiveMessage", (msg) => ProcessMessage(msg));
            connection.On<JsonElement>("ReceivePrivateMessage", (msg) => ProcessMessage(msg));
            connection.On<List<JsonElement>>("UpdateFriendList", (list) => Invoke(new Action(() => RenderFriendList(list))));
            connection.On<List<JsonElement>>("LoadHistory", (list) => Invoke(new Action(() => LoadHistory(list))));

            try { await connection.StartAsync(); await connection.InvokeAsync("RegisterConnection", myName); }
            catch (Exception ex) { MessageBox.Show("Lỗi kết nối: " + ex.Message); }
        }

        private void ProcessMessage(JsonElement msg)
        {
            if (InvokeRequired) { Invoke(new Action(() => ProcessMessage(msg))); return; }
            var sender = msg.GetProperty("senderName").GetString();
            
            // Chỉ hiện tin nhắn nếu đúng phòng chat
            if (currentChatTarget == "ALL" || sender == currentChatTarget || sender == myName)
            {
                AddMessageBubble(msg);
            }
        }

        private async Task SendMessage()
        {
            string txt = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(txt)) return;
            await connection.InvokeAsync("SendMessage", myName, currentChatTarget, txt, 0, "");
            txtMessage.Clear();
        }
        #endregion

        #region 4. LAYOUT ENGINE (FIXED)
        private void LoadHistory(List<JsonElement> msgs)
        {
            pnlChatHistory.SuspendLayout(); // Dừng vẽ để tối ưu
            pnlChatHistory.Controls.Clear();
            currentY = 20; // Reset vị trí Y về đầu
            lastSender = "";
            
            foreach (var msg in msgs) AddMessageBubble(msg, true);
            
            pnlChatHistory.ResumeLayout(); // Vẽ lại 1 lần
            ScrollToBottom();
        }

        private void AddMessageBubble(JsonElement msg, bool isBatch = false)
        {
            if(!isBatch) pnlChatHistory.SuspendLayout();

            var sender = msg.GetProperty("senderName").GetString();
            var content = msg.GetProperty("content").GetString();
            var type = msg.GetProperty("type").GetInt32();
            bool isMe = sender == myName;

            // Logic Grouping: Nếu người gửi giống tin trước thì khoảng cách (margin) nhỏ hơn
            bool isGroup = sender != lastSender;
            int topMargin = isGroup ? 20 : 5;
            currentY += topMargin;

            Control bubble = null;

            // --- TYPE: TEXT ---
            if(type == 0)
            {
                bubble = new CustomBubbleLabel(content, isMe ? CLR_ACCENT : CLR_BUBBLE_THEIRS) 
                { 
                    MaximumSize = new Size(500, 0), // Giới hạn chiều rộng bong bóng
                    AutoSize = true 
                };
            }
            // --- TYPE: IMAGE (Base64) ---
            else if(type == 1)
            {
                try {
                    // Xử lý ảnh Base64
                    byte[] bytes = Convert.FromBase64String(content.Contains(",") ? content.Split(',')[1] : content);
                    using(var ms = new MemoryStream(bytes)) {
                        bubble = new PictureBox { 
                            Image = Image.FromStream(ms), 
                            SizeMode = PictureBoxSizeMode.Zoom, 
                            Size = new Size(200, 200),
                            BackColor = Color.Black
                        };
                    }
                } catch { bubble = new Label { Text = "[Ảnh lỗi]", ForeColor = Color.Red, AutoSize = true }; }
            }

            // --- CALCULATE POSITION ---
            // Nếu là mình: Căn phải (Width Panel - Width Bubble - Padding)
            // Nếu là bạn: Căn trái (Padding + Avatar Space)
            int x = isMe ? (pnlChatHistory.ClientSize.Width - bubble.Width - 30) : 60;
            
            bubble.Location = new Point(x, currentY);
            pnlChatHistory.Controls.Add(bubble);

            // --- AVATAR (Chỉ vẽ khi bắt đầu nhóm tin nhắn mới của người khác) ---
            if(!isMe && isGroup)
            {
                var ava = new Label { 
                    Text = sender[0].ToString().ToUpper(), 
                    Size = new Size(35, 35), 
                    BackColor = GetColor(sender), 
                    ForeColor = Color.White, 
                    TextAlign = ContentAlignment.MiddleCenter, 
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Location = new Point(15, currentY)
                };
                // Bo tròn avatar đơn giản
                GraphicsPath p = new GraphicsPath(); p.AddEllipse(0,0,35,35); ava.Region = new Region(p);
                pnlChatHistory.Controls.Add(ava);
            }

            // CẬP NHẬT Y CHO TIN NHẮN TIẾP THEO
            currentY = bubble.Bottom; // Vị trí Y tiếp theo = Đáy của bubble hiện tại
            lastSender = sender;

            if(!isBatch)
            {
                pnlChatHistory.ResumeLayout();
                ScrollToBottom();
            }
        }

        private void RenderFriendList(List<JsonElement> list)
        {
            var pnl = pnlSidebar.Controls.Find("FriendListPanel", false).FirstOrDefault() as Panel;
            if(pnl == null) return;
            pnl.Controls.Clear();

            int y = 0;
            pnl.Controls.Add(CreateFriendItem("Public Chat", true, "ALL", ref y));

            foreach(var item in list)
            {
                string u = item.GetProperty("username").GetString();
                bool o = item.GetProperty("isOnline").GetBoolean();
                if(u != myName) pnl.Controls.Add(CreateFriendItem(u, o, u, ref y));
            }
        }

        private Control CreateFriendItem(string name, bool online, string target, ref int y)
        {
            Panel p = new Panel { Size = new Size(240, 50), Location = new Point(0, y), Cursor = Cursors.Hand };
            if(target == currentChatTarget) p.BackColor = Color.FromArgb(60, 64, 70); // Highlight active

            Label l = new Label { Text = name, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(50, 15), AutoSize = true };
            Label av = new Label { Text = name[0].ToString(), Size = new Size(30, 30), BackColor = GetColor(name), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(10, 10) };
            GraphicsPath path = new GraphicsPath(); path.AddEllipse(0,0,30,30); av.Region = new Region(path);

            p.Controls.Add(l); p.Controls.Add(av);
            
            EventHandler click = (s, e) => {
                currentChatTarget = target;
                lblChatTitle.Text = target == "ALL" ? "# Public Chat" : $"@ {name}";
                connection.InvokeAsync("LoadMessageHistory", myName, target);
                RenderFriendList(new List<JsonElement>()); // Hack để refresh UI highlight (trong thực tế nên tối ưu hơn)
            };
            p.Click += click; l.Click += click; av.Click += click;

            y += 55;
            return p;
        }
        #endregion

        #region 5. HELPERS
        private void ScrollToBottom() { pnlChatHistory.VerticalScroll.Value = pnlChatHistory.VerticalScroll.Maximum; }
        
        private Color GetColor(string s) { return Color.FromArgb(100 + (s.GetHashCode()%100), 100 + ((s.GetHashCode()/10)%100), 150); }
        
        // Custom Label có bo góc
        public class CustomBubbleLabel : Label
        {
            private Color _bg;
            public CustomBubbleLabel(string t, Color c) 
            { 
                Text = t; _bg = c; 
                ForeColor = Color.White; 
                Font = new Font("Segoe UI", 11); 
                Padding = new Padding(10);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using(GraphicsPath p = new GraphicsPath()) {
                    int r = 15;
                    p.AddArc(0,0,r,r,180,90); p.AddArc(Width-r,0,r,r,270,90);
                    p.AddArc(Width-r,Height-r,r,r,0,90); p.AddArc(0,Height-r,r,r,90,90);
                    using(SolidBrush b = new SolidBrush(_bg)) e.Graphics.FillPath(b, p);
                }
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter);
            }
        }
        #endregion
    }
}
