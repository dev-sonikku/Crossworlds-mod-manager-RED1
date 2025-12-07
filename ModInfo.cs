using System.Collections.Generic;
using System.Linq;

namespace CrossworldsModManager
{
    public class ModInfo
    {
        public string Name { get; set; } = "Unknown Mod";
        public string Author { get; set; } = "Unknown Author";
        public string Version { get; set; } = "1.0";
        public string Description { get; set; } = "No description provided.";
        public string DirectoryPath { get; set; } = "";

        // New properties for multi-group configuration
        public List<ModConfigurationGroup> ConfigurationGroups { get; set; } = new();
        public Dictionary<string, string> FileGroupMappings { get; set; } = new();
    }

    public class ModConfigurationGroup
    {
        public string GroupName { get; set; }
        public ModConfigType Type { get; set; }
        public string Description { get; set; }
        public List<string> Options { get; set; }

        public ModConfigurationGroup(string configSectionKey, Dictionary<string, string> configSection)
        {
            // Extract GroupName from "Config:GroupName"
            GroupName = configSectionKey.Split(':').Last().Trim();

            string typeStr = configSection.GetValueOrDefault("Type", "SelectOne");
            if (System.Enum.TryParse<ModConfigType>(typeStr, true, out var configType))
            {
                Type = configType;
            }
            else
            {
                Type = ModConfigType.SelectOne; // Default to SelectOne if parsing fails
            }
            Description = configSection.GetValueOrDefault("Description", $"Select an option for {GroupName}:");
            Options = configSection.GetValueOrDefault("Options", "").Split(',').Select(o => o.Trim()).ToList();
        }
    }
}