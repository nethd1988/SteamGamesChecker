using System;
using System.Windows.Forms;

namespace SteamGamesChecker
{
    static class Program
    {
        /// <summary>
        /// Điểm khởi đầu chính cho ứng dụng.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}