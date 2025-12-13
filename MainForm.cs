using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public partial class MainForm : Form
    {
        // Struct for receiving WM_COPYDATA messages
        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }
        private const int WM_COPYDATA = 0x004A;

        // This is a common executable name pattern for Unreal Engine games.
        // We check for the process name without the .exe extension.
        private const string GameProcessName = "SonicRacingCrossWorldsSteam";
        private Dictionary<string, (string Path, string? AppName)> _gameInstallations = new();
        private List<ListViewItem> _allModItems = new List<ListViewItem>();
        private string? _selectedPlatform;
        private LogForm? _logForm;
        private DeveloperForm? _devForm;
        private Button? btnBrowseMods; // Added for GameBanana browser
        private readonly string? _oneClickUrl;

        public MainForm(string? oneClickUrl, string appVersion)
        {
            InitializeComponent();
            // Set the form's icon from the executable's embedded icon.
            _oneClickUrl = oneClickUrl;
            this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // Use the version in the title bar
            this.Text = $"Blue Star Manager v{appVersion} - A Sonic Racing: CrossWorlds Mod Manager";

            // Apply the custom dark theme renderer for menus and tool strips
            ToolStripManager.Renderer = new DarkThemeMenuRenderer(new DarkThemeColorTable());
            LoadSettingsAndSetup();

            // Enable drag-and-drop for reordering
            modListView.AllowDrop = true;
            modListView.ItemDrag += modListView_ItemDrag;
            modListView.DragEnter += modListView_DragEnter;
            modListView.DragDrop += modListView_DragDrop;

            // Create debug log window but hide it by default; user can show it via button.
            try
            {
                _logForm = new LogForm();
                _logForm.Hide();
                btnToggleDebugLog.Enabled = true; // Enable the button now that log exists

                // Programmatically add the "Browse Mods" button below the debug log button
                btnBrowseMods = new Button
                {
                    Name = "btnBrowseMods",
                    Text = "Browse Mods...",
                    Anchor = btnToggleDebugLog.Anchor,
                    Location = new Point(btnToggleDebugLog.Location.X, btnToggleDebugLog.Location.Y + btnToggleDebugLog.Height + 6),
                    Size = btnToggleDebugLog.Size,
                    FlatStyle = btnToggleDebugLog.FlatStyle,
                    ForeColor = btnToggleDebugLog.ForeColor,
                    BackColor = btnToggleDebugLog.BackColor
                };
                btnBrowseMods.Click += btnBrowseMods_Click;
                this.Controls.Add(btnBrowseMods);
                btnBrowseMods.BringToFront(); // Ensure it's visible
            }
            catch
            {
                // If creating the log window fails, ignore and continue — logging will still use Console/Debug.
            }
        }

        private void LoadSettingsAndSetup()
        {
            SettingsManager.Load();

            MigrateToProfiles();

            if (string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory))
            {
                PromptForModsDirectory();
            }

            if (SettingsManager.Settings.CheckForGamesOnStartup)
            {
                DetectGameInstallations();
            }

            UpdateProfilesMenu();
            // Load the list of mods when the application starts.
            RefreshModList();
        }
        
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // This needs to be called after the main form is shown to position correctly.
            ToggleDeveloperForm();

            if (!string.IsNullOrEmpty(_oneClickUrl))
            {
                HandleOneClickInstallAsync(_oneClickUrl);
            }

            // Check for updates after the form is visible.
            Program.CheckForUpdates();

            // Check for mod updates after the form is visible.
            _ = CheckForModUpdatesAsync();
        }

        protected override void WndProc(ref Message m)
        {
            // Listen for WM_COPYDATA messages from other instances
            if (m.Msg == WM_COPYDATA)
            {
                try
                {
                    var cds = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT))!;
                    var url = Marshal.PtrToStringAnsi(cds.lpData);

                    if (!string.IsNullOrEmpty(url))
                    {
                        // Handle the URL on the UI thread
                        this.BeginInvoke((Action)(() => HandleOneClickInstallAsync(url)));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing WM_COPYDATA: {ex.Message}");
                }
            }
            base.WndProc(ref m);
        }

        private void DetectGameInstallations()
        {
            _gameInstallations = GameRegistry.FindGameInstallations();
        
            // If we found a game and the settings path is empty, auto-configure it.
            if (_gameInstallations.Any() && string.IsNullOrWhiteSpace(SettingsManager.Settings.GameDirectory))
            {
                var firstInstall = _gameInstallations.First();
                SettingsManager.Settings.GameDirectory = firstInstall.Value.Path;
                SettingsManager.Save();
                UpdateStatus($"Automatically set game directory to: {firstInstall.Value.Path}");
            }
        
            launchPlatformDropDown.DropDownItems.Clear();
        
            // Always provide options for Steam and Epic, using detected paths or falling back to the settings path.
            var platformsToShow = new List<string> { "Steam", "Epic Games" };
            foreach (var platformName in platformsToShow)
            {
                var item = new ToolStripMenuItem(platformName);
                item.Tag = platformName;
                item.Click += PlatformMenuItem_Click;
                item.ForeColor = Color.White;
                item.BackColor = Color.FromArgb(45, 45, 48);
                launchPlatformDropDown.DropDownItems.Add(item);
            }
        
            // Add a "Custom" option if a game directory is set in the settings.
            if (!string.IsNullOrWhiteSpace(SettingsManager.Settings.GameDirectory))
            {
                var customItem = new ToolStripMenuItem("Custom");
                customItem.Tag = "Custom";
                customItem.Click += PlatformMenuItem_Click;
                customItem.ForeColor = Color.White;
                customItem.BackColor = Color.FromArgb(45, 45, 48);
                launchPlatformDropDown.DropDownItems.Add(customItem);
            }
        
            // Select the first detected platform, or the first available option.
            if (launchPlatformDropDown.DropDownItems.Count > 0)
            {
                var defaultSelection = _gameInstallations.Any()
                    ? launchPlatformDropDown.DropDownItems.Cast<ToolStripMenuItem>().FirstOrDefault(i => i.Text == _gameInstallations.Keys.First())
                    : launchPlatformDropDown.DropDownItems[0];
        
                if (defaultSelection != null)
                    PlatformMenuItem_Click(defaultSelection, EventArgs.Empty);
            }
        }

        private void PlatformMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not string platform) return;

            _selectedPlatform = platform; // This is safe now
            launchPlatformDropDown.Text = _selectedPlatform; // This is safe now
            UpdateStatus($"Selected platform: {_selectedPlatform}");
        }
        
        /// <summary>
        /// Applies mod configuration by enabling/disabling files based on selected options.
        /// </summary>
        /// <param name="modInfo">The mod to configure.</param>
        /// <param name="selectedOptionIdentifiers">A list of "GroupName.OptionName" identifiers for the enabled options.</param>
        private void ApplyModConfiguration(ModInfo modInfo, List<string> selectedOptionIdentifiers)
        {
            if (modInfo.FileGroupMappings.Count == 0) return;

            // A special identifier "enable" is used for simple, non-configurable mods that are checked.
            // This block handles enabling all files for such mods.
            if (selectedOptionIdentifiers.Contains("enable"))
            {
                foreach (var file in Directory.EnumerateFiles(modInfo.DirectoryPath, "*.*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Move(file, file.Replace(".disabled", ""));
                    }
                }
                return;
            }

            // Iterate through all possible files defined in the [Files] section.
            foreach (var fileMapping in modInfo.FileGroupMappings)
            {
                var fileBase = fileMapping.Key;
                if (!modInfo.FileGroupMappings.TryGetValue(fileBase, out var group) || group == null) continue;

                // Correctly separate the directory and the base filename from the mod.ini entry.
                string combinedPath = Path.Combine(modInfo.DirectoryPath, fileBase);
                string? directory = Path.GetDirectoryName(combinedPath);
                string baseName = Path.GetFileName(combinedPath);

                if (string.IsNullOrEmpty(directory)) continue;

                // Find all related files (.pak, .utoc, .ucas, etc.)
                var filesToProcess = Directory.GetFiles(Path.GetFullPath(directory), baseName + ".*");

                foreach (var filePath in filesToProcess)
                {
                    // Ignore text-based config files
                    string ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (ext == ".ini" || ext == ".txt" || ext == ".md") continue;

                    bool shouldBeEnabled = selectedOptionIdentifiers.Contains(group);
                    string enabledPath = filePath.Replace(".disabled", "");
                    string disabledPath = enabledPath + ".disabled";

                    try
                    {
                        if (shouldBeEnabled && File.Exists(disabledPath))
                        {
                            File.Move(disabledPath, enabledPath);
                        }
                        else if (!shouldBeEnabled && File.Exists(enabledPath))
                        {
                            File.Move(enabledPath, disabledPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to apply option for '{baseName}': {ex.Message}",
                            "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            // Disable the Play button during save and show status
            btnPlay.Enabled = false;
            btnPlay.ForeColor = Color.Gray;
            btnPlay.Text = "Saving...";

            // Before applying configurations, check for newly enabled mods that need a default config.
            bool defaultsSet = false;
            // Use a copy of the items to iterate over, as the collection can be modified.
            var itemsToProcess = modListView.Items.Cast<ListViewItem>().ToList();
            foreach (ListViewItem item in modListView.Items)
            {
                if (item.Checked && item.Tag is ModInfo modInfo && modInfo.ConfigurationGroups.Any())
                {   
                    var activeProfile = GetActiveProfile();
                    if (activeProfile == null) continue;

                    // If a configurable mod is enabled but has no saved configuration, set the default.
                    foreach (var group in modInfo.ConfigurationGroups)
                    {
                        var configKey = $"{modInfo.Name}:{group.GroupName}";
                        if (!activeProfile.ModConfigurations.ContainsKey(configKey) && group.Options.Any())
                        {
                            // Set the default option for this group
                            activeProfile.ModConfigurations[configKey] = group.Options.First();
                            defaultsSet = true;
                        }
                    }
                }
            }

            if (defaultsSet) SettingsManager.Save(); // Save the new default settings.

            // Sort the view to bring enabled mods to the top, if the setting is enabled.
            SortModsView();

            // Now that defaults are set, save the final state of enabled mods and their order.
            SaveModListState();

            // Show progress in the status bar
            progressBar.Visible = true;
            progressBar.Value = 0;

            IProgress<string> progress = new Progress<string>(s =>
            {
                UpdateStatus(s);
                if (_logForm != null && !_logForm.IsDisposed)
                {
                    _logForm.AppendLog(s);
                }
            });
            IProgress<int> progressBarProgress = new Progress<int>(p => progressBar.Value = p);

            try
            {
                // Then, apply the current configurations for all mods.
                foreach (ListViewItem item in modListView.Items)
                {
                    if (item.Tag is ModInfo modInfo && modInfo.ConfigurationGroups.Any())
                    {
                        // Get the saved selection for this mod.
                        var activeProfile = GetActiveProfile();
                        if (activeProfile == null) continue;

                        var selectedOptionIdentifiers = new List<string>();
                        foreach (var group in modInfo.ConfigurationGroups)
                        {
                            var configKey = $"{modInfo.Name}:{group.GroupName}";
                            if (activeProfile.ModConfigurations.TryGetValue(configKey, out var selectedValue))
                            {
                                // For SelectMultiple, the value is comma-separated. Split it.
                                var options = selectedValue.Split(',').Select(s => s.Trim());
                                foreach (var option in options)
                                {
                                    selectedOptionIdentifiers.Add($"{group.GroupName}.{option}");
                                }
                            }
                        }
                        ApplyModConfiguration(modInfo, selectedOptionIdentifiers);
                    }
                    else if (item.Tag is ModInfo modInfoWithoutConfig) // Handle non-configurable mods
                    {
                        ApplyModConfiguration(modInfoWithoutConfig, item.Checked ? new List<string> { "enable" } : new List<string>());
                    }
                }

                progressBarProgress.Report(10);
                // Check if any enabled mods have JSON files.
                var enabledModsWithJson = modListView.Items.Cast<ListViewItem>()
                    .Where(i => i.Checked && i.Tag is ModInfo modInfo && // Only look at checked mods
                               // Enumerate all .json files, but crucially, ignore any that are currently disabled.
                               // This ensures we only merge JSON files that correspond to the user's selected configuration.
                               Directory.EnumerateFiles(modInfo.DirectoryPath, "*.json", SearchOption.AllDirectories)
                                   .Any(jsonPath => !jsonPath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
/*
                                Directory.EnumerateFiles(modInfo.DirectoryPath, "*.json", SearchOption.AllDirectories)
                                    .Any(jsonPath => {
                                        if (Path.GetFileName(jsonPath).Equals("info.json", StringComparison.OrdinalIgnoreCase))
                                        {
                                            // Check if it's a metadata file to be ignored.
                                            var content = File.ReadAllText(jsonPath);
                                            return !(content.Contains("\"name\"") && content.Contains("\"version\"") &&
                                                     content.Contains("\"author\"") && content.Contains("\"mod_type\""));
                                        }
                                        return true; // It's not info.json, so include it.
                                    })
*/
                    )
                    .ToList();

                if (enabledModsWithJson.Count == 0)
                {
                    progressBarProgress.Report(20);
                    progress.Report("No enabled mods with .json files found. Cleaning up old merged pak...");
                    if (!string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var gameInfo))
                    {
                        var targetModsDir = Path.Combine(gameInfo.Path, "UNION", "Content", "Paks", "~mods");
                        var locresPakPath = Path.Combine(targetModsDir, "LocresMod.pak");
                        if (File.Exists(locresPakPath))
                        {
                            try
                            {
                                File.Delete(locresPakPath);
                                progress.Report($"Deleted old merged pak: {locresPakPath}");
                            }
                            catch (Exception ex)
                            {
                                progress.Report($"Failed to delete old merged pak: {ex.Message}");
                            }
                        }
                        else
                        {
                            progress.Report("No old merged pak found to delete.");
                        }
                    }
                    progressBarProgress.Report(50);
                    await InstallModsAsync(); // Still install other mods
                    progressBarProgress.Report(100);
                    progress.Report("\n✓ Save and install complete!");
                }
                else
                {
                    // Run the full merge/pack process since there are JSON mods.
                    progressBarProgress.Report(20);
                    var installTask = InstallModsAsync();
                    // Pass the list of mods that we've already determined have active JSON files.
                    // This prevents the converter from having to re-scan all mods.
                    var modsToProcessForJson = enabledModsWithJson.Select(i => i.Tag as ModInfo).Where(m => m != null);

                    var jsonTask = LocresConverter.ProcessModJsonFilesAsync(modsToProcessForJson!, progress);

                    await Task.WhenAll(installTask, jsonTask);
                    progressBarProgress.Report(50);

                    // After merging JSON, automatically pack the results back to .locres
                    progress.Report("\nStarting to pack merged .locres files...");
                    var gamePathForPack = !string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var _gameInfoForPack)
                        ? _gameInfoForPack.Path
                        : string.Empty;
                    await LocresConverter.PackMergedLocresAsync(gamePathForPack, progress);
                    progressBarProgress.Report(80);

                    // After the pack operation completes, install the final produced pak into the game's ~mods folder.
                    try
                    {
                        var toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
                        var sourcePak = Path.Combine(toolsDir, "LocresMod.pak");
                        if (!File.Exists(sourcePak))
                        {
                            var found = Directory.GetFiles(toolsDir, "LocresMod.pak", SearchOption.AllDirectories).FirstOrDefault();
                            if (!string.IsNullOrEmpty(found)) sourcePak = found;
                        }

                        if (File.Exists(sourcePak))
                        {
                            if (!string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var gameInfo))
                            {
                                var targetModsDir = Path.Combine(gameInfo.Path, "UNION", "Content", "Paks", "~mods");
                                Directory.CreateDirectory(targetModsDir);
                                var destPak = Path.Combine(targetModsDir, "LocresMod.pak");
                                try
                                {
                                    if (File.Exists(destPak)) File.Delete(destPak);
                                    File.Move(sourcePak, destPak);
                                    progress.Report($"Moved merged pak to ~mods: {destPak}");
                                }
                                catch (Exception moveEx)
                                {
                                    try
                                    {
                                        File.Copy(sourcePak, destPak, true);
                                        File.Delete(sourcePak);
                                        progress.Report($"Copied merged pak to ~mods (fallback): {destPak}");
                                    }
                                    catch (Exception copyEx)
                                    {
                                        progress.Report($"Failed to move or copy merged pak: {moveEx.Message}; {copyEx.Message}");
                                    }
                                }
                            }
                            else
                            {
                                progress.Report("Merged pak exists, but game installation not found to install it.");
                            }
                        }
                        else
                        {
                            progress.Report("No merged pak found in Tools to install.");
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.Report($"Failed to install merged pak: {ex.Message}");
                    }

                    progressBarProgress.Report(100);
                    progress.Report("\n✓ All tasks completed successfully!");
                }
            }
            catch (Exception ex)
            {
                progress.Report($"\nAn error occurred during the save process: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;

                if (SettingsManager.Settings.AutoCleanTemporaryFiles)
                {
                    // Cleanup the LocresMod folder from the Tools directory as it's no longer needed.
                    var locresModTempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "LocresMod");
                    if (Directory.Exists(locresModTempPath))
                    {
                        try { Directory.Delete(locresModTempPath, true); progress.Report($"Cleaned up temporary folder: {locresModTempPath}"); }
                        catch (Exception ex) { progress.Report($"Could not clean up temporary folder: {ex.Message}"); }
                    }

                    // Cleanup the merged Game_*.json files from the Tools directory.
                    var toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
                    try
                    {
                        var mergedJsonFiles = Directory.GetFiles(toolsDir, "Game_*.json");
                        foreach (var file in mergedJsonFiles)
                        {
                            File.Delete(file);
                            progress.Report($"Cleaned up merged JSON: {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex) { progress.Report($"Could not clean up merged JSON files: {ex.Message}"); }
                }

                if (SettingsManager.Settings.AutoCloseLogOnSuccess && (_logForm?.IsHandleCreated ?? false) && !_logForm.IsDisposed)
                {
                    // Check if there were errors by looking for specific keywords in the log. A more robust method could be used in the future.
                    if (!_logForm.ContainsText("Failed") && !_logForm.ContainsText("Error"))
                    {
                        _logForm.Close();
                    }
                }

                // Mark the persistent log as done so the user can close it manually when they want.
                try { _logForm?.MarkDone(); } catch { }

                progress.Report("Saving Complete");

                // Re-enable the Play button
                btnPlay.Enabled = true;
                btnPlay.ForeColor = Color.White;
                btnPlay.Text = "▶ Play";
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPlatform) || !_gameInstallations.ContainsKey(_selectedPlatform))
            {
                MessageBox.Show("Could not find game installation to launch.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Check if the game process is already running.
            if (Process.GetProcessesByName(GameProcessName).Any())
            {
                MessageBox.Show("The game is already running.", "Game Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatus("Launch aborted: Game is already running.");
                return;
            }

            try
            {
                UpdateStatus($"Launching {_selectedPlatform} version...");
                string launchUrl = "";
                if (_selectedPlatform == "Steam")
                {
                    launchUrl = $"steam://run/{GameRegistry.SteamAppId}";
                }
                else if (_selectedPlatform == "Epic Games")
                {
                    launchUrl = "com.epicgames.launcher://apps/da1c2c6e190147019e4188f24687a17c%3A3cd74802827f4ecdac46214273fd701a%3A7133a8c315324112a3eee2458f0a8242?action=launch&silent=true";
                }
                else if (_selectedPlatform == "Custom")
                {
                    // For custom paths, we find and launch the executable directly.
                    var exePath = Path.Combine(_gameInstallations["Custom"].Path, "SonicRacingCrossWorldsSteam.exe"); // Assuming exe name
                    Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                    return; // Return early as we don't use launchUrl for this case
                }

                Process.Start(new ProcessStartInfo(launchUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch the game: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Launch failed.");
            }
        }

        private void RefreshModList()
        {
            _allModItems.Clear();
            modListView.Items.Clear();

            if (string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory) || !Directory.Exists(SettingsManager.Settings.ModsDirectory))
            {
                UpdateStatus("Mods directory not set or found. Please configure it in Settings.");
                return;
            }
            
            var modsDir = SettingsManager.Settings.ModsDirectory;
            var modDirectories = Directory.GetDirectories(modsDir);

            // Removed automatic mod.ini generation. Mod authors should provide mod.ini files manually.

            var activeProfile = GetActiveProfile();
            if (activeProfile == null) return;

            // Use saved settings
            var enabledMods = new HashSet<string>(activeProfile.EnabledMods, StringComparer.OrdinalIgnoreCase);
            var modLoadOrder = activeProfile.ModLoadOrder;
            var foundMods = new Dictionary<string, ModInfo>();
            
            foreach (var dir in modDirectories) // First, find all available mods
            {
                var modFolderName = Path.GetFileName(dir);
                if (string.IsNullOrEmpty(modFolderName)) continue;

                var iniPath = Path.Combine(dir, "mod.ini");
                ModInfo modInfo;

                if (File.Exists(iniPath))
                {
                    var iniSections = IniParser.Parse(iniPath);
                    if (!iniSections.TryGetValue("Main", out var mainSection))
                    {
                        continue;
                    }

                    modInfo = new ModInfo
                    {
                        Name = mainSection.GetValueOrDefault("Name", modFolderName),
                        Author = mainSection.GetValueOrDefault("Author", "Unknown"),
                        Version = mainSection.ContainsKey("Version") ? mainSection["Version"] : "1.0",
                        Description = mainSection.GetValueOrDefault("Description", "No description provided."),
                        DirectoryPath = dir
                    };

                    // New logic for multiple named [Config:GroupName] sections
                    var configSections = iniSections.Where(s => s.Key.StartsWith("Config:", StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var configKvp in configSections)
                    {
                        modInfo.ConfigurationGroups.Add(new ModConfigurationGroup(configKvp.Key, configKvp.Value));
                    }

                    var filesKvp = iniSections.FirstOrDefault(s => s.Key.Equals("Files", StringComparison.OrdinalIgnoreCase));
                    if (filesKvp.Value != null)
                    {
                        modInfo.FileGroupMappings = filesKvp.Value;
                    }
                }
                else
                {
                    // This is an "ini-less" mod. Treat the folder as a basic mod.
                    modInfo = new ModInfo
                    {
                        Name = modFolderName,
                        DirectoryPath = dir
                        // Author, Version, etc., will use default values.
                    };
                }

                foundMods[modFolderName] = modInfo;
            }

            // Now, add mods to the list view in the correct, saved order.
            foreach (var modName in modLoadOrder)
            {
                if (foundMods.TryGetValue(modName, out var modInfo)) // This should be case-insensitive in the future if needed
                {
                    _allModItems.Add(CreateModListViewItem(modInfo, enabledMods));
                    foundMods.Remove(modName); // Remove from dictionary so we don't add it again.
                }
            }

            // Finally, add any new, unsorted mods to the end of the list.
            foreach (var modInfo in foundMods.Values)
            {
                _allModItems.Add(CreateModListViewItem(modInfo, enabledMods));
            }

            ApplyFilter();
            UpdateStatus($"{_allModItems.Count} mod(s) found.");
        }

        private async Task CheckForModUpdatesAsync()
        {
            UpdateStatus("Checking for mod updates...");
            _logForm?.AppendLog("--- Starting GameBanana Mod Update Check ---");
            IProgress<string> logger = new Progress<string>(s =>
            {
                _logForm?.AppendLog(s);
            });

            var updateTasks = new List<Task>();

            foreach (ListViewItem item in _allModItems)
            {
                if (item.Tag is not ModInfo modInfo) continue;

                var iniPath = Path.Combine(modInfo.DirectoryPath, "mod.ini");
                if (!File.Exists(iniPath))
                {
                    logger.Report($"[{modInfo.Name}] Skipping: mod.ini not found.");
                    continue;
                }

                var iniData = IniParser.Parse(iniPath);
                if (!iniData.TryGetValue("GameBanana", out var gbSection) ||
                    !gbSection.TryGetValue("ItemId", out var itemIdStr) ||
                    !gbSection.TryGetValue("ItemType", out var itemType))
                {
                    logger.Report($"[{modInfo.Name}] Skipping: [GameBanana] section with ItemId/ItemType not found in mod.ini.");
                    continue;
                }

                if (!int.TryParse(itemIdStr, out var itemId)) continue;

                // Add a task to fetch and compare the version for this mod
                updateTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        logger.Report($"[{modInfo.Name}] Found GameBanana info. Item: {itemType}/{itemId}. Local version: '{modInfo.Version}'.");
                        string? latestVersionStr = await GameBananaApiService.GetLatestModVersionAsync(itemType, itemId);
                        
                        if (string.IsNullOrEmpty(latestVersionStr))
                        {
                            logger.Report($"[{modInfo.Name}] -> Could not fetch remote version from API.");
                            return;
                        }

                        logger.Report($"[{modInfo.Name}] -> Fetched remote version: '{latestVersionStr}'.");

                        if (int.TryParse(latestVersionStr, out int latestVersion) && int.TryParse(modInfo.Version, out int localVersion))
                        {
                            if (latestVersion > localVersion)
                            {
                                logger.Report($"[{modInfo.Name}] -> UPDATE AVAILABLE! (Remote: {latestVersion} > Local: {localVersion})");
                                // We need to update the UI on the UI thread.
                                this.Invoke((Action)(() => {
                                    item.ForeColor = Color.LimeGreen;
                                    // Place the "Update" action in the new "Update" column (index 4).
                                    var updateSubItem = item.SubItems[4];
                                    if (!updateSubItem.Text.Contains("Update"))
                                    {
                                        updateSubItem.Text = "🔄 Update";
                                    }
                                }));
                            }
                            else
                            {
                                logger.Report($"[{modInfo.Name}] -> Mod is up to date. (Remote: {latestVersion} <= Local: {localVersion})");
                            }
                        }
                    }
                    catch (Exception ex) { logger.Report($"[{modInfo.Name}] ERROR checking update: {ex.Message}"); }
                }));
            }

            await Task.WhenAll(updateTasks);
            UpdateStatus("Mod update check complete.");
            _logForm?.AppendLog("--- Mod Update Check Complete ---");
        }

        private ListViewItem CreateModListViewItem(ModInfo modInfo, HashSet<string> enabledMods)
        {
            var item = new ListViewItem(new[] 
                    {
                        modInfo.Name,
                        modInfo.Author,
                        modInfo.Version,
                        // Add text to the "Actions" column only if the mod is configurable.
                        modInfo.ConfigurationGroups.Any() ? "⚙️ Configure" : "",
                        "" // Placeholder for the new Update column
            }) 
            {
                Tag = modInfo
            };

            string modFolderName = Path.GetFileName(modInfo.DirectoryPath) ?? "";
            if (!string.IsNullOrEmpty(modFolderName))
            {
                item.Checked = enabledMods.Contains(modFolderName);
            }

            return item;
        }

        private void ApplyFilter()
        {
            modListView.BeginUpdate();
            modListView.Items.Clear();

            var searchText = txtSearch.Text;
            var filteredItems = string.IsNullOrWhiteSpace(searchText)
                ? _allModItems // If search is empty, show all mods
                : _allModItems.Where(item => item.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

            modListView.Items.AddRange(filteredItems.ToArray());
            modListView.EndUpdate();
        }
        
        private void SaveModListState()
        {
            var activeProfile = GetActiveProfile();
            if (activeProfile == null) return;

            var enabledMods = new List<string>();
            var modLoadOrder = new List<string>();
            foreach (ListViewItem item in modListView.Items)
            {
                if (item.Tag is ModInfo modInfo)
                {
                    var modFolderName = Path.GetFileName(modInfo.DirectoryPath);
                    if (!string.IsNullOrEmpty(modFolderName))
                    {
                        modLoadOrder.Add(modFolderName);
                        if (item.Checked)
                        {
                            enabledMods.Add(modFolderName);
                        }
                    }
                }
            }
            activeProfile.EnabledMods = enabledMods;
            activeProfile.ModLoadOrder = modLoadOrder;
            SettingsManager.Save();
        }

        private async Task<bool> InstallModsAsync()
        {
            string? gamePath = null;
            if (!string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var detectedGameInfo))
            {
                gamePath = detectedGameInfo.Path;
            }
            else gamePath = SettingsManager.Settings.GameDirectory;

            if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
            {
                UpdateStatus("Cannot install mods: Game path not found.");
                return false;
            }
            var targetModsDir = Path.Combine(gamePath, "UNION", "Content", "Paks", "~mods");

            try
            {
                // 1. Ensure the target directory exists.
                Directory.CreateDirectory(targetModsDir);

                // 2. Clear out old symbolic links created by this manager.
                foreach (var dir in Directory.GetDirectories(targetModsDir))
                {
                    if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    {
                        Directory.Delete(dir);
                    }
                }

                // 3. Create new links for checked mods.
                var installTasks = new List<Task<bool>>();
                // Iterate through all items in the list view to preserve the visual load order.
                for (int i = 0; i < modListView.Items.Count; i++)
                {
                    var item = modListView.Items[i];
                    // Only create links for mods that are actually checked.
                    if (item.Checked && item.Tag is ModInfo modInfo)
                    {
                        var modFolderName = Path.GetFileName(modInfo.DirectoryPath);
                        if (!string.IsNullOrEmpty(modFolderName))
                        {
                            // Assign a prefix to enforce load order. The game loads paks in alphabetical order.
                            // By starting at 000 for the top mod, we ensure it loads first. Subsequent mods
                            // with higher numbers will load later, overwriting any conflicting files from mods above them.
                            var linkName = Path.Combine(targetModsDir, $"{i:D3}-{modFolderName}");
                            installTasks.Add(CreateSymbolicLinkAsync(linkName, modInfo.DirectoryPath));
                        }
                    }
                }
                
                // 4. Handle Developer Mode files (if enabled)
                if (SettingsManager.Settings.DeveloperModeEnabled && _devForm is not null && !string.IsNullOrEmpty(_devForm.SelectedExportPath))
                {
                    var devExportSourcePath = _devForm.SelectedExportPath;
                    var enabledFileBases = _devForm.GetEnabledFileBaseNames();

                    if (enabledFileBases.Any())
                    {
                        if (string.IsNullOrEmpty(SettingsManager.Settings.ModsDirectory))
                        {
                            UpdateStatus("Developer mode files not installed: Mods directory is not set.");
                            return false;
                        }

                        // Create a temporary directory inside the mods folder to hold hardlinks
                        var devModDir = Path.Combine(SettingsManager.Settings.ModsDirectory, "_DevExport");
                        if (Directory.Exists(devModDir)) Directory.Delete(devModDir, true);
                        Directory.CreateDirectory(devModDir);

                        foreach (var baseName in enabledFileBases)
                        {
                            var sourceFiles = Directory.GetFiles(devExportSourcePath, $"{baseName}.*");
                            foreach (var sourceFile in sourceFiles)
                            {
                                // Append _P to the filename for the game to recognize it as a pak file.
                                var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                                var extension = Path.GetExtension(sourceFile);
                                var destFile = Path.Combine(devModDir, $"{fileName}_P{extension}");
                                await CreateHardLinkAsync(destFile, sourceFile);
                            }
                        }

                        // Now, create a symbolic link from ~mods to our _DevExport folder
                        var devLinkName = Path.Combine(targetModsDir, "000-DevExport"); // "000" prefix for highest priority
                        await CreateSymbolicLinkAsync(devLinkName, devModDir);
                        UpdateStatus("Installed developer export files.");
                    }
                }

                // Wait for all link creation tasks to complete.
                bool[] results = await Task.WhenAll(installTasks);
                int installedCount = results.Count(wasSuccessful => wasSuccessful);

                UpdateStatus($"Successfully installed {installedCount} of {installTasks.Count} enabled mod(s).");

                // Note: do NOT add the LocresMod pak here — it must be added after UnrealPak has
                // finished producing the final `LocresMod.pak`. That step is handled after
                // `PackMergedLocresAsync` completes so we don't install a clean/old pak.
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during mod installation: {ex.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Mod installation failed.");
                return false;
            }
        }

        private async Task<bool> CreateSymbolicLinkAsync(string linkPath, string targetPath)
        {
            string command;
            string arguments;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                command = "cmd.exe";
                arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"";
            }
            else // For Linux/macOS
            {
                command = "ln";
                arguments = $"-s \"{targetPath}\" \"{linkPath}\"";
            }

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                        FileName = command,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                };

                process.Start();
                await process.WaitForExitAsync(); // Asynchronously wait for the process to exit.

                if (process.ExitCode != 0)
                {
                    // Optionally log the error for debugging
                    // var error = await process.StandardError.ReadToEndAsync();
                    // Debug.WriteLine($"Failed to create link {linkPath}: {error}");
                    return false;
                }
                return true;
            }
        }

        private async Task<bool> CreateHardLinkAsync(string linkPath, string targetPath)
        {
            // Delete the old link if it exists, to ensure we can create a new one.
            if (File.Exists(linkPath))
            {
                File.Delete(linkPath);
            }

            string command;
            string arguments;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                command = "cmd.exe";
                arguments = $"/c mklink /H \"{linkPath}\" \"{targetPath}\"";
            }
            else // For Linux/macOS
            {
                command = "ln";
                arguments = $"-s \"{targetPath}\" \"{linkPath}\"";
            }

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
        }
        
        private void PromptForModsDirectory()
        {
            MessageBox.Show("Welcome! Please select a folder to store your mods.", "First-Time Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select or create a folder to store your mods";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    SettingsManager.Settings.ModsDirectory = fbd.SelectedPath;
                    SettingsManager.Save();
                }
                else
                {
                    // User cancelled. The app might not be fully functional, but let it load.
                    UpdateStatus("Warning: Mods directory not selected.");
                }
            }
        }

        private async void btnAddMod_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory) || !Directory.Exists(SettingsManager.Settings.ModsDirectory))
            {
                MessageBox.Show("The mods directory is not configured. Please set it in Settings before adding mods.", "Mods Directory Not Set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Mod Archive";
                ofd.Filter = "Mod Archives (*.zip, *.7z, *.rar)|*.zip;*.7z;*.rar|All files (*.*)|*.*";
                ofd.Multiselect = true;
        
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    int successCount = 0;
                    var modsDirectory = SettingsManager.Settings.ModsDirectory;
                    var toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
        
                    foreach (var file in ofd.FileNames)
                    {
                        try
                        {
                            string extension = Path.GetExtension(file).ToLowerInvariant();
                            string modName = Path.GetFileNameWithoutExtension(file);
                    string targetDir = Path.Combine(modsDirectory, modName); 

                            if (Directory.Exists(targetDir))
                            {
                                var result = MessageBox.Show($"A mod named '{modName}' already exists. Do you want to overwrite it?", "Mod Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                if (result == DialogResult.No) continue;
                                Directory.Delete(targetDir, true);
                            }

                            Directory.CreateDirectory(targetDir);
        
                            if (extension == ".rar")
                            {
                                var unrarPath = Path.Combine(toolsDir, "UnRAR.exe");
                                if (!File.Exists(unrarPath)) throw new FileNotFoundException("UnRAR.exe not found in Tools folder. It is required to extract .rar files.");
                                await ExtractWithToolAsync(unrarPath, file, targetDir);
                            }
                            else if (extension == ".zip")
                            {
                                // Use native .NET extraction for .zip files for better reliability.
                                await Task.Run(() => ZipFile.ExtractToDirectory(file, targetDir, true));
                            }
                            else if (extension == ".7z")
                            {
                                var sevenZipPath = Path.Combine(toolsDir, "7zr.exe");
                                if (!File.Exists(sevenZipPath)) throw new FileNotFoundException("7zr.exe not found in Tools folder. It is required to extract .7z files.");
                                await ExtractWithToolAsync(sevenZipPath, file, targetDir);
                            }
                            else
                            {
                                MessageBox.Show($"Unsupported archive format: {extension}", "Unsupported File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                continue;
                            }
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to install mod from '{Path.GetFileName(file)}':\n{ex.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    UpdateStatus($"{successCount} of {ofd.FileNames.Length} mod(s) installed.");
                    RefreshModList();
                }
            }
        }

        private void btnRemoveMod_Click(object sender, EventArgs e)
        {
            var selectedItems = modListView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please select a mod to remove.", "Remove Mod", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Are you sure you want to permanently delete the selected {selectedItems.Count} mod(s)? This cannot be undone.", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                foreach (ListViewItem item in selectedItems)
                {
                    if (item.Tag is ModInfo modInfo)
                    {
                        try
                        {
                            if (Directory.Exists(modInfo.DirectoryPath))
                            {
                                Directory.Delete(modInfo.DirectoryPath, true);
                            }
                            _allModItems.Remove(item);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to delete mod '{modInfo.Name}':\n{ex.Message}", "Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                ApplyFilter(); // Refresh the list view
                SaveModListState(); // Save the new load order without the deleted mods
                UpdateStatus($"{selectedItems.Count} mod(s) deleted.");
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshModList();
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(-1);
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(1);
        }

        private void MoveSelectedItem(int direction)
        {
            if (modListView.SelectedItems.Count != 1) return;

            var selectedItem = modListView.SelectedItems[0];
            int index = selectedItem.Index;
            int newIndex = index + direction;

            if (newIndex >= 0 && newIndex < modListView.Items.Count)
            {
                modListView.Items.RemoveAt(index);
                modListView.Items.Insert(newIndex, selectedItem);
                modListView.Items[newIndex].Selected = true;
                modListView.Focus();
            }
        }

        private void modListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count > 0)
            {
                // We store the description in the Tag property of the ListViewItem
                labelModInfo.Text = (modListView.SelectedItems[0].Tag as ModInfo)?.Description ?? "No description available.";
            }
            else
            {
                labelModInfo.Text = "Select a mod to see its description.";
            }
        }

        private void modListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            // To prevent this from running for every item during a refresh, we check if the listview has focus.
            if (modListView.Focused)
                e.Item.ForeColor = e.Item.Checked ? Color.White : Color.Gray;
        }

        private void SortModsView()
        {
            if (SettingsManager.Settings.SortEnabledModsToTop)
            {
                var enabledItems = new List<ListViewItem>();
                var disabledItems = new List<ListViewItem>();

                foreach (ListViewItem item in modListView.Items)
                {
                    item.ForeColor = item.Checked ? Color.White : Color.Gray;
                    if (item.Checked) enabledItems.Add(item);
                    else disabledItems.Add(item);
                }

                modListView.BeginUpdate();
                modListView.Items.Clear();
                modListView.Items.AddRange(enabledItems.ToArray());
                modListView.Items.AddRange(disabledItems.ToArray());
                modListView.EndUpdate();
            }
            else
            {
                // Just update colors without re-ordering
                foreach (ListViewItem item in modListView.Items)
                    item.ForeColor = item.Checked ? Color.White : Color.Gray;
            }
        }

        private void modListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            // Use a dark background for the header
            using (var solidBrush = new SolidBrush(Color.FromArgb(63, 63, 70)))
            {
                e.Graphics.FillRectangle(solidBrush, e.Bounds);
            }
            // Draw the header text in white (defensive null checks)
            var headerText = e.Header?.Text ?? string.Empty;
            var fontToUse = e.Font ?? this.Font ?? SystemFonts.DefaultFont;
            TextRenderer.DrawText(e.Graphics, headerText, fontToUse, e.Bounds, Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            // Draw a border for separation
            e.Graphics.DrawRectangle(Pens.Black, e.Bounds);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Reload everything to apply new settings
                    LoadSettingsAndSetup();
                    ToggleDeveloperForm();
                }
            }
        }

        private void ToggleDeveloperForm()
        {
            if (SettingsManager.Settings.DeveloperModeEnabled)
            {
                if (_devForm is null || _devForm.IsDisposed)
                {
                    _devForm = new DeveloperForm(); // This will be resolved once DeveloperForm.cs is in the project
                    _devForm.Show(this); // Show as non-modal, owned by MainForm
                    // Position it to the right of the main form
                    _devForm.Location = new Point(this.Right, this.Top);
                }
            }
            else
            {
                if (_devForm is not null && !_devForm.IsDisposed)
                {
                    _devForm.Close();
                    _devForm = null;
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }

        private void UpdateStatus(string message)
        {
            toolStripStatusLabel1.Text = message;
        }

        private void modListView_MouseClick(object sender, MouseEventArgs e)
        {
            var hitTestInfo = modListView.HitTest(e.X, e.Y);
            var item = hitTestInfo.Item;
            var subItem = hitTestInfo.SubItem;

            if (item == null || subItem == null) return;

            if (item.Tag is not ModInfo modInfo) return;

            int columnIndex = item.SubItems.IndexOf(subItem);

            // Check for a click in the "Update" column (index 4).
            if (columnIndex == 4 && subItem.Text.Contains("Update"))
            {
                HandleModUpdateClick(modInfo);
            }
            // Check for a click in the "Actions" column (index 3).
            else if (columnIndex == 3)
            {
                if (modInfo.ConfigurationGroups.Any() && subItem.Text.Contains("Configure"))
                {
                    ShowModConfigForm(modInfo);
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private async void HandleModUpdateClick(ModInfo modInfo)
        {
            var result = MessageBox.Show($"An update is available for '{modInfo.Name}'.\n\nWould you like to view the update details and install it now?", "Update Mod", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.No) return;

            try
            {
                // We need the ItemId and ItemType to fetch the full mod object.
                var iniPath = Path.Combine(modInfo.DirectoryPath, "mod.ini");
                if (!File.Exists(iniPath))
                {
                    MessageBox.Show("Could not find mod.ini file to get update information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var iniData = IniParser.Parse(iniPath);
                if (!iniData.TryGetValue("GameBanana", out var gbSection) ||
                    !gbSection.TryGetValue("ItemId", out var itemIdStr) ||
                    !int.TryParse(itemIdStr, out var itemId) ||
                    !gbSection.TryGetValue("ItemType", out var itemType))
                {
                    MessageBox.Show("Could not find valid GameBanana information in the mod's mod.ini file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateStatus($"Fetching update info for '{modInfo.Name}'...");
                var gameBananaMod = await GameBananaApiService.GetModFromProfilePageAsync(itemType, itemId);

                if (gameBananaMod == null)
                {
                    throw new Exception("Failed to retrieve mod details from GameBanana API.");
                }

                // Open the ModDetailsForm, which will handle the download/install process.
                // This is the same behavior as clicking "Download" in the mod browser.
                using (var modDetailsForm = new ModDetailsForm(gameBananaMod, _logForm?.GetLoggerProgress(), RefreshModList))
                {
                    modDetailsForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start update process:\n\n{ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void convertlocresToJsonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select .locres file to convert";
                ofd.Filter = "Localization Resource (*.locres)|*.locres";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    await LocresConverter.ConvertToJsonAsync(ofd.FileName);
                }
            }
        }

        private async void convertjsonTolocresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select .json file to convert";
                ofd.Filter = "JSON File (*.json)|*.json";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    await LocresConverter.ConvertToLocresAsync(ofd.FileName);
                }
            }
        }

        private void btnToggleDebugLog_Click(object sender, EventArgs e)
        {
            if (_logForm == null || _logForm.IsDisposed)
            {
                try
                {
                    _logForm = new LogForm();
                    _logForm.Show(this);
                    btnToggleDebugLog.Text = "Hide Debug Log";
                }
                catch { }
            }
            else if (_logForm.Visible)
            {
                _logForm.Hide();
                btnToggleDebugLog.Text = "Show Debug Log";
            }
            else
            {
                _logForm.Show(this);
                btnToggleDebugLog.Text = "Hide Debug Log";
            }
        }

        #region Context Menu

        private void modContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (modListView.SelectedItems.Count != 1)
            {
                e.Cancel = true; // Don't show the menu if no item is selected
                return;
            }

            var selectedItem = modListView.SelectedItems[0];
            var modInfo = selectedItem.Tag as ModInfo;

            // Configure option
            configureToolStripMenuItem.Enabled = modInfo?.ConfigurationGroups.Any() ?? false;

            // Move Up/Down options
            moveUpToolStripMenuItem1.Enabled = selectedItem.Index > 0;
            moveDownToolStripMenuItem1.Enabled = selectedItem.Index < modListView.Items.Count - 1;
        }

        private void ShowModConfigForm(ModInfo modInfo)
        {
            using (var configForm = new ModConfigForm(modInfo))
            {
                var activeProfile = GetActiveProfile();
                if (activeProfile == null) return;

                if (configForm.ShowDialog(this) == DialogResult.OK)
                {
                    // The new config form will save configurations directly to the active profile.
                    // We just need to save the settings file.
                    SettingsManager.Save();
                    UpdateStatus($"Configuration saved for '{modInfo.Name}'. Click Save to apply changes.");
                }
            }
        }

        private void configureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;
            var item = modListView.SelectedItems[0];

            if (item.Tag is ModInfo modInfo && modInfo.ConfigurationGroups.Any())
            {
                ShowModConfigForm(modInfo);
            }
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;
            var item = modListView.SelectedItems[0];

            if (item.Tag is ModInfo modInfo)
            {
                Process.Start("explorer.exe", modInfo.DirectoryPath);
            }
        }

        private void toggleEnabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;
            var item = modListView.SelectedItems[0];
            item.Checked = !item.Checked;
        }

        private void moveUpContextMenuItem_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(-1);
        }

        private void moveDownContextMenuItem_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(1);
        }

        #endregion

        #region Profile Management

        private void MigrateToProfiles()
        {
            // If old settings exist and no profiles exist, migrate them.
            if (SettingsManager.Settings.Profiles.Count == 0 && SettingsManager.Settings.EnabledMods != null)
            {
                const string defaultProfileName = "Default";
                var defaultProfile = new ModProfile
                {
                    EnabledMods = SettingsManager.Settings.EnabledMods,
                    ModConfigurations = SettingsManager.Settings.ModConfigurations ?? new Dictionary<string, string>(),
                    ModLoadOrder = SettingsManager.Settings.ModLoadOrder ?? new List<string>()
                };

                SettingsManager.Settings.Profiles[defaultProfileName] = defaultProfile;
                SettingsManager.Settings.ActiveProfileName = defaultProfileName;

                // Clear old properties to finalize migration
                SettingsManager.Settings.EnabledMods = null;
                SettingsManager.Settings.ModConfigurations = null;
                SettingsManager.Settings.ModLoadOrder = null;

                SettingsManager.Save();
            }
            else if (SettingsManager.Settings.Profiles.Count == 0)
            {
                // First run, create a default profile.
                const string defaultProfileName = "Default";
                SettingsManager.Settings.Profiles[defaultProfileName] = new ModProfile();
                SettingsManager.Settings.ActiveProfileName = defaultProfileName;
                SettingsManager.Save();
            }
        }

        private ModProfile? GetActiveProfile()
        {
            if (string.IsNullOrEmpty(SettingsManager.Settings.ActiveProfileName) ||
                !SettingsManager.Settings.Profiles.TryGetValue(SettingsManager.Settings.ActiveProfileName, out var profile))
            {
                // Fallback if active profile is missing
                if (SettingsManager.Settings.Profiles.Any())
                {
                    SettingsManager.Settings.ActiveProfileName = SettingsManager.Settings.Profiles.Keys.First();
                    return SettingsManager.Settings.Profiles.Values.First();
                }
                return null;
            }
            return profile;
        }

        private void UpdateProfilesMenu()
        {
            profilesToolStripMenuItem.DropDownItems.Clear();

            foreach (var profileName in SettingsManager.Settings.Profiles.Keys.OrderBy(p => p))
            {
                var item = new ToolStripMenuItem(profileName)
                {
                    Tag = profileName,
                    Checked = profileName == SettingsManager.Settings.ActiveProfileName,
                    CheckOnClick = true
                };
                item.Click += ProfileMenuItem_Click;
                profilesToolStripMenuItem.DropDownItems.Add(item);
            }

            profilesToolStripMenuItem.DropDownItems.Add(toolStripSeparator3);
            profilesToolStripMenuItem.DropDownItems.Add(newProfileToolStripMenuItem);
            profilesToolStripMenuItem.DropDownItems.Add(renameProfileToolStripMenuItem);
            profilesToolStripMenuItem.DropDownItems.Add(deleteProfileToolStripMenuItem);
        }

        private void ProfileMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem clickedItem || clickedItem.Tag is not string profileName) return;

            // Uncheck all other items
            foreach (var item in profilesToolStripMenuItem.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem && menuItem != clickedItem)
                {
                    menuItem.Checked = false;
                }
            }
            clickedItem.Checked = true;

            SettingsManager.Settings.ActiveProfileName = profileName;
            SettingsManager.Save();
            RefreshModList();
            UpdateStatus($"Switched to profile: {profileName}");
        }

        private void newProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string newProfileName = Prompt.ShowDialog("Enter a name for the new profile:", "New Profile");
            if (string.IsNullOrWhiteSpace(newProfileName) || SettingsManager.Settings.Profiles.ContainsKey(newProfileName))
            {
                if (!string.IsNullOrWhiteSpace(newProfileName))
                    MessageBox.Show("A profile with that name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SettingsManager.Settings.Profiles[newProfileName] = new ModProfile();
            SettingsManager.Settings.ActiveProfileName = newProfileName;
            SettingsManager.Save();
            UpdateProfilesMenu();
            RefreshModList();
        }

        private void renameProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentName = SettingsManager.Settings.ActiveProfileName;
            if (string.IsNullOrEmpty(currentName)) return;

            string newName = Prompt.ShowDialog("Enter a new name for the profile:", "Rename Profile", currentName);
            if (string.IsNullOrWhiteSpace(newName) || newName == currentName || SettingsManager.Settings.Profiles.ContainsKey(newName))
            {
                if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
                    MessageBox.Show("A profile with that name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var profile = SettingsManager.Settings.Profiles[currentName];
            SettingsManager.Settings.Profiles.Remove(currentName);
            SettingsManager.Settings.Profiles[newName] = profile;
            SettingsManager.Settings.ActiveProfileName = newName;
            SettingsManager.Save();
            UpdateProfilesMenu();
        }

        private void deleteProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentName = SettingsManager.Settings.ActiveProfileName;
            if (string.IsNullOrEmpty(currentName) || SettingsManager.Settings.Profiles.Count <= 1)
            {
                MessageBox.Show("You cannot delete the last profile.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show($"Are you sure you want to delete the '{currentName}' profile?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                SettingsManager.Settings.Profiles.Remove(currentName);
                SettingsManager.Settings.ActiveProfileName = SettingsManager.Settings.Profiles.Keys.First();
                SettingsManager.Save();
                UpdateProfilesMenu();
                RefreshModList();
            }
        }

        private void btnBrowseMods_Click(object? sender, EventArgs e)
        {
            // Create a logger that reports to our main debug log window
            IProgress<string> browserLogger = new Progress<string>(s =>
            {
                if (_logForm != null && !_logForm.IsDisposed)
                {
                    _logForm.AppendLog($"[GB Browser] {s}");
                }
            });

            using (var browserForm = new GameBananaBrowserForm(browserLogger, RefreshModList))
            {
                browserForm.ShowDialog(this);
            }
        }

        private async void HandleOneClickInstallAsync(string url)
        {
            try
            {
                // Format: bluestar:https://gamebanana.com/mmdl/DOWNLOAD_ID,TYPE,MOD_ID,ARCHIVE_TYPE
                var parts = url.Split(',');
                if (parts.Length < 3) throw new ArgumentException("Invalid 1-Click URL format (missing commas).");

                var urlPart = parts[0]; // e.g., bluestar:https://gamebanana.com/mmdl/1577381
                var downloadIdString = urlPart.Split('/').LastOrDefault();
                if (!int.TryParse(downloadIdString, out int downloadId))
                {
                    throw new ArgumentException("Could not parse Download ID from URL.");
                }
                
                if (!int.TryParse(parts[2], out int modId))
                {
                    throw new ArgumentException("Could not parse Mod ID from URL.");
                }

                var modType = parts[1];

                UpdateStatus($"1-Click Install: Fetching info for Mod ID {modId}, File ID {downloadId}...");

                // Fetch the full mod details from the profile page API before showing the dialog.
                var fullModInfo = await GameBananaApiService.GetModFromProfilePageAsync(modType, modId);

                if (fullModInfo == null)
                {
                    throw new Exception($"Could not retrieve mod information for ID {modId}. The mod may have been removed.");
                }

                // Now, get the list of files and find the one matching our download ID.
                var downloadPage = await GameBananaApiService.GetModDownloadPageAsync(fullModInfo);
                var fileToInstall = downloadPage?.Files?.FirstOrDefault(f => f.FileId == downloadId);

                if (fileToInstall == null)
                {
                    var availableIds = string.Join(", ", downloadPage?.Files?.Select(f => f.FileId.ToString()) ?? new[] { "none" });
                    throw new Exception($"The specified file (ID: {downloadId}) could not be found for this mod. Available file IDs: {availableIds}. The file may have been updated or removed.");
                }

                // Update the status with the correct file name.
                UpdateStatus($"Found file: {fileToInstall.FileName}");

                // Create a logger that reports to our main debug log window
                IProgress<string> browserLogger = new Progress<string>(s =>
                {
                    if (_logForm != null && !_logForm.IsDisposed)
                    {
                        _logForm.AppendLog($"[1-Click] {s}");
                    }
                });

                // Show the details form as a confirmation dialog.
                // The form will handle fetching full details, downloading, and installing.
                using (var detailsForm = new ModDetailsForm(fullModInfo, browserLogger, RefreshModList))
                {
                    detailsForm.SetConfirmationMode(); // Adapt the form for Yes/No confirmation
                    var result = detailsForm.ShowDialog(this);

                    if (result == DialogResult.OK)
                    {
                        await detailsForm.LaunchProgressForm(fileToInstall);
                        UpdateStatus($"1-Click Install for '{detailsForm.ModName}' successful!");
                    }
                    else
                    {
                        UpdateStatus("1-Click Install cancelled by user.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to handle 1-Click Install link:\n\n{ex.Message}", "1-Click Install Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("1-Click Install failed.");
            }
        }
        #endregion

        #region Drag and Drop Reordering

        private void modListView_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item != null)
            {
                modListView.DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void modListView_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void modListView_DragDrop(object? sender, DragEventArgs e)
        {
            ListViewItem? draggedItem = e.Data?.GetData(typeof(ListViewItem)) as ListViewItem;
            if (draggedItem == null) return;

            Point dropPoint = modListView.PointToClient(new Point(e.X, e.Y));
            ListViewItem? targetItem = modListView.GetItemAt(dropPoint.X, dropPoint.Y);

            int originalIndex = draggedItem.Index;
            int targetIndex = (targetItem != null) ? targetItem.Index : modListView.Items.Count - 1;

            if (originalIndex == targetIndex) return;

            modListView.Items.RemoveAt(originalIndex);
            modListView.Items.Insert(targetIndex, draggedItem);

            draggedItem.Selected = true;
            modListView.Focus();
        }

        #endregion

        #region Archive Extraction

        private async Task ExtractWithToolAsync(string toolPath, string archivePath, string destinationPath)
        {
            string arguments;
            string toolName = Path.GetFileName(toolPath).ToLowerInvariant();

            if (toolName == "unrar.exe")
            {
                arguments = $"x -o+ \"{archivePath}\" \"{destinationPath}\\\" -y";
            }
            else // Assumes 7zr.exe or 7z.exe
            {
                arguments = $"x \"{archivePath}\" -o\"{destinationPath}\" -y";
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"{Path.GetFileName(toolPath)} extraction failed with exit code {process.ExitCode}.\nError: {error}");
            }
        }
        #endregion
    }
}