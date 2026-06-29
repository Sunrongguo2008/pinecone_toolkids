using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Toolkids.Models;
using Toolkids.UI.Theming;

namespace Toolkids.UI.Dialogs
{
    /// <summary>编辑某软件的 toolconfig.json。保存时把界面值写回传入的 <see cref="ToolItem.Config"/>（不落盘，由调用方保存）。</summary>
    public sealed class ToolEditForm : ThemedForm
    {
        private readonly ToolItem _tool;
        private int _row;

        private readonly TextBox _name = NewText();
        private readonly TextBox _desc = NewText();
        private readonly TextBox _icon = NewText();
        private readonly TextBox _exe = NewText();
        private readonly TextBox _args = NewText();
        private readonly TextBox _workdir = NewText();
        private readonly CheckBox _runAsAdmin = new() { Text = "以管理员权限运行", AutoSize = true };
        private readonly ComboBox _waitMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 168 };
        private readonly TextBox _reg = NewMultiline();
        private readonly TextBox _files = NewMultiline();

        public ToolEditForm(ToolItem tool)
        {
            _tool = tool;
            Text = "编辑配置 - " + tool.FolderName;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(660, 720);
            MinimumSize = new Size(560, 560);
            ShowInTaskbar = false;

            BuildUi();
            LoadValues();
        }

        private static TextBox NewText() => new() { Width = 380 };
        private static TextBox NewMultiline() =>
            new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5f) };

        // 整个窗体用一张表格：单行字段用 AutoSize 行，两个多行框各占剩余高度的 50%，
        // 这样上下比例稳定、不会把多行框压没（之前“上半固定+下半填充”在高 DPI 下会塌）。
        private void BuildUi()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                Padding = new Padding(12)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // 标签列：随字体/DPI 自适应，避免中文被压窄换行
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // 输入列
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // 浏览按钮列：自适应，避免“浏览…”被裁

            AddField(grid, "名称", _name);
            AddField(grid, "简介", _desc);
            AddField(grid, "图标", _icon, BrowseIcon);
            AddField(grid, "启动程序", _exe, BrowseExe);
            AddField(grid, "参数", _args);
            AddField(grid, "工作目录", _workdir);

            _waitMode.Items.AddRange(new object[]
            {
                "不等待 (none)", "等待进程 (process)", "等待进程树 (tree)", "等待进程名 (named)"
            });
            var opts = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 2, 0, 2) };
            opts.Controls.Add(_runAsAdmin);
            opts.Controls.Add(new Label { Text = "等待方式：", AutoSize = true, Margin = new Padding(16, 6, 0, 0) });
            opts.Controls.Add(_waitMode);
            AddSpan(grid, opts, autoSize: true);

            AddSpan(grid, new Label { Text = @"注册表项（每行一个，如 HKCU\Software\厂商）— 沙盒规则", AutoSize = true, Margin = new Padding(0, 8, 0, 2) }, autoSize: true);
            AddSpan(grid, _reg, autoSize: false);
            AddSpan(grid, new Label { Text = @"文件 / 目录（每行一个，支持 %AppData% 等变量）", AutoSize = true, Margin = new Padding(0, 8, 0, 2) }, autoSize: true);
            AddSpan(grid, _files, autoSize: false);

            var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(14, 4, 14, 4) };
            var ok = new Button { Text = "保存", AutoSize = true, Padding = new Padding(14, 4, 14, 4), Margin = new Padding(6, 3, 0, 3) };
            ok.Click += OnSave;
            buttons.Controls.Add(ok);     // RightToLeft：先加的在最右
            buttons.Controls.Add(cancel);
            AddSpan(grid, buttons, autoSize: true);

            Controls.Add(grid);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void AddField(TableLayoutPanel grid, string label, Control field, EventHandler? browse = null)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 6, 4) }, 0, _row);

            field.Margin = new Padding(0, 4, 0, 4);
            if (field is TextBox) field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            grid.Controls.Add(field, 1, _row);

            if (browse != null)
            {
                var btn = new Button { Text = "浏览…", AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(8, 4, 0, 4), FlatStyle = FlatStyle.Flat };
                btn.Click += browse;
                grid.Controls.Add(btn, 2, _row);
            }
            else
            {
                grid.SetColumnSpan(field, 2);
            }
            _row++;
        }

        // 跨三列的一行。autoSize=false 表示占剩余高度的等分（用于多行框）。
        private void AddSpan(TableLayoutPanel grid, Control c, bool autoSize)
        {
            grid.RowStyles.Add(autoSize ? new RowStyle(SizeType.AutoSize) : new RowStyle(SizeType.Percent, 50f));
            if (c is TextBox) c.Dock = DockStyle.Fill;
            grid.Controls.Add(c, 0, _row);
            grid.SetColumnSpan(c, 3);
            _row++;
        }

        // ---------- 浏览 ----------

        private string InitialDir()
        {
            string apps = Path.Combine(_tool.FolderPath, "Apps");
            return Directory.Exists(apps) ? apps : _tool.FolderPath;
        }

        private void BrowseExe(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "选择启动程序",
                Filter = "程序 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                InitialDirectory = InitialDir()
            };
            if (dlg.ShowDialog(this) == DialogResult.OK) _exe.Text = ToRelative(dlg.FileName);
        }

        private void BrowseIcon(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "选择图标来源",
                Filter = "图标/图片/程序 (*.ico;*.png;*.exe)|*.ico;*.png;*.exe|所有文件 (*.*)|*.*",
                InitialDirectory = InitialDir()
            };
            if (dlg.ShowDialog(this) == DialogResult.OK) _icon.Text = ToRelative(dlg.FileName);
        }

        private string ToRelative(string fullPath)
        {
            try
            {
                string rel = Path.GetRelativePath(_tool.FolderPath, fullPath);
                if (rel.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(rel))
                {
                    MessageBox.Show(this, "所选文件不在该软件文件夹内，将以绝对路径保存（不利于便携，建议把文件放进 Apps 文件夹）。", "提示");
                    return fullPath;
                }
                return rel;
            }
            catch
            {
                return fullPath;
            }
        }

        // ---------- 读写 ----------

        private void LoadValues()
        {
            ToolConfig c = _tool.Config;
            _name.Text = c.Name;
            _desc.Text = c.Description;
            _icon.Text = c.Icon ?? "";
            _exe.Text = c.Launch.Exe;
            _args.Text = c.Launch.Args;
            _workdir.Text = c.Launch.WorkingDir;
            _runAsAdmin.Checked = c.Launch.RunAsAdmin;
            _waitMode.SelectedIndex = WaitModeToIndex(c.Launch.WaitMode);
            _reg.Lines = c.SandboxRules.Registry.ToArray();
            _files.Lines = c.SandboxRules.Files.ToArray();
        }

        private void OnSave(object? sender, EventArgs e)
        {
            ToolConfig c = _tool.Config;
            c.Name = _name.Text.Trim();
            c.Description = _desc.Text.Trim();
            c.Icon = string.IsNullOrWhiteSpace(_icon.Text) ? null : _icon.Text.Trim();
            c.Launch.Exe = _exe.Text.Trim();
            c.Launch.Args = _args.Text.Trim();
            c.Launch.WorkingDir = _workdir.Text.Trim();
            c.Launch.RunAsAdmin = _runAsAdmin.Checked;
            c.Launch.WaitMode = IndexToWaitMode(_waitMode.SelectedIndex);
            c.SandboxRules.Registry = CleanLines(_reg.Lines);
            c.SandboxRules.Files = CleanLines(_files.Lines);

            DialogResult = DialogResult.OK;
            Close();
        }

        private static List<string> CleanLines(string[] lines) =>
            lines.Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

        private static int WaitModeToIndex(WaitMode m) => m switch
        {
            WaitMode.None => 0,
            WaitMode.Process => 1,
            WaitMode.Named => 3,
            _ => 2
        };

        private static WaitMode IndexToWaitMode(int i) => i switch
        {
            0 => WaitMode.None,
            1 => WaitMode.Process,
            3 => WaitMode.Named,
            _ => WaitMode.Tree
        };
    }
}
