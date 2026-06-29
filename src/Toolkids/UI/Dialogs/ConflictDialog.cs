using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Toolkids.Services.Sandbox;
using Toolkids.UI.Theming;

namespace Toolkids.UI.Dialogs
{
    /// <summary>还原前的“覆盖确认”：列出系统中已存在、将被覆盖的项，让用户勾选哪些覆盖。</summary>
    public sealed class ConflictDialog : ThemedForm
    {
        private readonly List<ConflictItem> _items;
        private readonly CheckedListBox _list = new()
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false,
            BorderStyle = BorderStyle.None
        };

        public IReadOnlyList<ConflictItem> Items => _items;

        public ConflictDialog(string toolName, IReadOnlyList<ConflictItem> conflicts)
        {
            _items = conflicts.ToList();

            Text = "覆盖确认 - " + toolName;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 420);
            MinimumSize = new Size(480, 320);
            ShowInTaskbar = false;

            var info = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12, 10, 12, 4),
                Text = "以下项目在系统中已存在，还原会覆盖它们。\r\n勾选 = 用工具箱的备份覆盖；取消勾选 = 保留系统现有内容。"
            };

            foreach (ConflictItem c in _items)
                _list.Items.Add($"[{(c.Kind == ConflictKind.Registry ? "注册表" : "文件")}] {c.Target}", c.Overwrite);

            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(8)
            };
            var ok = new Button { Text = "确定", AutoSize = true, Padding = new Padding(14, 4, 14, 4), Margin = new Padding(6, 3, 0, 3) };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(14, 4, 14, 4) };
            var none = new Button { Text = "全不选", AutoSize = true, Padding = new Padding(10, 4, 10, 4), Margin = new Padding(6, 3, 0, 3) };
            var all = new Button { Text = "全选", AutoSize = true, Padding = new Padding(10, 4, 10, 4) };
            ok.Click += OnOk;
            all.Click += (s, e) => SetAll(true);
            none.Click += (s, e) => SetAll(false);
            bottom.Controls.Add(ok);
            bottom.Controls.Add(cancel);
            bottom.Controls.Add(none);
            bottom.Controls.Add(all);

            var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 8) };
            host.Controls.Add(_list);

            Controls.Add(host);
            Controls.Add(bottom);
            Controls.Add(info);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void SetAll(bool check)
        {
            for (int i = 0; i < _list.Items.Count; i++)
                _list.SetItemChecked(i, check);
        }

        private void OnOk(object? sender, EventArgs e)
        {
            for (int i = 0; i < _items.Count; i++)
                _items[i].Overwrite = _list.GetItemChecked(i);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
