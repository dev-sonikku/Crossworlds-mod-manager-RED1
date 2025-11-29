using System.IO;
using System.Windows.Forms;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CrossworldsModManager
{
    public static class LocresConverter
    {
        private const string ToolUrl = "https://github.com/anubi47/LocResUtility/releases/download/v2.1.0/LocResUtilityCli-v2.1.0-win-x64.7z";
        private static readonly string ToolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
        private static readonly string ToolExePath = Path.Combine(ToolsDir, "LocResUtilityCli", "LocResUtilityCli.exe");

        public static async Task ConvertToJsonAsync(string locresPath)
        {
            var jsonPath = Path.ChangeExtension(locresPath, ".json");
            try
            {
                string? exePath = await EnsureToolExistsAsync();
                if (exePath == null) return;

                // Use the 'export' command: export <outputPath> <targetPath>
                await RunProcessAsync(exePath, $"export \"{jsonPath}\" \"{locresPath}\" -y");
                MessageBox.Show($"Successfully converted to:\n{jsonPath}",
                    "Conversion Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to convert .locres to .json.\n\nError: {ex.Message}",
                    "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task ConvertToLocresAsync(string jsonPath)
        {
            var locresPath = Path.ChangeExtension(jsonPath, ".locres");
            try
            {
                string? exePath = await EnsureToolExistsAsync();
                if (exePath == null) return;

                var baseLocresPath = Path.Combine(ToolsDir, "Game.locres");
                if (!File.Exists(baseLocresPath))
                {
                    MessageBox.Show($"Base file 'Game.locres' not found in the Tools folder.\nPlease place a clean copy of the game's .locres file there to use as a base for importing.",
                        "Base File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Use the 'import' command: import <outputPath> <targetPath> <sourcePath>
                // outputPath = new .locres, targetPath = base Game.locres, sourcePath = our .json
                await RunProcessAsync(exePath, $"import \"{locresPath}\" \"{baseLocresPath}\" \"{jsonPath}\" -y");
                MessageBox.Show($"Successfully created new .locres at:\n{locresPath}",
                    "Conversion Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to convert .json to .locres.\n\nError: {ex.Message}",
                    "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task ProcessModJsonFilesAsync(IEnumerable<ModInfo> enabledMods, IProgress<string>? progress = null)
        {
            var jsonModifications = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(); // Language -> Namespace -> Key -> Value

            // 1. Collect all JSON modifications from enabled mods, respecting load order (reverse iteration)
            foreach (var modInfo in enabledMods.Reverse())
            {
                var modJsonFiles = Directory.EnumerateFiles(modInfo.DirectoryPath, "*.json", SearchOption.AllDirectories);
                foreach (var jsonFile in modJsonFiles)
                {
                    try
                    {
                        var modJson = JObject.Parse(await File.ReadAllTextAsync(jsonFile));
                        ExtractModifications(modJson, jsonModifications);
                        progress?.Report($"Loaded JSON: {jsonFile}");
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Could not parse or process JSON file '{jsonFile}': {ex.Message}";
                        if (progress != null) progress.Report(msg);
                        else Debug.WriteLine(msg);
                    }
                }
            }

            if (jsonModifications.Count == 0)
            {
                // No JSON files found, nothing to do.
                return;
            }

            // 2. Get base Game.locres and convert to JSON
            string? exePath = await EnsureToolExistsAsync();
            if (exePath == null) return;

            var baseLocresPath = Path.Combine(ToolsDir, "Game.locres");
            if (!File.Exists(baseLocresPath))
            {
                MessageBox.Show($"Base file 'Game.locres' not found in the Tools folder.\nPlease place a clean copy of the game's .locres file there to merge JSON text mods.",
                    "Base File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tempBaseJsonPath = Path.Combine(Path.GetTempPath(), "base_locres.json");
            await RunProcessAsync(exePath, $"export \"{tempBaseJsonPath}\" \"{baseLocresPath}\" -y", progress);
            if (!File.Exists(tempBaseJsonPath)) throw new Exception("Failed to convert base Game.locres to JSON.");

            var baseJson = JObject.Parse(await File.ReadAllTextAsync(tempBaseJsonPath));

            // 3. Apply modifications to the base JSON
            foreach (var langEntry in jsonModifications)
            {
                var lang = langEntry.Key;
                if (baseJson[lang]?["strings"] is not JObject namespaces) continue;

                foreach (var nsEntry in langEntry.Value)
                {
                    var ns = nsEntry.Key;
                    if (namespaces[ns] is not JArray stringEntries) continue;

                    foreach (var keyEntry in nsEntry.Value)
                    {
                        var key = keyEntry.Key;
                        var value = keyEntry.Value;

                        var entryToUpdate = stringEntries.FirstOrDefault(t => t["Key"]?.ToString() == key);
                        if (entryToUpdate != null)
                        {
                            entryToUpdate["Value"] = value;
                        }
                    }
                }
            }

            // 4. Save the final merged JSON to the Tools folder
            var outputJsonPath = Path.Combine(ToolsDir, "Game.json");
            await File.WriteAllTextAsync(outputJsonPath, baseJson.ToString(Newtonsoft.Json.Formatting.Indented));
            progress?.Report($"Merged JSON saved to: {outputJsonPath}");

            // Clean up temp file
            if (File.Exists(tempBaseJsonPath)) File.Delete(tempBaseJsonPath);

            // Notify via progress and also show the message box for users.
            progress?.Report("JSON merge complete.");
            MessageBox.Show($"Successfully merged JSON modifications into:\n{outputJsonPath}", "JSON Merge Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static async Task<string?> EnsureToolExistsAsync()
        {
            if (File.Exists(ToolExePath))
            {
                return ToolExePath;
            }

            var choice = MessageBox.Show("LocResUtility is not found. Would you like to download it now? (approx. 6 MB)",
                "Download Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (choice == DialogResult.No) return null;

            try
            {
                Directory.CreateDirectory(ToolsDir);
                var archivePath = Path.Combine(ToolsDir, "LocResUtility.7z");

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(ToolUrl);
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                using (var archive = SevenZipArchive.Open(archivePath))
                {
                    archive.WriteToDirectory(ToolsDir, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                }

                File.Delete(archivePath);

                if (File.Exists(ToolExePath))
                {
                    MessageBox.Show("LocResUtility downloaded and extracted successfully.", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return ToolExePath;
                }
                throw new FileNotFoundException("Failed to find LocResUtility.exe after extraction.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to download or extract LocResUtility.\n\nError: {ex.Message}", "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private static async Task RunProcessAsync(string fileName, string arguments, IProgress<string>? progress = null)
        {
            var header = $"\n> Running command: {Path.GetFileName(fileName)} {arguments}";
            if (progress != null) progress.Report(header);
            else Console.WriteLine(header);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(fileName, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            // Asynchronously read the output and error streams to avoid deadlocks
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(output))
            {
                var outMsg = $"[Output]:\n{output.Trim()}";
                if (progress != null) progress.Report(outMsg);
                else Console.WriteLine(outMsg);
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                var errMsg = $"[Error]:\n{error.Trim()}";
                if (progress != null) progress.Report(errMsg);
                else Console.WriteLine(errMsg);
            }
        }

        private static void ExtractModifications(JObject modJson, Dictionary<string, Dictionary<string, Dictionary<string, string>>> modifications)
        {
            foreach (var langProperty in modJson.Properties())
            {
                var language = langProperty.Name;
                if (langProperty.Value?["strings"] is not JObject namespaces) continue;

                if (!modifications.ContainsKey(language))
                {
                    modifications[language] = new Dictionary<string, Dictionary<string, string>>();
                }

                foreach (var nsProperty in namespaces.Properties())
                {
                    var ns = nsProperty.Name;
                    if (nsProperty.Value is not JArray stringEntries) continue;

                    if (!modifications[language].ContainsKey(ns))
                    {
                        modifications[language][ns] = new Dictionary<string, string>();
                    }

                    foreach (var entry in stringEntries)
                    {
                        var key = entry["Key"]?.ToString();
                        var value = entry["Value"]?.ToString();
                        if (key != null && value != null) modifications[language][ns][key] = value;
                    }
                }
            }
        }
    }
}