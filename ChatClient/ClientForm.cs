using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace ChatClient
{
    public class Form1 : Form
    {
        private WebView2 webView;

        public Form1()
        {
            this.Text = "Zalo Desktop Ultimate (WebView)";
            this.Size = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            // **FIX**: Xóa Cache để luôn tải code mới nhất khi dev
            // Lấy đường dẫn thư mục cache của WebView2
            string webViewCacheDir = Path.Combine(Path.GetTempPath(), "ZaloChat_WebView2_Cache");
            if (Directory.Exists(webViewCacheDir)) {
                try {
                    Directory.Delete(webViewCacheDir, true);
                } catch (Exception ex) {
                    Console.WriteLine("Could not clear cache: " + ex.Message);
                }
            }
            var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, webViewCacheDir);

            // Khởi tạo WebView2 với môi trường đã xóa cache
            await webView.EnsureCoreWebView2Async(env);

            // Trỏ vào file giao diện
            webView.Source = new Uri("http://localhost:5000/chat_client_desktop.html");

            // Chặn các tính năng mặc định của trình duyệt
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        }
    }
}
