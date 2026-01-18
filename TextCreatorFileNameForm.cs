using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public class TextCreatorFileNameForm : Form
    {
        public string FileName { get; private set; } = "";
        private TextBox? txtFileName;
        private Button? btnOk;
        private Button? btnCancel;
        private Button? btnLoad;

        public TextCreatorFileNameForm()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Text = "New Text Mod File";
            this.Size = new Size(400, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            var lblPrompt = new Label
            {
                Text = "Enter a name for your JSON file:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F)
            };

            txtFileName = new TextBox
            {
                Location = new Point(20, 50),
                Width = 340,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F)
            };

            btnOk = new Button
            {
                Text = "Create",
                DialogResult = DialogResult.OK,
                Location = new Point(180, 90),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => { FileName = txtFileName.Text; };

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(280, 90),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnLoad = new Button
            {
                Text = "Load Existing...",
                Location = new Point(20, 90),
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White
            };
            btnLoad.FlatAppearance.BorderSize = 0;
            btnLoad.Click += BtnLoad_Click;

            this.Controls.Add(lblPrompt);
            this.Controls.Add(txtFileName);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnLoad);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void BtnLoad_Click(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select JSON Mod File";
                ofd.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    FileName = ofd.FileName;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }
    }
}