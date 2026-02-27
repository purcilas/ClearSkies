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

        var systemCaches = new List<CacheInfo>();
        var msfsCaches = new List<CacheInfo>();

        foreach (var cache in currentCaches)
        {
            if (cache.Name.Contains("MSFS") || cache.Name.Contains("StreamedPackages") || cache.Name.Contains("Content.xml"))
                msfsCaches.Add(cache);
            else
                systemCaches.Add(cache);
        }

        if (systemCaches.Count > 0)
        {
            AddSectionHeader("System & GPU");
            foreach (var cache in systemCaches)
                AddCacheCard(cache);
        }

        AddSectionHeader("MSFS 2020/2024");
        foreach (var cache in msfsCaches)
            AddCacheCard(cache);
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
            Text = cache.Name,
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

        var totalSize = new CacheInfo { SizeInBytes = selectedCaches.Sum(c => c.SizeInBytes) }.SizeFormatted;
        var result = MessageBox.Show(
            $"This will delete {selectedCaches.Count} cache(s) totaling {totalSize}.\n\n" +
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
                $"Successfully cleaned {cleaned} cache(s)!",
                "Cleaning Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        if (totalSkipped > 0 && !IsAdministrator())
        {
            MessageBox.Show(
                $"{totalSkipped} file(s) were locked and could not be deleted or scheduled for removal.\n\n" +
                "To handle locked files, right-click the app and select \"Run as administrator\". " +
                "This allows the app to schedule locked files for deletion on the next restart.",
                "Tip: Run as Administrator",
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
            "IMPORTANT: Select your MSFS CACHE folder, NOT your Community folder!\n\n" +
            "The cache folder is typically named \"cache\" or \"shadercache\" and is located inside " +
            "your MSFS packages directory.\n\n" +
            "DO NOT select your Community folder - all files in the selected folder will be deleted during cleanup.",
            "WARNING - Read Before Selecting",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select the MSFS CACHE folder (NOT the Community folder!)"
        };

        if (!string.IsNullOrWhiteSpace(appSettings.MsfsCachePath) && Directory.Exists(appSettings.MsfsCachePath))
        {
            dialog.InitialDirectory = appSettings.MsfsCachePath;
        }

        if (dialog.ShowDialog() == true)
        {
            var selectedPath = dialog.FolderName;

            if (IsCommunityFolder(selectedPath))
            {
                MessageBox.Show(
                    "STOP! You have selected what appears to be the MSFS Community folder!\n\n" +
                    "Cleaning this folder will DELETE all your addons, liveries, and mods.\n\n" +
                    "This selection has been rejected. Please select the correct CACHE folder instead.",
                    "INVALID SELECTION - Community Folder Detected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            appSettings.MsfsCachePath = selectedPath;
            appSettings.Save();
            ScanCaches();
        }
    }

    private bool IsCommunityFolder(string path)
    {
        var folderName = System.IO.Path.GetFileName(path);

        if (string.Equals(folderName, "Community", StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            var subdirs = Directory.GetDirectories(path);

            foreach (var subdir in subdirs)
            {
                if (string.Equals(System.IO.Path.GetFileName(subdir), "Community", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            foreach (var subdir in subdirs.Take(20))
            {
                if (File.Exists(System.IO.Path.Combine(subdir, "layout.json")) ||
                    File.Exists(System.IO.Path.Combine(subdir, "manifest.json")))
                    return true;
            }
        }
        catch
        {
        }

        return false;
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
