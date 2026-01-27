// c:\games\Projects\Crossworlds mod manager RED1\CustomMessageBox.cs
using System;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public static class CustomMessageBox
    {
        public static DialogResult Show(string text)
        {
            return Show(null, text, "Message", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption)
        {
            return Show(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            return Show(null, text, caption, buttons, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return Show(null, text, caption, buttons, icon, MessageBoxDefaultButton.Button1);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
        {
            return Show(null, text, caption, buttons, icon, defaultButton);
        }

        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
        {
            // If no owner is specified, try to use the active form.
            // This fixes issues where the message box might appear behind other modal forms (like MegaManPromoForm).
            if (owner == null)
            {
                owner = Form.ActiveForm;
            }

            using (var form = new Form())
            {
                form.Text = caption;
                form.StartPosition = owner != null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowIcon = false;
                form.ShowInTaskbar = false;
                form.BackColor = Color.FromArgb(45, 45, 48);
                form.ForeColor = Color.White;
                form.KeyPreview = true; // Enable key preview for Ctrl+C

                // Play system sound
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        switch (icon)
                        {
                            case MessageBoxIcon.Error: SystemSounds.Hand.Play(); break;
                            case MessageBoxIcon.Exclamation: SystemSounds.Exclamation.Play(); break;
                            case MessageBoxIcon.Question: SystemSounds.Question.Play(); break;
                            case MessageBoxIcon.Asterisk: SystemSounds.Asterisk.Play(); break;
                            default: SystemSounds.Beep.Play(); break;
                        }
                    }
                    catch { }
                }

                int textX = 20;

                // Add Icon
                if (icon != MessageBoxIcon.None)
                {
                    var pbox = new PictureBox();
                    pbox.Location = new Point(20, 20);
                    pbox.Size = new Size(32, 32);
                    pbox.SizeMode = PictureBoxSizeMode.Zoom;
                    
                    pbox.Image = CreateIcon(icon);
                    form.Controls.Add(pbox);
                    textX = 70;
                }
                
                // Create label for text
                var lbl = new Label();
                lbl.Text = text;
                lbl.Location = new Point(textX, 20);
                lbl.Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 10F);
                
                // Manual sizing for better cross-platform wrapping
                lbl.AutoSize = false;
                lbl.Width = 480 - textX;
                Size preferredSize = TextRenderer.MeasureText(text, lbl.Font, new Size(lbl.Width, 0), TextFormatFlags.WordBreak);
                lbl.Height = preferredSize.Height + 10;
                form.Controls.Add(lbl);

                // Ctrl+C to copy text
                form.KeyDown += (s, e) => {
                    if (e.Control && e.KeyCode == Keys.C)
                    {
                        Clipboard.SetText($"---------------------------\n{caption}\n---------------------------\n{text}\n---------------------------");
                    }
                };

                // Calculate size
                int contentHeight = lbl.Bottom + 20;
                int buttonPanelHeight = 80;
                form.ClientSize = new Size(500, Math.Max(150, contentHeight + buttonPanelHeight));

                // Button panel
                var pnlButtons = new FlowLayoutPanel();
                pnlButtons.FlowDirection = FlowDirection.RightToLeft;
                pnlButtons.Dock = DockStyle.Bottom;
                pnlButtons.Height = buttonPanelHeight;
                pnlButtons.Padding = new Padding(10);
                pnlButtons.BackColor = Color.FromArgb(45, 45, 48);
                form.Controls.Add(pnlButtons);

                void AddButton(string label, DialogResult result, bool isDefault = false)
                {
                    var btn = new Button();
                    btn.Text = label;
                    btn.DialogResult = result;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.ForeColor = Color.White;
                    btn.Size = new Size(85, 30);
                    btn.UseVisualStyleBackColor = false;
                    btn.Margin = new Padding(5, 0, 0, 0);
                    btn.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
                    
                    if (isDefault)
                    {
                        btn.BackColor = Color.FromArgb(0, 122, 204); // Blue
                        form.AcceptButton = btn;
                    }
                    else
                    {
                        btn.BackColor = Color.FromArgb(63, 63, 70); // Dark Gray
                    }
                    
                    if (result == DialogResult.Cancel || result == DialogResult.No)
                    {
                        form.CancelButton = btn;
                    }

                    pnlButtons.Controls.Add(btn);
                }

                bool IsDef(MessageBoxDefaultButton btn) => defaultButton == btn;

                // Add buttons based on type (RightToLeft flow means add rightmost button first)
                switch (buttons)
                {
                    case MessageBoxButtons.OK:
                        AddButton("OK", DialogResult.OK, true);
                        break;
                    case MessageBoxButtons.OKCancel:
                        AddButton("Cancel", DialogResult.Cancel, IsDef(MessageBoxDefaultButton.Button2));
                        AddButton("OK", DialogResult.OK, IsDef(MessageBoxDefaultButton.Button1));
                        break;
                    case MessageBoxButtons.YesNo:
                        AddButton("No", DialogResult.No, IsDef(MessageBoxDefaultButton.Button2));
                        AddButton("Yes", DialogResult.Yes, IsDef(MessageBoxDefaultButton.Button1));
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        AddButton("Cancel", DialogResult.Cancel, IsDef(MessageBoxDefaultButton.Button3));
                        AddButton("No", DialogResult.No, IsDef(MessageBoxDefaultButton.Button2));
                        AddButton("Yes", DialogResult.Yes, IsDef(MessageBoxDefaultButton.Button1));
                        break;
                    case MessageBoxButtons.RetryCancel:
                        AddButton("Cancel", DialogResult.Cancel, IsDef(MessageBoxDefaultButton.Button2));
                        AddButton("Retry", DialogResult.Retry, IsDef(MessageBoxDefaultButton.Button1));
                        break;
                     case MessageBoxButtons.AbortRetryIgnore:
                        AddButton("Ignore", DialogResult.Ignore, IsDef(MessageBoxDefaultButton.Button3));
                        AddButton("Retry", DialogResult.Retry, IsDef(MessageBoxDefaultButton.Button2));
                        AddButton("Abort", DialogResult.Abort, IsDef(MessageBoxDefaultButton.Button1));
                        break;
                }

                ThemeManager.ApplyTheme(form);

                if (owner != null)
                    return form.ShowDialog(owner);
                else
                    return form.ShowDialog();
            }
        }

        private static Bitmap CreateIcon(MessageBoxIcon icon)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                var fontName = SystemFonts.MessageBoxFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily;

                switch (icon)
                {
                    case MessageBoxIcon.Error: // Red X
                        g.FillEllipse(Brushes.IndianRed, 1, 1, 30, 30);
                        using (var pen = new Pen(Color.White, 3))
                        {
                            g.DrawLine(pen, 10, 10, 22, 22);
                            g.DrawLine(pen, 22, 10, 10, 22);
                        }
                        break;

                    case MessageBoxIcon.Question: // Blue ?
                        g.FillEllipse(Brushes.DodgerBlue, 1, 1, 30, 30);
                        using (var font = new Font(fontName, 20, FontStyle.Bold, GraphicsUnit.Pixel))
                        {
                            var size = g.MeasureString("?", font);
                            g.DrawString("?", font, Brushes.White, (32 - size.Width) / 2, (32 - size.Height) / 2 + 1);
                        }
                        break;

                    case MessageBoxIcon.Warning: // Orange !
                        g.FillEllipse(Brushes.DarkOrange, 1, 1, 30, 30);
                        using (var font = new Font(fontName, 20, FontStyle.Bold, GraphicsUnit.Pixel))
                        {
                            var size = g.MeasureString("!", font);
                            g.DrawString("!", font, Brushes.White, (32 - size.Width) / 2, (32 - size.Height) / 2 + 1);
                        }
                        break;

                    case MessageBoxIcon.Information: // Blue i
                        g.FillEllipse(Brushes.DodgerBlue, 1, 1, 30, 30);
                        using (var font = new Font(fontName, 20, FontStyle.Bold, GraphicsUnit.Pixel))
                        {
                            var size = g.MeasureString("i", font);
                            g.DrawString("i", font, Brushes.White, (32 - size.Width) / 2, (32 - size.Height) / 2 + 1);
                        }
                        break;
                }
            }
            return bmp;
        }
    }
#pragma warning restore CA1416
}
