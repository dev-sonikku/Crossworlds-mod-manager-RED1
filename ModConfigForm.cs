using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace CrossworldsModManager
{
    public partial class ModConfigForm : Form
    {
        private readonly ModInfo _modInfo;
        private readonly ModProfile _activeProfile;
        // The key is the GroupName, the value is a list of controls (RadioButtons or CheckBoxes).
        private readonly Dictionary<string, List<Control>> _groupControls = new();
        
        public ModConfigForm(ModInfo modInfo)
        {
            InitializeComponent();
            _modInfo = modInfo;
            this.Text = $"Configure '{_modInfo.Name}'";

            // This is a bit of a shortcut. A better way would be to pass the profile in.
            // But for now, this will work.
            var activeProfileName = SettingsManager.Settings.ActiveProfileName ?? "";
            if (!SettingsManager.Settings.Profiles.TryGetValue(activeProfileName, out _activeProfile!))
                _activeProfile = new ModProfile(); // Fallback to an empty profile if something is wrong.
        }
        
        private void ModConfigForm_Load(object? sender, EventArgs e)
        {
            // Clear designer controls and set up the form for dynamic content
            this.Controls.Clear();
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.ClientSize = new Size(450, 600);
            this.MinimumSize = new Size(450, 300);
            this.StartPosition = FormStartPosition.CenterParent;

            // Panel for scrollable content
            var pnlScrollableContent = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };

            // Panel for buttons (fixed at the bottom)
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(37, 37, 38),
                Padding = new Padding(5)
            };

            // Save Button
            var btnSave = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(75, 30),
                Location = new Point(pnlButtons.ClientSize.Width - 80, (pnlButtons.ClientSize.Height - 30) / 2)
            };
            btnSave.Click += btnSave_Click;
            pnlButtons.Controls.Add(btnSave);

            // Cancel Button
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(75, 30),
                Location = new Point(pnlButtons.ClientSize.Width - 160, (pnlButtons.ClientSize.Height - 30) / 2)
            };
            pnlButtons.Controls.Add(btnCancel);

            // Add panels to form
            this.Controls.Add(pnlScrollableContent);
            this.Controls.Add(pnlButtons);

            int topOffset = 0; // Start from top of pnlScrollableContent

            foreach (var group in _modInfo.ConfigurationGroups)
            {
                var groupBox = new GroupBox
                {
                    Text = group.Description,
                    Tag = group, // Store the group object for later reference
                    Location = new Point(10, topOffset), // Relative to pnlScrollableContent
                    AutoSize = true,
                    ForeColor = Color.White,
                    // Set width relative to pnlScrollableContent's client width
                    MinimumSize = new Size(pnlScrollableContent.ClientSize.Width - 40, 0), // Adjust padding
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right // Anchor to scrollable panel
                };
                pnlScrollableContent.Controls.Add(groupBox); // Add to scrollable panel

                if (group.Type == ModConfigType.SelectOne)
                {
                    BuildSelectOneUI(groupBox, group);
                }
                else if (group.Type == ModConfigType.SelectMultiple)
                {
                    BuildSelectMultipleUI(groupBox, group);
                }

                topOffset += groupBox.Height + 10; // Add spacing between group boxes
            }
        }

        private void BuildSelectOneUI(GroupBox groupBox, ModConfigurationGroup group)
        {
            var configKey = $"{_modInfo.Name}:{group.GroupName}";
            _activeProfile.ModConfigurations.TryGetValue(configKey, out var savedOption);

            var comboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(15, 25),
                Width = groupBox.ClientSize.Width - 30, // Relative to GroupBox client width
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            comboBox.Items.AddRange(group.Options.ToArray());

            // Set the selected item based on saved configuration, or default to the first item.
            if (!string.IsNullOrEmpty(savedOption) && group.Options.Contains(savedOption))
            {
                comboBox.SelectedItem = savedOption;
            }
            else if (group.Options.Any())
            {
                comboBox.SelectedIndex = 0;
            }

            groupBox.Controls.Add(comboBox);
            // The GroupBox needs a bit more height for a ComboBox than for RadioButtons.
            groupBox.Height = 70;
            _groupControls[group.GroupName] = new List<Control> { comboBox };
        }

        private void BuildSelectMultipleUI(GroupBox groupBox, ModConfigurationGroup group)
        {
            var configKey = $"{_modInfo.Name}:{group.GroupName}";
            _activeProfile.ModConfigurations.TryGetValue(configKey, out var savedOptionsString);
            var savedOptions = savedOptionsString?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();

            var controls = new List<Control>();
            int yPos = 20;
            foreach (string optionName in group.Options)
            {
                var checkBox = new CheckBox
                {
                    Text = optionName,
                    Tag = optionName,
                    Location = new Point(15, yPos),
                    AutoSize = true,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent
                };

                // Check the box if the option is in our saved list.
                if (savedOptions.Contains(optionName))
                {
                    checkBox.Checked = true;
                }

                groupBox.Controls.Add(checkBox);
                controls.Add(checkBox);
                yPos += 25;
            }
            _groupControls[group.GroupName] = controls;
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            foreach (var group in _modInfo.ConfigurationGroups)
            {
                var configKey = $"{_modInfo.Name}:{group.GroupName}";
                var controls = _groupControls[group.GroupName];

                if (group.Type == ModConfigType.SelectOne)
                {
                    var comboBox = controls.OfType<ComboBox>().FirstOrDefault();
                    if (comboBox?.SelectedItem != null)
                        _activeProfile.ModConfigurations[configKey] = comboBox.SelectedItem?.ToString() ?? string.Empty;
                }
                else if (group.Type == ModConfigType.SelectMultiple)
                {
                    var enabledOptions = new List<string>();
                    foreach (CheckBox cb in controls.OfType<CheckBox>())
                    {
                        if (cb.Checked)
                        {
                            enabledOptions.Add(cb.Text);
                        }
                    }
                    _activeProfile.ModConfigurations[configKey] = string.Join(",", enabledOptions);
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}