using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Toolkids.Services.Sandbox;
using Toolkids.UI.Theming;

namespace Toolkids.UI.Dialogs
{
    /// <summary>展示扫描到的新增项，勾选要写入沙盒规则的项。</summary>
    public sealed class ScanResultDialog : ThemedForm
    {
        private readonly List<ScanItem> _items;
        private readonly CheckedListBox _list = new()
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false,
            BorderStyle = BorderStyle.None
        };

        public IReadOnlyList<ScanItem> Selected => _items.Where(i => i.Selected).ToList();

        public ScanResultDialog(string toolName, IReadOnlyList<ScanItem> found)
        {
            _items = found.ToList();

            Text = "扫描结果 - " + toolName;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(660, 460);
            MinimumSize = new Size(500, 340);
            ShowInTaskbar = false;

            var info = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12, 10, 12, 4),
                Text = "扫描到以下“新增”项目。勾选要写入该软件沙盒规则的项，点“写入配置”。\r\n（一般勾选属于这个软件自己的注册表项/目录即可。）"
            };

            foreach (ScanItem it in _items)
                _list.Items.Add(it.Display, it.Selected);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(8) };
            var ok = new Button { Text = "写入配置", AutoSize = true, Padding = new Padding(14, 4, 14, 4), Margin = new Padding(6, 3, 0, 3) };
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
                _items[i].Selected = _list.GetItemChecked(i);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
