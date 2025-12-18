using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public partial class MegaManPromoForm : Form
    {
        public bool DoNotShowAgain { get; private set; }
        private CheckBox _chkDoNotShow = null!;

        public MegaManPromoForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "A Message from the Developer";
            this.Size = new Size(650, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;

            var mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.RowCount = 4;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Image
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Text
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Link
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Bottom controls
            this.Controls.Add(mainLayout);

            var pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            LoadImageAsync(pb, "https://static.wikia.nocookie.net/vsdebating/images/a/ae/Megaman-PNG-HD.png?format=png");
            mainLayout.Controls.Add(pb, 0, 0);

            var lblMsg = new Label();
            lblMsg.Text = "The development of this mod manager was sponsored by my love for megaman. BUY HIS GAMES";
            lblMsg.Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 12, FontStyle.Bold);
            lblMsg.TextAlign = ContentAlignment.MiddleCenter;
            lblMsg.Dock = DockStyle.Fill;
            lblMsg.AutoSize = true;
            lblMsg.MaximumSize = new Size(this.ClientSize.Width - 20, 0);
            lblMsg.Padding = new Padding(10);
            mainLayout.Controls.Add(lblMsg, 0, 1);

            var lnk = new LinkLabel();
            lnk.Text = "https://store.steampowered.com/curator/34827987";
            lnk.TextAlign = ContentAlignment.MiddleCenter;
            lnk.Dock = DockStyle.Fill;
            lnk.AutoSize = true;
            lnk.Padding = new Padding(0, 0, 0, 10);
            lnk.LinkClicked += (s, e) => {
                try {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(lnk.Text) { UseShellExecute = true });
                } catch {}
            };
            mainLayout.Controls.Add(lnk, 0, 2);

            var bottomPanel = new FlowLayoutPanel();
            bottomPanel.Dock = DockStyle.Fill;
            bottomPanel.FlowDirection = FlowDirection.RightToLeft;
            bottomPanel.AutoSize = true;
            bottomPanel.Padding = new Padding(10);

            var btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.DialogResult = DialogResult.OK;
            
            _chkDoNotShow = new CheckBox();
            _chkDoNotShow.Text = "Do not show next time";
            _chkDoNotShow.AutoSize = true;
            _chkDoNotShow.Padding = new Padding(0, 6, 10, 0);

            bottomPanel.Controls.Add(btnOk);
            bottomPanel.Controls.Add(_chkDoNotShow);
            
            mainLayout.Controls.Add(bottomPanel, 0, 3);
            
            this.AcceptButton = btnOk;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DoNotShowAgain = _chkDoNotShow.Checked;
            base.OnFormClosing(e);
        }

        private async void LoadImageAsync(PictureBox pb, string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    var data = await client.GetByteArrayAsync(url);
                    pb.Image = Image.FromStream(new MemoryStream(data));
                }
            }
            catch { /* Ignore image load errors */ }
        }
    }
}