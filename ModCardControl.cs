using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public partial class ModCardControl : UserControl
    {
        public event Action<GameBananaMod> DownloadClicked = delegate { };
        private readonly GameBananaMod _mod;

        private PictureBox picThumbnail = null!;
        private Label lblModName = null!;
        private Label lblAuthor = null!;
        private Button btnDownload = null!;

        public ModCardControl(GameBananaMod mod)
        {
            _mod = mod;
            InitializeComponent();
            PopulateData();
        }

        private void InitializeComponent()
        {
            this.picThumbnail = new PictureBox();
            this.lblModName = new Label();
            this.lblAuthor = new Label();
            this.btnDownload = new Button();

            // UserControl
            this.BackColor = Color.FromArgb(63, 63, 70);
            this.Size = new Size(250, 280);
            this.Margin = new Padding(10);
            this.BorderStyle = BorderStyle.FixedSingle;

            // Thumbnail
            picThumbnail.Dock = DockStyle.Top;
            picThumbnail.Height = 140;
            picThumbnail.SizeMode = PictureBoxSizeMode.StretchImage;
            picThumbnail.BackColor = Color.FromArgb(37, 37, 38);
            picThumbnail.Padding = new Padding(1); // Use padding for a subtle border effect

            // Mod Name
            lblModName.Dock = DockStyle.Top;
            lblModName.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblModName.ForeColor = Color.White;
            lblModName.Padding = new Padding(5, 5, 5, 0);
            lblModName.Height = 50;
            lblModName.AutoEllipsis = true;

            // Author
            lblAuthor.Dock = DockStyle.Top;
            lblAuthor.Font = new Font("Segoe UI", 8F);
            lblAuthor.ForeColor = Color.LightGray;
            lblAuthor.Padding = new Padding(5, 0, 5, 5);
            lblAuthor.Height = 25;

            // Download Button
            btnDownload.Dock = DockStyle.Bottom;
            btnDownload.Text = "Download";
            btnDownload.Height = 35;
            btnDownload.FlatStyle = FlatStyle.Flat;
            btnDownload.BackColor = Color.FromArgb(0, 122, 204);
            btnDownload.FlatAppearance.BorderSize = 0;
            btnDownload.Click += (s, e) => DownloadClicked(_mod);

            this.Controls.Add(btnDownload);
            this.Controls.Add(lblAuthor);
            this.Controls.Add(lblModName);
            this.Controls.Add(picThumbnail);
        }

        private void PopulateData()
        {
            lblModName.Text = _mod.Name;
            lblAuthor.Text = $"by {_mod.Author}";
            if (!string.IsNullOrEmpty(_mod.ThumbnailUrl))
            {
                picThumbnail.LoadAsync(_mod.ThumbnailUrl);
            }
        }
    }
}