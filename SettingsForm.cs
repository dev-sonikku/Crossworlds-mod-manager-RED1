using System;
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
            txtGameDir.Text = SettingsManager.Settings.GameDirectory;
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
        }

        private void btnBrowseGameDir_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select the game's installation directory";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtGameDir.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnBrowseModsDir_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select the directory to store your mods";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtModsDir.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SettingsManager.Settings.GameDirectory = txtGameDir.Text;
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
            SettingsManager.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}