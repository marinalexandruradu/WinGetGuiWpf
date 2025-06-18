using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using WinGetGuiWpf.ViewModels;

namespace WinGetGuiWpf
{
    public partial class App : Application
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
               // System.IO.File.AppendAllText("toast_launch.log", $"MAIN ENTRY: {string.Join(" | ", args)} at {DateTime.Now:O}\n");
                ToastNotificationHandler.Initialize();
                SetCurrentProcessExplicitAppUserModelID("WinGetGuiWpf");
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
              //  System.IO.File.AppendAllText("startup_crash.log", ex.ToString() + Environment.NewLine);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

         //   System.IO.File.AppendAllText("toast_launch.log", $"OnStartup: {string.Join(" | ", e.Args)} at {DateTime.Now:O}\n");

            // 1. Check for background update trigger first
            if (e.Args.Contains("-checkforupdates", StringComparer.OrdinalIgnoreCase))
            {
             //   System.IO.File.AppendAllText("toast_launch.log", "Action: CheckForUpdates\n");
                MainViewModel.CheckAndNotifyUpdates();
                Shutdown(); // <-- Exit WITHOUT showing the window!
                return;
            }

            // 2. Check for toast/UI activation
            if (e.Args.Any(arg => arg.Contains("launchEZ", StringComparison.OrdinalIgnoreCase)) ||
                e.Args.Any(arg => arg.Contains("action=launchEZ", StringComparison.OrdinalIgnoreCase)))
            {
               // System.IO.File.AppendAllText("toast_launch.log", "Action: launchEZ\n");
                ShowMainWindow("toast/launchEZ");
                return;
            }

            // 3. Protocol activation or other custom logic if needed...

            // 4. Default: normal launch
          //  System.IO.File.AppendAllText("toast_launch.log", "Action: Default (show main window)\n");
            ShowMainWindow("default");
        }


        private void ShowMainWindow(string source)
        {
           // System.IO.File.AppendAllText("toast_launch.log", $"ShowMainWindow called from: {source} at {DateTime.Now:O}\n");
            var mainWindow = new MainWindow
            {
                WindowState = WindowState.Normal,
                Topmost = true // Try to force window on top
            };
            mainWindow.Show();
            mainWindow.Activate();
        }

    }
}
