using System.Collections.Generic;

namespace Toolkids.Models
{
    /// <summary>
    /// 全局配置，对应工具箱根目录下的 <c>kidconfig.conf</c>（内容为 JSON）。
    /// </summary>
    public sealed class GlobalConfig
    {
        /// <summary>配置结构版本，便于以后升级迁移。</summary>
        public int Version { get; set; } = 1;

        /// <summary>主题：<c>dark</c> 或 <c>light</c>。</summary>
        public string Theme { get; set; } = "dark";

        /// <summary>软件区布局：<c>grid</c>（网格）或 <c>list</c>（列表）。</summary>
        public string Layout { get; set; } = "grid";

        /// <summary>软件容器目录名，相对工具箱根目录。</summary>
        public string DataDir { get; set; } = "Data";

        /// <summary>全部分类。</summary>
        public List<Category> Categories { get; set; } = new();

        /// <summary>其它行为开关。</summary>
        public AppSettings Settings { get; set; } = new();
    }

    /// <summary>一个分类（左侧导航的一项）。</summary>
    public sealed class Category
    {
        /// <summary>稳定的内部 Id（重命名不影响它）。</summary>
        public string Id { get; set; } = "";

        /// <summary>显示名称。</summary>
        public string Name { get; set; } = "";

        /// <summary>该分类下软件的“文件夹名”（位于 Data 目录下）。</summary>
        public List<string> Apps { get; set; } = new();
    }

    /// <summary>全局行为开关。</summary>
    public sealed class AppSettings
    {
        /// <summary>还原前若系统已存在同名项，是否弹出确认（阶段2 使用）。</summary>
        public bool ConfirmConflictBeforeRestore { get; set; } = true;

        /// <summary>软件退出后是否询问“备份并清理”（阶段2 使用）。</summary>
        public bool AskBackupOnExit { get; set; } = true;

        /// <summary>检测到 WinPE 时跳过沙盒备份/清理（阶段2 使用）。</summary>
        public bool SkipSandboxInPE { get; set; } = true;

        /// <summary>沙盒扫描范围（阶段3 使用）。</summary>
        public SnapshotScope Snapshot { get; set; } = new();
    }

    /// <summary>沙盒扫描（快照对比）的范围。</summary>
    public sealed class SnapshotScope
    {
        /// <summary>要扫描的注册表根，如 <c>HKCU\Software</c>。</summary>
        public List<string> RegistryRoots { get; set; } = new() { "HKCU\\Software", "HKLM\\Software" };

        /// <summary>要扫描的文件根，支持环境变量，如 <c>%AppData%</c>。</summary>
        public List<string> FileRoots { get; set; } = new() { "%AppData%", "%LocalAppData%", "%ProgramData%" };
    }
}
