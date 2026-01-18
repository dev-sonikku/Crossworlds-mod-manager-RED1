using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public class ThemeEditorForm : Form
    {
        private SerializableTheme _theme;
        public SerializableTheme ResultTheme => _theme;

        public ThemeEditorForm(SerializableTheme theme)
        {
            // Create a copy so we don't modify the settings directly until saved
            _theme = new SerializableTheme
            {
                BackColor = theme.BackColor,
                ForeColor = theme.ForeColor,
                ControlBackColor = theme.ControlBackColor,
                ControlForeColor = theme.ControlForeColor,
                ButtonBackColor = theme.ButtonBackColor,
                ButtonForeColor = theme.ButtonForeColor,
                AccentColor = theme.AccentColor,
                PlayButtonColor = theme.PlayButtonColor,
                BorderColor = theme.BorderColor,
                MenuBackColor = theme.MenuBackColor,
                MenuForeColor = theme.MenuForeColor
            };
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Text = "Edit Custom Theme";
            this.Size = new Size(400, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            AddColorPicker(layout, "Background", () => Color.FromArgb(_theme.BackColor), c => _theme.BackColor = c.ToArgb());
            AddColorPicker(layout, "Control Background", () => Color.FromArgb(_theme.ControlBackColor), c => _theme.ControlBackColor = c.ToArgb());
            AddColorPicker(layout, "Button Background", () => Color.FromArgb(_theme.ButtonBackColor), c => _theme.ButtonBackColor = c.ToArgb());
            AddColorPicker(layout, "Menu Background", () => Color.FromArgb(_theme.MenuBackColor), c => _theme.MenuBackColor = c.ToArgb());
            AddColorPicker(layout, "Accent Color", () => Color.FromArgb(_theme.AccentColor), c => _theme.AccentColor = c.ToArgb());

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft };
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, Size = new Size(80, 30) };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(63, 63, 70), ForeColor = Color.White, Size = new Size(80, 30) };
            
            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnOk);

            this.Controls.Add(layout);
            this.Controls.Add(btnPanel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void AddColorPicker(TableLayoutPanel layout, string name, Func<Color> getter, Action<Color> setter)
        {
            var lbl = new Label { Text = name, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ThemeManager.CurrentTheme.ForeColor };
            var pnl = new Panel { Height = 25, Width = 50, BorderStyle = BorderStyle.FixedSingle, BackColor = getter(), Cursor = Cursors.Hand, Tag = "ColorSwatch" };
            
            pnl.Click += (s, e) => {
                using (var cd = new ColorDialog())
                {
                    cd.Color = pnl.BackColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        pnl.BackColor = cd.Color;
                        setter(cd.Color);
                        ThemeManager.ReloadCustomTheme(_theme);
                        ThemeManager.ApplyTheme(this);
                    }
                }
            };

            layout.RowCount++;
            layout.Controls.Add(lbl);
            layout.Controls.Add(pnl);
        }
    }
}