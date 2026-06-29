using System;
using System.Diagnostics;
using System.IO;
using Toolkids.Models;

namespace Toolkids.Services
{
    /// <summary>
    /// 负责启动软件。阶段1 只做“启动”；沙盒的还原/备份/清理将在阶段2 接入到这里。
    /// </summary>
    public sealed class AppLauncher
    {
        private readonly Logger _log;

        public AppLauncher(Logger log) => _log = log;

        /// <summary>启动软件，返回进程对象；失败抛异常由调用方提示。</summary>
        public Process Launch(ToolItem tool)
        {
            LaunchInfo launch = tool.Config.Launch;
            if (string.IsNullOrWhiteSpace(launch.Exe))
                throw new InvalidOperationException("未配置启动程序（Exe）。请先在“编辑配置”里设置。");

            string exePath = Path.GetFullPath(Path.Combine(tool.FolderPath, launch.Exe));
            if (!File.Exists(exePath))
                throw new FileNotFoundException("找不到启动程序：" + exePath);

            string workingDir = string.IsNullOrWhiteSpace(launch.WorkingDir)
                ? Path.GetDirectoryName(exePath)!
                : Path.GetFullPath(Path.Combine(tool.FolderPath, launch.WorkingDir));

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = launch.Args ?? "",
                WorkingDirectory = workingDir,
                UseShellExecute = true // 用 ShellExecute 才能用 "runas" 动词提权
            };
            if (launch.RunAsAdmin) psi.Verb = "runas";

            _log.Info($"启动 {tool.DisplayName} -> {exePath} (admin={launch.RunAsAdmin})");
            Process? proc = Process.Start(psi);
            if (proc == null)
                throw new InvalidOperationException("启动失败（系统未返回进程）。");
            return proc;
        }
    }
}
