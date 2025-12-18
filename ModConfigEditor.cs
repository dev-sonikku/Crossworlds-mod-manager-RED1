using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Data Models
    public class ModConfig
    {
        public Dictionary<string, string> MainSection { get; set; } = new Dictionary<string, string>();
        public List<ConfigGroup> Groups { get; set; } = new List<ConfigGroup>();
    }

    public class ConfigGroup
    {
        public string Name { get; set; } = "NewGroup";
        public string Type { get; set; } = "SelectOne";
        public string Description { get; set; } = "";
        public List<ConfigOption> Options { get; set; } = new List<ConfigOption>();

        public override string ToString() => Name;
    }

    public class ConfigOption
    {
        public string Name { get; set; } = "NewOption";
        public List<string> Files { get; set; } = new List<string>();

        public override string ToString() => Name;
    }

    // The Editor Form
    public class ModConfigEditor : Form
    {
        private string _modPath;
        private string _configPath;
        private ModConfig _config = new ModConfig();

        // UI Controls
        private ListBox _lstGroups = null!;
        private TextBox _txtGroupName = null!;
        private ComboBox _cmbGroupType = null!;
        private TextBox _txtGroupDesc = null!;
        
        private ListBox _lstOptions = null!;
        private ListBox _lstFiles = null!;
        private Panel _pnlRight = null!;

        public ModConfigEditor(string modPath)
        {
            _modPath = modPath;
            _configPath = Path.Combine(modPath, "mod.ini");
            
            InitializeComponent();
            LoadConfig();
        }

        private void InitializeComponent()
        {
            this.Text = "Mod Configuration Editor";
            this.Size = new Size(800, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = SystemFonts.MessageBoxFont;

            var splitContainer = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
            this.Controls.Add(splitContainer);
            splitContainer.SplitterDistance = 220;

            // --- Left Panel (Groups List) ---
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            splitContainer.Panel1.Controls.Add(pnlLeft);

            var lblGroups = new Label { Text = "Configuration Groups", Dock = DockStyle.Top, Height = 25, Font = new Font(this.Font, FontStyle.Bold) };
            _lstGroups = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            _lstGroups.SelectedIndexChanged += (s, e) => UpdateRightPanel();
            
            var pnlGroupButtons = new Panel { Dock = DockStyle.Bottom, Height = 35, Padding = new Padding(0, 5, 0, 0) };
            var btnAddGroup = new Button { Text = "+", Width = 35, Dock = DockStyle.Left };
            btnAddGroup.Click += (s, e) => AddGroup();
            var btnRemoveGroup = new Button { Text = "-", Width = 35, Dock = DockStyle.Left };
            btnRemoveGroup.Click += (s, e) => RemoveGroup();
            var btnSave = new Button { Text = "Save && Close", Dock = DockStyle.Right, Width = 100 };
            btnSave.Click += (s, e) => SaveConfig();

            pnlGroupButtons.Controls.Add(btnSave);
            pnlGroupButtons.Controls.Add(btnRemoveGroup);
            pnlGroupButtons.Controls.Add(btnAddGroup);

            pnlLeft.Controls.Add(_lstGroups);
            pnlLeft.Controls.Add(lblGroups);
            pnlLeft.Controls.Add(pnlGroupButtons);

            // --- Right Panel (Details) ---
            _pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), Visible = false };
            splitContainer.Panel2.Controls.Add(_pnlRight);

            // 1. Group Properties (Top)
            var grpProps = new GroupBox { Text = "Group Properties", Dock = DockStyle.Top, Height = 120 };
            
            var lblName = new Label { Text = "Name:", Location = new Point(15, 25), AutoSize = true };
            _txtGroupName = new TextBox { Location = new Point(90, 22), Width = 200 };
            _txtGroupName.TextChanged += (s, e) => { 
                if (_lstGroups.SelectedItem is ConfigGroup g) { 
                    g.Name = _txtGroupName.Text; 
                } 
            };
            _txtGroupName.Leave += (s, e) => {
                if (_lstGroups.SelectedIndex != -1) {
                    // Refresh ListBox text when focus leaves the textbox
                    _lstGroups.Items[_lstGroups.SelectedIndex] = _lstGroups.Items[_lstGroups.SelectedIndex];
                }
            };

            var lblType = new Label { Text = "Type:", Location = new Point(15, 55), AutoSize = true };
            _cmbGroupType = new ComboBox { Location = new Point(90, 52), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbGroupType.Items.AddRange(new object[] { "SelectOne", "SelectMultiple" });
            _cmbGroupType.SelectedIndexChanged += (s, e) => { if (_lstGroups.SelectedItem is ConfigGroup g) g.Type = _cmbGroupType.SelectedItem?.ToString() ?? "SelectOne"; };

            var lblDesc = new Label { Text = "Description:", Location = new Point(15, 85), AutoSize = true };
            _txtGroupDesc = new TextBox { Location = new Point(90, 82), Width = 350 };
            _txtGroupDesc.TextChanged += (s, e) => { if (_lstGroups.SelectedItem is ConfigGroup g) g.Description = _txtGroupDesc.Text; };

            grpProps.Controls.AddRange(new Control[] { lblName, _txtGroupName, lblType, _cmbGroupType, lblDesc, _txtGroupDesc });

            // 2. Splitter for Options and Files
            var splitRight = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 200 };
            
            // 3. Options List (Middle)
            var pnlOptions = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 0) };
            var lblOptHeader = new Label { Text = "Options (e.g. Vanilla, Modded)", Dock = DockStyle.Top, Height = 20, Font = new Font(this.Font, FontStyle.Bold) };
            _lstOptions = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            _lstOptions.SelectedIndexChanged += (s, e) => UpdateFilesList();

            var pnlOptButtons = new Panel { Dock = DockStyle.Bottom, Height = 30 };
            var btnAddOpt = new Button { Text = "Add Option", Width = 80, Dock = DockStyle.Left };
            btnAddOpt.Click += (s, e) => AddOption();
            var btnRemOpt = new Button { Text = "Remove", Width = 80, Dock = DockStyle.Left };
            btnRemOpt.Click += (s, e) => RemoveOption();
            pnlOptButtons.Controls.Add(btnRemOpt);
            pnlOptButtons.Controls.Add(btnAddOpt);

            pnlOptions.Controls.Add(_lstOptions);
            pnlOptions.Controls.Add(pnlOptButtons);
            pnlOptions.Controls.Add(lblOptHeader);
            splitRight.Panel1.Controls.Add(pnlOptions);

            // 4. Files List (Bottom)
            var pnlFiles = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 0) };
            var lblFileHeader = new Label { Text = "Files for Selected Option", Dock = DockStyle.Top, Height = 20, Font = new Font(this.Font, FontStyle.Bold) };
            _lstFiles = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            
            var pnlFileButtons = new Panel { Dock = DockStyle.Bottom, Height = 30 };
            var btnAddFile = new Button { Text = "Add Files", Width = 80, Dock = DockStyle.Left };
            btnAddFile.Click += BtnAddFile_Click;
            var btnRemFile = new Button { Text = "Remove", Width = 80, Dock = DockStyle.Left };
            btnRemFile.Click += (s, e) => RemoveFile();
            pnlFileButtons.Controls.Add(btnRemFile);
            pnlFileButtons.Controls.Add(btnAddFile);

            pnlFiles.Controls.Add(_lstFiles);
            pnlFiles.Controls.Add(pnlFileButtons);
            pnlFiles.Controls.Add(lblFileHeader);
            splitRight.Panel2.Controls.Add(pnlFiles);

            // Add to Right Panel (Order matters for Docking: Fill first, then Top)
            _pnlRight.Controls.Add(splitRight);
            _pnlRight.Controls.Add(grpProps);
        }

        private void LoadConfig()
        {
            _config = new ModConfig();
            if (!File.Exists(_configPath)) return;

            try
            {
                var lines = File.ReadAllLines(_configPath);
                string currentSection = "";
                ConfigGroup? currentGroup = null;
                var tempFiles = new Dictionary<string, string>();

                foreach (var line in lines)
                {
                    var trim = line.Trim();
                    if (string.IsNullOrWhiteSpace(trim) || trim.StartsWith(";") || trim.StartsWith("#")) continue;

                    if (trim.StartsWith("[") && trim.EndsWith("]"))
                    {
                        currentSection = trim.Substring(1, trim.Length - 2);
                        if (currentSection.StartsWith("Config:", StringComparison.OrdinalIgnoreCase))
                        {
                            var groupName = currentSection.Substring(7);
                            currentGroup = new ConfigGroup { Name = groupName };
                            _config.Groups.Add(currentGroup);
                        }
                        else
                        {
                            currentGroup = null;
                        }
                        continue;
                    }

                    var parts = trim.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;
                    var key = parts[0].Trim();
                    var val = parts[1].Trim();

                    if (currentSection.Equals("Main", StringComparison.OrdinalIgnoreCase))
                    {
                        _config.MainSection[key] = val;
                    }
                    else if (currentSection.Equals("Files", StringComparison.OrdinalIgnoreCase))
                    {
                        tempFiles[key] = val;
                    }
                    else if (currentGroup != null)
                    {
                        if (key.Equals("Type", StringComparison.OrdinalIgnoreCase)) currentGroup.Type = val;
                        else if (key.Equals("Description", StringComparison.OrdinalIgnoreCase)) currentGroup.Description = val;
                        else if (key.Equals("Options", StringComparison.OrdinalIgnoreCase))
                        {
                            // Options=Vanilla,Grand Prix Final Laps
                            var opts = val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var o in opts)
                            {
                                currentGroup.Options.Add(new ConfigOption { Name = o.Trim() });
                            }
                        }
                    }
                }

                // Map files to options
                foreach (var kvp in tempFiles)
                {
                    var file = kvp.Key;
                    var pointer = kvp.Value; // Group.Option
                    var ptrParts = pointer.Split('.');
                    if (ptrParts.Length == 2)
                    {
                        var gName = ptrParts[0];
                        var oName = ptrParts[1];
                        var group = _config.Groups.FirstOrDefault(g => g.Name.Equals(gName, StringComparison.OrdinalIgnoreCase));
                        var opt = group?.Options.FirstOrDefault(o => o.Name.Equals(oName, StringComparison.OrdinalIgnoreCase));
                        opt?.Files.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading config: {ex.Message}");
            }

            RefreshGroupList();
        }

        private void RefreshGroupList()
        {
            _lstGroups.Items.Clear();
            foreach (var grp in _config.Groups)
            {
                _lstGroups.Items.Add(grp);
            }
            if (_lstGroups.Items.Count > 0) _lstGroups.SelectedIndex = 0;
        }

        private void UpdateRightPanel()
        {
            if (_lstGroups.SelectedItem is ConfigGroup grp)
            {
                _pnlRight.Visible = true;
                _txtGroupName.Text = grp.Name;
                _cmbGroupType.SelectedItem = grp.Type;
                _txtGroupDesc.Text = grp.Description;

                _lstOptions.Items.Clear();
                foreach (var opt in grp.Options) _lstOptions.Items.Add(opt);
                
                if (_lstOptions.Items.Count > 0) _lstOptions.SelectedIndex = 0;
                else _lstFiles.Items.Clear();
            }
            else
            {
                _pnlRight.Visible = false;
            }
        }

        private void UpdateFilesList()
        {
            _lstFiles.Items.Clear();
            if (_lstOptions.SelectedItem is ConfigOption opt)
            {
                foreach (var f in opt.Files) _lstFiles.Items.Add(f);
            }
        }

        private void AddGroup()
        {
            var newGrp = new ConfigGroup { Name = "NewGroup" };
            _config.Groups.Add(newGrp);
            _lstGroups.Items.Add(newGrp);
            _lstGroups.SelectedItem = newGrp;
        }

        private void RemoveGroup()
        {
            if (_lstGroups.SelectedItem is ConfigGroup grp)
            {
                _config.Groups.Remove(grp);
                _lstGroups.Items.Remove(grp);
                UpdateRightPanel();
            }
        }

        private void AddOption()
        {
            if (_lstGroups.SelectedItem is ConfigGroup grp)
            {
                string name = Microsoft.VisualBasic.Interaction.InputBox("Enter Option Name:", "New Option", "NewOption");
                if (string.IsNullOrWhiteSpace(name)) return;

                var newOpt = new ConfigOption { Name = name };
                grp.Options.Add(newOpt);
                _lstOptions.Items.Add(newOpt);
                _lstOptions.SelectedItem = newOpt;
            }
        }

        private void RemoveOption()
        {
            if (_lstGroups.SelectedItem is ConfigGroup grp && _lstOptions.SelectedItem is ConfigOption opt)
            {
                grp.Options.Remove(opt);
                _lstOptions.Items.Remove(opt);
                UpdateFilesList();
            }
        }

        private void BtnAddFile_Click(object? sender, EventArgs e)
        {
            if (!(_lstOptions.SelectedItem is ConfigOption opt)) return;

            using (var ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Filter = "Mod Files|*.pak;*.utoc;*.ucas;*.json|All Files|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var file in ofd.FileNames)
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        string nameToRegister;

                        // Logic: JSON keeps extension, PAK/UTOC/UCAS strips extension
                        if (ext == ".json")
                        {
                            nameToRegister = Path.GetFileName(file);
                        }
                        else if (ext == ".pak" || ext == ".utoc" || ext == ".ucas")
                        {
                            nameToRegister = Path.GetFileNameWithoutExtension(file);
                        }
                        else
                        {
                            // Fallback for other files
                            nameToRegister = Path.GetFileName(file);
                        }

                        if (!opt.Files.Contains(nameToRegister))
                        {
                            opt.Files.Add(nameToRegister);
                            _lstFiles.Items.Add(nameToRegister);
                        }
                    }
                }
            }
        }

        private void RemoveFile()
        {
            if (_lstOptions.SelectedItem is ConfigOption opt && _lstFiles.SelectedItem is string file)
            {
                opt.Files.Remove(file);
                _lstFiles.Items.Remove(file);
            }
        }

        private void SaveConfig()
        {
            var sb = new StringBuilder();
            
            // [Main]
            sb.AppendLine("[Main]");
            if (_config.MainSection.Count == 0)
            {
                sb.AppendLine($"Name={new DirectoryInfo(_modPath).Name}");
                sb.AppendLine("Author=Unknown");
                sb.AppendLine("Version=1.0");
            }
            else
            {
                foreach (var kvp in _config.MainSection)
                {
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");
                }
            }
            sb.AppendLine();

            // [Config:Group]
            foreach (var grp in _config.Groups)
            {
                sb.AppendLine($"[Config:{grp.Name}]");
                sb.AppendLine($"Type={grp.Type}");
                sb.AppendLine($"Description={grp.Description}");
                sb.AppendLine($"Options={string.Join(",", grp.Options.Select(o => o.Name))}");
                sb.AppendLine();
            }

            // [Files]
            sb.AppendLine("[Files]");
            foreach (var grp in _config.Groups)
            {
                foreach (var opt in grp.Options)
                {
                    foreach (var file in opt.Files)
                    {
                        sb.AppendLine($"{file}={grp.Name}.{opt.Name}");
                    }
                }
            }

            File.WriteAllText(_configPath, sb.ToString());
            MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}