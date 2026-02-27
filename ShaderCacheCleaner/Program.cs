namespace ShaderCacheCleaner;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // Check for command-line arguments
        if (args.Length > 0 && args[0].ToLower() == "/clean")
        {
            // Run automatic cleaning without UI
            RunAutomaticClean();
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
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

            // Log completion (optional - could write to event log)
            System.Diagnostics.Debug.WriteLine($"Automatic cache cleaning completed. Cleaned {existingCaches.Count} caches.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during automatic cleaning: {ex.Message}");
        }
    }
}