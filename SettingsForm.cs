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
            SettingsManager.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}