using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PeTest
{
    // 这是“PE 排雷”最小程序：唯一目的就是验证
    // “.NET 6 自包含 WinForms” 能否在目标系统（尤其是 WinPE / Win7 x64）启动，
    // 并顺便采集一份环境自检信息，用来设计正式版的 PE 行为。
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // DPI 设置必须在创建任何窗口之前
            try { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string info = BuildInfo();
            string savedTo = TrySaveReport(info);
            string shown = info + Environment.NewLine + "报告已保存到 : " + savedTo + Environment.NewLine;

            MessageBox.Show(
                "如果你能看到这个对话框，说明 .NET 6 自包含 WinForms 可以在当前系统启动。\r\n" +
                "点“确定”查看详细的环境自检信息。",
                "Toolkids 启动成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var form = new Form
            {
                Text = "Toolkids 环境自检",
                Width = 720,
                Height = 540,
                StartPosition = FormStartPosition.CenterScreen
            };

            var box = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Text = shown,
                Font = new System.Drawing.Font("Consolas", 10f)
            };

            var btn = new Button
            {
                Text = "复制全部信息并退出",
                Dock = DockStyle.Bottom,
                Height = 42
            };
            btn.Click += (s, e) =>
            {
                try { Clipboard.SetText(shown); } catch { }
                Application.Exit();
            };

            form.Controls.Add(btn);   // 先放底部按钮
            form.Controls.Add(box);   // 再放填充文本框
            box.BringToFront();       // 确保文本框填充按钮之上的剩余空间

            Application.Run(form);
        }

        static string BuildInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Toolkids 环境自检 ===");
            sb.AppendLine("OS 版本        : " + Safe(() => Environment.OSVersion.VersionString));
            sb.AppendLine(".NET 运行时    : " + Safe(() => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription));
            sb.AppendLine("进程位数       : " + (Environment.Is64BitProcess ? "64 位" : "32 位"));
            sb.AppendLine("系统位数       : " + (Environment.Is64BitOperatingSystem ? "64 位" : "32 位"));
            sb.AppendLine("机器名         : " + Safe(() => Environment.MachineName));
            sb.AppendLine("用户名         : " + Safe(() => Environment.UserName));
            sb.AppendLine("系统盘         : " + Safe(() => Environment.GetEnvironmentVariable("SystemDrive")));
            sb.AppendLine("程序所在目录   : " + Safe(() => AppContext.BaseDirectory));
            sb.AppendLine("是否 WinPE     : " + IsWinPE());
            sb.AppendLine("程序目录可写   : " + CanWrite(Safe(() => AppContext.BaseDirectory)));
            sb.AppendLine("%TEMP% 可写    : " + CanWrite(Safe(() => Path.GetTempPath())));
            sb.AppendLine("%AppData%      : " + Safe(() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
            sb.AppendLine("%LocalAppData% : " + Safe(() => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
            return sb.ToString();
        }

        // WinPE 的经典判定：注册表存在 HKLM\SYSTEM\CurrentControlSet\Control\MiniNT
        static string IsWinPE()
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\MiniNT"))
                    return k != null ? "是（检测到 MiniNT）" : "否";
            }
            catch (Exception ex) { return "检测失败: " + ex.Message; }
        }

        static string CanWrite(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir)) return "未知";
                string probe = Path.Combine(dir, "_toolkids_write_test.tmp");
                File.WriteAllText(probe, "x");
                File.Delete(probe);
                return "可写";
            }
            catch (Exception ex) { return "不可写（" + ex.Message + "）"; }
        }

        // 优先存程序目录，失败再存 %TEMP%
        static string TrySaveReport(string info)
        {
            string[] dirs = { Safe(() => AppContext.BaseDirectory), Safe(() => Path.GetTempPath()) };
            foreach (var d in dirs)
            {
                try
                {
                    if (string.IsNullOrEmpty(d)) continue;
                    string p = Path.Combine(d, "Toolkids_PEtest.txt");
                    File.WriteAllText(p, info, Encoding.UTF8);
                    return p;
                }
                catch { }
            }
            return "（保存失败）";
        }

        static string Safe(Func<string> f)
        {
            try { return f(); } catch (Exception ex) { return "?(" + ex.Message + ")"; }
        }
    }
}
