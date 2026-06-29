using System;
using System.Drawing;
using System.Windows.Forms;
using Toolkids.Models;
using Toolkids.Services;
using Toolkids.UI.Theming;

namespace Toolkids.UI.Dialogs
{
    /// <summary>全局设置。保存时把界面值写回传入的 <see cref="GlobalConfig"/>（不落盘，由调用方保存）。自适应 DPI。</summary>
    public sealed class SettingsForm : ThemedForm
    {
        private readonly GlobalConfig _cfg;

        private readonly ComboBox _theme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Anchor = AnchorStyles.Left };
        private readonly ComboBox _layout = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Anchor = AnchorStyles.Left };
        private readonly TextBox _dataDir = new() { Width = 200, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        private readonly CheckBox _confirmConflict = new() { Text = "还原前若系统已存在同名项，弹出确认", AutoSize = true };
        private readonly CheckBox _askBackup = new() { Text = "软件退出后询问是否备份并清理", AutoSize = true };
        private readonly CheckBox _skipPE = new() { Text = "在 WinPE 中跳过沙盒备份/清理", AutoSize = true };

        public SettingsForm(GlobalConfig cfg, AppPaths paths)
        {
            _cfg = cfg;
            Text = "设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(460, 0);
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

            BuildUi(paths);
            LoadValues();
        }

        private void BuildUi(AppPaths paths)
        {
            _theme.Items.AddRange(new object[] { "深色", "浅色" });
            _layout.Items.AddRange(new object[] { "网格", "列表" });

            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, Padding = new Padding(16) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int r = 0;
            AddField(grid, "主题：", _theme, ref r);
            AddField(grid, "软件区布局：", _layout, ref r);
            AddField(grid, "软件目录（Data）：", _dataDir, ref r);

            AddSpan(grid, _confirmConflict, ref r);
            AddSpan(grid, _askBackup, ref r);
            AddSpan(grid, _skipPE, ref r);

            var info = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 6),
                ForeColor = AppTheme.Current.SubtleForeground,
                Text =
                    "配置/日志位置：" + paths.WritableRoot + "\r\n" +
                    "程序目录可写：" + (paths.IsRootWritable ? "是" : "否（已回退到临时目录）") + "\r\n" +
                    "当前环境：" + (PeEnvironment.IsWinPE ? "WinPE" : "普通 Windows")
            };
            AddSpan(grid, info, ref r);

            var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) };
            var ok = new Button { Text = "保存", AutoSize = true, Padding = new Padding(14, 4, 14, 4), Margin = new Padding(6, 3, 0, 3) };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(14, 4, 14, 4) };
            ok.Click += OnSave;
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            grid.Controls.Add(buttons, 1, r);

            Controls.Add(grid);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private static void AddField(TableLayoutPanel grid, string label, Control field, ref int r)
        {
            grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 6, 6) }, 0, r);
            field.Margin = new Padding(0, 4, 0, 4);
            grid.Controls.Add(field, 1, r);
            r++;
        }

        private static void AddSpan(TableLayoutPanel grid, Control field, ref int r)
        {
            field.Margin = new Padding(0, 4, 0, 4);
            grid.Controls.Add(field, 0, r);
            grid.SetColumnSpan(field, 2);
            r++;
        }

        private void LoadValues()
        {
            _theme.SelectedIndex = string.Equals(_cfg.Theme, "light", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            _layout.SelectedIndex = string.Equals(_cfg.Layout, "list", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            _dataDir.Text = _cfg.DataDir;
            _confirmConflict.Checked = _cfg.Settings.ConfirmConflictBeforeRestore;
            _askBackup.Checked = _cfg.Settings.AskBackupOnExit;
            _skipPE.Checked = _cfg.Settings.SkipSandboxInPE;
        }

        private void OnSave(object? sender, EventArgs e)
        {
            _cfg.Theme = _theme.SelectedIndex == 1 ? "light" : "dark";
            _cfg.Layout = _layout.SelectedIndex == 1 ? "list" : "grid";
            string dir = _dataDir.Text.Trim();
            _cfg.DataDir = string.IsNullOrWhiteSpace(dir) ? "Data" : dir;
            _cfg.Settings.ConfirmConflictBeforeRestore = _confirmConflict.Checked;
            _cfg.Settings.AskBackupOnExit = _askBackup.Checked;
            _cfg.Settings.SkipSandboxInPE = _skipPE.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
