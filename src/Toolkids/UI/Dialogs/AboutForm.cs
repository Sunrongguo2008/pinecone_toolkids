using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Toolkids.Services;
using Toolkids.UI.Theming;

namespace Toolkids.UI.Dialogs
{
    /// <summary>关于页：作者、来源、开源地址、环境自检，以及日志/更新/Issue 入口。</summary>
    public sealed class AboutForm : ThemedForm
    {
        private const string RepoUrl = "https://github.com/Sunrongguo2008/pinecone_toolkids";
        private const string IssuesUrl = RepoUrl + "/issues";
        private const string ReleasesUrl = RepoUrl + "/releases";

        private readonly AppPaths _paths;

        private static string AppVersion
        {
            get
            {
                Version? v = typeof(AboutForm).Assembly.GetName().Version;
                return v == null ? "0.1.0" : $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }

        public AboutForm(AppPaths paths)
        {
            _paths = paths;

            Text = "关于 Toolkids";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(520, 0);
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

            BuildUi();
        }

        private void BuildUi()
        {
            var root = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20, 16, 20, 14)
            };

            // 标题 + 副标题
            root.Controls.Add(new Label
            {
                Text = "🧰  Toolkids 便携工具箱",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 2)
            });
            root.Controls.Add(new Label
            {
                Text = $"v{AppVersion}   ·   给绿色软件穿“隐身衣”的工具箱",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            });

            // 信息表
            var info = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10) };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            AddInfo(info, "作者", MakeValue("Sunong2008"));
            AddInfo(info, "怎么来的", MakeValue("人类掌舵 + AI 结对（vibe coding）🤖🧑‍💻"));
            AddInfo(info, "开源地址", MakeLinkRow());
            AddInfo(info, "许可证", MakeValue("MIT（宠你，随便用）"));
            AddInfo(info, "致谢", MakeValue(".NET · WinForms · Newtonsoft.Json"));
            root.Controls.Add(info);

            // 环境自检
            root.Controls.Add(new Label { Text = "── 环境自检 ──────────────", AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
            root.Controls.Add(new Label
            {
                Text = EnvInfo(),
                AutoSize = true,
                Font = new Font("Consolas", 9.5f),
                Margin = new Padding(0, 0, 0, 6)
            });

            var copyBtn = MakeBtn("复制环境信息（报 Bug 用）");
            copyBtn.Margin = new Padding(0, 0, 0, 12);
            copyBtn.Click += (s, e) =>
            {
                try { Clipboard.SetText(ClipText()); copyBtn.Text = "已复制 ✓"; } catch { }
            };
            root.Controls.Add(copyBtn);

            // 动作按钮
            var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 12) };
            var bLog = MakeBtn("打开日志"); bLog.Click += (s, e) => OpenLog();
            var bUpd = MakeBtn("查看更新"); bUpd.Click += (s, e) => OpenUrl(ReleasesUrl);
            var bIssue = MakeBtn("提个 Issue"); bIssue.Click += (s, e) => OpenUrl(IssuesUrl);
            actions.Controls.Add(bLog);
            actions.Controls.Add(bUpd);
            actions.Controls.Add(bIssue);
            root.Controls.Add(actions);

            // 页脚
            root.Controls.Add(new Label { Text = "欢迎 Issue / PR 💬   ——   觉得好用，点个 ⭐ 也行", AutoSize = true, Margin = new Padding(0, 0, 0, 2) });
            root.Controls.Add(new Label { Text = "⚠️ 本工具会修改注册表与文件，请了解后使用，风险自负。", AutoSize = true, Margin = new Padding(0, 0, 0, 2) });
            root.Controls.Add(new Label { Text = "© 2026 Sunong2008", AutoSize = true, Margin = new Padding(0, 0, 0, 12) });

            var close = MakeBtn("关闭");
            close.DialogResult = DialogResult.OK;
            close.Anchor = AnchorStyles.Right;
            root.Controls.Add(close);

            Controls.Add(root);
            AcceptButton = close;
            CancelButton = close;
        }

        private static Label MakeValue(string text) =>
            new() { Text = text, AutoSize = true, Margin = new Padding(0, 4, 0, 4) };

        private Control MakeLinkRow()
        {
            var p = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 1, 0, 1) };
            p.Controls.Add(new Label { Text = RepoUrl, AutoSize = true, Margin = new Padding(0, 5, 6, 0) });
            var open = MakeBtn("打开");
            open.Margin = new Padding(0);
            open.Click += (s, e) => OpenUrl(RepoUrl);
            p.Controls.Add(open);
            return p;
        }

        private static void AddInfo(TableLayoutPanel grid, string key, Control value)
        {
            grid.Controls.Add(new Label
            {
                Text = key,
                AutoSize = true,
                Margin = new Padding(0, 5, 16, 4),
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold)
            });
            grid.Controls.Add(value);
        }

        private static Button MakeBtn(string text) => new()
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat
        };

        private static string EnvInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("系统    ： " + Safe(() => Environment.OSVersion.VersionString) + "  " + (Environment.Is64BitOperatingSystem ? "64位" : "32位"));
            sb.AppendLine("环境    ： " + (PeEnvironment.IsWinPE ? "WinPE" : "普通 Windows"));
            sb.AppendLine("进程    ： " + (Environment.Is64BitProcess ? "64位" : "32位"));
            sb.AppendLine("运行时  ： " + Safe(() => RuntimeInformation.FrameworkDescription));
            sb.Append("版本    ： v" + AppVersion);
            return sb.ToString();
        }

        private string ClipText() => $"Toolkids v{AppVersion}\r\n{EnvInfo()}\r\n{RepoUrl}";

        private void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(this, "打不开链接：" + ex.Message, "提示"); }
        }

        private void OpenLog()
        {
            try
            {
                string target = File.Exists(_paths.LogPath) ? _paths.LogPath : _paths.WritableRoot;
                Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show(this, "打不开日志：" + ex.Message, "提示"); }
        }

        private static string Safe(Func<string> f)
        {
            try { return f(); } catch { return "?"; }
        }
    }
}
