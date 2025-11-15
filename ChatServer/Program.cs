program.cs bên chat server
using System;
using System.Windows.Forms;

namespace ChatServer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}