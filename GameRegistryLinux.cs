using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CrossworldsModManager
{
    public static class GameRegistryLinux
    {
        public const string SteamAppId = "2486820";
        private const string GameDisplayName = "Sonic Racing: Crossworlds"; // Used for Epic detection

        [SupportedOSPlatform("linux")]
        public static Dictionary<string, (string Path, string? AppName)> FindGameInstallations()
        {

            var installations = new Dictionary<string, (string Path, string? AppName)>();

            // Find Steam
            var steamPath = FindSteamGamePath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                installations["Steam"] = (steamPath, null);
            }

            return installations;
        }

        [SupportedOSPlatform("linux")]
        private static string? FindSteamGamePath()
        {
            try
            {
                // 1. Get Steam path
                var steamInstallPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam");

                // 2. Find all library folders
                var libraryFoldersFile = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(libraryFoldersFile)) return null;

                var libraryPaths = new List<string> { steamInstallPath };
                var vdfContent = File.ReadAllText(libraryFoldersFile);
                var matches = Regex.Matches(vdfContent, @"""path""\s+""(.+?)""");
                libraryPaths.AddRange(matches.Cast<Match>().Select(match => match.Groups[1].Value));

                // 3. Check each library for the game's appmanifest
                foreach (var libraryPath in libraryPaths.Distinct())
                {
                    var appManifestPath = Path.Combine(libraryPath, "steamapps", $"appmanifest_{SteamAppId}.acf");
                    if (File.Exists(appManifestPath))
                    {
                        // 4. Parse the manifest to get the installation directory
                        var manifestContent = File.ReadAllText(appManifestPath);
                        var match = Regex.Match(manifestContent, @"""installdir""\s+""(.+?)""");
                        if (match.Success)
                        {
                            return Path.Combine(libraryPath, "steamapps", "common", match.Groups[1].Value);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Suppress errors (e.g., registry access denied)
            }
            return null;
        }

        [SupportedOSPlatform("linux")]
        private static (string Path, string AppName)? FindEpicGameInfo()
        {
            return null;
        }
    }
}