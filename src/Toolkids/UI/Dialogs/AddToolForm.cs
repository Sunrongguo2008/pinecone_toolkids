using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Toolkids.UI.Theming;

namespace Toolkids.UI.Dialogs
{
    /// <summary>“添加软件”对话框：从 Data 已有文件夹添加，或新建一个空白软件。自适应 DPI。</summary>
    public sealed class AddToolForm : ThemedForm
    {
        private readonly RadioButton _rbExisting;
        private readonly RadioButton _rbNew;
        private readonly ComboBox _cboFolders;
        private readonly TextBox _txtNewName;

        public bool IsNew => _rbNew.Checked;

        public string FolderOrName =>
            IsNew ? _txtNewName.Text.Trim() : (_cboFolders.SelectedItem as string ?? "");

        public AddToolForm(IReadOnlyList<string> availableFolders)
        {
            Text = "添加软件";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(440, 0);
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

            _rbExisting = new RadioButton { Text = "从 Data 已有文件夹添加", AutoSize = true, Checked = availableFolders.Count > 0, Margin = new Padding(0, 8, 0, 2) };
            _cboFolders = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(22, 0, 0, 8) };
            _cboFolders.Items.AddRange(availableFolders.Cast<object>().ToArray());
            if (_cboFolders.Items.Count > 0) _cboFolders.SelectedIndex = 0;

            _rbNew = new RadioButton { Text = "新建空白软件（之后把程序文件放进它的 Apps 文件夹）", AutoSize = true, Checked = availableFolders.Count == 0, Margin = new Padding(0, 4, 0, 2) };

            var namePanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(22, 0, 0, 8) };
            namePanel.Controls.Add(new Label { Text = "名称：", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            _txtNewName = new TextBox { Width = 300 };
            namePanel.Controls.Add(_txtNewName);

            var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) };
            var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, AutoSize = true, Padding = new Padding(12, 4, 12, 4), Margin = new Padding(6, 3, 0, 3) };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(12, 4, 12, 4) };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Padding = new Padding(14) };
            layout.Controls.Add(_rbExisting, 0, 0);
            layout.Controls.Add(_cboFolders, 0, 1);
            layout.Controls.Add(_rbNew, 0, 2);
            layout.Controls.Add(namePanel, 0, 3);
            layout.Controls.Add(buttons, 0, 4);
            Controls.Add(layout);

            AcceptButton = ok;
            CancelButton = cancel;

            _rbExisting.CheckedChanged += (s, e) => UpdateEnabled();
            _rbNew.CheckedChanged += (s, e) => UpdateEnabled();
            UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            _cboFolders.Enabled = _rbExisting.Checked;
            _txtNewName.Enabled = _rbNew.Checked;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (IsNew && string.IsNullOrWhiteSpace(_txtNewName.Text))
                {
                    MessageBox.Show(this, "请输入软件名称。", "提示");
                    e.Cancel = true;
                }
                else if (!IsNew && _cboFolders.SelectedItem == null)
                {
                    MessageBox.Show(this, "请选择一个文件夹，或改用“新建空白软件”。", "提示");
                    e.Cancel = true;
                }
            }
            base.OnFormClosing(e);
        }
    }
}
