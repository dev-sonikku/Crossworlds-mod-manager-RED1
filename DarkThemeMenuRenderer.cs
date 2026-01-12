using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public class DarkThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color _menuItemSelectedColor = Color.FromArgb(255, 255, 255); // White background for selected item
        private readonly Color _menuItemTextColor = Color.Black; // Black text for selected item
        private readonly Color _menuStripBackgroundColor = Color.FromArgb(60, 60, 60); // Dark background for the strip
        private readonly Color _dropDownBackgroundColor = Color.FromArgb(45, 45, 48); // Dark background for dropdowns

        public DarkThemeMenuRenderer(ProfessionalColorTable colorTable) : base(colorTable)
        {
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Owner is MenuStrip && e.Item.Selected)
            {
                e.Graphics.FillRectangle(new SolidBrush(_menuItemSelectedColor), new Rectangle(Point.Empty, e.Item.Size));
                TextRenderer.DrawText(e.Graphics, e.Item.Text, e.Item.Font, e.Item.ContentRectangle, _menuItemTextColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
                using (var brush = new SolidBrush(_menuStripBackgroundColor))
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
                e.TextColor = e.Item.Enabled ? Color.White : Color.Gray;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_dropDownBackgroundColor))
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

            using (var pen = new Pen(Color.White, 2))
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