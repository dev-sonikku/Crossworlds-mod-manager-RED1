using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public class DynamicThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        public DynamicThemeMenuRenderer(ProfessionalColorTable colorTable) : base(colorTable)
        {
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Owner is MenuStrip && e.Item.Selected)
            {
                e.Graphics.FillRectangle(new SolidBrush(ThemeManager.CurrentTheme.ButtonBackColor), new Rectangle(Point.Empty, e.Item.Size));
                // Text color is handled by OnRenderItemText, but we rely on the theme there.
                TextRenderer.DrawText(e.Graphics, e.Item.Text, e.Item.Font, e.Item.ContentRectangle, ThemeManager.CurrentTheme.MenuForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is MenuStrip)
            {
                using (var brush = new SolidBrush(ThemeManager.CurrentTheme.MenuBackColor))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }
            else
            {
                base.OnRenderToolStripBackground(e);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Owner is ToolStripDropDownMenu)
            {
                e.TextColor = e.Item.Enabled ? ThemeManager.CurrentTheme.MenuForeColor : Color.Gray;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(ThemeManager.CurrentTheme.ControlBackColor))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = e.ImageRectangle;
            var center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

            using (var pen = new Pen(ThemeManager.CurrentTheme.MenuForeColor, 2))
            {
                g.DrawLines(pen, new PointF[] {
                    new PointF(center.X - 4.5f, center.Y - 1.5f),
                    new PointF(center.X - 1.5f, center.Y + 3.5f),
                    new PointF(center.X + 4.5f, center.Y - 4.5f)
                });
            }
        }
    }
#pragma warning restore CA1416
}