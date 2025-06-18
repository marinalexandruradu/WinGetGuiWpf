using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinGetGuiWpf.ViewModels;


namespace WinGetGuiWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            System.IO.File.AppendAllText("toast_launch.log", $"MainWindow constructor called at {DateTime.Now:O}\n");
            InitializeComponent();
            var viewModel = (MainViewModel)DataContext;

            UpgradeButton.Click += (s, e) =>
            {
                var selected = UpgradesListBox.SelectedItems.Cast<string>().ToList();
                var viewModel = (MainViewModel)DataContext;
                viewModel.UpgradeSelectedAsync(selected);
            };



        }

        private void StartSpinner()
        {
            var storyboard = (Storyboard)FindResource("SpinnerAnimation");
            storyboard.Begin();
        }

        private void StopSpinner()
        {
            var storyboard = (Storyboard)FindResource("SpinnerAnimation");
            storyboard.Stop();
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel)
                return;

            var selected = SearchResultsListBox.SelectedItems
                .Cast<WingetSearchResult>()
                .ToList();

            if (selected.Count == 0)
                return;

            await viewModel.InstallSelectedPackagesAsync(selected);
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.alexandrumarin.com",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open browser: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var optionsWindow = new OptionsWindow();
            optionsWindow.Owner = this;
            optionsWindow.ShowDialog();
        }


    }
}