using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public class DarkThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color _menuItemSelectedColor = Color.FromArgb(255, 255, 255); // White background for selected item
        private readonly Color _menuItemTextColor = Color.Black; // Black text for selected item

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
    }
}