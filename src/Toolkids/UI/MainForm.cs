using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Toolkids.Models;
using Toolkids.Services;
using Toolkids.Services.Sandbox;
using Toolkids.UI.Dialogs;
using Toolkids.UI.Theming;

namespace Toolkids.UI
{
    /// <summary>主窗口：左侧分类、右侧软件网格。只负责界面与交互，业务逻辑委托给 Services。</summary>
    public sealed class MainForm : ThemedForm
    {
        private readonly AppServices _svc;
        private GlobalConfig _config;
        private Theme _theme;

        // 正在运行的软件（文件夹名），避免重复启动同一沙盒软件
        private readonly HashSet<string> _running = new(StringComparer.OrdinalIgnoreCase);

        private ListBox _categoryList = null!;
        private ListView _toolList = null!;
        private ImageList _iconList = null!;
        private ImageList _iconListSmall = null!;
        private Label _emptyHint = null!;
        private Label _status = null!;

        public MainForm(AppServices svc)
        {
            _svc = svc;
            _config = svc.Config.LoadGlobal();
            _theme = Theme.FromKey(_config.Theme);
            AppTheme.Current = _theme;

            BuildUi();
            ApplyLayout();
            RefreshCategories();
        }

        // ============ UI 构建 ============

        private void BuildUi()
        {
            Text = "Toolkids 便携工具箱";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1120, 720);
            MinimumSize = new Size(900, 580);

            // 顶部工具条（按内容自适应高度，避免高 DPI 裁剪）
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Padding = new Padding(10, 8, 10, 8) };
            var btnAddTool = MakeButton("添加软件");
            btnAddTool.Click += (s, e) => AddTool();
            var btnSettings = MakeButton("设置");
            btnSettings.Click += (s, e) => OpenSettings();
            var btnRefresh = MakeButton("刷新");
            btnRefresh.Click += (s, e) => ReloadAll();
            var btnAbout = MakeButton("关于");
            btnAbout.Click += (s, e) => OpenAbout();
            top.Controls.Add(btnAddTool);
            top.Controls.Add(btnSettings);
            top.Controls.Add(btnRefresh);
            top.Controls.Add(btnAbout);

            // 左侧分类
            var left = new Panel { Dock = DockStyle.Left, Width = 240, Padding = new Padding(10) };
            var leftHeader = new Label { Text = "分类", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(2, 2, 0, 8), Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold) };
            _categoryList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            _categoryList.SelectedIndexChanged += (s, e) => RefreshTools();
            _categoryList.ContextMenuStrip = BuildCategoryMenu();

            var catButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
            var bAdd = MakeButton("新建"); bAdd.Click += (s, e) => AddCategory();
            var bRen = MakeButton("改名"); bRen.Click += (s, e) => RenameCategory();
            var bDel = MakeButton("删除"); bDel.Click += (s, e) => DeleteCategory();
            catButtons.Controls.Add(bAdd);
            catButtons.Controls.Add(bRen);
            catButtons.Controls.Add(bDel);

            left.Controls.Add(_categoryList);
            left.Controls.Add(catButtons);
            left.Controls.Add(leftHeader);

            var splitter = new Splitter { Dock = DockStyle.Left, Width = 3 };

            // 右侧软件区
            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            _iconList = new ImageList { ImageSize = new Size(64, 64), ColorDepth = ColorDepth.Depth32Bit };
            _iconListSmall = new ImageList { ImageSize = new Size(24, 24), ColorDepth = ColorDepth.Depth32Bit };
            _toolList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.LargeIcon,
                MultiSelect = false,
                HideSelection = false,
                ShowItemToolTips = true,
                LargeImageList = _iconList,
                SmallImageList = _iconListSmall
            };
            _toolList.ItemActivate += (s, e) => RunSelectedTool();
            _toolList.ContextMenuStrip = BuildToolMenu();
            // 自绘列头，修掉深色模式下白色列头；表项仍用默认绘制
            _toolList.OwnerDraw = true;
            _toolList.DrawColumnHeader += OnDrawColumnHeader;
            _toolList.DrawItem += (s, e) => { if (_toolList.View != View.Details) e.DrawDefault = true; };
            _toolList.DrawSubItem += (s, e) => e.DrawDefault = true;
            _toolList.Resize += (s, e) => FillLastColumn();

            _emptyHint = new Label
            {
                Text = "这个分类还没有软件。\r\n点上方“添加软件”把工具加进来。",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            right.Controls.Add(_emptyHint);
            right.Controls.Add(_toolList);

            _status = new Label { Dock = DockStyle.Bottom, AutoSize = false, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), Text = "就绪" };

            // 添加顺序：Fill 先加；需要占满整行宽度的 Top/Bottom 后加（后加 = 先布局 = 占满整条）
            Controls.Add(right);
            Controls.Add(splitter);
            Controls.Add(left);
            Controls.Add(top);
            Controls.Add(_status);
        }

        private static Button MakeButton(string text) => new()
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 8, 16, 8),
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat
        };

        private ContextMenuStrip BuildCategoryMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("新建分类", null, (s, e) => AddCategory());
            menu.Items.Add("重命名", null, (s, e) => RenameCategory());
            menu.Items.Add("删除", null, (s, e) => DeleteCategory());
            return menu;
        }

        private ContextMenuStrip BuildToolMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("运行", null, (s, e) => RunSelectedTool());
            menu.Items.Add("编辑配置", null, (s, e) => EditSelectedTool());
            menu.Items.Add("扫描沙盒", null, (s, e) => ScanSelectedTool());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("打开所在文件夹", null, (s, e) => OpenSelectedToolFolder());
            menu.Items.Add("从该分类移除", null, (s, e) => RemoveToolFromCategory());
            menu.Items.Add("删除（连同文件）", null, (s, e) => DeleteSelectedTool());
            return menu;
        }

        // ============ 布局 / 刷新 ============

        private void ApplyLayout()
        {
            bool list = string.Equals(_config.Layout, "list", StringComparison.OrdinalIgnoreCase);
            _toolList.View = list ? View.Details : View.LargeIcon;
            _toolList.Columns.Clear();
            if (list)
            {
                _toolList.Columns.Add("名称", 240);
                _toolList.Columns.Add("简介", 380);
                FillLastColumn();
            }
        }

        // 自绘 ListView 列头：深色填充 + 浅色文字，修掉深色模式下的白色列头
        private void OnDrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using var bg = new SolidBrush(_theme.Surface);
            e.Graphics.FillRectangle(bg, e.Bounds);
            using var pen = new Pen(_theme.Border);
            e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top + 4, e.Bounds.Right - 1, e.Bounds.Bottom - 4);
            var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", _toolList.Font, textRect, _theme.Foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // 让最后一列填满剩余宽度，消除列头右侧的空白块
        private void FillLastColumn()
        {
            if (_toolList.View != View.Details || _toolList.Columns.Count == 0) return;
            int used = 0;
            for (int i = 0; i < _toolList.Columns.Count - 1; i++) used += _toolList.Columns[i].Width;
            int last = _toolList.ClientSize.Width - used - 1;
            if (last > 80) _toolList.Columns[_toolList.Columns.Count - 1].Width = last;
        }

        private void ReloadAll()
        {
            _config = _svc.Config.LoadGlobal();
            _theme = Theme.FromKey(_config.Theme);
            AppTheme.Current = _theme;
            ThemeManager.Apply(this, _theme);
            ApplyLayout();
            RefreshCategories();
        }

        private void RefreshCategories()
        {
            string? keep = CurrentCategory?.Id;

            _categoryList.BeginUpdate();
            _categoryList.Items.Clear();
            foreach (Category cat in _config.Categories)
                _categoryList.Items.Add(cat.Name);
            _categoryList.EndUpdate();

            if (_config.Categories.Count == 0)
            {
                RefreshTools();
                return;
            }

            int idx = 0;
            if (keep != null)
            {
                int found = _config.Categories.FindIndex(c => c.Id == keep);
                if (found >= 0) idx = found;
            }
            _categoryList.SelectedIndex = idx; // 触发 RefreshTools
        }

        private void RefreshTools()
        {
            _toolList.BeginUpdate();
            _toolList.Items.Clear();
            _iconList.Images.Clear();
            _iconListSmall.Images.Clear();

            Category? cat = CurrentCategory;
            int count = 0;
            if (cat != null)
            {
                foreach (ToolItem tool in _svc.Tools.LoadTools(_config, cat))
                {
                    Image? img = _svc.Icons.GetIcon(tool);
                    string imgKey = "";
                    if (img != null)
                    {
                        imgKey = tool.FolderName;
                        if (!_iconList.Images.ContainsKey(imgKey))
                        {
                            _iconList.Images.Add(imgKey, img);
                            _iconListSmall.Images.Add(imgKey, img);
                        }
                    }

                    var item = new ListViewItem(tool.DisplayName)
                    {
                        Tag = tool,
                        ImageKey = imgKey,
                        ToolTipText = tool.Config.Description
                    };
                    item.SubItems.Add(tool.Config.Description);
                    _toolList.Items.Add(item);
                    count++;
                }
            }

            _toolList.EndUpdate();
            _emptyHint.Text = _config.Categories.Count == 0
                ? "还没有分类。\r\n点左下角“新建”先创建一个分类，再添加软件。"
                : "这个分类还没有软件。\r\n点上方“添加软件”把工具加进来。";
            _emptyHint.Visible = count == 0;
            _toolList.Visible = count > 0;
        }

        // ============ 分类操作 ============

        private void AddCategory()
        {
            string? name = InputDialog.Show(this, "新建分类", "分类名称：", "");
            if (string.IsNullOrWhiteSpace(name)) return;

            _config.Categories.Add(new Category
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = name
            });
            SaveConfig();
            RefreshCategories();
            _categoryList.SelectedIndex = _config.Categories.Count - 1;
        }

        private void RenameCategory()
        {
            Category? cat = CurrentCategory;
            if (cat == null) { Info("请先选择一个分类。"); return; }

            string? name = InputDialog.Show(this, "重命名分类", "新的名称：", cat.Name);
            if (string.IsNullOrWhiteSpace(name)) return;

            cat.Name = name;
            SaveConfig();
            RefreshCategories();
        }

        private void DeleteCategory()
        {
            Category? cat = CurrentCategory;
            if (cat == null) { Info("请先选择一个分类。"); return; }

            if (!Confirm($"确定删除分类“{cat.Name}”吗？\r\n（只从列表移除分类，软件文件不会被删除）")) return;

            _config.Categories.Remove(cat);
            SaveConfig();
            RefreshCategories();
        }

        // ============ 软件操作 ============

        private void AddTool()
        {
            Category? cat = CurrentCategory;
            if (cat == null) { Info("请先在左侧新建或选择一个分类。"); return; }

            IReadOnlyList<string> available = _svc.Tools.AllToolFolders(_config)
                .Where(f => !cat.Apps.Contains(f, StringComparer.OrdinalIgnoreCase))
                .ToList();

            using var dlg = new AddToolForm(available);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                string folderName;
                if (dlg.IsNew)
                {
                    folderName = SanitizeFolderName(dlg.FolderOrName);
                    if (_svc.Tools.FolderExists(_config, folderName))
                    {
                        if (!Confirm($"文件夹“{folderName}”已存在，直接把它加入该分类吗？")) return;
                    }
                    else
                    {
                        _svc.Tools.CreateTool(_config, folderName, new ToolConfig { Name = dlg.FolderOrName.Trim() });
                        OpenInExplorer(Path.Combine(_svc.Tools.DataDir(_config), folderName, "Apps"));
                        Info("已创建。请把该软件的文件复制到刚打开的 Apps 文件夹，\r\n然后右键“编辑配置”设置启动程序。");
                    }
                }
                else
                {
                    folderName = dlg.FolderOrName;
                }

                if (!cat.Apps.Contains(folderName, StringComparer.OrdinalIgnoreCase))
                    cat.Apps.Add(folderName);
                SaveConfig();
                RefreshTools();
            }
            catch (Exception ex)
            {
                _svc.Log.Error("添加软件失败", ex);
                Error("添加软件失败：" + ex.Message);
            }
        }

        private void EditSelectedTool()
        {
            ToolItem? tool = SelectedTool;
            if (tool == null) { Info("请先选择一个软件。"); return; }

            using var dlg = new ToolEditForm(tool);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                _svc.Tools.SaveTool(tool);
                RefreshTools();
            }
            catch (Exception ex)
            {
                _svc.Log.Error("保存软件配置失败", ex);
                Error("保存失败：" + ex.Message);
            }
        }

        private void RunSelectedTool()
        {
            ToolItem? tool = SelectedTool;
            if (tool == null) return;
            if (_running.Contains(tool.FolderName)) { Info("该软件正在运行中。"); return; }

            _running.Add(tool.FolderName);
            _status.Text = "运行中：" + tool.DisplayName;
            AppSettings settings = _config.Settings;
            var adapter = new SandboxUiAdapter(this);

            // 沙盒流程会阻塞到软件退出（还原→运行→备份清理），放后台线程跑，避免卡界面
            Task.Run(() =>
            {
                try { _svc.Sandbox.Run(tool, settings, adapter); }
                catch (Exception ex) { _svc.Log.Error("沙盒运行异常", ex); adapter.OnError("运行出错：" + ex.Message); }
                finally { SafeBeginInvoke(() => { _running.Remove(tool.FolderName); UpdateRunningStatus(); }); }
            });
        }

        private void UpdateRunningStatus() =>
            _status.Text = _running.Count == 0 ? "就绪" : $"运行中：{_running.Count} 个";

        private void ShowStatus(string text) => SafeBeginInvoke(() => _status.Text = text);

        private void SafeBeginInvoke(Action action)
        {
            try { if (!IsDisposed && IsHandleCreated) BeginInvoke(action); }
            catch { /* 窗体可能已关闭 */ }
        }

        /// <summary>把沙盒引擎的回调 marshal 回 UI 线程显示对话框。</summary>
        private sealed class SandboxUiAdapter : ISandboxInteraction
        {
            private readonly MainForm _f;
            public SandboxUiAdapter(MainForm f) => _f = f;

            public ConflictDecision ResolveConflicts(string toolName, IReadOnlyList<ConflictItem> conflicts)
            {
                try
                {
                    object? r = _f.Invoke(new Func<ConflictDecision>(() =>
                    {
                        using var dlg = new ConflictDialog(toolName, conflicts);
                        return dlg.ShowDialog(_f) == DialogResult.OK
                            ? new ConflictDecision { Proceed = true, Items = dlg.Items }
                            : new ConflictDecision { Proceed = false };
                    }));
                    return (ConflictDecision)r!;
                }
                catch { return new ConflictDecision { Proceed = false }; }
            }

            public bool ConfirmBackupOnExit(string toolName)
            {
                try
                {
                    object? r = _f.Invoke(new Func<bool>(() => MessageBox.Show(_f,
                        $"“{toolName}”已退出。\r\n是否把本次产生的设置备份回工具箱，并从系统中清理（无痕）？",
                        "备份并清理", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes));
                    return (bool)r!;
                }
                catch { return false; }
            }

            public void OnProgress(string message) => _f.ShowStatus("运行 · " + message);

            public void OnError(string message) =>
                _f.SafeBeginInvoke(() => MessageBox.Show(_f, message, "出错了", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }

        private void ScanSelectedTool()
        {
            ToolItem? tool = SelectedTool;
            if (tool == null) { Info("请先选择一个软件。"); return; }
            if (_running.Contains(tool.FolderName)) { Info("该软件正在运行中。"); return; }
            if (!Confirm("扫描沙盒会：\r\n1) 先给系统(注册表/相关目录)拍快照；\r\n2) 启动该软件，你正常使用后关闭它；\r\n3) 再拍一次快照对比，找出它新增的注册表项/文件。\r\n\r\n现在开始？"))
                return;

            _running.Add(tool.FolderName);
            _status.Text = "正在扫描：" + tool.DisplayName;
            SnapshotScope scope = _config.Settings.Snapshot;
            var adapter = new ScanUiAdapter(this);
            Task.Run(() =>
            {
                try
                {
                    int added = _svc.Scan.Scan(tool, scope, adapter);
                    if (added > 0)
                    {
                        _svc.Tools.SaveTool(tool);
                        SafeBeginInvoke(() =>
                        {
                            RefreshTools();
                            MessageBox.Show(this, $"已写入 {added} 条沙盒规则到该软件的配置。", "扫描完成",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                    }
                }
                catch (Exception ex) { _svc.Log.Error("扫描异常", ex); adapter.OnError("扫描出错：" + ex.Message); }
                finally { SafeBeginInvoke(() => { _running.Remove(tool.FolderName); UpdateRunningStatus(); }); }
            });
        }

        /// <summary>把扫描引擎的回调 marshal 回 UI 线程。</summary>
        private sealed class ScanUiAdapter : IScanInteraction
        {
            private readonly MainForm _f;
            public ScanUiAdapter(MainForm f) => _f = f;

            public IReadOnlyList<ScanItem>? ChooseItems(string toolName, IReadOnlyList<ScanItem> found)
            {
                try
                {
                    object? r = _f.Invoke(new Func<IReadOnlyList<ScanItem>?>(() =>
                    {
                        using var dlg = new ScanResultDialog(toolName, found);
                        return dlg.ShowDialog(_f) == DialogResult.OK ? dlg.Selected : null;
                    }));
                    return (IReadOnlyList<ScanItem>?)r;
                }
                catch { return null; }
            }

            public void OnProgress(string message) => _f.ShowStatus("扫描 · " + message);

            public void OnInfo(string message) =>
                _f.SafeBeginInvoke(() => MessageBox.Show(_f, message, "扫描", MessageBoxButtons.OK, MessageBoxIcon.Information));

            public void OnError(string message) =>
                _f.SafeBeginInvoke(() => MessageBox.Show(_f, message, "出错了", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }

        private void OpenSelectedToolFolder()
        {
            ToolItem? tool = SelectedTool;
            if (tool == null) { Info("请先选择一个软件。"); return; }
            OpenInExplorer(tool.FolderPath);
        }

        private void RemoveToolFromCategory()
        {
            Category? cat = CurrentCategory;
            ToolItem? tool = SelectedTool;
            if (cat == null || tool == null) { Info("请先选择一个软件。"); return; }

            cat.Apps.RemoveAll(a => string.Equals(a, tool.FolderName, StringComparison.OrdinalIgnoreCase));
            SaveConfig();
            RefreshTools();
        }

        private void DeleteSelectedTool()
        {
            ToolItem? tool = SelectedTool;
            if (tool == null) { Info("请先选择一个软件。"); return; }

            if (!Confirm($"确定删除软件“{tool.DisplayName}”吗？\r\n这会从磁盘删除整个文件夹：\r\n{tool.FolderPath}")) return;

            try
            {
                _svc.Tools.DeleteToolFolder(_config, tool.FolderName);
                foreach (Category c in _config.Categories)
                    c.Apps.RemoveAll(a => string.Equals(a, tool.FolderName, StringComparison.OrdinalIgnoreCase));
                SaveConfig();
                RefreshTools();
            }
            catch (Exception ex)
            {
                _svc.Log.Error("删除软件失败", ex);
                Error("删除失败：" + ex.Message);
            }
        }

        // ============ 设置 ============

        private void OpenSettings()
        {
            using var dlg = new SettingsForm(_config, _svc.Paths);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            SaveConfig();
            _theme = Theme.FromKey(_config.Theme);
            AppTheme.Current = _theme;
            ThemeManager.Apply(this, _theme);
            ApplyLayout();
            RefreshTools();
        }

        private void OpenAbout()
        {
            using var dlg = new AboutForm(_svc.Paths);
            dlg.ShowDialog(this);
        }

        // ============ 辅助 ============

        private Category? CurrentCategory
        {
            get
            {
                int i = _categoryList.SelectedIndex;
                return (i >= 0 && i < _config.Categories.Count) ? _config.Categories[i] : null;
            }
        }

        private ToolItem? SelectedTool =>
            _toolList.SelectedItems.Count > 0 ? _toolList.SelectedItems[0].Tag as ToolItem : null;

        private void SaveConfig()
        {
            try
            {
                _svc.Config.SaveGlobal(_config);
            }
            catch (Exception ex)
            {
                _svc.Log.Error("保存配置失败", ex);
                Error("保存配置失败：" + ex.Message);
            }
        }

        private static string SanitizeFolderName(string name)
        {
            string clean = name.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                clean = clean.Replace(c, '_');
            return clean;
        }

        private void OpenInExplorer(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _svc.Log.Warn("打开文件夹失败：" + path + " | " + ex.Message);
            }
        }

        private void Info(string msg) =>
            MessageBox.Show(this, msg, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private void Error(string msg) =>
            MessageBox.Show(this, msg, "出错了", MessageBoxButtons.OK, MessageBoxIcon.Error);

        private bool Confirm(string msg) =>
            MessageBox.Show(this, msg, "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }
}
