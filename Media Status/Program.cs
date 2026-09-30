using System;
using System.Windows.Forms;
using MediaControlApp.Forms;
using Media_Status; // Make sure this matches your AppConfig namespace

namespace MediaControlApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Checks for users.json; if missing, triggers your dark-themed UserSetupForm
            if (!AppConfig.InitializeUserSession())
            {
                return; // Exits the application cleanly if the user cancels setup
            }

            Application.Run(new MainForm());
        }
    }
}