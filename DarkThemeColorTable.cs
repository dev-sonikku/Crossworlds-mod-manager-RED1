using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public class DynamicThemeColorTable : ProfessionalColorTable
    {
        // Menu styling
        public override Color MenuBorder => ThemeManager.CurrentTheme.BorderColor;
        public override Color MenuItemBorder => ThemeManager.CurrentTheme.AccentColor;
        public override Color MenuItemSelected => ThemeManager.CurrentTheme.ButtonBackColor;
        public override Color MenuItemPressedGradientBegin => ThemeManager.CurrentTheme.ButtonBackColor;
        public override Color MenuItemPressedGradientEnd => ThemeManager.CurrentTheme.ButtonBackColor;
        
        // Dropdown and strip styling
        public override Color ToolStripDropDownBackground => ThemeManager.CurrentTheme.ControlBackColor;
        public override Color ToolStripBorder => ThemeManager.CurrentTheme.BorderColor;
        
        // Image margins
        public override Color ImageMarginGradientBegin => ThemeManager.CurrentTheme.ControlBackColor;
        public override Color ImageMarginGradientMiddle => ThemeManager.CurrentTheme.ControlBackColor;
        public override Color ImageMarginGradientEnd => ThemeManager.CurrentTheme.ControlBackColor;
        
        // Separators
        public override Color SeparatorDark => ThemeManager.CurrentTheme.BorderColor;
        public override Color SeparatorLight => ThemeManager.CurrentTheme.BorderColor;
        
        // Button and text styling
        public override Color ButtonSelectedBorder => ThemeManager.CurrentTheme.AccentColor;
        public override Color ButtonSelectedGradientBegin => ThemeManager.CurrentTheme.ButtonBackColor;
        public override Color ButtonSelectedGradientEnd => ThemeManager.CurrentTheme.ButtonBackColor;
        public override Color ButtonSelectedHighlight => ThemeManager.CurrentTheme.ButtonBackColor;
        
        // Checked styling
        public override Color CheckBackground => ThemeManager.CurrentTheme.AccentColor;
        public override Color CheckSelectedBackground => ThemeManager.CurrentTheme.AccentColor;
        
        // Grip styling
        public override Color GripDark => ThemeManager.CurrentTheme.BorderColor;
        public override Color GripLight => ThemeManager.CurrentTheme.BackColor;
    }
}