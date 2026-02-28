using System;
using System.IO;
using System.Text.Json;

namespace ClearSkies
{
    public class AppSettings
    {
        public string MsfsCachePath { get; set; } = string.Empty;

        private static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
    }
}
