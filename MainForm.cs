using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public partial class MainForm : Form
    {
        // This is a common executable name pattern for Unreal Engine games.
        // We check for the process name without the .exe extension.
        private const string GameProcessName = "SonicRacingCrossWorldsSteam";
        private Dictionary<string, (string Path, string? AppName)> _gameInstallations = new();
        private List<ListViewItem> _allModItems = new List<ListViewItem>();
        private string? _selectedPlatform;
        private LogForm? _logForm;

        public MainForm()
        {
            InitializeComponent();
            // Apply the custom dark theme renderer for menus and tool strips
            ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new DarkThemeColorTable());
            LoadSettingsAndSetup();

            // Show persistent debug log window so it's available at all times.
            try
            {
                _logForm = new LogForm();
                _logForm.Show(this);
            }
            catch
            {
                // If creating the log window fails, ignore and continue — logging will still use Console/Debug.
            }
        }

        private void LoadSettingsAndSetup()
        {
            SettingsManager.Load();

            if (string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory))
            {
                PromptForModsDirectory();
            }

            DetectGameInstallations();
            // Load the list of mods when the application starts.
            RefreshModList();
        }

        private void DetectGameInstallations()
        {
            _gameInstallations = GameRegistry.FindGameInstallations();

            // If auto-detection fails, check settings for a manually set path.
            if (_gameInstallations.Count == 0 && !string.IsNullOrWhiteSpace(SettingsManager.Settings.GameDirectory))
            {
                // We'll add it as a "Custom" platform type.
                if (Directory.Exists(SettingsManager.Settings.GameDirectory))
                {
                    // We don't know if it's Steam or Epic, so AppName is null. Launching will be by executable.
                    _gameInstallations["Custom"] = (SettingsManager.Settings.GameDirectory, null);
                }
            }

            launchPlatformDropDown.DropDownItems.Clear();

            if (_gameInstallations.Count == 0)
            {
                launchPlatformDropDown.Text = "Game Not Found";
                launchPlatformDropDown.Enabled = false;
                btnSave.Enabled = false;
                btnPlay.Enabled = false;
            }
            else if (_gameInstallations.Count == 1)
            {
                var platform = _gameInstallations.First();
                _selectedPlatform = platform.Key;
                launchPlatformDropDown.Text = $"{platform.Key} Version";
                launchPlatformDropDown.Enabled = false; // No other options to choose
            }
            else
            {
                foreach (var platform in _gameInstallations)
                {
                    var item = new ToolStripMenuItem(platform.Key);
                    item.Tag = platform.Key;
                    item.Click += PlatformMenuItem_Click;
                    item.ForeColor = Color.White;
                    item.BackColor = Color.FromArgb(45, 45, 48);
                    launchPlatformDropDown.DropDownItems.Add(item);
                }
                // Select the first one by default
                PlatformMenuItem_Click(launchPlatformDropDown.DropDownItems[0], EventArgs.Empty);
            }
        }

        private void PlatformMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not string platform) return;

            _selectedPlatform = platform;
            launchPlatformDropDown.Text = _selectedPlatform; // This is safe now
            UpdateStatus($"Selected platform: {_selectedPlatform}");
        }

        private void ApplyModConfiguration(ModInfo modInfo, string configurationString)
        {
            if (modInfo.FileGroupMappings.Count == 0) return;

            var selectedOptions = configurationString?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();

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
                    if (ext == ".ini" || ext == ".json" || ext == ".txt" || ext == ".md") continue;

                    bool shouldBeEnabled = selectedOptions.Contains(group);
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
            // First, save which mods are enabled.
            SaveModListState();

            // Reorder the list to show enabled mods at the top.
            var enabledItems = new List<ListViewItem>();
            var disabledItems = new List<ListViewItem>();

            foreach (ListViewItem item in modListView.Items)
            {
                if (item.Checked)
                {
                    enabledItems.Add(item);
                }
                else
                {
                    disabledItems.Add(item);
                }
            }

            modListView.BeginUpdate(); // Prevent flickering during reordering
            modListView.Items.Clear();
            modListView.Items.AddRange(enabledItems.ToArray());
            modListView.Items.AddRange(disabledItems.ToArray());
            modListView.EndUpdate();

            // Before applying configurations, check for newly enabled mods that need a default config.
            bool defaultsSet = false;
            foreach (ListViewItem item in modListView.Items)
            {
                if (item.Checked && item.Tag is ModInfo modInfo && modInfo.ConfigType != ModConfigType.None)
                {
                    // If a configurable mod is enabled but has no saved configuration, set the default.
                    if (!SettingsManager.Settings.ModConfigurations.ContainsKey(modInfo.Name))
                    {
                        if (modInfo.ConfigOptions.Count > 0)
                        {
                            SettingsManager.Settings.ModConfigurations[modInfo.Name] = modInfo.ConfigOptions[0];
                            defaultsSet = true;
                        }
                    }
                }
            }

            if (defaultsSet) SettingsManager.Save(); // Save the new default settings.

            // Then, apply the current configurations for all mods.
            foreach (ListViewItem item in modListView.Items)
            {
                if (item.Tag is ModInfo modInfo && modInfo.ConfigOptions.Count > 0)
                {
                    // Get the saved selection for this mod.
                    if (SettingsManager.Settings.ModConfigurations.TryGetValue(modInfo.Name, out var selectedOption))
                    {
                        ApplyModConfiguration(modInfo, selectedOption);
                    }
                    else if (modInfo.ConfigOptions.Count > 0)
                    {
                        // If no setting is saved, apply the default (first) option.
                        ApplyModConfiguration(modInfo, modInfo.ConfigOptions[0]);
                    }
                }
            }

            // Finally, asynchronously install mods and run the JSON merge while writing into the persistent debug log.
            // Ensure the persistent log form exists and is visible.
            if (_logForm == null || _logForm.IsDisposed)
            {
                _logForm = new LogForm();
                _logForm.Show(this);
            }

            var progress = new Progress<string>(s =>
            {
                try { _logForm?.AppendLog(s); } catch { /* best-effort logging */ }
            });

            // Start the async tasks
            var installTask = InstallModsAsync();
            var jsonTask = LocresConverter.ProcessModJsonFilesAsync(modListView.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => i.Tag as ModInfo).Where(m => m != null)!, progress);

            try
            {
                await Task.WhenAll(installTask, jsonTask);
            }
            finally
            {
                // Mark the persistent log as done so the user can close it manually when they want.
                try { _logForm?.MarkDone(); } catch { }
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
                    var epicAppName = _gameInstallations[_selectedPlatform].AppName;
                    if (string.IsNullOrEmpty(epicAppName)) throw new InvalidOperationException("Epic Games AppName not found.");
                    launchUrl = $"com.epicgames.launcher://apps/{epicAppName}?action=launch&silent=true";
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

            // Use saved settings
            var enabledMods = SettingsManager.Settings.EnabledMods ?? new List<string>();
            var modLoadOrder = SettingsManager.Settings.ModLoadOrder ?? new List<string>();
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
                        Version = mainSection.GetValueOrDefault("Version", "1.0"),
                        Description = mainSection.GetValueOrDefault("Description", "No description provided."),
                        DirectoryPath = dir
                    };

                    var configKvp = iniSections.FirstOrDefault(s => s.Key.Equals("Config", StringComparison.OrdinalIgnoreCase));
                    if (configKvp.Value != null)
                    {
                        var configSection = configKvp.Value;
                        string typeStr = configSection.GetValueOrDefault("Type", "SelectOne");
                        if (Enum.TryParse<ModConfigType>(typeStr, true, out var configType))
                        {
                            modInfo.ConfigType = configType;
                        }

                        modInfo.ConfigDescription = configSection.GetValueOrDefault("Description", "Select an option:");

                        if (modInfo.ConfigType == ModConfigType.SelectOne)
                        {
                            modInfo.ConfigOptions = configSection.GetValueOrDefault("Options", "").Split(',').Select(o => o.Trim()).ToList();
                        }
                    }

                    var filesKvp = iniSections.FirstOrDefault(s => s.Key.Equals("Files", StringComparison.OrdinalIgnoreCase));
                    if (filesKvp.Value != null)
                    {
                        var filesSection = filesKvp.Value;
                        modInfo.FileGroupMappings = filesSection;
                        if (modInfo.ConfigType == ModConfigType.SelectMultiple)
                        {
                            modInfo.ConfigOptions = filesSection.Values.Distinct().ToList();
                        }
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

        private ListViewItem CreateModListViewItem(ModInfo modInfo, List<string> enabledMods)
        {
            var item = new ListViewItem(new[] 
                    {
                        modInfo.Name,
                        modInfo.Author,
                        modInfo.Version,
                        // Add text to the "Actions" column only if the mod is configurable.
                        modInfo.ConfigType != ModConfigType.None ? "⚙️ Configure" : ""
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
            SettingsManager.Settings.EnabledMods = enabledMods;
            SettingsManager.Settings.ModLoadOrder = modLoadOrder;
            SettingsManager.Save();
        }

        private async Task<bool> InstallModsAsync()
        {
            if (string.IsNullOrEmpty(_selectedPlatform) || !_gameInstallations.TryGetValue(_selectedPlatform, out var gameInfo))
            {
                UpdateStatus("Cannot install mods: Game path not found.");
                return false;
            }

            var targetModsDir = Path.Combine(gameInfo.Path, "UNION", "Content", "Paks", "~mods");

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
                var enabledMods = modListView.Items.Cast<ListViewItem>().Where(i => i.Checked).ToList();
                var installTasks = new List<Task<bool>>();
                for (int i = 0; i < enabledMods.Count; i++)
                {
                    var item = enabledMods[i];
                    if (item.Tag is ModInfo modInfo)
                    {
                        var modFolderName = Path.GetFileName(modInfo.DirectoryPath);
                        if (!string.IsNullOrEmpty(modFolderName))
                        {
                            // Assign a prefix to enforce load order. Higher numbers load last and have priority.
                            // The mod at the top of the list (i=0) gets the highest prefix.
                            var linkName = Path.Combine(targetModsDir, $"{enabledMods.Count - 1 - i:D3}-{modFolderName}");
                            installTasks.Add(CreateSymbolicLinkAsync(linkName, modInfo.DirectoryPath));
                        }
                    }
                }
                
                // Wait for all link creation tasks to complete.
                bool[] results = await Task.WhenAll(installTasks);
                int installedCount = results.Count(success => success);

                UpdateStatus($"Successfully installed {installedCount} of {installTasks.Count} enabled mod(s).");
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
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                        FileName = "cmd.exe",
                        Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
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

        private void btnAddMod_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Mod Archive";
                ofd.Filter = "Mod Archives (*.zip, *.7z, *.rar)|*.zip;*.7z;*.rar|All files (*.*)|*.*";
                ofd.Multiselect = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // TODO: Implement the logic to copy/extract the selected file(s)
                    // to the _modsDirectory and then refresh the list.
                    MessageBox.Show($"{ofd.FileNames.Length} mod(s) selected. (Not yet implemented)", "Add Mod");
                    RefreshModList();
                }
            }
        }

        private void btnRemoveMod_Click(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a mod to remove.", "Remove Mod", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Are you sure you want to remove the selected mod(s)?", "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                foreach (ListViewItem item in modListView.SelectedItems)
                {
                    // TODO: Implement logic to delete the actual mod files/folders.
                    modListView.Items.Remove(item);
                }
                UpdateStatus("Mod(s) removed.");
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshModList();
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;

            var selectedItem = modListView.SelectedItems[0];
            int index = selectedItem.Index;

            if (index > 0)
            {
                modListView.Items.RemoveAt(index);
                modListView.Items.Insert(index - 1, selectedItem);
                modListView.Items[index - 1].Selected = true;
                modListView.Focus();
            }
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;

            var selectedItem = modListView.SelectedItems[0];
            int index = selectedItem.Index;

            if (index < modListView.Items.Count - 1)
            {
                modListView.Items.RemoveAt(index);
                modListView.Items.Insert(index + 1, selectedItem);
                modListView.Items[index + 1].Selected = true;
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
            e.Item.ForeColor = e.Item.Checked ? Color.White : Color.Gray;
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

        private void installModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnSave_Click(sender, e);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Reload everything to apply new settings
                    LoadSettingsAndSetup();
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

            // Check if the click was on the "Actions" column (index 3).
            if (item.SubItems.IndexOf(subItem) == 3)
            {
                if (item.Tag is ModInfo modInfo && modInfo.ConfigType != ModConfigType.None)
                {
                    using (var configForm = new ModConfigForm(modInfo))
                    {
                        if (configForm.ShowDialog() == DialogResult.OK)
                        {
                            // Save the selected option to the settings file.
                            SettingsManager.Settings.ModConfigurations[modInfo.Name] = configForm.ConfigurationString ?? "";
                            SettingsManager.Save();
                            UpdateStatus($"Configuration saved for '{modInfo.Name}'. Click Save to apply changes.");
                        }
                    }
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
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
    }
}