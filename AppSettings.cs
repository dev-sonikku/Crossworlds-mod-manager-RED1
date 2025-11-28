using System.Collections.Generic;

namespace CrossworldsModManager
{
    public class AppSettings
    {
        public string? GameDirectory { get; set; }
        public string? ModsDirectory { get; set; }
        public List<string> EnabledMods { get; set; } = new();
        // Key: Mod Name, Value: For SelectOne, it's the single selected option.
        // For SelectMultiple, it's a comma-separated string of enabled options.
        public Dictionary<string, string> ModConfigurations { get; set; } = new();
        public List<string> ModLoadOrder { get; set; } = new();
    }
}