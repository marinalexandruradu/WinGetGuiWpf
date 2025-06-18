using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace WinGetGuiWpf
{
    // This class must be registered for COM activation (CLSID matches manifest/registry).
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("D55B53B4-04B9-4E3A-9DA8-7C78A471AAAD")]
    public class MyNotificationActivator
    {
        // No need to implement anything here—CommunityToolkit will use COM registration only!
    }

    public static class ToastNotificationHandler
    {
        private static bool _isInitialized = false;

        public static void Initialize()
        {
            // This must be called exactly once per process!
            if (!_isInitialized)
            {
                ToastNotificationManagerCompat.OnActivated += OnToastActivated;
                Log("ToastNotificationHandler.Initialize called.");
                _isInitialized = true;
            }
        }

        private static void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            try
            {
                Log($"OnToastActivated fired! Args: {e.Argument}");

                // This is the argument from the toast button
                if (e.Argument.Contains("launchEZ"))
                {
                    Log("Launching a new instance with 'action=launchEZ'...");

                    // Use ProcessStartInfo to launch your app as a new process
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
                        Arguments = "action=launchEZ",
                        UseShellExecute = true
                    });

                    Log("Process.Start issued.");
                }
                else
                {
                    Log("No recognized argument in OnToastActivated.");
                }
            }
            catch (System.Exception ex)
            {
                Log($"EXCEPTION in OnToastActivated: {ex}");
            }
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText("toast_activator.log", $"{System.DateTime.Now:O}: {message}\n");
            }
            catch { /* ignore secondary errors */ }
        }
    }
}
