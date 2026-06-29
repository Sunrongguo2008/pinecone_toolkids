using System;
using System.Drawing;
using System.Windows.Forms;
using Toolkids.UI.Theming;

namespace Toolkids.UI.Dialogs
{
    /// <summary>通用的单行文本输入对话框（用于新建/重命名分类等）。自适应 DPI。</summary>
    public static class InputDialog
    {
        /// <summary>返回用户输入；点取消返回 <c>null</c>。</summary>
        public static string? Show(IWin32Window owner, string title, string prompt, string initial)
        {
            using var form = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                AutoScaleMode = AutoScaleMode.Font,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(380, 0),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(14)
            };

            var lbl = new Label { Text = prompt, AutoSize = true, Margin = new Padding(0, 0, 0, 6) };
            var txt = new TextBox { Text = initial, Width = 340, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 0, 10) };

            var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0) };
            var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, AutoSize = true, Padding = new Padding(12, 4, 12, 4), Margin = new Padding(6, 3, 0, 3) };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(12, 4, 12, 4) };
            buttons.Controls.Add(ok);     // RightToLeft：先加的在最右
            buttons.Controls.Add(cancel);

            layout.Controls.Add(lbl, 0, 0);
            layout.Controls.Add(txt, 0, 1);
            layout.Controls.Add(buttons, 0, 2);
            form.Controls.Add(layout);

            form.AcceptButton = ok;
            form.CancelButton = cancel;

            // 在 Load（DPI 缩放完成后）套主题，避免颜色被重置
            form.Load += (s, e) => ThemeManager.Apply(form, AppTheme.Current);
            form.Shown += (s, e) => txt.SelectAll();

            return form.ShowDialog(owner) == DialogResult.OK ? txt.Text.Trim() : null;
        }
    }
}
