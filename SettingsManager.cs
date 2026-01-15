using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace CrossworldsModManager
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Environment.GetEnvironmentVariable("APPIMAGE") != null ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bluestar", "settings.json") : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        public static AppSettings Settings { get; private set; } = new AppSettings();

        public static void Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                try
                {
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch (JsonException)
                {
                    // The settings file is corrupt or in an old format.
                    // Delete it and start fresh.
                    File.Delete(SettingsFilePath);
                    Settings = new AppSettings();
                }
            }
        }

        public static void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(Settings, options);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Environment.GetEnvironmentVariable("APPIMAGE") != null)
            {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bluestar")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bluestar"));
                }
            }
            File.WriteAllText(SettingsFilePath, json);
        }
    }
}