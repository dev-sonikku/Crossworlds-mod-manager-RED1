using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using System.IO.Compression;
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

        public ModDetailsForm(GameBananaMod mod, IProgress<string>? logger = null, Action? onModsChanged = null)
        {
            _mod = mod;
            _logger = logger;
            _onModsChanged = onModsChanged;
            InitializeComponent();
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
            TableLayoutPanel mainLayout = new TableLayoutPanel();
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
            
            btnDownload.Enabled = false;
            btnDownload.Text = "Downloading...";
            lstFiles.Enabled = false;

            prgDownload.Visible = true;
            lblProgress.Visible = true;
            lblProgress.Text = "Starting...";

            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossworldsModManager", "Downloads");
            Directory.CreateDirectory(appDataPath);
            var downloadedFilePath = Path.Combine(appDataPath, selectedFile.FileName);

            try
            {
                _logger?.Report($"Starting download: {selectedFile.DownloadUrl}");
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(selectedFile.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var totalBytesRead = 0L;

                    using (var fs = new FileStream(downloadedFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
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
                                    prgDownload.Value = progressPercentage;
                                    lblProgress.Text = $"Downloading... {progressPercentage}%";
                                }
                            }
                        }
                    }
                }
                _logger?.Report($"Download complete: {downloadedFilePath}");

                btnDownload.Text = "Extracting...";
                lblProgress.Text = "Extracting...";
                await InstallModAsync(downloadedFilePath);

                MessageBox.Show($"Successfully installed '{_mod.Name}'!", "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _onModsChanged?.Invoke(); // Trigger a refresh on the main form
                this.Close();
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to download or install mod: {ex.Message}";
                _logger?.Report($"ERROR: {errorMsg}");
                MessageBox.Show(errorMsg, "Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable controls if the form is still open
                if (!this.IsDisposed)
                {
                    btnDownload.Enabled = true;
                    btnDownload.Text = "Download Selected File";
                    lstFiles.Enabled = true;
                    prgDownload.Visible = false;
                    lblProgress.Visible = false;
                }

                // Clean up the downloaded file
                if (File.Exists(downloadedFilePath))
                {
                    try { File.Delete(downloadedFilePath); }
                    catch (Exception ex) { _logger?.Report($"Could not clean up temporary file '{downloadedFilePath}': {ex.Message}"); }
                }
            }
        }

        private async Task InstallModAsync(string archivePath)
        {
            var modsDirectory = SettingsManager.Settings.ModsDirectory;
            if (string.IsNullOrEmpty(modsDirectory)) throw new InvalidOperationException("Mods directory is not set.");

            var toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
            var unrarPath = Path.Combine(toolsDir, "UnRAR.exe");
            if (!File.Exists(unrarPath)) throw new FileNotFoundException("UnRAR.exe not found in Tools folder. It is required to extract archives.");

            // Use the mod's GameBanana name for the folder, sanitizing it for file system compatibility.
            string modName = SanitizeFolderName(_mod.Name);
            string targetDir = Path.Combine(modsDirectory, modName); 

            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            Directory.CreateDirectory(targetDir);

            string extension = Path.GetExtension(archivePath).ToLowerInvariant();
            
            if (extension == ".zip")
            {
                _logger?.Report("Extracting .zip file using built-in method...");
                await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, targetDir, true));
            }
            else if (extension == ".rar")
            {
                _logger?.Report("Extracting .rar file using UnRAR.exe...");
                await ExtractWithToolAsync(unrarPath, $"x -o+ \"{archivePath}\" \"{targetDir}\\\" -y", _logger);
            }
            else if (extension == ".7z")
            {
                _logger?.Report("Extracting .7z file using 7zr.exe...");
                var sevenZipPath = Path.Combine(toolsDir, "7zr.exe");
                if (!File.Exists(sevenZipPath)) throw new FileNotFoundException("7zr.exe not found in Tools folder. It is required to extract .zip and .7z files.");
                await ExtractWithToolAsync(sevenZipPath, $"x \"{archivePath}\" -o\"{targetDir}\" -y", _logger);
            }
            else
            {
                throw new NotSupportedException($"Unsupported archive format: {extension}");
            }
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

        private static async Task ExtractWithToolAsync(string toolPath, string arguments, IProgress<string>? progress)
        {
            progress?.Report($"Extracting with {Path.GetFileName(toolPath)}...");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            
            // Start reading the output and error streams asynchronously.
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            // Wait for the process to exit completely.
            await process.WaitForExitAsync();

            // Now, await the results of the stream reading.
            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new Exception($"{Path.GetFileName(toolPath)} failed with exit code {process.ExitCode}.\nOutput: {output}\nError: {error}");
            }
        }
    }
}