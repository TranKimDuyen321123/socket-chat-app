using System;
using System.Windows.Forms;

namespace ChatClient
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Cấu hình hiển thị cho Windows Forms (High DPI support)
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // CHẠY TRỰC TIẾP FORM 1 (Giao diện mới đã tích hợp Login)
            // Không dùng LoginForm cũ nữa
            Application.Run(new Form1());
        }
    }
}
