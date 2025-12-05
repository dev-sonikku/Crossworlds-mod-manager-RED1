using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;


namespace CrossworldsModManager
{
    public partial class ModDetailsForm : Form
    {
        private readonly GameBananaMod _mod;
        private readonly IProgress<string>? _logger;

        private PictureBox picModImage = null!;
        private Label lblModName = null!;
        private Label lblAuthor = null!;
        private LinkLabel lnkProfileUrl = null!; 
        private Label lblLikeCount = null!;
        private WebBrowser webDescription = null!; 
        private Button btnDownload = null!;
        private ListBox lstFiles = null!;

        public ModDetailsForm(GameBananaMod mod, IProgress<string>? logger = null)
        {
            _mod = mod;
            _logger = logger;
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
                _logger?.Report($"Fetching details for mod '{_mod.Name}' (ID: {_mod.Id})...");
                var modProfile = await GameBananaApiService.GetModDetailsAsync(_mod);
                if (modProfile != null && !string.IsNullOrEmpty(modProfile.Description))
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

                _logger?.Report($"Fetching download page for mod '{_mod.Name}'...");
                var downloadPage = await GameBananaApiService.GetModDownloadPageAsync(_mod);
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
                _logger?.Report($"ERROR fetching mod details: {ex.Message}");
                webDescription.DocumentText = $"<body style='background-color:#252526; color:white; font-family:sans-serif;'>Failed to load description. See debug log for details.</body>";
                btnDownload.Enabled = false;
                btnDownload.Text = "Error Loading Files";
            }
        }

        private void btnDownload_Click(object? sender, EventArgs e)
        {
            if (lstFiles.SelectedItem is not GameBananaFile selectedFile)
            {
                MessageBox.Show("Please select a file to download.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // TODO: This event should trigger the download process in the MainForm or a dedicated service.
            _logger?.Report($"Download initiated for file: {selectedFile.FileName} from {selectedFile.DownloadUrl}");
            MessageBox.Show($"Downloading '{selectedFile.FileName}'...", "Download Initiated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // We can open the URL in the browser for now as a placeholder for actual download logic
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(selectedFile.DownloadUrl) { UseShellExecute = true });

            this.Close();
        }
    }
}