using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ClearSkies
{
    public class CacheInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public bool Exists { get; set; }
        public string Category { get; set; } = "System & GPU";
        public string? FilePattern { get; set; }

        public string SizeFormatted => FormatBytes(SizeInBytes);

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class CleanResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public int DeletedFiles { get; set; }
        public int PendingRebootFiles { get; set; }
        public int SkippedFiles { get; set; }
    }

    public class CacheManager
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, int dwFlags);

        private const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        private readonly string userProfile;
        private readonly string programData;
        private readonly string appData;
        private readonly string localAppData;

        private static readonly (string Label, string RelativePath)[] MsfsInstallPaths =
        {
            ("MSFS 2020", @"Microsoft Flight Simulator"),
            ("MSFS 2024", @"Microsoft Flight Simulator 2024"),
        };

        private static readonly (string Label, string RelativePath)[] MsfsStorePaths =
        {
            ("MSFS 2020", @"Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache"),
            ("MSFS 2024", @"Packages\Microsoft.Limitless_8wekyb3d8bbwe\LocalCache"),
        };

        public CacheManager()
        {
            userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        public List<(string Label, string BasePath)> DetectMsfsInstallations()
        {
            var installations = new List<(string Label, string BasePath)>();

            // Check Steam paths (%AppData%\Microsoft Flight Simulator[\2024])
            foreach (var (label, rel) in MsfsInstallPaths)
            {
                var basePath = Path.Combine(appData, rel);
                if (File.Exists(Path.Combine(basePath, "UserCfg.opt")))
                    installations.Add(($"{label}", basePath));
            }

            // Check MS Store paths (%LocalAppData%\Packages\...)
            foreach (var (label, rel) in MsfsStorePaths)
            {
                var basePath = Path.Combine(localAppData, rel);
                // Only add if not already found via Steam path (avoid duplicates for same version)
                if (File.Exists(Path.Combine(basePath, "UserCfg.opt")) &&
                    !installations.Any(i => i.Label == label))
                    installations.Add(($"{label}", basePath));
            }

            return installations;
        }

        public List<CacheInfo> GetAllCaches(string? msfsCachePath = null)
        {
            var caches = new List<CacheInfo>();

            // NVIDIA DXCache
            AddCache(caches, "NVIDIA DirectX Shader Cache",
                Path.Combine(userProfile, @"AppData\Local\NVIDIA\DXCache"));

            // NVIDIA GLCache
            AddCache(caches, "NVIDIA OpenGL Shader Cache",
                Path.Combine(userProfile, @"AppData\Local\NVIDIA\GLCache"));

            // NVIDIA NV_Cache
            AddCache(caches, "NVIDIA GPU Cache",
                Path.Combine(programData, @"NVIDIA Corporation\NV_Cache"));

            // DirectX Shader Cache
            AddCache(caches, "DirectX Shader Cache",
                Path.Combine(userProfile, @"AppData\Local\D3DSCache"));

            // AMD Shader Cache (in case user has/had AMD GPU)
            AddCache(caches, "AMD DX11 Shader Cache",
                Path.Combine(userProfile, @"AppData\Local\AMD\DxCache"));

            AddCache(caches, "AMD DX12 Shader Cache",
                Path.Combine(userProfile, @"AppData\Local\AMD\DxcCache"));

            // Auto-detect MSFS installations
            var msfsInstalls = DetectMsfsInstallations();
            foreach (var (label, basePath) in msfsInstalls)
            {
                // Rolling Cache — always show if MSFS is detected, only targets .ccc files
                AddCache(caches, $"{label} Rolling Cache", basePath, label, "*.ccc");

                // SceneryIndexes
                var sceneryPath = Path.Combine(basePath, "SceneryIndexes");
                AddCache(caches, $"{label} SceneryIndexes", sceneryPath, label);
            }

            // Manual MSFS cache path (fallback/override)
            if (!string.IsNullOrWhiteSpace(msfsCachePath))
            {
                // Only add if not already covered by auto-detection
                bool alreadyCovered = msfsInstalls.Any(i =>
                    msfsCachePath.StartsWith(i.BasePath, StringComparison.OrdinalIgnoreCase));
                if (!alreadyCovered)
                    AddCache(caches, "MSFS Cache (Manual)", msfsCachePath, "MSFS (Manual)");
            }

            return caches;
        }

        private bool HasCacheFiles(string path)
        {
            try
            {
                return Directory.Exists(path) &&
                       Directory.EnumerateFiles(path, "*.ccc").Any();
            }
            catch
            {
                return false;
            }
        }

        private void AddCache(List<CacheInfo> caches, string name, string path, string? category = null, string? filePattern = null)
        {
            var cache = new CacheInfo
            {
                Name = name,
                Path = path,
                Exists = Directory.Exists(path),
                Category = category ?? "System & GPU",
                FilePattern = filePattern
            };

            if (cache.Exists)
            {
                cache.SizeInBytes = filePattern != null
                    ? CalculatePatternSize(path, filePattern)
                    : CalculateDirectorySize(path);
            }

            caches.Add(cache);
        }

        private long CalculatePatternSize(string path, string pattern)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                return dirInfo.EnumerateFiles(pattern).Sum(file => file.Length);
            }
            catch
            {
                return 0;
            }
        }

        private long CalculateDirectorySize(string path)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);
            }
            catch
            {
                return 0;
            }
        }

        public CleanResult CleanCache(CacheInfo cache, Action<string>? logCallback = null)
        {
            var result = new CleanResult();

            if (!cache.Exists)
            {
                result.Error = "Cache directory does not exist.";
                return result;
            }

            try
            {
                var dirInfo = new DirectoryInfo(cache.Path);

                logCallback?.Invoke($"[{cache.Name}] Starting cleanup...");

                // Delete files (filtered by pattern if set, otherwise all files recursively)
                var searchPattern = cache.FilePattern ?? "*";
                var searchOption = cache.FilePattern != null ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
                foreach (var file in dirInfo.EnumerateFiles(searchPattern, searchOption))
                {
                    var relativePath = file.FullName.Replace(cache.Path, "").TrimStart('\\');
                    try
                    {
                        file.Delete();
                        result.DeletedFiles++;
                        logCallback?.Invoke($"  ✓ Deleted: {relativePath}");
                    }
                    catch
                    {
                        // File is locked — schedule deletion on next reboot
                        if (MoveFileEx(file.FullName, null, MOVEFILE_DELAY_UNTIL_REBOOT))
                        {
                            result.PendingRebootFiles++;
                            logCallback?.Invoke($"  ⏳ Pending reboot: {relativePath}");
                        }
                        else
                        {
                            result.SkippedFiles++;
                            logCallback?.Invoke($"  ✗ Skipped: {relativePath} (file is locked)");
                        }
                    }
                }

                // Delete empty directories (skip when using file pattern to avoid touching parent dir)
                if (cache.FilePattern == null)
                {
                    foreach (var dir in dirInfo.EnumerateDirectories("*", SearchOption.AllDirectories).OrderByDescending(d => d.FullName.Length))
                    {
                        try
                        {
                            if (!dir.EnumerateFileSystemInfos().Any())
                            {
                                var relativePath = dir.FullName.Replace(cache.Path, "").TrimStart('\\');
                                dir.Delete();
                                logCallback?.Invoke($"  ✓ Removed directory: {relativePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Could not delete directory {dir.Name}: {ex.Message}");
                        }
                    }
                }

                var summary = $"[{cache.Name}] Completed: {result.DeletedFiles} deleted";
                if (result.PendingRebootFiles > 0)
                    summary += $", {result.PendingRebootFiles} scheduled for reboot";
                if (result.SkippedFiles > 0)
                    summary += $", {result.SkippedFiles} skipped";
                logCallback?.Invoke(summary);
                logCallback?.Invoke("");

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                logCallback?.Invoke($"[{cache.Name}] ERROR: {ex.Message}");
                logCallback?.Invoke("");
                return result;
            }
        }

        public long GetTotalCacheSize(List<CacheInfo> caches)
        {
            return caches.Where(c => c.Exists).Sum(c => c.SizeInBytes);
        }
    }
}
