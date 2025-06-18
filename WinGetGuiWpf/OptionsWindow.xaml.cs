using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WinGetGuiWpf
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
            DailyCheckBox.IsChecked = IsTaskRegistered();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (DailyCheckBox.IsChecked == true)
                RegisterScheduledTask();
            else
                UnregisterScheduledTask();
        }

        private bool IsTaskRegistered()
        {
            var result = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = "/Query /TN \"WinGetGuiDailyCheck\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            result.WaitForExit();
            return result.ExitCode == 0;
        }

        private void RegisterScheduledTask()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Create /SC DAILY /TN \"WinGetGuiDailyCheck\" /TR \"\\\"{exePath}\\\" -checkforupdates\" /F",
                UseShellExecute = false,
                CreateNoWindow = true
            });

        }

        private void UnregisterScheduledTask()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = "/Delete /TN \"WinGetGuiDailyCheck\" /F",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
    }

}
