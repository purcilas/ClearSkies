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

        public CacheManager()
        {
            userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
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

            // MSFS Cache (user-configured path)
            if (!string.IsNullOrWhiteSpace(msfsCachePath))
            {
                AddCache(caches, "MSFS Cache Data", msfsCachePath);
            }

            return caches;
        }

        private void AddCache(List<CacheInfo> caches, string name, string path)
        {
            var cache = new CacheInfo
            {
                Name = name,
                Path = path,
                Exists = Directory.Exists(path)
            };

            if (cache.Exists)
            {
                cache.SizeInBytes = CalculateDirectorySize(path);
            }

            caches.Add(cache);
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

                // Delete all files in the directory
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
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

                // Delete empty directories
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
