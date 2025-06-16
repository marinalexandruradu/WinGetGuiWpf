using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace WinGetGuiWpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<string> upgradablePackages = new();

    [ObservableProperty]
    private ObservableCollection<string> selectedPackages = new();

    [ObservableProperty]
    private bool isBusy;

    private ObservableCollection<UpgradeStatusItem> _upgradeStatusItems = new();
    public ObservableCollection<UpgradeStatusItem> UpgradeStatusItems
    {
        get => _upgradeStatusItems;
        set => SetProperty(ref _upgradeStatusItems, value);
    }

    private ObservableCollection<UpgradeStatusItem> _installStatusItems = new();
    public ObservableCollection<UpgradeStatusItem> InstallStatusItems
    {
        get => _installStatusItems;
        set => SetProperty(ref _installStatusItems, value);
    }

    [ObservableProperty]
    private string searchTerm = "";

    public ObservableCollection<WingetSearchResult> SearchResults { get; set; } = new();

    private Process? _activeWingetProcess;
    private readonly object _processLock = new();

    [RelayCommand]
    public void CancelOperation()
    {
        lock (_processLock)
        {
            try
            {
                if (_activeWingetProcess != null && !_activeWingetProcess.HasExited)
                {
                    _activeWingetProcess.Kill(true);
                    _activeWingetProcess = null;
                    IsBusy = false;
                    MessageBox.Show("Operation cancelled.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cancel failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsLoading = true;
        UpgradablePackages.Clear();

        await Task.Run(() =>
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "upgrade",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split('\n')
                .Skip(1)
                .Where(line =>
                    !string.IsNullOrWhiteSpace(line) &&
                    !line.Contains("----") &&
                    !line.ToLower().Contains("the following") &&
                    !line.StartsWith("No") &&
                    line.Contains(" "))
                .ToList();

            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var line in lines)
                {
                    var columns = System.Text.RegularExpressions.Regex.Split(line.Trim(), @"\s{2,}");
                    if (columns.Length >= 2)
                    {
                        var name = columns[0].Trim();
                        var id = columns[1].Trim();
                        UpgradablePackages.Add($"{name} [{id}]");
                    }
                }

                IsLoading = false;
            });
        });
    }

    public void UpgradeSelectedAsync(List<string> selectedPackages)
    {
        if (selectedPackages == null || selectedPackages.Count == 0)
            return;

        IsLoading = true;

        App.Current.Dispatcher.Invoke(() =>
        {
            UpgradeStatusItems.Add(new UpgradeStatusItem
            {
                PackageName = "-----",
                Status = "",
                Message = $"Upgrading selected package(s) at {DateTime.Now:T}",
                FullOutput = ""
            });
        });

        Task.Run(() =>
        {
        foreach (var package in selectedPackages)
        {
            var cleaned = package.Replace("\"", "").Trim();
            var start = cleaned.IndexOf('[');
            var end = cleaned.IndexOf(']');

            if (start == -1 || end == -1 || end <= start)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    UpgradeStatusItems.Add(new UpgradeStatusItem
                    {
                        PackageName = cleaned,
                        Status = "Failed",
                        Message = "Could not extract ID from selection.",
                        FullOutput = ""
                    });
                });
                continue;
            }

            var id = cleaned.Substring(start + 1, end - start - 1);
            UpgradeStatusItem item = null;

            App.Current.Dispatcher.Invoke(() =>
            {
                item = new UpgradeStatusItem
                {
                    PackageName = cleaned,
                    Status = "Pending",
                    Message = $"Running: winget upgrade --id \"{id}\"",
                    FullOutput = ""
                };
                UpgradeStatusItems.Add(item);
            });

            var outputBuffer = new StringBuilder();
            var timer = new System.Timers.Timer(200);
            timer.Elapsed += (s, e) =>
            {
                string snapshot;
                lock (outputBuffer)
                {
                    snapshot = outputBuffer.ToString();
                    outputBuffer.Clear();
                }

                if (!string.IsNullOrEmpty(snapshot))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        item.FullOutput = item.FullOutput + snapshot;
                    });
                }
            };
            timer.Start();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"upgrade --id \"{id}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            lock (_processLock)
            {
                _activeWingetProcess = process;
                IsBusy = true;
            }

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    lock (outputBuffer)
                    {
                        outputBuffer.AppendLine(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    lock (outputBuffer)
                    {
                        outputBuffer.AppendLine("[ERR] " + e.Data);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
                process.WaitForExit();
                timer.Stop();

                lock (_processLock)
                {
                    _activeWingetProcess = null;
                    IsBusy = false;
                }

                string finalChunk;
                lock (outputBuffer)
                {
                    finalChunk = outputBuffer.ToString();
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    item.FullOutput = item.FullOutput + finalChunk;

                    bool isSuccess =
                        item.FullOutput.Contains("Successfully installed", StringComparison.OrdinalIgnoreCase) ||
                        item.FullOutput.Contains("Installation completed", StringComparison.OrdinalIgnoreCase) ||
                        item.FullOutput.Contains("No applicable update found", StringComparison.OrdinalIgnoreCase);

                    item.Status = isSuccess ? "Success" : "Failed";
                    item.Message = isSuccess ? "Installed successfully." : "An error occurred.";
                });
            }

            App.Current.Dispatcher.Invoke(() => IsLoading = false);
        });
    }

    [RelayCommand]
    public async Task InstallSelectedPackagesAsync(List<WingetSearchResult> selectedPackages)
    {
        if (selectedPackages == null || selectedPackages.Count == 0)
            return;

        IsLoading = true;

        await Task.Run(() =>
        {
            foreach (var package in selectedPackages)
            {
                UpgradeStatusItem item = null;

                App.Current.Dispatcher.Invoke(() =>
                {
                    item = new UpgradeStatusItem
                    {
                        PackageName = package.Name,
                        Status = "Pending",
                        Message = $"Running: winget install --id \"{package.Id}\"",
                        FullOutput = ""
                    };
                    InstallStatusItems.Add(item);
                });

                var outputBuffer = new StringBuilder();
                var timer = new System.Timers.Timer(200);
                timer.Elapsed += (s, e) =>
                {
                    string snapshot;
                    lock (outputBuffer)
                    {
                        snapshot = outputBuffer.ToString();
                        outputBuffer.Clear();
                    }

                    if (!string.IsNullOrEmpty(snapshot))
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            item.FullOutput = item.FullOutput + snapshot;
                        });
                    }
                };
                timer.Start();

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = $"install --exact --id \"{package.Id}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    },
                    EnableRaisingEvents = true
                };

                lock (_processLock)
                {
                    _activeWingetProcess = process;
                    IsBusy = true;
                }

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (outputBuffer)
                        {
                            outputBuffer.AppendLine(e.Data);
                        }
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (outputBuffer)
                        {
                            outputBuffer.AppendLine("[ERR] " + e.Data);
                        }
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                timer.Stop();

                lock (_processLock)
                {
                    _activeWingetProcess = null;
                    IsBusy = false;
                }

                string finalChunk;
                lock (outputBuffer)
                {
                    finalChunk = outputBuffer.ToString();
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    item.FullOutput = item.FullOutput + finalChunk;

                    string combined = item.FullOutput;
                    if (combined.Contains("Successfully installed", StringComparison.OrdinalIgnoreCase))
                    {
                        item.Status = "Success";
                        item.Message = "Installed successfully.";
                    }
                    else if (!string.IsNullOrWhiteSpace(combined) &&
                             combined.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        item.Status = "Failed";
                        item.Message = "An error occurred.";
                    }
                    else
                    {
                        item.Status = "Unknown";
                        item.Message = "Status unclear.";
                    }
                });
            }

            App.Current.Dispatcher.Invoke(() => IsLoading = false);
        });
    }

    [RelayCommand]
    public async Task UpgradeAllAsync()
    {
        IsLoading = true;

        await CheckForUpdatesAsync();

        if (UpgradablePackages.Count == 0)
        {
            IsLoading = false;
            MessageBox.Show("You're already up to date!", "No Updates Found",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var item = new UpgradeStatusItem
        {
            PackageName = "[ALL PACKAGES]",
            Status = "Pending",
            Message = "Running: winget upgrade --all",
            FullOutput = ""
        };

        App.Current.Dispatcher.Invoke(() =>
        {
            UpgradeStatusItems.Add(item);
            new LogViewerWindow(item).Show();
        });

        await Task.Run(() =>
        {
            var outputBuffer = new StringBuilder();
            var timer = new System.Timers.Timer(200);
            timer.Elapsed += (s, e) =>
            {
                string snapshot;
                lock (outputBuffer)
                {
                    snapshot = outputBuffer.ToString();
                    outputBuffer.Clear();
                }

                if (!string.IsNullOrEmpty(snapshot))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        item.FullOutput = item.FullOutput + snapshot;
                    });
                }
            };
            timer.Start();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "upgrade --all --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            lock (_processLock)
            {
                _activeWingetProcess = process;
                IsBusy = true;
            }

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    lock (outputBuffer)
                    {
                        outputBuffer.AppendLine(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    lock (outputBuffer)
                    {
                        outputBuffer.AppendLine("[ERR] " + e.Data);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            timer.Stop();

            lock (_processLock)
            {
                _activeWingetProcess = null;
                IsBusy = false;
            }

            string finalChunk;
            lock (outputBuffer)
            {
                finalChunk = outputBuffer.ToString();
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                item.FullOutput = item.FullOutput + finalChunk;

                bool isSuccess = item.FullOutput.Contains("Successfully installed", StringComparison.OrdinalIgnoreCase) ||
                                 item.FullOutput.Contains("Installation completed", StringComparison.OrdinalIgnoreCase) ||
                                 item.FullOutput.Contains("No applicable update found", StringComparison.OrdinalIgnoreCase);

                item.Status = isSuccess ? "Success" : "Failed";
                item.Message = isSuccess ? "All packages upgraded successfully." : "Some packages may have failed.";
                IsLoading = false;
            });
        });
    }

    [RelayCommand]
    public void ShowInstallLog(UpgradeStatusItem item)
    {
        if (item is null)
            return;

        var logWindow = new LogViewerWindow(item);
        logWindow.Show();
    }

    [RelayCommand]
    public void ShowLog(UpgradeStatusItem item)
    {
        if (item is null)
            return;

        var logWindow = new LogViewerWindow(item);
        logWindow.Show();
    }
}
