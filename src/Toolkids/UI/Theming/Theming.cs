using System;
using System.Drawing;
using System.Windows.Forms;

namespace Toolkids.UI.Theming
{
    /// <summary>一套配色（扁平风格，深色为主，兼容 Win7/PE，不依赖 DWM 特效）。</summary>
    public sealed class Theme
    {
        public string Key { get; init; } = "dark";
        public Color Background { get; init; }
        public Color Surface { get; init; }
        public Color Foreground { get; init; }
        public Color SubtleForeground { get; init; }
        public Color Accent { get; init; }
        public Color Border { get; init; }
        public Color SelectionBack { get; init; }

        public static readonly Theme Dark = new()
        {
            Key = "dark",
            Background = Color.FromArgb(32, 32, 32),
            Surface = Color.FromArgb(43, 43, 43),
            Foreground = Color.FromArgb(232, 232, 232),
            SubtleForeground = Color.FromArgb(160, 160, 160),
            Accent = Color.FromArgb(55, 148, 255),
            Border = Color.FromArgb(62, 62, 62),
            SelectionBack = Color.FromArgb(0, 90, 158),
        };

        public static readonly Theme Light = new()
        {
            Key = "light",
            Background = Color.FromArgb(245, 245, 245),
            Surface = Color.White,
            Foreground = Color.FromArgb(30, 30, 30),
            SubtleForeground = Color.FromArgb(110, 110, 110),
            Accent = Color.FromArgb(0, 120, 215),
            Border = Color.FromArgb(210, 210, 210),
            SelectionBack = Color.FromArgb(204, 232, 255),
        };

        public static Theme FromKey(string? key) =>
            string.Equals(key, "light", StringComparison.OrdinalIgnoreCase) ? Light : Dark;
    }

    /// <summary>当前生效的主题（供各对话框统一取用）。</summary>
    public static class AppTheme
    {
        public static Theme Current { get; set; } = Theme.Dark;
    }

    /// <summary>把主题应用到窗体及其所有子控件（递归）。</summary>
    public static class ThemeManager
    {
        public static void Apply(Control root, Theme theme)
        {
            root.BackColor = theme.Background;
            root.ForeColor = theme.Foreground;
            ApplyRecursive(root, theme);
        }

        private static void ApplyRecursive(Control parent, Theme theme)
        {
            foreach (Control c in parent.Controls)
            {
                StyleControl(c, theme);
                ApplyRecursive(c, theme);
            }
        }

        private static void StyleControl(Control c, Theme theme)
        {
            switch (c)
            {
                case Button b:
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = theme.Border;
                    b.BackColor = theme.Surface;
                    b.ForeColor = theme.Foreground;
                    break;
                case TextBox tb:
                    tb.BackColor = theme.Surface;
                    tb.ForeColor = theme.Foreground;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ComboBox cb:
                    cb.BackColor = theme.Surface;
                    cb.ForeColor = theme.Foreground;
                    cb.FlatStyle = FlatStyle.Flat;
                    break;
                case ListBox lb:
                    lb.BackColor = theme.Surface;
                    lb.ForeColor = theme.Foreground;
                    lb.BorderStyle = BorderStyle.None;
                    break;
                case ListView lv:
                    lv.BackColor = theme.Surface;
                    lv.ForeColor = theme.Foreground;
                    lv.BorderStyle = BorderStyle.None;
                    break;
                case CheckBox chk:
                    chk.ForeColor = theme.Foreground;
                    chk.BackColor = Color.Transparent;
                    break;
                case RadioButton rb:
                    rb.ForeColor = theme.Foreground;
                    rb.BackColor = Color.Transparent;
                    break;
                case Label lbl:
                    lbl.ForeColor = theme.Foreground;
                    lbl.BackColor = Color.Transparent;
                    break;
                case Panel: // 含 FlowLayoutPanel / TableLayoutPanel（都派生自 Panel）
                    c.BackColor = theme.Background;
                    break;
                default:
                    c.BackColor = theme.Background;
                    c.ForeColor = theme.Foreground;
                    break;
            }
        }
    }
}
