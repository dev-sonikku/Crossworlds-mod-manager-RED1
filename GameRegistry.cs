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
    public static class GameRegistry
    {
        public const string SteamAppId = "2486820";

        [SupportedOSPlatform("windows")]
        public static Dictionary<string, (string Path, string? AppName)> FindGameInstallations()
        {
            // If not on Windows, return an empty dictionary to avoid crashing.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Dictionary<string, (string Path, string? AppName)>();
            }

            var installations = new Dictionary<string, (string Path, string? AppName)>();

            // Find Steam
            var steamPath = FindSteamGamePath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                installations["Steam"] = (steamPath, null);
            }

            return installations;
        }

        [SupportedOSPlatform("windows")]
        private static string? FindSteamGamePath()
        {
            try
            {
                // 1. Find Steam's own installation path from the registry
                using var steamKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                // The value could be null, so we declare it as nullable.
                var steamInstallPath = steamKey?.GetValue("InstallPath") as string; 

                if (string.IsNullOrEmpty(steamInstallPath)) return null;

                // 2. Find all library folders
                var libraryFoldersFile = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(libraryFoldersFile)) return null;

                var libraryPaths = new List<string> { steamInstallPath };
                var vdfContent = File.ReadAllText(libraryFoldersFile);
                var matches = Regex.Matches(vdfContent, @"""path""\s+""(.+?)""");
                libraryPaths.AddRange(matches.Cast<Match>().Select(m => m.Groups[1].Value.Replace(@"\\", @"\")));

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
    }
}