using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ToolkidsTestApp
{
    // 阶段3/阶段2 的测试程序：
    //  - 往固定的注册表项 HKCU\Software\ToolkidsTest 和文件 %AppData%\ToolkidsTest\settings.txt 写数据；
    //  - “扫描沙盒”能把这两处抓出来；
    //  - 配成沙盒规则后，可验证“退出清理(没残留) / 再开还原(数据回来)”。
    internal static class Program
    {
        internal const string RegSub = @"Software\ToolkidsTest";

        internal static readonly string FileDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ToolkidsTest");

        internal static readonly string FilePath = Path.Combine(FileDir, "settings.txt");

        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TestForm());
        }
    }

    internal sealed class TestForm : Form
    {
        private readonly TextBox _note = new() { Width = 220 };
        private readonly TextBox _status = new()
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Font = new System.Drawing.Font("Consolas", 9.5f)
        };

        public TestForm()
        {
            Text = "Toolkids 测试程序";
            Width = 580;
            Height = 440;
            StartPosition = FormStartPosition.CenterScreen;

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10), WrapContents = true };
            top.Controls.Add(new Label { Text = "备注内容：", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            top.Controls.Add(_note);

            var write = MakeBtn("写入/更新数据");
            var clear = MakeBtn("清空测试数据");
            var refresh = MakeBtn("刷新显示");
            write.Click += (s, e) => { Write(); ShowState(); };
            clear.Click += (s, e) => { Clear(); ShowState(); };
            refresh.Click += (s, e) => ShowState();
            top.Controls.Add(write);
            top.Controls.Add(clear);
            top.Controls.Add(refresh);

            Controls.Add(_status);
            Controls.Add(top);

            ShowState();
        }

        private static Button MakeBtn(string text) =>
            new() { Text = text, AutoSize = true, Padding = new Padding(10, 4, 10, 4), Margin = new Padding(8, 0, 0, 0) };

        private int ReadCount()
        {
            try
            {
                using RegistryKey k = Registry.CurrentUser.OpenSubKey(Program.RegSub);
                return k?.GetValue("Count") is int i ? i : 0;
            }
            catch { return 0; }
        }

        private void Write()
        {
            int count = ReadCount() + 1;
            string note = _note.Text;
            try
            {
                using RegistryKey k = Registry.CurrentUser.CreateSubKey(Program.RegSub);
                k.SetValue("Note", note);
                k.SetValue("Count", count, RegistryValueKind.DWord);
            }
            catch (Exception ex) { MessageBox.Show(this, "写注册表失败：" + ex.Message); }

            try
            {
                Directory.CreateDirectory(Program.FileDir);
                File.WriteAllText(Program.FilePath, $"Note={note}\r\nCount={count}\r\nTime={DateTime.Now}", Encoding.UTF8);
            }
            catch (Exception ex) { MessageBox.Show(this, "写文件失败：" + ex.Message); }
        }

        private void Clear()
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(Program.RegSub, throwOnMissingSubKey: false); } catch { }
            try { if (Directory.Exists(Program.FileDir)) Directory.Delete(Program.FileDir, recursive: true); } catch { }
        }

        private void ShowState()
        {
            var sb = new StringBuilder();
            sb.AppendLine("== 本程序读写以下两个位置 ==");
            sb.AppendLine(@"注册表： HKCU\" + Program.RegSub);
            sb.AppendLine("文件：   " + Program.FilePath);
            sb.AppendLine();
            sb.AppendLine("== 配沙盒规则时填 ==");
            sb.AppendLine(@"  注册表规则： HKCU\Software\ToolkidsTest");
            sb.AppendLine(@"  文件规则：   %AppData%\ToolkidsTest");
            sb.AppendLine();
            sb.AppendLine("== 当前实际内容 ==");

            try
            {
                using RegistryKey k = Registry.CurrentUser.OpenSubKey(Program.RegSub);
                sb.AppendLine(k == null
                    ? "注册表：(不存在)"
                    : $"注册表：Note=\"{k.GetValue("Note")}\"  Count={k.GetValue("Count")}");
            }
            catch (Exception ex) { sb.AppendLine("注册表读取失败：" + ex.Message); }

            try
            {
                sb.AppendLine(File.Exists(Program.FilePath)
                    ? "文件内容：\r\n  " + File.ReadAllText(Program.FilePath).Replace("\n", "\n  ")
                    : "文件：(不存在)");
            }
            catch (Exception ex) { sb.AppendLine("文件读取失败：" + ex.Message); }

            _status.Text = sb.ToString();
        }
    }
}
