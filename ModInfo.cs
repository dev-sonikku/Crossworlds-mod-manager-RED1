using System.Collections.Generic;

namespace CrossworldsModManager
{
    public class ModInfo
    {
        public string Name { get; set; } = "Unknown Mod";
        public string Author { get; set; } = "Unknown Author";
        public string Version { get; set; } = "1.0";
        public string Description { get; set; } = "No description provided.";
        public string DirectoryPath { get; set; } = "";

        public ModConfigType ConfigType { get; set; } = ModConfigType.None;
        public string ConfigDescription { get; set; } = "";
        public List<string> ConfigOptions { get; set; } = new();
        public Dictionary<string, string> FileGroupMappings { get; set; } = new();
    }
}