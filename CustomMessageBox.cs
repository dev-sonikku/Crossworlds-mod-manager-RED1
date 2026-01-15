// c:\games\Projects\Crossworlds mod manager RED1\CustomMessageBox.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
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
            return Show(null, text, caption, buttons, icon);
        }

        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (var form = new Form())
            {
                form.Text = caption;
                form.StartPosition = owner != null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowIcon = false;
                form.BackColor = Color.FromArgb(45, 45, 48);
                form.ForeColor = Color.White;
                
                // Create label for text
                var lbl = new Label();
                lbl.Text = text;
                lbl.AutoSize = true;
                lbl.MaximumSize = new Size(460, 0);
                lbl.Location = new Point(20, 20);
                lbl.Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 10F);
                form.Controls.Add(lbl);

                // Calculate size
                int contentHeight = lbl.PreferredHeight + 40;
                int buttonPanelHeight = 50;
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

                // Add buttons based on type (RightToLeft flow means add rightmost button first)
                switch (buttons)
                {
                    case MessageBoxButtons.OK:
                        AddButton("OK", DialogResult.OK, true);
                        break;
                    case MessageBoxButtons.OKCancel:
                        AddButton("Cancel", DialogResult.Cancel);
                        AddButton("OK", DialogResult.OK, true);
                        break;
                    case MessageBoxButtons.YesNo:
                        AddButton("No", DialogResult.No);
                        AddButton("Yes", DialogResult.Yes, true);
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        AddButton("Cancel", DialogResult.Cancel);
                        AddButton("No", DialogResult.No);
                        AddButton("Yes", DialogResult.Yes, true);
                        break;
                    case MessageBoxButtons.RetryCancel:
                        AddButton("Cancel", DialogResult.Cancel);
                        AddButton("Retry", DialogResult.Retry, true);
                        break;
                     case MessageBoxButtons.AbortRetryIgnore:
                        AddButton("Ignore", DialogResult.Ignore);
                        AddButton("Retry", DialogResult.Retry);
                        AddButton("Abort", DialogResult.Abort);
                        break;
                }

                if (owner != null)
                    return form.ShowDialog(owner);
                else
                    return form.ShowDialog();
            }
        }
    }
}
