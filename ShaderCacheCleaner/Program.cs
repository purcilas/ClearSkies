using System.Windows;

namespace ShaderCacheCleaner;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0].ToLower() == "/clean")
        {
            RunAutomaticClean();
            return;
        }

        var app = new Application();

        // Load theme resources
        var theme = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Theme.xaml")
        };
        app.Resources.MergedDictionaries.Add(theme);

        app.Run(new MainWindow());
    }

    private static void RunAutomaticClean()
    {
        try
        {
            var settings = AppSettings.Load();
            var cacheManager = new CacheManager();
            var caches = cacheManager.GetAllCaches(settings.MsfsCachePath);
            var existingCaches = caches.Where(c => c.Exists && c.SizeInBytes > 0).ToList();

            foreach (var cache in existingCaches)
            {
                cacheManager.CleanCache(cache);
            }

            System.Diagnostics.Debug.WriteLine($"Automatic cache cleaning completed. Cleaned {existingCaches.Count} caches.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during automatic cleaning: {ex.Message}");
        }
    }
}
