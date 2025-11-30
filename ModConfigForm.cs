using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace CrossworldsModManager
{
    public partial class ModConfigForm : Form
    {
        private readonly ModInfo _modInfo;
        private readonly ModProfile _activeProfile;
        public string ConfigurationString { get; private set; } = "";

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

        private void ModConfigForm_Load(object sender, EventArgs e)
        {
            groupBoxOptions.Text = _modInfo.ConfigDescription;

            if (_modInfo.ConfigType == ModConfigType.SelectOne)
            {
                BuildSelectOneUI();
            }
            else if (_modInfo.ConfigType == ModConfigType.SelectMultiple)
            {
                BuildSelectMultipleUI();
            }
        }

        private void BuildSelectOneUI()
        {
            _activeProfile.ModConfigurations.TryGetValue(_modInfo.Name, out var savedOption);

            int yPos = 20;
            foreach (string optionName in _modInfo.ConfigOptions)
            {
                var radioButton = new RadioButton
                {
                    Text = optionName,
                    Tag = optionName,
                    Location = new System.Drawing.Point(15, yPos),
                    AutoSize = true,
                    ForeColor = System.Drawing.Color.White,
                    BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
                };

                if ((!string.IsNullOrEmpty(savedOption) && optionName == savedOption) || (string.IsNullOrEmpty(savedOption) && yPos == 20))
                {
                    radioButton.Checked = true;
                }

                groupBoxOptions.Controls.Add(radioButton);
                yPos += 25;
            }
        }

        private void BuildSelectMultipleUI()
        {
            _activeProfile.ModConfigurations.TryGetValue(_modInfo.Name, out var savedOptionsString);
            var savedOptions = savedOptionsString?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();

            int yPos = 20;
            foreach (string optionName in _modInfo.ConfigOptions)
            {
                var checkBox = new CheckBox
                {
                    Text = optionName,
                    Tag = optionName,
                    Location = new System.Drawing.Point(15, yPos),
                    AutoSize = true,
                    ForeColor = System.Drawing.Color.White,
                    BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
                };

                // Check the box if the option is in our saved list.
                if (savedOptions.Contains(optionName))
                {
                    checkBox.Checked = true;
                }

                groupBoxOptions.Controls.Add(checkBox);
                yPos += 25;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_modInfo.ConfigType == ModConfigType.SelectOne)
            {
                foreach (RadioButton rb in groupBoxOptions.Controls.OfType<RadioButton>())
                {
                    if (rb.Checked)
                    {
                        ConfigurationString = rb.Text;
                        break;
                    }
                }
            }
            else if (_modInfo.ConfigType == ModConfigType.SelectMultiple)
            {
                var enabledOptions = new List<string>();
                foreach (CheckBox cb in groupBoxOptions.Controls.OfType<CheckBox>())
                {
                    if (cb.Checked)
                    {
                        enabledOptions.Add(cb.Text);
                    }
                }
                ConfigurationString = string.Join(",", enabledOptions);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}