using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using System.IO.Compression;
using SharpCompress.Archives;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;


namespace CrossworldsModManager
{
    public partial class ModDetailsForm : Form
    {
        private readonly GameBananaMod _mod;
        private readonly IProgress<string>? _logger;
        private readonly Action? _onModsChanged;

        private PictureBox picModImage = null!;
        private Label lblModName = null!;
        private Label lblAuthor = null!;
        private LinkLabel lnkProfileUrl = null!; 
        private Label lblLikeCount = null!;
        private WebBrowser webDescription = null!; 
        private Button btnDownload = null!;
        private ListBox lstFiles = null!;
        private ProgressBar prgDownload = null!;
        private Label lblProgress = null!;
        private TableLayoutPanel mainLayout = null!;

        public string ModName => _mod.Name;

        public ModDetailsForm(GameBananaMod mod, IProgress<string>? logger = null, Action? onModsChanged = null)
        {
            _mod = mod;
            _logger = logger;
            _onModsChanged = onModsChanged;
            InitializeComponent();
            // Set the form's icon from the executable's embedded icon.
            this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);

            this.Load += async (s, e) => await PopulateDataAsync();
        }

        private void InitializeComponent()
        {
            // Initialize controls
            this.picModImage = new PictureBox();
            this.lblModName = new Label();
            this.lblAuthor = new Label();
            this.lnkProfileUrl = new LinkLabel();
            this.lblLikeCount = new Label();
            this.webDescription = new WebBrowser(); 
            this.btnDownload = new Button();
            this.lstFiles = new ListBox();
            this.prgDownload = new ProgressBar();
            this.lblProgress = new Label();

            // Main layout panel
            this.mainLayout = new TableLayoutPanel();
            mainLayout.ColumnCount = 2;
            mainLayout.RowCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Details on left
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Download on right
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.BackColor = Color.FromArgb(45, 45, 48);

            // Create a dedicated panel for the WebBrowser control
            Panel pnlWebContainer = new Panel();
            pnlWebContainer.Dock = DockStyle.Fill;
            pnlWebContainer.BorderStyle = BorderStyle.None;

            // Left panel for mod details
            Panel pnlDetails = new Panel();
            pnlDetails.Dock = DockStyle.Fill;
            pnlDetails.Padding = new Padding(10);
            pnlDetails.AutoScroll = true;
            pnlDetails.BackColor = Color.FromArgb(37, 37, 38);
            pnlDetails.Controls.Add(pnlWebContainer); // Add web container to details panel

            // Right panel for download options
            Panel pnlDownloadOptions = new Panel();
            pnlDownloadOptions.Dock = DockStyle.Fill;
            pnlDownloadOptions.Padding = new Padding(10);
            pnlDownloadOptions.BackColor = Color.FromArgb(45, 45, 48);

            // picModImage
            this.picModImage.Dock = DockStyle.Top;
            this.picModImage.SizeMode = PictureBoxSizeMode.Zoom;
            this.picModImage.Height = 200; // Fixed height for image
            this.picModImage.BackColor = Color.Black;

            // lblModName
            this.lblModName.Dock = DockStyle.Top;
            this.lblModName.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblModName.ForeColor = Color.White;
            this.lblModName.Padding = new Padding(0, 10, 0, 5);
            this.lblModName.AutoSize = false; // Allow text to wrap
            this.lblModName.Height = 60; // Give it some height for wrapping

            // lblAuthor
            this.lblAuthor.Dock = DockStyle.Top;
            this.lblAuthor.Font = new Font("Segoe UI", 10F);
            this.lblAuthor.ForeColor = Color.LightGray;
            this.lblAuthor.Padding = new Padding(0, 0, 0, 5);
            this.lblAuthor.AutoSize = true;

            // lnkProfileUrl
            this.lnkProfileUrl.Dock = DockStyle.Top;
            this.lnkProfileUrl.Font = new Font("Segoe UI", 9F);
            this.lnkProfileUrl.ForeColor = Color.FromArgb(0, 122, 204);
            this.lnkProfileUrl.LinkColor = Color.FromArgb(0, 122, 204);
            this.lnkProfileUrl.Padding = new Padding(0, 0, 0, 10);
            this.lnkProfileUrl.AutoSize = true;
            this.lnkProfileUrl.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_mod.ProfileUrl) { UseShellExecute = true });

            // lblLikeCount
            this.lblLikeCount.Dock = DockStyle.Top;
            this.lblLikeCount.Font = new Font("Segoe UI", 9F);
            this.lblLikeCount.ForeColor = Color.LightGray;
            this.lblLikeCount.Padding = new Padding(0, 0, 0, 10);
            this.lblLikeCount.AutoSize = true;

            // webDescription
            this.webDescription.Dock = DockStyle.Fill;
            this.webDescription.MinimumSize = new System.Drawing.Size(20, 20);
            this.webDescription.IsWebBrowserContextMenuEnabled = false;
            this.webDescription.WebBrowserShortcutsEnabled = false;
            this.webDescription.Navigating += (s, e) =>
            {
                var url = e.Url;
                if (url != null && url.Scheme != "about")
                {
                    e.Cancel = true; // Cancel internal navigation
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url.ToString()) { UseShellExecute = true });
                }
            };

            // lstFiles
            this.lstFiles.Dock = DockStyle.Fill;
            this.lstFiles.BackColor = Color.FromArgb(37, 37, 38);
            this.lstFiles.ForeColor = Color.White;
            this.lstFiles.BorderStyle = BorderStyle.FixedSingle;
            this.lstFiles.IntegralHeight = false;

            // lblProgress
            this.lblProgress.Dock = DockStyle.Bottom;
            this.lblProgress.Height = 20;
            this.lblProgress.TextAlign = ContentAlignment.MiddleCenter;
            this.lblProgress.Visible = false;
            this.lblProgress.ForeColor = Color.White;

            // prgDownload
            this.prgDownload.Dock = DockStyle.Bottom;
            this.prgDownload.Height = 10;
            this.prgDownload.Visible = false;
            this.prgDownload.Style = ProgressBarStyle.Continuous;


            // btnDownload
            this.btnDownload.Dock = DockStyle.Bottom;
            this.btnDownload.Text = "Download Selected File";
            this.btnDownload.Height = 40;
            this.btnDownload.FlatStyle = FlatStyle.Flat;
            this.btnDownload.BackColor = Color.FromArgb(0, 122, 204);
            this.btnDownload.FlatAppearance.BorderSize = 0;
            this.btnDownload.ForeColor = Color.White;
            this.btnDownload.Click += btnDownload_Click;
            
            // Add controls to panels
            pnlWebContainer.Controls.Add(this.webDescription); // Add browser to its container
            pnlDetails.Controls.Add(this.lnkProfileUrl);
            pnlDetails.Controls.Add(this.lblAuthor);
            pnlDetails.Controls.Add(this.lblLikeCount);
            pnlDetails.Controls.Add(this.lblModName);
            pnlDetails.Controls.Add(this.picModImage);

            pnlDownloadOptions.Controls.Add(this.lstFiles);
            pnlDownloadOptions.Controls.Add(this.prgDownload);
            pnlDownloadOptions.Controls.Add(this.lblProgress);
            pnlDownloadOptions.Controls.Add(this.btnDownload);

            // Add panels to main layout
            mainLayout.Controls.Add(pnlDetails, 0, 0);
            mainLayout.Controls.Add(pnlDownloadOptions, 1, 0);

            // Form settings
            this.Controls.Add(mainLayout);
            this.Text = "Mod Details: " + _mod.Name;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.ClientSize = new Size(800, 600);
            this.MinimumSize = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
        }

        public void SetConfirmationMode()
        {
            this.Text = "Confirm Mod Installation";
            btnDownload.Text = "Install";
            btnDownload.DialogResult = DialogResult.OK; // Will be handled by click event

            // Undock the original button so we can place it in a new panel
            btnDownload.Dock = DockStyle.None;
            btnDownload.Anchor = AnchorStyles.Right;
            btnDownload.Width = 100;

            // Add a cancel button
            var btnCancel = new Button
            {
                Name = "btnCancel",
                Text = "Cancel",
                Anchor = AnchorStyles.Right,
                Width = 100,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            // Use a FlowLayoutPanel to keep buttons next to each other at the bottom right
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 50,
                Padding = new Padding(0, 5, 0, 5)
            };

            // Capture the original parent before changing it.
            var originalParent = btnDownload.Parent;

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnDownload);

            // Add the new button panel to the parent of the original download button
            if (originalParent is Panel parentPanel)
            {
                parentPanel.Controls.Add(buttonPanel);
                // Bring the button panel to the front to ensure it's at the very bottom
                buttonPanel.BringToFront();
            }
        }

        private void ShowProgressView()
        {
            // Hide the main details and file list panels
            mainLayout.ColumnStyles[0].Width = 0;
            mainLayout.ColumnStyles[1].Width = 100;
            mainLayout.Controls[0].Visible = false; // pnlDetails

            // In the right panel, hide everything except the progress indicators
            lstFiles.Visible = false;
            btnDownload.Visible = false;
            // The FlowLayoutPanel with the buttons is a child of btnDownload's original parent
            if (btnDownload.Parent is FlowLayoutPanel buttonPanel)
            {
                buttonPanel.Visible = false;
            }
        }

        private void ShowCompletionView()
        {
            // This is called after a successful 1-Click install.
            // It reconfigures the progress view to show a final "OK" button.

            prgDownload.Visible = false;
            lblProgress.Text = "Installation Complete!";
            lblProgress.Font = new Font(lblProgress.Font, FontStyle.Bold);

            // Create a final OK button to close the form.
            var btnOk = new Button
            {
                Name = "btnOk",
                Text = "OK",
                DialogResult = DialogResult.OK,
                Height = 40,
                Dock = DockStyle.Bottom,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White
            };
            btnOk.FlatAppearance.BorderSize = 0;

            if (prgDownload.Parent != null)
            {
                prgDownload.Parent.Controls.Add(btnOk);
            }
        }
        private async Task PopulateDataAsync()
        {
            if (this.IsDisposed || webDescription.IsDisposed) return;

            lblModName.Text = _mod.Name;
            lblAuthor.Text = $"by {_mod.Author}";
            lnkProfileUrl.Text = _mod.ProfileUrl;
            lblLikeCount.Text = $"Likes: {_mod.LikeCount:N0}";
            webDescription.DocumentText = "<body style='background-color:#252526; color:white; font-family:sans-serif;'>Loading description...</body>";

            if (!string.IsNullOrEmpty(_mod.ThumbnailUrl))
            {
                picModImage.LoadAsync(_mod.ThumbnailUrl);
            }

            // Asynchronously load description and file list
            try
            {
                _logger?.Report($"Fetching details and files for mod '{_mod.Name}' (ID: {_mod.Id})...");

                // Run API calls in parallel for faster loading
                var detailsTask = GameBananaApiService.GetModDetailsAsync(_mod);
                var downloadPageTask = GameBananaApiService.GetModDownloadPageAsync(_mod);
                await Task.WhenAll(detailsTask, downloadPageTask);

                var modProfile = await detailsTask;
                var downloadPage = await downloadPageTask;

                if (this.IsDisposed) return;

                if (webDescription.IsDisposed) return;

                // Populate description
                if (modProfile?.Description != null && !string.IsNullOrEmpty(modProfile.Description))
                {
                    _mod.Name = modProfile.Name; // Update name from profile
                    lblLikeCount.Text = $"Likes: {modProfile.LikeCount:N0}"; // Update with more accurate count from profile if available
                    _logger?.Report($"Successfully fetched details. Description length: {modProfile.Description.Length} characters.");
                    // Inject some basic CSS to make the HTML content match the dark theme.
                    string styledHtml = "<style>body { background-color: #252526; color: white; font-family: sans-serif; } a { color: #569CD6; }</style>" + modProfile.Description;
                    webDescription.DocumentText = styledHtml;
                }
                else
                {
                    _logger?.Report("Received a valid response, but it contained no description.");
                    webDescription.DocumentText = "<body style='background-color:#252526; color:white; font-family:sans-serif;'>No description available for this mod.</body>";
                }

                if (this.IsDisposed || lstFiles.IsDisposed) return;

                // Populate file list
                if (downloadPage?.Files != null && downloadPage.Files.Any())
                {
                    lstFiles.Items.AddRange(downloadPage.Files.ToArray());
                    lstFiles.SelectedIndex = 0; // Select the first file by default
                    _logger?.Report($"Found {downloadPage.Files.Count} file(s).");
                }
                else
                {
                    _logger?.Report("No downloadable files found for this mod.");
                    btnDownload.Enabled = false;
                    btnDownload.Text = "No Files Found";
                }
            }
            catch (Exception ex)
            {
                if (this.IsDisposed) return;
                _logger?.Report($"ERROR fetching mod details: {ex.Message}");
                webDescription.DocumentText = $"<body style='background-color:#252526; color:white; font-family:sans-serif;'>Failed to load description. See debug log for details.</body>";
                btnDownload.Enabled = false;
                btnDownload.Text = "Error Loading Files";
            }
        }

        private async void btnDownload_Click(object? sender, EventArgs e)
        {
            if (lstFiles.SelectedItem is not GameBananaFile selectedFile)
            {
                MessageBox.Show("Please select a file to download.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // For 1-Click installs, this form acts as a confirmation dialog.
            // Clicking "Install" sets the DialogResult and closes this form.
            // The main form will then handle launching the progress window.
            if (this.Owner is MainForm)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            // For regular downloads from the browser, we launch the progress form directly.
            await LaunchProgressForm(selectedFile);
            this.DialogResult = DialogResult.OK; // Mark as successful to close the details form.
            this.Close();
        }

        public GameBananaFile? GetSelectedFile()
        {
            return lstFiles.SelectedItem as GameBananaFile;
        }

        private async Task RunFullInstallProcessAsync(GameBananaFile selectedFile, ProgressForm progressForm)
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossworldsModManager", "Downloads");
            Directory.CreateDirectory(appDataPath);
            var downloadedFilePath = Path.Combine(appDataPath, selectedFile.FileName);

            try
            {
                await DownloadFileAsync(selectedFile, downloadedFilePath, progressForm);
                await ExtractAndInstallAsync(downloadedFilePath, progressForm);
                _onModsChanged?.Invoke(); // Trigger a refresh on the main form
                progressForm.ShowCompletion("Installation Complete!");
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to download or install mod: {ex.Message}";
                _logger?.Report($"ERROR: {errorMsg}");
                progressForm.ShowCompletion($"Error: {ex.Message}");
            }
            finally
            {
                // Clean up the downloaded file
                if (File.Exists(downloadedFilePath))
                {
                    try { File.Delete(downloadedFilePath); }
                    catch (Exception ex) { _logger?.Report($"Could not clean up temporary file '{downloadedFilePath}': {ex.Message}"); }
                }
            }
        }

        public async Task LaunchProgressForm(GameBananaFile fileToInstall)
        {
            using (var progressForm = new ProgressForm($"Installing '{_mod.Name}'..."))
            {
                // Run the installation process asynchronously while the form is shown modally.
                progressForm.Shown += async (s, e) => await RunFullInstallProcessAsync(fileToInstall, progressForm);
                progressForm.ShowDialog(this);
            }
        }

        private async Task DownloadFileAsync(GameBananaFile selectedFile, string destinationPath, ProgressForm progressForm)
        {
            _logger?.Report($"Starting download: {selectedFile.DownloadUrl}");
            progressForm.UpdateStatus("Downloading...");

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(selectedFile.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var totalBytesRead = 0L;

                using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        var buffer = new byte[81920];
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            if (totalBytes != -1)
                            {
                                var progressPercentage = (int)((totalBytesRead * 100) / totalBytes);
                                progressForm.UpdateProgress(progressPercentage);
                                progressForm.UpdateStatus($"Downloading... {progressPercentage}%");
                            }
                        }
                    }
                }
            }
            _logger?.Report($"Download complete: {destinationPath}");
        }

        private async Task ExtractAndInstallAsync(string archivePath, ProgressForm progressForm)
        {
            var modsDirectory = SettingsManager.Settings.ModsDirectory;
            if (string.IsNullOrEmpty(modsDirectory)) throw new InvalidOperationException("Mods directory is not set.");

            progressForm.UpdateStatus("Extracting...");
            progressForm.UpdateProgress(100); // Keep bar full during extraction

            // Use the mod's GameBanana name for the folder, sanitizing it for file system compatibility.
            string modName = SanitizeFolderName(_mod.Name);
            string targetDir = Path.Combine(modsDirectory, modName);

            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            Directory.CreateDirectory(targetDir);

            _logger?.Report($"Extracting {Path.GetFileName(archivePath)} using SharpCompress...");
            await Task.Run(() =>
            {
                using var archive = ArchiveFactory.Open(archivePath);
                archive.WriteToDirectory(targetDir, new SharpCompress.Common.ExtractionOptions { ExtractFullPath = true, Overwrite = true });
            });
        }
        
        private static string SanitizeFolderName(string name)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            // Trim any leading/trailing spaces or dots that might cause issues
            return name.Trim(' ', '.');
        }
    }
}