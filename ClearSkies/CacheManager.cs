using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private readonly Dictionary<CacheInfo, (string Path, string? Pattern)> approvedTargets = new();

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
                // Never include manual caches or unrelated .ccc files.
                AddCache(caches, $"{label} Rolling Cache", basePath, label, "ROLLINGCACHE.CCC");

                // SceneryIndexes
                var sceneryPath = Path.Combine(basePath, "SceneryIndexes");
                AddCache(caches, $"{label} SceneryIndexes", sceneryPath, label);
            }

            // Manual MSFS cache path (fallback/override)
            if (!string.IsNullOrWhiteSpace(msfsCachePath))
            {
                if (TryResolveRollingCacheFolder(msfsCachePath, out var resolvedPath, out _) &&
                    !caches.Any(c => string.Equals(c.Path, resolvedPath, StringComparison.OrdinalIgnoreCase) &&
                                     c.FilePattern == "ROLLINGCACHE.CCC"))
                    AddCache(caches, "MSFS Rolling Cache (Manual)", resolvedPath, "MSFS (Manual)", "ROLLINGCACHE.CCC");
            }

            return caches;
        }

        public bool TryResolveRollingCacheFolder(string path, out string resolvedPath, out string error)
        {
            resolvedPath = string.Empty;
            error = "Select a folder containing ROLLINGCACHE.CCC, or an MSFS package folder whose LocalCache contains it. No other files will be cleaned.";
            try
            {
                if (!Path.IsPathFullyQualified(path)) return false;
                var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
                foreach (var candidate in new[] { fullPath, Path.Combine(fullPath, "LocalCache") })
                {
                    var file = Path.Combine(candidate, "ROLLINGCACHE.CCC");
                    if (Directory.Exists(candidate) && File.Exists(file) && !HasReparsePoint(file))
                    {
                        resolvedPath = candidate;
                        error = string.Empty;
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasReparsePoint(string path)
        {
            for (string? current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
            return false;
        }

        private static IEnumerable<FileInfo> EnumerateCacheFiles(string path, string? pattern)
        {
            if (HasReparsePoint(path)) throw new IOException("Linked cache folders are not supported.");
            return new DirectoryInfo(path).EnumerateFiles(pattern ?? "*", new EnumerationOptions
            {
                RecurseSubdirectories = pattern == null,
                AttributesToSkip = FileAttributes.ReparsePoint,
                IgnoreInaccessible = false,
                MatchType = MatchType.Simple
            });
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
            approvedTargets[cache] = (cache.Path, cache.FilePattern);
        }

        private long CalculatePatternSize(string path, string pattern)
        {
            try
            {
                return EnumerateCacheFiles(path, pattern).Sum(file => file.Length);
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
                return EnumerateCacheFiles(path, null)
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

            if (!approvedTargets.TryGetValue(cache, out var target) ||
                cache.Path != target.Path || cache.FilePattern != target.Pattern)
            {
                result.Error = "Cleanup rejected: target was not safely identified by the cache scanner.";
                logCallback?.Invoke(result.Error);
                return result;
            }

            if (!cache.Exists)
            {
                result.Error = "Cache directory does not exist.";
                return result;
            }

            try
            {
                logCallback?.Invoke($"[{cache.Name}] Starting cleanup...");

                // Delete files (filtered by pattern if set, otherwise all files recursively)
                foreach (var file in EnumerateCacheFiles(target.Path, target.Pattern))
                {
                    var relativePath = file.FullName.Replace(cache.Path, "").TrimStart('\\');
                    try
                    {
                        if (HasReparsePoint(file.FullName)) throw new IOException("Linked files are not supported.");
                        file.Delete();
                        result.DeletedFiles++;
                        logCallback?.Invoke($"  ✓ Deleted: {relativePath}");
                    }
                    catch
                    {
                        result.SkippedFiles++;
                        logCallback?.Invoke($"  Skipped: {relativePath} (locked, inaccessible, or linked)");
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
