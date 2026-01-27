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
    public class CustomFileBrowser : Form
    {
        public enum BrowserMode { OpenFile, SaveFile, SelectFolder }

        public BrowserMode Mode { get; set; } = BrowserMode.OpenFile;
        public string InitialDirectory { get; set; } = "";
        public string Filter { get; set; } = "All files (*.*)|*.*";
        public int FilterIndex { get; set; } = 1;
        public string FileName { get; set; } = "";
        public string[] FileNames { get; private set; } = Array.Empty<string>();
        public bool Multiselect { get; set; } = false;
        public bool OverwritePrompt { get; set; } = true;
        public string SelectedPath { get; private set; } = "";

        private TextBox txtPath = null!;
        private TextBox txtFileName = null!;
        private ComboBox cmbFilter = null!;
        private ListView lvFiles = null!;
        private Button btnAction = null!;
        private Button btnCancel = null!;
        private Button btnUp = null!;
        private Button btnNewFolder = null!;
        private Button btnRefresh = null!;
        private Button btnHome = null!;
        private Button btnView = null!;
        private ListBox lstDrives = null!;
        private Label lblFileName = null!;
        private Label lblFilter = null!;

        private string _currentPath = "";
        private List<string> _filterPatterns = new List<string>();
        private ImageList _iconList = null!;
        private ContextMenuStrip ctxViewMenu = null!;
        private bool _showHidden = false;
        private ContextMenuStrip _ctxFavorites = null!;
        private ContextMenuStrip _ctxFileList = null!;

        public CustomFileBrowser()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;

            // --- Top Panel (Navigation & Address) ---
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(5), BackColor = Color.FromArgb(45, 45, 48) };
            
            // Navigation Buttons
            btnUp = CreateFlatButton("▲", 35);
            btnUp.Click += (s, e) => NavigateUp();

            btnHome = CreateFlatButton("🏠", 35);
            btnHome.Click += (s, e) => Navigate(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            btnRefresh = CreateFlatButton("↻", 35);
            btnRefresh.Click += (s, e) => RefreshFileList();

            // Action Buttons (Right aligned)
            btnView = CreateFlatButton("👁", 35);
            btnView.Dock = DockStyle.Right;
            btnView.Click += BtnView_Click;

            btnNewFolder = CreateFlatButton("+📁", 40);
            btnNewFolder.Dock = DockStyle.Right;
            btnNewFolder.Click += BtnNewFolder_Click;

            // Address Bar
            var pnlPathContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5, 7, 5, 5) };
            txtPath = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10F) };
            txtPath.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Navigate(txtPath.Text); };
            pnlPathContainer.Controls.Add(txtPath);

            // Add to Top Panel (Add Fill first so it's docked last, Add Edge controls last so they are docked first)
            pnlTop.Controls.Add(pnlPathContainer);
            pnlTop.Controls.Add(btnNewFolder);
            pnlTop.Controls.Add(btnView);
            pnlTop.Controls.Add(btnRefresh);
            pnlTop.Controls.Add(btnHome);
            pnlTop.Controls.Add(btnUp);

            // --- Left Panel (Drives / Quick Access) ---
            lstDrives = new ListBox { Dock = DockStyle.Left, Width = 180, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F), ItemHeight = 24, DrawMode = DrawMode.OwnerDrawFixed };
            lstDrives.SelectedIndexChanged += LstDrives_SelectedIndexChanged;
            lstDrives.DrawItem += LstDrives_DrawItem;

            // --- Center (File List) ---
            lvFiles = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BorderStyle = BorderStyle.FixedSingle, MultiSelect = false, Font = new Font("Segoe UI", 10F) };
            lvFiles.Columns.Add("Name", 350);
            lvFiles.Columns.Add("Date Modified", 140);
            lvFiles.Columns.Add("Type", 100);
            lvFiles.Columns.Add("Size", 100);
            lvFiles.DoubleClick += LvFiles_DoubleClick;
            lvFiles.SelectedIndexChanged += LvFiles_SelectedIndexChanged;

            // --- Bottom Panel (Inputs & Actions) ---
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10), BackColor = Color.FromArgb(45, 45, 48) };
            
            btnAction = new Button { Text = "Open", Dock = DockStyle.Right, Width = 90, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Click += BtnAction_Click;

            btnCancel = new Button { Text = "Cancel", Dock = DockStyle.Right, Width = 90, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(63, 63, 70), ForeColor = Color.White };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.DialogResult = DialogResult.Cancel;

            var pnlInputs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
            pnlInputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Label
            pnlInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70)); // File Name
            pnlInputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Filter Label
            pnlInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // Filter Combo
            
            lblFileName = new Label { Text = "File name:", Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            txtFileName = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10F) };
            
            lblFilter = new Label { Text = "Type:", Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleRight };
            cmbFilter = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F) };
            cmbFilter.SelectedIndexChanged += (s, e) => RefreshFileList();

            pnlInputs.Controls.Add(lblFileName, 0, 0);
            pnlInputs.Controls.Add(txtFileName, 1, 0);
            pnlInputs.Controls.Add(lblFilter, 2, 0);
            pnlInputs.Controls.Add(cmbFilter, 3, 0);

            pnlBottom.Controls.Add(pnlInputs); // Add Fill first
            pnlBottom.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 });
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 });
            pnlBottom.Controls.Add(btnAction);

            this.Controls.Add(lvFiles);
            this.Controls.Add(lstDrives);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBottom);

            this.AcceptButton = btnAction;
            this.CancelButton = btnCancel;

            // View Menu
            ctxViewMenu = new ContextMenuStrip();
            var itemDetails = new ToolStripMenuItem("Details", null, (s, e) => SetView(View.Details));
            var itemList = new ToolStripMenuItem("List", null, (s, e) => SetView(View.List));
            var itemTiles = new ToolStripMenuItem("Tiles", null, (s, e) => SetView(View.Tile));
            var itemLarge = new ToolStripMenuItem("Large Icons", null, (s, e) => SetView(View.LargeIcon));
            var itemSmall = new ToolStripMenuItem("Small Icons", null, (s, e) => SetView(View.SmallIcon));
            var itemHidden = new ToolStripMenuItem("Show Hidden Files", null, (s, e) => ToggleHiddenFiles());
            itemHidden.CheckOnClick = true;
            itemHidden.Checked = _showHidden;

            ctxViewMenu.Items.AddRange(new ToolStripItem[] { itemDetails, itemList, itemTiles, itemLarge, itemSmall, new ToolStripSeparator(), itemHidden });
            ThemeManager.ApplyTheme(ctxViewMenu);

            // Favorites Context Menu
            _ctxFavorites = new ContextMenuStrip();
            var itemRemoveFav = new ToolStripMenuItem("Remove from Favorites", null, (s, e) => RemoveFavorite());
            _ctxFavorites.Items.Add(itemRemoveFav);
            ThemeManager.ApplyTheme(_ctxFavorites);
            
            lstDrives.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right)
                {
                    int index = lstDrives.IndexFromPoint(e.Location);
                    if (index != ListBox.NoMatches && lstDrives.Items[index] is BrowserItem item && item.Type == "favorite")
                    {
                        lstDrives.SelectedIndex = index;
                        _ctxFavorites.Show(lstDrives, e.Location);
                    }
                }
            };

            // File List Context Menu
            _ctxFileList = new ContextMenuStrip();
            var itemAddFav = new ToolStripMenuItem("Add to Favorites", null, (s, e) => AddFavorite());
            _ctxFileList.Items.Add(itemAddFav);
            ThemeManager.ApplyTheme(_ctxFileList);
            lvFiles.ContextMenuStrip = _ctxFileList;
        }

        private Button CreateFlatButton(string text, int width)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(63, 63, 70),
                Font = new Font("Segoe UI", 10F)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LstDrives_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            
            var itemObj = lstDrives.Items[e.Index];
            var text = itemObj.ToString() ?? "";
            bool isHeader = itemObj is BrowserItem bi && bi.Type == "header";
            
            if (isHeader)
            {
                e.Graphics.FillRectangle(new SolidBrush(ThemeManager.CurrentTheme.ControlBackColor), e.Bounds);
                using (var brush = new SolidBrush(ThemeManager.CurrentTheme.ForeColor))
                    e.Graphics.DrawString(text, new Font(e.Font ?? SystemFonts.DefaultFont, FontStyle.Bold), brush, e.Bounds.X + 2, e.Bounds.Y + 4);
                return;
            }
            
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                e.Graphics.FillRectangle(new SolidBrush(ThemeManager.CurrentTheme.AccentColor), e.Bounds);
            else
                e.Graphics.FillRectangle(new SolidBrush(ThemeManager.CurrentTheme.ControlBackColor), e.Bounds);

            using (var brush = new SolidBrush(ThemeManager.CurrentTheme.ControlForeColor))
                e.Graphics.DrawString(text, e.Font ?? SystemFonts.DefaultFont, brush, e.Bounds.X + 10, e.Bounds.Y + 4);
            e.DrawFocusRectangle();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetupMode();
            LoadDrives();
            ParseFilter();
            InitializeIcons();
            
            string startPath = InitialDirectory;

            // If no initial directory is specified, try the last visited path
            if (string.IsNullOrEmpty(startPath))
            {
                startPath = SettingsManager.Settings.LastFileBrowserPath ?? "";
            }

            // If still empty or doesn't exist, default to Documents (Windows) or Home/Documents (Linux)
            if (string.IsNullOrEmpty(startPath) || !Directory.Exists(startPath))
            {
                startPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            // Fallback if Documents doesn't exist (e.g. some Linux environments)
            if (!Directory.Exists(startPath))
            {
                startPath = Directory.GetCurrentDirectory();
            }

            Navigate(startPath);

            if (!string.IsNullOrEmpty(FileName))
            {
                txtFileName.Text = Path.GetFileName(FileName);
            }
        }

        private void InitializeIcons()
        {
            _iconList = new ImageList();
            _iconList.ColorDepth = ColorDepth.Depth32Bit;
            _iconList.ImageSize = new Size(16, 16);
            _iconList.Images.Add("folder", DrawFolderIcon());
            _iconList.Images.Add("file", DrawFileIcon());
            lvFiles.SmallImageList = _iconList;
        }

        private Bitmap DrawFolderIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // Folder tab
                g.FillRectangle(Brushes.Goldenrod, 1, 1, 6, 4);
                // Folder body
                g.FillRectangle(Brushes.Goldenrod, 1, 3, 14, 11);
                // Outline
                using (var p = new Pen(Color.DarkGoldenrod))
                {
                    g.DrawRectangle(p, 1, 3, 13, 10); // Body outline
                    g.DrawLine(p, 1, 3, 1, 1); // Left tab line
                    g.DrawLine(p, 1, 1, 7, 1); // Top tab line
                    g.DrawLine(p, 7, 1, 7, 3); // Right tab line
                }
            }
            return bmp;
        }

        private Bitmap DrawFileIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.WhiteSmoke, 3, 1, 10, 14);
                using (var p = new Pen(Color.Gray))
                {
                    g.DrawRectangle(p, 3, 1, 10, 14);
                }
                // Folded corner effect
                Point[] points = { new Point(13, 1), new Point(10, 1), new Point(13, 4) };
                g.FillPolygon(Brushes.LightGray, points);
            }
            return bmp;
        }

        private void SetupMode()
        {
            switch (Mode)
            {
                case BrowserMode.OpenFile:
                    this.Text = string.IsNullOrEmpty(Text) ? "Open File" : Text;
                    btnAction.Text = "Open";
                    lblFilter.Text = "Files of type:";
                    lvFiles.MultiSelect = Multiselect;
                    break;
                case BrowserMode.SaveFile:
                    this.Text = string.IsNullOrEmpty(Text) ? "Save File" : Text;
                    btnAction.Text = "Save";
                    lblFilter.Text = "Save as type:";
                    break;
                case BrowserMode.SelectFolder:
                    this.Text = string.IsNullOrEmpty(Text) ? "Select Folder" : Text;
                    btnAction.Text = "Select Folder";
                    lblFileName.Text = "Folder:";
                    cmbFilter.Enabled = false;
                    break;
            }
        }

        private void LoadDrives()
        {
            lstDrives.Items.Clear();
            
            if (SettingsManager.Settings.FavoritePaths.Count > 0)
            {
                lstDrives.Items.Add(new BrowserItem("Favorites", "", "header"));
                foreach (var path in SettingsManager.Settings.FavoritePaths)
                {
                    lstDrives.Items.Add(new BrowserItem(Path.GetFileName(path), path, "favorite"));
                }
            }

            lstDrives.Items.Add(new BrowserItem("Quick Access", "", "header"));
            lstDrives.Items.Add(new BrowserItem("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "special"));
            lstDrives.Items.Add(new BrowserItem("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "special"));
            lstDrives.Items.Add(new BrowserItem("Downloads", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), "special"));
            
            lstDrives.Items.Add(new BrowserItem("Drives", "", "header"));
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady) lstDrives.Items.Add(new BrowserItem(drive.Name, drive.Name, "drive"));
            }
        }

        private void LstDrives_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstDrives.SelectedItem == null) return;
            
            if (lstDrives.SelectedItem is BrowserItem item && item.Type != "header")
            {
                Navigate(item.Path);
            }
        }

        private void ParseFilter()
        {
            if (string.IsNullOrEmpty(Filter)) return;
            var parts = Filter.Split('|');
            for (int i = 0; i < parts.Length; i += 2)
            {
                cmbFilter.Items.Add(parts[i]);
                if (i + 1 < parts.Length) _filterPatterns.Add(parts[i + 1]);
                else _filterPatterns.Add("*.*");
            }
            if (cmbFilter.Items.Count > 0)
                cmbFilter.SelectedIndex = Math.Max(0, Math.Min(FilterIndex - 1, cmbFilter.Items.Count - 1));
        }

        private void Navigate(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!Directory.Exists(path))
            {
                // Try to navigate to parent if path is a file or invalid
                try { path = Path.GetDirectoryName(path) ?? path; } catch { }
                if (!Directory.Exists(path)) return;
            }

            _currentPath = path;
            txtPath.Text = _currentPath;
            
            // Remember this path
            SettingsManager.Settings.LastFileBrowserPath = _currentPath;
            
            RefreshFileList();
        }

        private void NavigateUp()
        {
            try
            {
                var parent = Directory.GetParent(_currentPath);
                if (parent != null) Navigate(parent.FullName);
            }
            catch { }
        }

        private void RefreshFileList()
        {
            lvFiles.Items.Clear();
            try
            {
                var dirInfo = new DirectoryInfo(_currentPath);
                
                // Folders
                foreach (var dir in dirInfo.GetDirectories())
                {
                    if (!_showHidden && (dir.Attributes & FileAttributes.Hidden) != 0) continue;
                    var item = new ListViewItem(dir.Name);
                    item.SubItems.Add(dir.LastWriteTime.ToString("g"));
                    item.SubItems.Add("File folder");
                    item.SubItems.Add("");
                    item.Tag = "folder";
                    item.ImageKey = "folder";
                    item.ForeColor = ThemeManager.CurrentTheme.AccentColor; // Highlight folders
                    lvFiles.Items.Add(item);
                }

                // Files (if not in folder selection mode, or just show them anyway but grayed out? Standard is show)
                if (Mode != BrowserMode.SelectFolder)
                {
                    string pattern = "*.*";
                    if (cmbFilter.SelectedIndex >= 0 && cmbFilter.SelectedIndex < _filterPatterns.Count)
                    {
                        pattern = _filterPatterns[cmbFilter.SelectedIndex];
                    }
                    // Handle multiple patterns e.g. "*.jpg;*.png"
                    var patterns = pattern.Split(';');
                    
                    var files = new List<FileInfo>();
                    foreach (var p in patterns)
                    {
                        files.AddRange(dirInfo.GetFiles(p.Trim()));
                    }
                    // Distinct in case of overlapping patterns
                    foreach (var file in files.GroupBy(f => f.FullName).Select(g => g.First()))
                    {
                        if (!_showHidden && (file.Attributes & FileAttributes.Hidden) != 0) continue;
                        var item = new ListViewItem(file.Name);
                        item.SubItems.Add(file.LastWriteTime.ToString("g"));
                        item.SubItems.Add(file.Extension);
                        item.SubItems.Add(FormatSize(file.Length));
                        item.Tag = "file";
                        item.ImageKey = "file";
                        item.ForeColor = ThemeManager.CurrentTheme.ForeColor;
                        lvFiles.Items.Add(item);
                    }
                }
            }
            catch
            {
                // Access denied or other error
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void LvFiles_DoubleClick(object? sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count == 0) return;
            var item = lvFiles.SelectedItems[0];
            string name = item.Text;
            string fullPath = Path.Combine(_currentPath, name);

            if (item.Tag?.ToString() == "folder")
            {
                Navigate(fullPath);
            }
            else if (Mode != BrowserMode.SelectFolder)
            {
                SelectFileAndClose(fullPath);
            }
        }

        private void LvFiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count > 0)
            {
                var item = lvFiles.SelectedItems[0];
                if (Mode == BrowserMode.SelectFolder)
                {
                    if (item.Tag?.ToString() == "folder")
                        txtFileName.Text = item.Text;
                }
                else
                {
                    if (item.Tag?.ToString() == "file")
                        txtFileName.Text = item.Text;
                }
            }
        }

        private void BtnNewFolder_Click(object? sender, EventArgs e)
        {
            string name = Prompt.ShowDialog("Enter new folder name:", "New Folder");
            if (!string.IsNullOrWhiteSpace(name))
            {
                try
                {
                    string path = Path.Combine(_currentPath, name);
                    Directory.CreateDirectory(path);
                    RefreshFileList();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to create folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnView_Click(object? sender, EventArgs e)
        {
            ctxViewMenu.Show(btnView, new Point(0, btnView.Height));
        }

        private void SetView(View view)
        {
            lvFiles.View = view;
        }

        private void ToggleHiddenFiles()
        {
            _showHidden = !_showHidden;
            RefreshFileList();
        }

        private void AddFavorite()
        {
            string pathToAdd = _currentPath;
            if (lvFiles.SelectedItems.Count > 0 && lvFiles.SelectedItems[0].Tag?.ToString() == "folder")
            {
                pathToAdd = Path.Combine(_currentPath, lvFiles.SelectedItems[0].Text);
            }

            if (!SettingsManager.Settings.FavoritePaths.Contains(pathToAdd))
            {
                SettingsManager.Settings.FavoritePaths.Add(pathToAdd);
                SettingsManager.Save();
                LoadDrives();
            }
        }

        private void RemoveFavorite()
        {
            if (lstDrives.SelectedItem is BrowserItem item && item.Type == "favorite")
            {
                SettingsManager.Settings.FavoritePaths.Remove(item.Path);
                SettingsManager.Save();
                LoadDrives();
            }
        }

        private class BrowserItem
        {
            public string Name { get; }
            public string Path { get; }
            public string Type { get; } // "header", "favorite", "special", "drive"

            public BrowserItem(string name, string path, string type)
            {
                Name = name;
                Path = path;
                Type = type;
            }

            public override string ToString() => Name;
        }

        private void BtnAction_Click(object? sender, EventArgs e)
        {
            if (Mode == BrowserMode.SelectFolder)
            {
                // If a folder is selected in the list, use that. Otherwise use current path.
                string path = _currentPath;
                if (!string.IsNullOrWhiteSpace(txtFileName.Text))
                {
                    string combined = Path.Combine(_currentPath, txtFileName.Text);
                    if (Directory.Exists(combined)) path = combined;
                }
                
                SelectedPath = path;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                string name = txtFileName.Text;
                if (string.IsNullOrWhiteSpace(name)) return;

                string fullPath = Path.Combine(_currentPath, name);

                if (Mode == BrowserMode.OpenFile)
                {
                    if (File.Exists(fullPath))
                    {
                        SelectFileAndClose(fullPath);
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        Navigate(fullPath);
                        txtFileName.Text = "";
                    }
                    else
                    {
                        CustomMessageBox.Show("File not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (Mode == BrowserMode.SaveFile)
                {
                    // Append extension if missing
                    if (cmbFilter.SelectedIndex >= 0 && cmbFilter.SelectedIndex < _filterPatterns.Count)
                    {
                        string pattern = _filterPatterns[cmbFilter.SelectedIndex];
                        if (pattern.StartsWith("*.") && !pattern.Contains(";") && !Path.HasExtension(fullPath))
                        {
                            fullPath += pattern.Substring(1);
                        }
                    }

                    if (OverwritePrompt && File.Exists(fullPath))
                    {
                        var res = CustomMessageBox.Show($"'{Path.GetFileName(fullPath)}' already exists.\nDo you want to replace it?", "Confirm Save As", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (res != DialogResult.Yes) return;
                    }

                    FileName = fullPath;
                    FileNames = new[] { fullPath };
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
        }

        private void SelectFileAndClose(string path)
        {
            if (Multiselect)
            {
                var selected = new List<string>();
                foreach (ListViewItem item in lvFiles.SelectedItems)
                {
                    if (item.Tag?.ToString() == "file")
                        selected.Add(Path.Combine(_currentPath, item.Text));
                }
                if (selected.Count == 0) selected.Add(path);
                FileNames = selected.ToArray();
                FileName = selected.FirstOrDefault() ?? "";
            }
            else
            {
                FileName = path;
                FileNames = new[] { path };
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SettingsManager.Save();
            base.OnFormClosing(e);
        }
    }
#pragma warning restore CA1416
}