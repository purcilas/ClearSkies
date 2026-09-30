using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace ClearSkies;

public partial class MainWindow : Window
{
    private CacheManager cacheManager;
    private List<CacheInfo> currentCaches;
    private AppSettings appSettings;
    private Dictionary<CheckBox, CacheInfo> checkBoxMap = new();

    private static readonly SolidColorBrush AccentBrush =
        new((Color)ColorConverter.ConvertFromString("#2dd4bf"));
    private static readonly SolidColorBrush WarningBrush =
        new((Color)ColorConverter.ConvertFromString("#f59e0b"));

    public MainWindow()
    {
        InitializeComponent();
        cacheManager = new CacheManager();
        currentCaches = new List<CacheInfo>();
        appSettings = AppSettings.Load();
        if (!string.IsNullOrWhiteSpace(appSettings.MsfsCachePath) &&
            !cacheManager.TryResolveRollingCacheFolder(appSettings.MsfsCachePath, out _, out var savedPathError))
        {
            MessageBox.Show($"The saved manual cache folder will be ignored:\n{appSettings.MsfsCachePath}\n\n{savedPathError}",
                "Manual cache folder needs attention", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        lblVersion.Text = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "";
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!IsAdministrator())
        {
            lblStatus.Text = "Not running as admin — some caches may be inaccessible";
            lblStatus.Foreground = WarningBrush;
        }
        else
        {
            lblStatus.Text = "Ready";
            btnRunAsAdmin.Visibility = Visibility.Collapsed;
        }

        ScanCaches();
    }

    private bool IsAdministrator()
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private async void ScanCaches()
    {
        btnCleanSelected.IsEnabled = false;
        lblStatus.Text = "Scanning...";
        lblStatus.Foreground = (SolidColorBrush)FindResource("TextBrush");
        progressBar.IsIndeterminate = true;

        await Task.Run(() =>
        {
            currentCaches = cacheManager.GetAllCaches(appSettings.MsfsCachePath);
        });

        UpdateCacheList();

        progressBar.IsIndeterminate = false;
        progressBar.Value = 0;
        lblStatus.Text = IsAdministrator() ? "Ready" : "Ready (no admin)";
        lblStatus.Foreground = (SolidColorBrush)FindResource("TextBrush");
        btnCleanSelected.IsEnabled = true;
    }

    private Grid? currentGrid;
    private int currentGridIndex;

    private void UpdateCacheList()
    {
        pnlCheckboxes.Children.Clear();
        checkBoxMap.Clear();
        currentGrid = null;
        currentGridIndex = 0;

        // Group caches by category, preserving order
        var groups = currentCaches
            .GroupBy(c => c.Category)
            .OrderBy(g => g.Key == "System & GPU" ? 0 : 1);

        foreach (var group in groups)
        {
            AddSectionHeader(group.Key);
            foreach (var cache in group)
                AddCacheCard(cache);
            if (group.Key == "System & GPU")
                AddNvidiaAppHelp();
        }

        // Always show MSFS config button (for manual override)
        if (!currentCaches.Any(c => c.Category != "System & GPU"))
            AddSectionHeader("MSFS");
        AddConfigButton();

        var totalSize = cacheManager.GetTotalCacheSize(currentCaches);
        var formattedSize = new CacheInfo { SizeInBytes = totalSize }.SizeFormatted;
        lblTotalSize.Text = formattedSize;
    }

    private void AddSectionHeader(string text)
    {
        var header = new TextBlock
        {
            Text = text,
            Foreground = (SolidColorBrush)FindResource("SubtextBrush"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(4, 12, 0, 4)
        };
        pnlCheckboxes.Children.Add(header);

        currentGrid = new Grid();
        currentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        currentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        currentGridIndex = 0;
        pnlCheckboxes.Children.Add(currentGrid);
    }

    private void AddCacheCard(CacheInfo cache)
    {
        var card = new Border
        {
            Background = (SolidColorBrush)FindResource("PanelBgBrush"),
            BorderBrush = (SolidColorBrush)FindResource("PanelBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(2, 2, 2, 2),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = cache.Path
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal };

        var checkBox = new CheckBox
        {
            Style = (Style)FindResource("DarkCheckBox"),
            IsEnabled = cache.Exists,
            IsChecked = cache.Exists && cache.SizeInBytes > 0,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        sp.Children.Add(checkBox);

        var nameBlock = new TextBlock
        {
            Text = $"{cache.Name}\n{cache.Path}",
            Foreground = cache.Exists
                ? (SolidColorBrush)FindResource("TextBrush")
                : (SolidColorBrush)FindResource("SubtextBrush"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        sp.Children.Add(nameBlock);

        var sizeBlock = new TextBlock
        {
            Text = cache.Exists ? cache.SizeFormatted : "n/a",
            Foreground = cache.Exists && cache.SizeInBytes > 0
                ? AccentBrush
                : (SolidColorBrush)FindResource("SubtextBrush"),
            FontSize = 11,
            FontWeight = cache.Exists && cache.SizeInBytes > 0 ? FontWeights.SemiBold : FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0)
        };
        sp.Children.Add(sizeBlock);

        card.Child = sp;

        card.MouseLeftButtonDown += (s, e) =>
        {
            if (checkBox.IsEnabled)
            {
                checkBox.IsChecked = !checkBox.IsChecked;
                e.Handled = true;
            }
        };

        checkBoxMap[checkBox] = cache;

        if (currentGrid != null)
        {
            int row = currentGridIndex / 2;
            int col = currentGridIndex % 2;

            if (row >= currentGrid.RowDefinitions.Count)
                currentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(card, row);
            Grid.SetColumn(card, col);
            currentGrid.Children.Add(card);
            currentGridIndex++;
        }
        else
        {
            pnlCheckboxes.Children.Add(card);
        }
    }

    private static string? FindNvidiaApp()
    {
        foreach (var folder in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
        {
            var programFiles = Environment.GetFolderPath(folder);
            if (string.IsNullOrWhiteSpace(programFiles)) continue;
            var executable = Path.Combine(programFiles, "NVIDIA Corporation", "NVIDIA app", "CEF", "NVIDIA App.exe");
            if (File.Exists(executable)) return executable;
        }
        return null;
    }

    private void AddNvidiaAppHelp()
    {
        var button = new Button
        {
            Content = "NVIDIA DirectX cache: NVIDIA App instructions",
            Style = (Style)FindResource("DarkButton"),
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 11,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(2, 6, 2, 2),
            ToolTip = "Learn how to clear and rebuild shaders using NVIDIA App, and open it if installed."
        };
        button.Click += (_, _) => ShowNvidiaAppHelp();
        pnlCheckboxes.Children.Add(button);
    }

    private void ShowNvidiaAppHelp()
    {
            var executable = FindNvidiaApp();
            var instructions =
                "Clear and rebuild the shader cache using NVIDIA App:\n\n" +
                "1. Close MSFS and other games.\n" +
                "2. Open NVIDIA App > Graphics > Global Settings.\n" +
                "3. Open Shader Cache (called Shader Cache Size in some versions).\n" +
                "4. Open the three-dot menu and select Clear cache.\n" +
                "5. Confirm the cleanup and wait for it to finish.\n" +
                "6. Enable Auto Shader Compilation (beta) if needed.\n" +
                "7. Open the three-dot menu again and select Compile now to rebuild supported shaders.\n" +
                "8. Wait for compilation to finish before launching MSFS or other games.\n\n" +
                "If Clear cache or Compile now is missing, update NVIDIA App and your graphics driver, then check again. " +
                "Availability depends on the installed version.\n\n" +
                "NVIDIA clears most cache files. Compilation supports eligible DirectX 12 shaders; " +
                "games may still need to compile additional shaders when played. " +
                "ClearSkies does not invoke these actions automatically.\n\n" +
                "If you use NVIDIA App instead, uncheck NVIDIA DirectX Shader Cache before cleaning in ClearSkies.\n\n";
            if (executable == null)
            {
                MessageBox.Show(this, instructions + "NVIDIA App was not found in the standard installation folders. Open it from Start if installed elsewhere.",
                    "NVIDIA shader cache cleanup", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(this, instructions + "Open NVIDIA App now?", "NVIDIA shader cache cleanup",
                MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes) return;
            try
            {
                Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not open NVIDIA App. Open it from Start instead.\n\n{ex.Message}",
                    "NVIDIA App", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
    }

    private void AddConfigButton()
    {
        if (currentGrid == null) return;

        var btn = new Border
        {
            Background = (SolidColorBrush)FindResource("ButtonBgBrush"),
            BorderBrush = (SolidColorBrush)FindResource("PanelBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(2, 2, 2, 2),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal };

        var icon = new TextBlock
        {
            Text = "\uE115",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 12,
            Foreground = (SolidColorBrush)FindResource("SubtextBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        sp.Children.Add(icon);

        var label = new TextBlock
        {
            Text = "Set Folder",
            Foreground = (SolidColorBrush)FindResource("SubtextBrush"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        sp.Children.Add(label);

        btn.Child = sp;
        btn.MouseLeftButtonDown += (s, e) =>
        {
            BtnConfigMsfs_Click(s, new RoutedEventArgs());
            e.Handled = true;
        };

        int row = currentGridIndex / 2;
        int col = currentGridIndex % 2;

        if (row >= currentGrid.RowDefinitions.Count)
            currentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid.SetRow(btn, row);
        Grid.SetColumn(btn, col);
        currentGrid.Children.Add(btn);
        currentGridIndex++;
    }

    private async void BtnCleanSelected_Click(object sender, RoutedEventArgs e)
    {
        var selectedCaches = checkBoxMap
            .Where(kvp => kvp.Key.IsChecked == true && kvp.Value.Exists)
            .Select(kvp => kvp.Value)
            .ToList();

        if (!selectedCaches.Any())
        {
            MessageBox.Show("Please select at least one cache to clean.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selectedCaches.Any(c => c.Name == "NVIDIA DirectX Shader Cache"))
        {
            var nvidiaChoice = MessageBox.Show(this,
                "NVIDIA App offers an option to clear and rebuild supported shaders.\n\n" +
                "Use Graphics > Global Settings > Shader Cache > three-dot menu > Clear cache, " +
                "then Compile now. Wait for compilation to finish before launching your game.\n\n" +
                "Yes: Show instructions and the option to open NVIDIA App. No cleanup will run.\n" +
                "No: Continue to the ClearSkies cleanup confirmation.\n" +
                "Cancel: Stop without cleaning.",
                "NVIDIA shader cache alternative", MessageBoxButton.YesNoCancel,
                MessageBoxImage.Information, MessageBoxResult.Cancel);
            if (nvidiaChoice == MessageBoxResult.Yes)
            {
                ShowNvidiaAppHelp();
                return;
            }
            if (nvidiaChoice != MessageBoxResult.No) return;
        }

        var totalSize = new CacheInfo { SizeInBytes = selectedCaches.Sum(c => c.SizeInBytes) }.SizeFormatted;
        var result = MessageBox.Show(
            $"This will delete {selectedCaches.Count} cache(s) totaling {totalSize}.\n\n" +
            string.Join("\n", selectedCaches.Select(c => $"{c.Path} — {c.FilePattern ?? "cache files in this folder and subfolders"}")) + "\n\n" +
            "Files currently in use by applications will be skipped.\n\n" +
            "Do you want to continue?",
            "Confirm Cleaning",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        await CleanCaches(selectedCaches);
    }

    private void AppendLog(string message)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => AppendLog(message));
            return;
        }

        txtLog.AppendText(message + Environment.NewLine);
        txtLog.ScrollToEnd();
    }

    private async Task CleanCaches(List<CacheInfo> cachesToClean)
    {
        btnCleanSelected.IsEnabled = false;
        btnSelectAll.IsEnabled = false;
        btnDeselectAll.IsEnabled = false;
        btnSchedule.IsEnabled = false;
        progressBar.Maximum = cachesToClean.Count;
        progressBar.Value = 0;
        progressBar.IsIndeterminate = false;

        txtLog.Clear();
        AppendLog($"=== Starting cache cleanup at {DateTime.Now:HH:mm:ss} ===");
        AppendLog("");

        int cleaned = 0;
        int failed = 0;
        int totalSkipped = 0;
        var errors = new List<string>();

        foreach (var cache in cachesToClean)
        {
            lblStatus.Text = $"Cleaning: {cache.Name}...";

            CleanResult? cleanResult = null;
            await Task.Run(() =>
            {
                cleanResult = cacheManager.CleanCache(cache, AppendLog);
            });

            if (cleanResult!.Success)
            {
                cleaned++;
                totalSkipped += cleanResult.SkippedFiles;
            }
            else
            {
                failed++;
                errors.Add($"{cache.Name}: {cleanResult.Error}");
            }

            progressBar.Value++;
        }

        AppendLog($"=== Cleanup completed at {DateTime.Now:HH:mm:ss} ===");
        AppendLog($"Summary: {cleaned} cache(s) cleaned successfully{(failed > 0 ? $", {failed} failed" : "")}");
        AppendLog("");

        lblStatus.Text = $"Done — {cleaned} cleaned{(failed > 0 ? $", {failed} failed" : "")}";
        lblStatus.Foreground = failed > 0 ? WarningBrush : AccentBrush;

        if (errors.Any())
        {
            MessageBox.Show(
                $"Some caches could not be fully cleaned:\n\n{string.Join("\n", errors)}",
                "Cleaning Completed with Errors",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        else
        {
            MessageBox.Show(
                $"Cleaned {cleaned} cache(s). Skipped {totalSkipped} file(s).",
                "Cleaning Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        if (totalSkipped > 0 && !IsAdministrator())
        {
            MessageBox.Show(
                $"{totalSkipped} file(s) could not be deleted. Close applications using the caches and try again. No deletions were scheduled for restart.",
                "Files skipped",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        progressBar.Value = 0;
        btnCleanSelected.IsEnabled = true;
        btnSelectAll.IsEnabled = true;
        btnDeselectAll.IsEnabled = true;
        btnSchedule.IsEnabled = true;

        ScanCaches();
    }

    private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var kvp in checkBoxMap)
        {
            if (kvp.Value.Exists)
                kvp.Key.IsChecked = true;
        }
    }

    private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var kvp in checkBoxMap)
        {
            kvp.Key.IsChecked = false;
        }
    }

    private void BtnSchedule_Click(object sender, RoutedEventArgs e)
    {
        var scheduleWindow = new ScheduleWindow();
        scheduleWindow.Owner = this;
        scheduleWindow.ShowDialog();
    }

    private void BtnConfigMsfs_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Select the folder containing ROLLINGCACHE.CCC. You can also select the MSFS package folder if its LocalCache contains that file.\n\n" +
            "Manual cleanup deletes only ROLLINGCACHE.CCC. MSFS may recreate it at its default size; restore your preferred size in the simulator afterward.",
            "Select rolling cache",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select folder containing ROLLINGCACHE.CCC"
        };

        if (!string.IsNullOrWhiteSpace(appSettings.MsfsCachePath) && Directory.Exists(appSettings.MsfsCachePath))
        {
            dialog.InitialDirectory = appSettings.MsfsCachePath;
        }

        if (dialog.ShowDialog() == true)
        {
            var selectedPath = dialog.FolderName;

            if (!cacheManager.TryResolveRollingCacheFolder(selectedPath, out var resolvedPath, out var error))
            {
                MessageBox.Show(error, "No rolling cache found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            appSettings.MsfsCachePath = resolvedPath;
            appSettings.Save();
            ScanCaches();
        }
    }

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        txtLog.Clear();
    }

    private void BtnRunAsAdmin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath == null) return;

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            Close();
        }
        catch
        {
            // User cancelled UAC prompt
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click title bar: toggle maximize
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }
}
