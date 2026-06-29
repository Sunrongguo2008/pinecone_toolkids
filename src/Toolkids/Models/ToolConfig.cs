using System.Collections.Generic;

namespace Toolkids.Models
{
    /// <summary>
    /// 单个软件的配置，对应该软件文件夹下的 <c>toolconfig.json</c>。
    /// </summary>
    public sealed class ToolConfig
    {
        /// <summary>显示名称。为空时回退为文件夹名。</summary>
        public string Name { get; set; } = "";

        /// <summary>简介（鼠标悬停提示）。</summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 图标来源：相对“软件文件夹”的路径，可为某个 exe（提取其图标）或 .ico/.png；
        /// 为空时取启动程序的图标。
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>启动配置。</summary>
        public LaunchInfo Launch { get; set; } = new();

        /// <summary>便携化规则（阶段2 使用）。</summary>
        public SandboxRules SandboxRules { get; set; } = new();
    }

    /// <summary>软件的启动配置。</summary>
    public sealed class LaunchInfo
    {
        /// <summary>可执行文件，相对“软件文件夹”，如 <c>Apps\cpuz.exe</c>。</summary>
        public string Exe { get; set; } = "";

        /// <summary>命令行参数。</summary>
        public string Args { get; set; } = "";

        /// <summary>工作目录，相对“软件文件夹”；为空时用 exe 所在目录。</summary>
        public string WorkingDir { get; set; } = "";

        /// <summary>是否以管理员权限启动。</summary>
        public bool RunAsAdmin { get; set; } = true;

        /// <summary>等待退出的方式（阶段2 起用于沙盒清理时机）。</summary>
        public WaitMode WaitMode { get; set; } = WaitMode.Tree;

        /// <summary><see cref="WaitMode.Named"/> 时要等待的进程名（不含 .exe）。</summary>
        public string? WaitProcessName { get; set; }
    }

    /// <summary>便携化规则：随软件一起“还原 / 备份 / 清理”的注册表项与文件目录。</summary>
    public sealed class SandboxRules
    {
        /// <summary>注册表项，如 <c>HKCU\Software\CPUID</c>。</summary>
        public List<string> Registry { get; set; } = new();

        /// <summary>文件或目录，支持环境变量，如 <c>%AppData%\CPUID</c>。</summary>
        public List<string> Files { get; set; } = new();
    }

    /// <summary>等待目标软件退出的方式。</summary>
    public enum WaitMode
    {
        /// <summary>不等待，启动后立即返回。</summary>
        None,

        /// <summary>仅等待被直接启动的进程。</summary>
        Process,

        /// <summary>等待整棵进程树（Job Object），适合会拉起子进程的启动器。</summary>
        Tree,

        /// <summary>等待指定进程名结束。</summary>
        Named
    }
}
