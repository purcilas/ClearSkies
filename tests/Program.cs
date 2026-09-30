using ClearSkies;
using System.Reflection;

// All deletion tests use newly created disposable fixtures, never real simulator paths.
var root = Path.Combine(Path.GetTempPath(), "ClearSkiesSafety-" + Guid.NewGuid());
Directory.CreateDirectory(root);
var manager = new CacheManager();
foreach (var field in new[] { "userProfile", "programData", "appData", "localAppData" })
    typeof(CacheManager).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, root);
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}
string Write(string relative, string text = "keep")
{
    var path = Path.Combine(root, relative);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, text);
    return path;
}
var document = Write("Documents/important.txt");
Check(!manager.GetAllCaches(Path.GetDirectoryName(document)).Any(c => c.Category == "MSFS (Manual)"), "Old Documents setting is ignored");
Check(!manager.CleanCache(new CacheInfo { Path = Path.GetDirectoryName(document)!, Exists = true }).Success, "Unregistered recursive cleanup rejected");
var package = Path.Combine(root, "PackageRoot");
var rolling = Write("PackageRoot/LocalCache/ROLLINGCACHE.CCC");
var config = Write("PackageRoot/LocalCache/UserCfg.opt");
var manual = Write("PackageRoot/LocalCache/MANUALCACHE.CCC");
var addon = Write("PackageRoot/LocalCache/Packages/Community/addon/file.txt");
var profile = Write("PackageRoot/LocalState/localprofile");
var target = manager.GetAllCaches(package).Single(c => c.Category == "MSFS (Manual)");
Check(target.Path == Path.GetDirectoryName(rolling) && target.SizeInBytes == 4, "Package root resolves to exact rolling cache; size excludes other data");
target.FilePattern = null;
Check(!manager.CleanCache(target).Success, "Mutated filter rejected at deletion boundary");
target.FilePattern = "ROLLINGCACHE.CCC";
var originalPath = target.Path;
target.Path = Path.GetDirectoryName(document)!;
Check(!manager.CleanCache(target).Success, "Mutated folder rejected at deletion boundary");
target.Path = originalPath;
using (var locked = new FileStream(rolling, FileMode.Open, FileAccess.Read, FileShare.None))
{
    var result = manager.CleanCache(target);
    Check(result.SkippedFiles == 1 && result.PendingRebootFiles == 0, "Locked file skipped without reboot deletion");
}
Check(manager.CleanCache(target).DeletedFiles == 1 && !File.Exists(rolling), "Manual rolling cache cleanup works");
Check(new[] { document, config, manual, addon, profile }.All(File.Exists), "Documents, profile, config, manual cache and addons survive");
Write("Custom/ROLLINGCACHE.CCC");
var custom = manager.GetAllCaches(Path.Combine(root, "Custom")).Single(c => c.Category == "MSFS (Manual)");
Check(manager.CleanCache(custom).DeletedFiles == 1, "Custom rolling cache folder supported");
var autoBase = "Packages/Microsoft.Limitless_8wekyb3d8bbwe/LocalCache/";
Write(autoBase + "UserCfg.opt");
var autoRolling = Write(autoBase + "ROLLINGCACHE.CCC");
var autoManual = Write(autoBase + "MANUALCACHE.CCC");
var scenery = Write(autoBase + "SceneryIndexes/sub/index.dat");
var caches = manager.GetAllCaches();
Check(manager.CleanCache(caches.Single(c => c.Name == "MSFS 2024 Rolling Cache")).DeletedFiles == 1 && File.Exists(autoManual), "Automatic rolling cleanup preserves manual cache");
Check(manager.CleanCache(caches.Single(c => c.Name == "MSFS 2024 SceneryIndexes")).DeletedFiles == 1 && !File.Exists(scenery), "Recognized scenery cache still cleans recursively");
var outside = Write("Outside/valuable.txt");
var junction = Path.Combine(root, autoBase, "SceneryIndexes", "linked");
var start = new System.Diagnostics.ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
start.ArgumentList.Add("-NoProfile");
start.ArgumentList.Add("-Command");
start.ArgumentList.Add($"New-Item -ItemType Junction -Path '{junction}' -Target '{Path.GetDirectoryName(outside)}' -ErrorAction Stop | Out-Null");
using (var process = System.Diagnostics.Process.Start(start)!)
{
    process.WaitForExit();
    Check(process.ExitCode == 0, "Junction fixture created");
}
var linkedCaches = manager.GetAllCaches();
var linkedScenery = linkedCaches.Single(c => c.Name == "MSFS 2024 SceneryIndexes");
Check(linkedScenery.SizeInBytes == 0 && manager.CleanCache(linkedScenery).DeletedFiles == 0 && File.Exists(outside), "Scan and cleanup do not follow directory junctions");
Write("Outside/ROLLINGCACHE.CCC");
Check(!manager.TryResolveRollingCacheFolder(junction, out _, out _), "Manual junction target rejected");
Console.WriteLine("All safety checks passed. Fixtures retained at " + root);
