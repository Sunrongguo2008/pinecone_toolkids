using System;
using System.Windows.Forms;
using Toolkids.Services;
using Toolkids.UI;
using Toolkids.UI.Theming;

namespace Toolkids
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // DPI 必须在创建任何窗口之前设置
            try { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); } catch { /* PE/老系统忽略 */ }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppPaths paths = AppPaths.Create();
            var log = new Logger(paths.LogPath);
            log.Info($"启动 root={paths.RootDir} writable={paths.IsRootWritable} PE={PeEnvironment.IsWinPE}");

            // 全局异常兜底：记日志 + 友好提示，避免直接崩
            Application.ThreadException += (s, e) => ShowFatal(log, e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowFatal(log, e.ExceptionObject as Exception);

            AppServices? services = null;
            try
            {
                services = new AppServices(paths, log);
                AppTheme.Current = Theme.FromKey(services.Config.LoadGlobal().Theme);
                Application.Run(new MainForm(services));
            }
            catch (Exception ex)
            {
                ShowFatal(log, ex);
            }
            finally
            {
                services?.Icons.Dispose();
                log.Info("退出");
            }
        }

        private static void ShowFatal(Logger log, Exception? ex)
        {
            log.Error("未处理异常", ex);
            MessageBox.Show(
                "发生未处理的错误：\r\n" + (ex?.Message ?? "未知错误") + "\r\n\r\n详情见日志文件。",
                "Toolkids", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
