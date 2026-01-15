using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public partial class DeveloperForm : Form
    {
        private Button btnSelectPath = null!;
        private Button btnRefresh = null!;
        private CheckedListBox checkedListBoxFiles = null!;
        private Label lblPath = null!;

        public string? SelectedExportPath { get; private set; }

        public DeveloperForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            SelectedExportPath = SettingsManager.Settings.DeveloperExportPath;
            lblPath.Text = string.IsNullOrEmpty(SelectedExportPath) ? "No export path selected." : SelectedExportPath;
            ScanAndListFiles();
        }

        private void InitializeComponent()
        {
            var pnlTop = new Panel();
            this.btnSelectPath = new Button();
            this.btnRefresh = new Button();
            this.checkedListBoxFiles = new CheckedListBox();
            this.lblPath = new Label();
            this.SuspendLayout();

            // pnlTop
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 40;
            pnlTop.Padding = new Padding(5);

            // lblPath
            this.lblPath.Dock = DockStyle.Bottom;
            this.lblPath.Text = "No export path selected.";
            this.lblPath.ForeColor = Color.Gray;
            this.lblPath.Height = 30;

            // btnSelectPath
            this.btnSelectPath.Location = new Point(5, 5);
            this.btnSelectPath.Size = new Size(160, 30);
            this.btnSelectPath.Text = "Select UE Export Path...";
            this.btnSelectPath.Click += BtnSelectPath_Click;
            this.btnSelectPath.ForeColor = Color.White;
            this.btnSelectPath.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;

            // checkedListBoxFiles
            this.checkedListBoxFiles.Dock = DockStyle.Fill;
            this.checkedListBoxFiles.BackColor = Color.FromArgb(37, 37, 38);
            this.checkedListBoxFiles.ForeColor = Color.White;
            this.checkedListBoxFiles.BorderStyle = BorderStyle.None;
            this.checkedListBoxFiles.CheckOnClick = true;
            this.checkedListBoxFiles.ItemCheck += CheckedListBoxFiles_ItemCheck;

            // btnRefresh
            this.btnRefresh.Location = new Point(170, 5);
            this.btnRefresh.Size = new Size(75, 30);
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Click += BtnRefresh_Click;
            this.btnRefresh.ForeColor = Color.White;
            this.btnRefresh.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;

            pnlTop.Controls.Add(this.btnSelectPath);
            pnlTop.Controls.Add(this.btnRefresh);

            // DeveloperForm
            this.Text = "Developer Tools";
            this.ClientSize = new Size(400, 600);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.Controls.Add(this.checkedListBoxFiles);
            this.Controls.Add(this.lblPath);
            this.Controls.Add(pnlTop);
            this.ResumeLayout(false);
        }

        private void BtnSelectPath_Click(object? sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select your Unreal Engine content export directory";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    SelectedExportPath = fbd.SelectedPath;
                    SettingsManager.Settings.DeveloperExportPath = SelectedExportPath;
                    SettingsManager.Save();
                    lblPath.Text = SelectedExportPath;
                    lblPath.ForeColor = Color.White;
                    ScanAndListFiles();
                }
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            ScanAndListFiles();
        }

        private void ScanAndListFiles()
        {
            // Temporarily unsubscribe from the event to prevent it from firing during programmatic updates.
            checkedListBoxFiles.ItemCheck -= CheckedListBoxFiles_ItemCheck;

            checkedListBoxFiles.Items.Clear();
            if (string.IsNullOrEmpty(SelectedExportPath) || !Directory.Exists(SelectedExportPath))
            {
                // Re-subscribe before exiting
                checkedListBoxFiles.ItemCheck += CheckedListBoxFiles_ItemCheck;
                return;
            }

            var unifiedFiles = Directory.GetFiles(SelectedExportPath)
                .Select(f => Path.GetFileNameWithoutExtension(f)!) // Use null-forgiving as we filter nulls next
                .Where(f => !string.IsNullOrEmpty(f)) 
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var enabledFiles = new HashSet<string>(SettingsManager.Settings.DeveloperEnabledFiles, StringComparer.OrdinalIgnoreCase);

            foreach (var fileBaseName in unifiedFiles.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                bool isEnabled = enabledFiles.Contains(fileBaseName);
                checkedListBoxFiles.Items.Add(fileBaseName, isEnabled);
            }

            // Re-subscribe to the event now that the list is populated.
            checkedListBoxFiles.ItemCheck += CheckedListBoxFiles_ItemCheck;
        }

        private void CheckedListBoxFiles_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            // This event fires *before* the state is updated, so we schedule the save to happen right after.
            this.BeginInvoke((Action)(() => SaveCheckedItems()));
        }

        public List<string> GetEnabledFileBaseNames()
        {
            return checkedListBoxFiles.CheckedItems.Cast<string>().ToList();
        }

        private void SaveCheckedItems()
        {
            SettingsManager.Settings.DeveloperEnabledFiles = GetEnabledFileBaseNames();
            SettingsManager.Save();
        }
    }
#pragma warning restore CA1416
}