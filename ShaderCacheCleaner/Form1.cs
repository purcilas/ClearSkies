using System.Diagnostics;

namespace ShaderCacheCleaner;

public partial class Form1 : Form
{
    private CacheManager cacheManager;
    private List<CacheInfo> currentCaches;
    private AppSettings appSettings;

    public Form1()
    {
        InitializeComponent();
        cacheManager = new CacheManager();
        currentCaches = new List<CacheInfo>();
        appSettings = AppSettings.Load();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        // Check if running as administrator
        if (!IsAdministrator())
        {
            lblStatus.Text = "Not running as Administrator. Some caches may not be accessible.";
            lblStatus.ForeColor = Color.Orange;
        }

        ScanCaches();
    }

    private bool IsAdministrator()
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private async void BtnScan_Click(object sender, EventArgs e)
    {
        ScanCaches();
    }

    private async void ScanCaches()
    {
        btnScan.Enabled = false;
        btnCleanSelected.Enabled = false;
        lblStatus.Text = "Scanning caches...";
        lblStatus.ForeColor = SystemColors.ControlText;
        progressBar.Style = ProgressBarStyle.Marquee;

        await Task.Run(() =>
        {
            currentCaches = cacheManager.GetAllCaches(appSettings.MsfsCachePath);
        });

        UpdateCacheList();

        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = 0;
        lblStatus.Text = "Scan complete";
        btnScan.Enabled = true;
        btnCleanSelected.Enabled = true;
    }

    private void UpdateCacheList()
    {
        listViewCaches.Items.Clear();

        foreach (var cache in currentCaches)
        {
            var item = new ListViewItem(cache.Name);
            item.SubItems.Add(cache.Path);
            item.SubItems.Add(cache.Exists ? cache.SizeFormatted : "Not found");
            item.Tag = cache;

            if (!cache.Exists)
            {
                item.ForeColor = Color.Gray;
            }
            else if (cache.SizeInBytes > 0)
            {
                item.Checked = true; // Auto-select caches that have data
            }

            listViewCaches.Items.Add(item);
        }

        var totalSize = cacheManager.GetTotalCacheSize(currentCaches);
        var formattedSize = new CacheInfo { SizeInBytes = totalSize }.SizeFormatted;
        lblTotalSize.Text = $"Total Cache Size: {formattedSize}";
    }

    private async void BtnCleanSelected_Click(object sender, EventArgs e)
    {
        var selectedCaches = listViewCaches.CheckedItems.Cast<ListViewItem>()
            .Select(item => item.Tag as CacheInfo)
            .Where(cache => cache != null && cache.Exists)
            .Cast<CacheInfo>()
            .ToList();

        if (!selectedCaches.Any())
        {
            MessageBox.Show("Please select at least one cache to clean.", "No Selection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var totalSize = new CacheInfo { SizeInBytes = selectedCaches.Sum(c => c.SizeInBytes) }.SizeFormatted;
        var result = MessageBox.Show(
            $"This will delete {selectedCaches.Count} cache(s) totaling {totalSize}.\n\n" +
            "Files currently in use by applications will be skipped.\n\n" +
            "Do you want to continue?",
            "Confirm Cleaning",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        await CleanCaches(selectedCaches);
    }

    private void AppendLog(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() => AppendLog(message));
            return;
        }

        txtLog.AppendText(message + Environment.NewLine);
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
    }

    private async Task CleanCaches(List<CacheInfo> cachesToClean)
    {
        btnScan.Enabled = false;
        btnCleanSelected.Enabled = false;
        btnSchedule.Enabled = false;
        progressBar.Maximum = cachesToClean.Count;
        progressBar.Value = 0;
        progressBar.Style = ProgressBarStyle.Continuous;

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

        lblStatus.Text = $"Cleaned {cleaned} cache(s). {(failed > 0 ? $"{failed} failed." : "")}";
        lblStatus.ForeColor = failed > 0 ? Color.Orange : Color.Green;

        if (errors.Any())
        {
            MessageBox.Show(
                $"Some caches could not be fully cleaned:\n\n{string.Join("\n", errors)}",
                "Cleaning Completed with Errors",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        else
        {
            MessageBox.Show(
                $"Successfully cleaned {cleaned} cache(s)!",
                "Cleaning Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // Alert user about locked files that couldn't be scheduled for reboot deletion
        if (totalSkipped > 0 && !IsAdministrator())
        {
            MessageBox.Show(
                $"{totalSkipped} file(s) were locked and could not be deleted or scheduled for removal.\n\n" +
                "To handle locked files, right-click the app and select \"Run as administrator\". " +
                "This allows the app to schedule locked files for deletion on the next restart.",
                "Tip: Run as Administrator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        progressBar.Value = 0;
        btnScan.Enabled = true;
        btnCleanSelected.Enabled = true;
        btnSchedule.Enabled = true;

        // Rescan to show new sizes
        ScanCaches();
    }

    private void BtnSelectAll_Click(object sender, EventArgs e)
    {
        foreach (ListViewItem item in listViewCaches.Items)
        {
            var cache = item.Tag as CacheInfo;
            if (cache != null && cache.Exists)
            {
                item.Checked = true;
            }
        }
    }

    private void BtnDeselectAll_Click(object sender, EventArgs e)
    {
        foreach (ListViewItem item in listViewCaches.Items)
        {
            item.Checked = false;
        }
    }

    private void BtnSchedule_Click(object sender, EventArgs e)
    {
        var scheduleForm = new ScheduleForm();
        scheduleForm.ShowDialog(this);
    }

    private void BtnConfigMsfs_Click(object sender, EventArgs e)
    {
        // Show a warning before the folder picker so users know what to look for
        MessageBox.Show(
            "IMPORTANT: Select your MSFS CACHE folder, NOT your Community folder!\n\n" +
            "The cache folder is typically named \"cache\" or \"shadercache\" and is located inside " +
            "your MSFS packages directory.\n\n" +
            "DO NOT select your Community folder — all files in the selected folder will be deleted during cleanup.",
            "WARNING — Read Before Selecting",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);

        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the MSFS CACHE folder (NOT the Community folder!)",
            UseDescriptionForTitle = true
        };

        if (!string.IsNullOrWhiteSpace(appSettings.MsfsCachePath) && Directory.Exists(appSettings.MsfsCachePath))
        {
            dialog.InitialDirectory = appSettings.MsfsCachePath;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var selectedPath = dialog.SelectedPath;

            if (IsCommunityFolder(selectedPath))
            {
                MessageBox.Show(
                    "STOP! You have selected what appears to be the MSFS Community folder!\n\n" +
                    "Cleaning this folder will DELETE all your addons, liveries, and mods.\n\n" +
                    "This selection has been rejected. Please select the correct CACHE folder instead.",
                    "INVALID SELECTION — Community Folder Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            appSettings.MsfsCachePath = selectedPath;
            appSettings.Save();
            ScanCaches();
        }
    }

    private bool IsCommunityFolder(string path)
    {
        var folderName = Path.GetFileName(path);

        // Direct name match
        if (string.Equals(folderName, "Community", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if it contains typical Community folder indicators (addon directories with layout.json/manifest.json)
        try
        {
            var subdirs = Directory.GetDirectories(path);
            int addonCount = 0;
            foreach (var subdir in subdirs.Take(20)) // Check first 20 subdirectories
            {
                if (File.Exists(Path.Combine(subdir, "layout.json")) ||
                    File.Exists(Path.Combine(subdir, "manifest.json")))
                {
                    addonCount++;
                }
                if (addonCount >= 2)
                    return true;
            }
        }
        catch
        {
            // If we can't read the directory, just rely on the name check
        }

        return false;
    }

    private void BtnClearLog_Click(object sender, EventArgs e)
    {
        txtLog.Clear();
    }
}
