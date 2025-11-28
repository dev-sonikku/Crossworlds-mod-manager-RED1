using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public class DarkThemeColorTable : ProfessionalColorTable
    {
        // Color palette matching VS Code dark theme
        private readonly Color _backColor = Color.FromArgb(45, 45, 48);           // Dark background
        private readonly Color _selectionColor = Color.FromArgb(63, 63, 70);      // Selection/hover color
        private readonly Color _borderColor = Color.FromArgb(80, 80, 80);         // Border color
        private readonly Color _darkGray = Color.FromArgb(60, 60, 60);            // Slightly darker

        // Menu styling
        public override Color MenuBorder => _borderColor;
        public override Color MenuItemBorder => _selectionColor;
        public override Color MenuItemSelected => _selectionColor;
        public override Color MenuItemPressedGradientBegin => _selectionColor;
        public override Color MenuItemPressedGradientEnd => _selectionColor;
        
        // Dropdown and strip styling
        public override Color ToolStripDropDownBackground => _backColor;
        public override Color ToolStripBorder => _borderColor;
        
        // Image margins
        public override Color ImageMarginGradientBegin => _backColor;
        public override Color ImageMarginGradientMiddle => _backColor;
        public override Color ImageMarginGradientEnd => _backColor;
        
        // Separators
        public override Color SeparatorDark => _borderColor;
        public override Color SeparatorLight => _darkGray;
        
        // Button and text styling
        public override Color ButtonSelectedBorder => _selectionColor;
        public override Color ButtonSelectedGradientBegin => _selectionColor;
        public override Color ButtonSelectedGradientEnd => _selectionColor;
        public override Color ButtonSelectedHighlight => _selectionColor;
        
        // Checked styling
        public override Color CheckBackground => _selectionColor;
        public override Color CheckSelectedBackground => _selectionColor;
        
        // Grip styling
        public override Color GripDark => _darkGray;
        public override Color GripLight => _backColor;
    }
}