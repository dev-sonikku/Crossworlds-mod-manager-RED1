using System;
using System.IO;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SettingsManager.Settings.GameDirectory) && !string.IsNullOrEmpty(SettingsManager.Settings.GameExecutableName))
            {
                txtGameDir.Text = Path.Combine(SettingsManager.Settings.GameDirectory, SettingsManager.Settings.GameExecutableName);
            }
            else
            {
                txtGameDir.Text = SettingsManager.Settings.GameDirectory;
            }
            txtModsDir.Text = SettingsManager.Settings.ModsDirectory;
            chkSortEnabled.Checked = SettingsManager.Settings.SortEnabledModsToTop;
            chkAutoClean.Checked = SettingsManager.Settings.AutoCleanTemporaryFiles;
            chkCheckForGames.Checked = SettingsManager.Settings.CheckForGamesOnStartup;
            chkAutoCloseLog.Checked = SettingsManager.Settings.AutoCloseLogOnSuccess;
            chkDeveloperMode.Checked = SettingsManager.Settings.DeveloperModeEnabled;
            var doNotBackupChk = this.Controls.Find("chkDoNotBackup", true);
            if (doNotBackupChk.Length > 0 && doNotBackupChk[0] is CheckBox cb)
            {
                cb.Checked = SettingsManager.Settings.DoNotBackupModsAutomatically;
            }
            var doNotConfirmChk = this.Controls.Find("chkDoNotConfirmEnableDisable", true);
            if (doNotConfirmChk.Length > 0 && doNotConfirmChk[0] is CheckBox cb2)
            {
                cb2.Checked = SettingsManager.Settings.DoNotConfirmEnableDisable;
            }

            var cmbThemeControl = this.Controls.Find("cmbTheme", true);
            if (cmbThemeControl.Length > 0 && cmbThemeControl[0] is ComboBox cmbTheme)
            {
                cmbTheme.Items.Clear();
                cmbTheme.Items.AddRange(ThemeManager.GetAvailableThemes().ToArray());
                cmbTheme.SelectedItem = SettingsManager.Settings.SelectedTheme;
                UpdateCustomizeButtonVisibility();
            }

            ThemeManager.ApplyTheme(this);
        }

        private void btnBrowseGameDir_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Game Executable (SonicRacingCrossWorlds.exe)";
                ofd.Filter = "SonicRacingCrossWorlds.exe|SonicRacingCrossWorlds.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtGameDir.Text = ofd.FileName;
                }
            }
        }

        private void btnBrowseModsDir_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select the directory to store your mods";
                while (fbd.ShowDialog() == DialogResult.OK)
                {
                    var dirName = new System.IO.DirectoryInfo(fbd.SelectedPath).Name;
                    if (dirName.Equals("~mods", StringComparison.OrdinalIgnoreCase))
                    {
                        CustomMessageBox.Show("You cannot select the game's '~mods' folder as your mod storage directory.\n\nThis folder is used by the manager to install mods. Please select a different folder to store your source mods.", "Invalid Directory", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                    txtModsDir.Text = fbd.SelectedPath;
                    break;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string inputPath = txtGameDir.Text;
            if (File.Exists(inputPath))
            {
                SettingsManager.Settings.GameDirectory = Path.GetDirectoryName(inputPath);
                SettingsManager.Settings.GameExecutableName = Path.GetFileName(inputPath);
            }
            else
            {
                // Fallback if user entered a directory manually or cleared it
                SettingsManager.Settings.GameDirectory = inputPath;
                if (string.IsNullOrEmpty(SettingsManager.Settings.GameExecutableName))
                    SettingsManager.Settings.GameExecutableName = "SonicRacingCrossWorlds.exe";
            }

            SettingsManager.Settings.ModsDirectory = txtModsDir.Text;
            SettingsManager.Settings.SortEnabledModsToTop = chkSortEnabled.Checked;
            SettingsManager.Settings.AutoCleanTemporaryFiles = chkAutoClean.Checked;
            SettingsManager.Settings.CheckForGamesOnStartup = chkCheckForGames.Checked;
            SettingsManager.Settings.AutoCloseLogOnSuccess = chkAutoCloseLog.Checked;
            SettingsManager.Settings.DeveloperModeEnabled = chkDeveloperMode.Checked;
            var doNotBackupChk2 = this.Controls.Find("chkDoNotBackup", true);
            if (doNotBackupChk2.Length > 0 && doNotBackupChk2[0] is CheckBox cb2)
            {
                SettingsManager.Settings.DoNotBackupModsAutomatically = cb2.Checked;
            }
            var doNotConfirmChk2 = this.Controls.Find("chkDoNotConfirmEnableDisable", true);
            if (doNotConfirmChk2.Length > 0 && doNotConfirmChk2[0] is CheckBox cb3)
            {
                SettingsManager.Settings.DoNotConfirmEnableDisable = cb3.Checked;
            }
            var cmbThemeControl = this.Controls.Find("cmbTheme", true);
            if (cmbThemeControl.Length > 0 && cmbThemeControl[0] is ComboBox cmbTheme && cmbTheme.SelectedItem != null)
            {
                SettingsManager.Settings.SelectedTheme = cmbTheme.SelectedItem.ToString() ?? "Default";
            }
            SettingsManager.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cmbTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateCustomizeButtonVisibility();
        }

        private void UpdateCustomizeButtonVisibility()
        {
            var cmbThemeControl = this.Controls.Find("cmbTheme", true);
            var btnCustomizeControl = this.Controls.Find("btnCustomizeTheme", true);
            if (cmbThemeControl.Length > 0 && cmbThemeControl[0] is ComboBox cmbTheme && 
                btnCustomizeControl.Length > 0 && btnCustomizeControl[0] is Button btnCustomize)
            {
                btnCustomize.Visible = (cmbTheme.SelectedItem?.ToString() == "Custom");
            }
        }

        private void btnCustomizeTheme_Click(object sender, EventArgs e)
        {
            using (var editor = new ThemeEditorForm(SettingsManager.Settings.CustomTheme))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    SettingsManager.Settings.CustomTheme = editor.ResultTheme;
                    ThemeManager.ReloadCustomTheme(SettingsManager.Settings.CustomTheme);
                    ThemeManager.ApplyTheme(this);
                }
                else
                {
                    ThemeManager.ReloadCustomTheme(SettingsManager.Settings.CustomTheme);
                    ThemeManager.ApplyTheme(this);
                }
            }
        }
    }
}