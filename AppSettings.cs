using System.Collections.Generic;

namespace CrossworldsModManager
{
    public class ModProfile
    {
        public List<string> EnabledMods { get; set; } = new();
        public Dictionary<string, string> ModConfigurations { get; set; } = new();
        public List<string> ModLoadOrder { get; set; } = new();
    }

    public class AppSettings
    {
        public string? GameDirectory { get; set; }
        public string? ModsDirectory { get; set; }

        public Dictionary<string, ModProfile> Profiles { get; set; } = new();
        public string? ActiveProfileName { get; set; }

        public bool SortEnabledModsToTop { get; set; } = true;
        public bool AutoCleanTemporaryFiles { get; set; } = true;
        public bool CheckForGamesOnStartup { get; set; } = true;
        public bool AutoCloseLogOnSuccess { get; set; } = false;

        // If true, the app will NOT create automatic backups before operations.
        // Default is false (automatic backups enabled).
        public bool DoNotBackupModsAutomatically { get; set; } = true;

        // If true, the app will NOT show confirmation when enabling/disabling all mods.
        // Default is false (show confirmation).
        public bool DoNotConfirmEnableDisable { get; set; } = false;

        // Deprecated properties for migration
        public List<string>? EnabledMods { get; set; }
        public Dictionary<string, string>? ModConfigurations { get; set; }
        public List<string>? ModLoadOrder { get; set; }

        public bool DeveloperModeEnabled { get; set; } = false;

        // Developer Mode Settings
        public string? DeveloperExportPath { get; set; }
        public List<string> DeveloperEnabledFiles { get; set; } = new();
        public bool SuppressExFatWarning { get; set; } = false;
    }
}